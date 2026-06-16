using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Controllers;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Logging;

namespace MotorcycleDealer.WebApi.Middleware;

/// <summary>
/// Records every user HTTP action against /api/v1/* with verified JWT principal context,
/// correlated X-Correlation-Id / X-Client-Platform / X-Client-Session-Id / X-Client-Screen headers,
/// semantic controller/action naming, HTTP status code, and latency.
/// </summary>
public sealed class UserActionLoggingMiddleware
{
    public const string CorrelationIdItemKey = "MotorcycleDealer.CorrelationId";

    private readonly RequestDelegate _next;
    private readonly IUserActionAuditStore _auditStore;

    public UserActionLoggingMiddleware(RequestDelegate next, IUserActionAuditStore auditStore)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _auditStore = auditStore ?? throw new ArgumentNullException(nameof(auditStore));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveOrCreateCorrelationId(context);
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(UserActionHeaderNames.CorrelationId))
            {
                context.Response.Headers[UserActionHeaderNames.CorrelationId] = correlationId;
            }

            return Task.CompletedTask;
        });

        var pathValue = context.Request.Path.Value ?? string.Empty;
        if (!ShouldLogRequestPath(pathValue))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? unhandled = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            unhandled = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            RecordHttpAction(context, correlationId, pathValue, stopwatch.ElapsedMilliseconds, unhandled);
        }
    }

    public static string ResolveOrCreateCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdItemKey, out var cached) &&
            cached is string existing &&
            !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        if (context.Request.Headers.TryGetValue(UserActionHeaderNames.CorrelationId, out var rawHeader))
        {
            var cleaned = UserActionLogSanitizer.CleanText(rawHeader.ToString(), 64);
            if (cleaned.Length > 0)
            {
                return cleaned;
            }
        }

        return Guid.CreateVersion7().ToString("N");
    }

    private void RecordHttpAction(
        HttpContext context,
        string correlationId,
        string pathValue,
        long elapsedMs,
        Exception? unhandled)
    {
        var user = context.User;
        Guid? userId = TryParseGuidClaim(
            user.FindFirstValue(JwtClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier));
        var email = UserActionLogSanitizer.CleanOptional(
            user.FindFirstValue(JwtClaimNames.Email) ?? user.FindFirstValue(ClaimTypes.Email),
            160);
        var role = UserActionLogSanitizer.CleanOptional(
            user.FindFirstValue(JwtClaimNames.Role) ?? user.FindFirstValue(ClaimTypes.Role),
            48);
        Guid? dealerId = TryParseGuidClaim(user.FindFirstValue(JwtClaimNames.DealerId));

        var rawPlatform = context.Request.Headers[UserActionHeaderNames.ClientPlatform].ToString();
        var clientPlatform = UserActionLogSanitizer.NormalizePlatform(rawPlatform, context.Request.Headers.UserAgent.ToString());
        var sessionId = UserActionLogSanitizer.CleanText(
            context.Request.Headers[UserActionHeaderNames.ClientSessionId].ToString(),
            64,
            fallback: "server-direct");
        var clientScreen = UserActionLogSanitizer.CleanOptional(
            context.Request.Headers[UserActionHeaderNames.ClientScreen].ToString(),
            120);
        var clientAction = UserActionLogSanitizer.CleanOptional(
            context.Request.Headers[UserActionHeaderNames.ClientAction].ToString(),
            120);

        var statusCode = unhandled is not null && context.Response.StatusCode < 400
            ? StatusCodes.Status500InternalServerError
            : context.Response.StatusCode;

        var outcome = statusCode switch
        {
            >= 500 => "ServerError",
            StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden => "Denied",
            >= 400 => "ClientError",
            _ => "Success"
        };

        var endpoint = context.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var actionName = actionDescriptor is not null
            ? $"{actionDescriptor.ControllerName}.{actionDescriptor.ActionName}"
            : $"{context.Request.Method} {pathValue}";
        var category = ResolveCategory(pathValue, actionDescriptor?.ControllerName);

        var sanitizedQuery = UserActionLogSanitizer.SanitizeQueryString(context.Request.QueryString);
        var fullSanitizedPath = string.IsNullOrEmpty(sanitizedQuery)
            ? pathValue
            : $"{pathValue}{sanitizedQuery}";

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(clientAction))
        {
            metadata["clientAction"] = clientAction;
        }

        if (unhandled is not null)
        {
            metadata["exceptionType"] = unhandled.GetType().Name;
        }

        var entry = new UserActionLogEntry(
            EventId: Guid.CreateVersion7().ToString("N"),
            TimestampUtc: DateTimeOffset.UtcNow,
            Source: "Server",
            ClientPlatform: clientPlatform,
            SessionId: sessionId,
            CorrelationId: correlationId,
            TraceId: GlobalExceptionHandler.ResolveTraceId(context),
            UserId: userId,
            Email: email,
            Role: role ?? (userId.HasValue ? "Authenticated" : "Anonymous"),
            DealerId: dealerId,
            Category: category,
            Action: actionName,
            Screen: clientScreen,
            Target: $"{context.Request.Method} {pathValue}",
            HttpMethod: context.Request.Method,
            Path: UserActionLogSanitizer.CleanOptional(fullSanitizedPath, 320),
            StatusCode: statusCode,
            Outcome: outcome,
            DurationMs: elapsedMs,
            RemoteIp: context.Connection.RemoteIpAddress?.ToString(),
            Metadata: metadata.Count > 0 ? metadata : null);

        _auditStore.Record(entry);
    }

    private static bool ShouldLogRequestPath(string pathValue)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return false;
        }

        if (pathValue.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            pathValue.StartsWith("/api/v1/health", StringComparison.OrdinalIgnoreCase) ||
            pathValue.StartsWith("/api/v1/activity-logs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return pathValue.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCategory(string pathValue, string? controllerName)
    {
        var token = (controllerName ?? pathValue).ToLowerInvariant();
        if (token.Contains("auth", StringComparison.Ordinal)) return "Auth";
        if (token.Contains("booking", StringComparison.Ordinal) || token.Contains("timeslot", StringComparison.Ordinal) || token.Contains("time-slot", StringComparison.Ordinal)) return "Booking";
        if (token.Contains("testdrive", StringComparison.Ordinal) || token.Contains("test-drive", StringComparison.Ordinal)) return "TestDrive";
        if (token.Contains("loyalty", StringComparison.Ordinal) || token.Contains("voucher", StringComparison.Ordinal)) return "Loyalty";
        if (token.Contains("invoice", StringComparison.Ordinal) || token.Contains("sparepart", StringComparison.Ordinal) || token.Contains("spare-part", StringComparison.Ordinal)) return "Invoice";
        if (token.Contains("sales", StringComparison.Ordinal) || token.Contains("inventor", StringComparison.Ordinal)) return "Sales";
        if (token.Contains("vehicle", StringComparison.Ordinal)) return "Garage";
        if (token.Contains("warranty", StringComparison.Ordinal)) return "Warranty";
        if (token.Contains("review", StringComparison.Ordinal)) return "Review";
        if (token.Contains("motorcycle", StringComparison.Ordinal) || token.Contains("dealer", StringComparison.Ordinal)) return "Showroom";
        if (token.Contains("notification", StringComparison.Ordinal)) return "Notification";
        if (token.Contains("analytic", StringComparison.Ordinal)) return "Analytics";
        return "Api";
    }

    private static Guid? TryParseGuidClaim(string? raw) =>
        Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty ? parsed : null;
}
