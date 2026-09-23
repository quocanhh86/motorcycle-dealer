using System.Diagnostics;

namespace MotorcycleDealer.Mobile.Core.Logging;

/// <summary>
/// Outermost DelegatingHandler that attaches correlated platform/session/screen/correlation headers
/// (X-Client-Platform, X-Client-Session-Id, X-Client-Screen, X-Correlation-Id) to every outgoing
/// mobile HTTP request and records local structured API timings on Android and iOS.
/// </summary>
public sealed class UserActionHttpLoggingHandler : DelegatingHandler
{
    private readonly IUserActionLogger _userActionLogger;

    public UserActionHttpLoggingHandler(IUserActionLogger userActionLogger)
    {
        _userActionLogger = userActionLogger ?? throw new ArgumentNullException(nameof(userActionLogger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = ResolveOrAttachHeader(request, MobileUserActionHeaders.CorrelationId, _userActionLogger.CreateCorrelationId());
        _ = ResolveOrAttachHeader(request, MobileUserActionHeaders.ClientPlatform, _userActionLogger.ClientPlatform);
        _ = ResolveOrAttachHeader(request, MobileUserActionHeaders.ClientSessionId, _userActionLogger.SessionId);
        _ = ResolveOrAttachHeader(request, MobileUserActionHeaders.ClientScreen, _userActionLogger.CurrentScreen);

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var shouldLogLocally = !path.StartsWith("/api/v1/activity-logs", StringComparison.OrdinalIgnoreCase) &&
                               !path.StartsWith("/api/v1/health", StringComparison.OrdinalIgnoreCase);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (shouldLogLocally)
            {
                var status = (int)response.StatusCode;
                var outcome = status switch
                {
                    >= 500 => "ServerError",
                    401 or 403 => "Denied",
                    >= 400 => "ClientError",
                    _ => "Success"
                };

                _userActionLogger.Track(
                    action: $"Api.{request.Method.Method} {path}",
                    category: "Api",
                    target: path,
                    outcome: outcome,
                    correlationId: correlationId,
                    httpMethod: request.Method.Method,
                    path: request.RequestUri?.PathAndQuery ?? path,
                    statusCode: status,
                    durationMs: sw.ElapsedMilliseconds,
                    syncToServer: false);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            if (shouldLogLocally)
            {
                _userActionLogger.Track(
                    action: $"Api.{request.Method.Method} {path}",
                    category: "Api",
                    target: path,
                    outcome: "Failed",
                    correlationId: correlationId,
                    httpMethod: request.Method.Method,
                    path: request.RequestUri?.PathAndQuery ?? path,
                    statusCode: 0,
                    durationMs: sw.ElapsedMilliseconds,
                    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["exceptionType"] = ex.GetType().Name
                    },
                    syncToServer: false);
            }

            throw;
        }
    }

    private static string ResolveOrAttachHeader(HttpRequestMessage request, string name, string defaultValue)
    {
        if (request.Headers.TryGetValues(name, out var existing))
        {
            var first = existing.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        request.Headers.TryAddWithoutValidation(name, defaultValue);
        return defaultValue;
    }
}
