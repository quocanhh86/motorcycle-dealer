using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.WebApi.Logging;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Hubs;

/// <summary>
/// Strongly-typed real-time SignalR hub mapped at /hubs/notifications (ADR-004, FR-06).
/// Group membership is derived strictly from verified JWT claims on OnConnectedAsync;
/// zero client-invokable group subscription methods are exposed.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IUserActionAuditStore? _auditStore;

    public NotificationHub(
        ILogger<NotificationHub> logger,
        IServiceProvider? serviceProvider = null,
        IUserActionAuditStore? auditStore = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider;
        _auditStore = auditStore;
    }

    public static string BuildUserGroup(Guid userId) => $"user-{userId:D}";
    public static string BuildDealerGroup(Guid dealerId) => $"dealer-{dealerId:D}";
    public const string AdminGroup = "role-Admin";

    public override async Task OnConnectedAsync()
    {
        var userIdRaw = Context.User?.FindFirstValue(JwtClaimNames.Sub) ??
                        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(JwtClaimNames.Role) ??
                   Context.User?.FindFirstValue(ClaimTypes.Role);
        var dealerIdRaw = Context.User?.FindFirstValue(JwtClaimNames.DealerId);

        if (!Guid.TryParse(userIdRaw, out var userId) || userId == Guid.Empty)
        {
            _logger.LogWarning(
                "Aborting SignalR connection {ConnectionId}: missing or invalid verified user claim.",
                Context.ConnectionId);
            Context.Abort();
            return;
        }

        if (_serviceProvider is not null && Context.User is not null)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetService<MotorcycleDealerDbContext>();
            if (dbContext is not null)
            {
                var outcome = await ActivePrincipalMiddleware.EvaluatePersistedPrincipalAsync(
                    dbContext,
                    userId,
                    Context.User,
                    Context.ConnectionAborted);

                if (!outcome.IsAllowed)
                {
                    _logger.LogWarning(
                        "Aborting SignalR connection {ConnectionId} for UserId={UserId}: {ErrorCode}.",
                        Context.ConnectionId,
                        userId,
                        outcome.ErrorCode);
                    Context.Abort();
                    return;
                }
            }
        }

        var parsedDealerId = Guid.Empty;
        if (string.Equals(role, RoleNames.DealerStaff, StringComparison.Ordinal))
        {
            if (!Guid.TryParse(dealerIdRaw, out parsedDealerId) || parsedDealerId == Guid.Empty)
            {
                _logger.LogWarning(
                    "Aborting SignalR connection {ConnectionId} for DealerStaff UserId={UserId}: missing or invalid dealer_id claim.",
                    Context.ConnectionId,
                    userId);
                Context.Abort();
                return;
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, BuildUserGroup(userId));

        if (string.Equals(role, RoleNames.DealerStaff, StringComparison.Ordinal))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BuildDealerGroup(parsedDealerId));
        }
        else if (string.Equals(role, RoleNames.Admin, StringComparison.Ordinal))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }

        _logger.LogInformation(
            "SignalR client connected: ConnectionId={ConnectionId}, UserId={UserId}, Role={Role}, DealerId={DealerId}",
            Context.ConnectionId,
            userId,
            role ?? "unknown",
            dealerIdRaw ?? "none");

        RecordHubAudit("SignalR.Connected", "Success", userId, role, parsedDealerId == Guid.Empty ? null : parsedDealerId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdRaw = Context.User?.FindFirstValue(JwtClaimNames.Sub) ??
                        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(JwtClaimNames.Role) ??
                   Context.User?.FindFirstValue(ClaimTypes.Role);
        var dealerIdRaw = Context.User?.FindFirstValue(JwtClaimNames.DealerId);

        Guid? userId = Guid.TryParse(userIdRaw, out var parsedUser) && parsedUser != Guid.Empty ? parsedUser : null;
        Guid? dealerId = Guid.TryParse(dealerIdRaw, out var parsedDealer) && parsedDealer != Guid.Empty ? parsedDealer : null;

        RecordHubAudit(
            "SignalR.Disconnected",
            exception is null ? "Success" : "Error",
            userId,
            role,
            dealerId,
            exception?.GetType().Name);

        await base.OnDisconnectedAsync(exception);
    }

    private void RecordHubAudit(
        string action,
        string outcome,
        Guid? userId,
        string? role,
        Guid? dealerId,
        string? errorType = null)
    {
        if (_auditStore is null)
        {
            return;
        }

        var httpContext = Context.GetHttpContext();
        var rawPlatform = httpContext?.Request.Headers[UserActionHeaderNames.ClientPlatform].ToString();
        var clientPlatform = UserActionLogSanitizer.NormalizePlatform(rawPlatform, httpContext?.Request.Headers.UserAgent.ToString());
        var sessionId = UserActionLogSanitizer.CleanText(
            httpContext?.Request.Headers[UserActionHeaderNames.ClientSessionId].ToString(),
            64,
            fallback: Context.ConnectionId);
        var email = UserActionLogSanitizer.CleanOptional(
            Context.User?.FindFirstValue(JwtClaimNames.Email) ?? Context.User?.FindFirstValue(ClaimTypes.Email),
            160);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["connectionId"] = Context.ConnectionId
        };
        if (!string.IsNullOrWhiteSpace(errorType))
        {
            metadata["exceptionType"] = errorType;
        }

        _auditStore.Record(new UserActionLogEntry(
            EventId: Guid.CreateVersion7().ToString("N"),
            TimestampUtc: DateTimeOffset.UtcNow,
            Source: "Server",
            ClientPlatform: clientPlatform,
            SessionId: sessionId,
            CorrelationId: httpContext is not null
                ? UserActionLoggingMiddleware.ResolveOrCreateCorrelationId(httpContext)
                : Context.ConnectionId,
            TraceId: httpContext is not null ? GlobalExceptionHandler.ResolveTraceId(httpContext) : Context.ConnectionId,
            UserId: userId,
            Email: email,
            Role: role ?? "Authenticated",
            DealerId: dealerId,
            Category: "Realtime",
            Action: action,
            Screen: "/hubs/notifications",
            Target: Context.ConnectionId,
            HttpMethod: "WS",
            Path: "/hubs/notifications",
            StatusCode: null,
            Outcome: outcome,
            DurationMs: null,
            RemoteIp: httpContext?.Connection.RemoteIpAddress?.ToString(),
            Metadata: metadata));
    }
}
