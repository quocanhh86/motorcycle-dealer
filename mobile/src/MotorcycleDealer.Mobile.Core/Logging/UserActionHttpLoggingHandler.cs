using System.Diagnostics;
using System.Globalization;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.Core.Logging;

/// <summary>
/// Outermost DelegatingHandler that attaches correlated platform/session/screen/correlation headers
/// (X-Client-Platform, X-Client-Session-Id, X-Client-Screen, X-Correlation-Id) and the active
/// Accept-Language header (vi-VN / en-US) to every outgoing mobile HTTP request while recording
/// structured API timings on Android and iOS.
/// </summary>
public sealed class UserActionHttpLoggingHandler : DelegatingHandler
{
    private const string AcceptLanguageHeader = "Accept-Language";

    private readonly IUserActionLogger _userActionLogger;
    private readonly LocalizationService? _localizationService;

    public UserActionHttpLoggingHandler(
        IUserActionLogger userActionLogger,
        LocalizationService? localizationService = null)
    {
        _userActionLogger = userActionLogger ?? throw new ArgumentNullException(nameof(userActionLogger));
        _localizationService = localizationService;
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
        _ = ResolveOrAttachHeader(request, AcceptLanguageHeader, ResolveAcceptLanguageHeader());

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

    private string ResolveAcceptLanguageHeader()
    {
        if (_localizationService is not null)
        {
            return _localizationService.AcceptLanguageHeaderValue;
        }

        var culture = AppResources.Culture ?? CultureInfo.CurrentUICulture;
        return culture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? "en-US,en;q=0.9,vi-VN;q=0.8,vi;q=0.7"
            : "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7";
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
