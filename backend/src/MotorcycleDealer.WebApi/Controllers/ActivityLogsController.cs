using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Logging;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

/// <summary>
/// Correlated user action audit endpoint accepting real client/mobile interaction logs
/// (Web SPA, MAUI Android, MAUI iOS) and exposing live query feeds for the active user and Admin HQ.
/// </summary>
[ApiController]
[Route("api/v1/activity-logs")]
public sealed class ActivityLogsController : ControllerBase
{
    public const int MaxBatchSize = 50;

    private readonly IUserActionAuditStore _auditStore;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ActivityLogsController(
        IUserActionAuditStore auditStore,
        IDateTimeProvider dateTimeProvider)
    {
        _auditStore = auditStore ?? throw new ArgumentNullException(nameof(auditStore));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    /// <summary>
    /// Ingests one or a batch of real user action events emitted by Web SPA or MAUI Android/iOS clients.
    /// Principal claims (UserId, Email, Role, DealerId) are always derived authoritatively from the verified JWT.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<UserActionLogIngestResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiEnvelope<UserActionLogIngestResultDto>> IngestClientActions(
        [FromBody] UserActionLogBatchRequest? request)
    {
        if (request is null)
        {
            var problem = GlobalExceptionHandler.CreateCustomProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Activity log payload cannot be empty.",
                errorCode: "INVALID_ACTIVITY_LOG_PAYLOAD");
            return BadRequest(problem);
        }

        var items = new List<UserActionLogIngestItem>();
        if (request.Events is { Count: > 0 })
        {
            items.AddRange(request.Events);
        }
        else if (!string.IsNullOrWhiteSpace(request.Action))
        {
            items.Add(new UserActionLogIngestItem(
                EventId: null,
                TimestampUtc: null,
                Source: request.Source,
                ClientPlatform: request.ClientPlatform,
                SessionId: request.SessionId,
                CorrelationId: request.CorrelationId,
                Category: request.Category,
                Action: request.Action,
                Screen: request.Screen,
                Target: request.Target,
                HttpMethod: request.HttpMethod,
                Path: request.Path,
                StatusCode: request.StatusCode,
                Outcome: request.Outcome,
                DurationMs: request.DurationMs,
                Metadata: request.Metadata));
        }

        if (items.Count == 0)
        {
            var problem = GlobalExceptionHandler.CreateCustomProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Provide at least one user action event in 'events' or 'action'.",
                errorCode: "EMPTY_ACTIVITY_LOG_BATCH");
            return BadRequest(problem);
        }

        var requestCorrelationId = UserActionLoggingMiddleware.ResolveOrCreateCorrelationId(HttpContext);
        var headerPlatform = Request.Headers[UserActionHeaderNames.ClientPlatform].ToString();
        var headerSessionId = Request.Headers[UserActionHeaderNames.ClientSessionId].ToString();
        var headerScreen = Request.Headers[UserActionHeaderNames.ClientScreen].ToString();
        var traceId = GlobalExceptionHandler.ResolveTraceId(HttpContext);
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var user = HttpContext.User;
        Guid? userId = TryParseGuidClaim(
            user.FindFirstValue(JwtClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier));
        var email = UserActionLogSanitizer.CleanOptional(
            user.FindFirstValue(JwtClaimNames.Email) ?? user.FindFirstValue(ClaimTypes.Email),
            160);
        var role = UserActionLogSanitizer.CleanOptional(
            user.FindFirstValue(JwtClaimNames.Role) ?? user.FindFirstValue(ClaimTypes.Role),
            48);
        Guid? dealerId = TryParseGuidClaim(user.FindFirstValue(JwtClaimNames.DealerId));

        var accepted = 0;
        var rejected = 0;
        var nowUtc = _dateTimeProvider.UtcNow;

        foreach (var item in items.Take(MaxBatchSize))
        {
            var cleanAction = UserActionLogSanitizer.CleanText(item.Action, 128);
            if (cleanAction.Length == 0)
            {
                rejected++;
                continue;
            }

            var platform = UserActionLogSanitizer.NormalizePlatform(
                !string.IsNullOrWhiteSpace(item.ClientPlatform) ? item.ClientPlatform : headerPlatform,
                Request.Headers.UserAgent.ToString());
            var source = UserActionLogSanitizer.NormalizeSource(item.Source, platform);
            var sessionId = UserActionLogSanitizer.CleanText(
                !string.IsNullOrWhiteSpace(item.SessionId) ? item.SessionId : headerSessionId,
                64,
                fallback: "client-session");
            var correlationId = UserActionLogSanitizer.CleanText(
                !string.IsNullOrWhiteSpace(item.CorrelationId) ? item.CorrelationId : requestCorrelationId,
                64,
                fallback: requestCorrelationId);

            var eventTimestamp = item.TimestampUtc.HasValue &&
                                 item.TimestampUtc.Value <= nowUtc.AddMinutes(5) &&
                                 item.TimestampUtc.Value >= nowUtc.AddDays(-7)
                ? item.TimestampUtc.Value.ToUniversalTime()
                : nowUtc;

            var entry = new UserActionLogEntry(
                EventId: UserActionLogSanitizer.CleanText(item.EventId, 64, Guid.CreateVersion7().ToString("N")),
                TimestampUtc: eventTimestamp,
                Source: source,
                ClientPlatform: platform,
                SessionId: sessionId,
                CorrelationId: correlationId,
                TraceId: traceId,
                UserId: userId,
                Email: email,
                Role: role ?? (userId.HasValue ? "Authenticated" : "Anonymous"),
                DealerId: dealerId,
                Category: UserActionLogSanitizer.CleanText(item.Category, 48, "UserInteraction"),
                Action: cleanAction,
                Screen: UserActionLogSanitizer.CleanOptional(
                    !string.IsNullOrWhiteSpace(item.Screen) ? item.Screen : headerScreen,
                    120),
                Target: UserActionLogSanitizer.CleanOptional(item.Target, 200),
                HttpMethod: UserActionLogSanitizer.CleanOptional(item.HttpMethod, 16),
                Path: UserActionLogSanitizer.CleanOptional(item.Path, 320),
                StatusCode: item.StatusCode,
                Outcome: UserActionLogSanitizer.CleanText(item.Outcome, 32, "Success"),
                DurationMs: item.DurationMs.HasValue ? Math.Clamp(item.DurationMs.Value, 0, 600_000) : null,
                RemoteIp: remoteIp,
                Metadata: UserActionLogSanitizer.SanitizeMetadata(item.Metadata));

            _auditStore.Record(entry);
            accepted++;
        }

        if (items.Count > MaxBatchSize)
        {
            rejected += items.Count - MaxBatchSize;
        }

        var result = new UserActionLogIngestResultDto(
            AcceptedCount: accepted,
            RejectedCount: rejected,
            CorrelationId: requestCorrelationId,
            RecordedAtUtc: nowUtc);

        return Ok(WrapSuccess(result));
    }

    /// <summary>
    /// Returns the authenticated caller's own correlated user actions across Server, WebClient, and MobileApp.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<UserActionLogQueryResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiEnvelope<UserActionLogQueryResultDto>> GetMyActivityLogs(
        [FromQuery] string? source = null,
        [FromQuery] string? clientPlatform = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int take = 100)
    {
        var userId = TryParseGuidClaim(
            HttpContext.User.FindFirstValue(JwtClaimNames.Sub) ??
            HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = _auditStore.Query(
            source: source,
            clientPlatform: clientPlatform,
            category: category,
            userId: userId.Value,
            search: search,
            take: take);

        return Ok(WrapSuccess(result));
    }

    /// <summary>
    /// Admin nationwide correlated user action log explorer across Server, Web SPA, and Mobile (Android &amp; iOS).
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Analytics.ViewAllDealers)]
    [ProducesResponseType(typeof(ApiEnvelope<UserActionLogQueryResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public ActionResult<ApiEnvelope<UserActionLogQueryResultDto>> GetAllActivityLogs(
        [FromQuery] string? source = null,
        [FromQuery] string? clientPlatform = null,
        [FromQuery] string? category = null,
        [FromQuery] string? role = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? dealerId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? search = null,
        [FromQuery] int take = 150)
    {
        var result = _auditStore.Query(
            source: source,
            clientPlatform: clientPlatform,
            category: category,
            role: role,
            userId: userId,
            dealerId: dealerId,
            correlationId: correlationId,
            search: search,
            take: take);

        return Ok(WrapSuccess(result));
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));

    private static Guid? TryParseGuidClaim(string? raw) =>
        Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty ? parsed : null;
}
