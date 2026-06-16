using System.Text;

namespace MotorcycleDealer.WebApi.Logging;

public static class UserActionHeaderNames
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string ClientPlatform = "X-Client-Platform";
    public const string ClientSessionId = "X-Client-Session-Id";
    public const string ClientScreen = "X-Client-Screen";
    public const string ClientAction = "X-Client-Action";
}

public sealed record UserActionLogEntry(
    string EventId,
    DateTimeOffset TimestampUtc,
    string Source,
    string ClientPlatform,
    string SessionId,
    string CorrelationId,
    string TraceId,
    Guid? UserId,
    string? Email,
    string? Role,
    Guid? DealerId,
    string Category,
    string Action,
    string? Screen,
    string? Target,
    string? HttpMethod,
    string? Path,
    int? StatusCode,
    string Outcome,
    long? DurationMs,
    string? RemoteIp,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record UserActionLogIngestItem(
    string? EventId,
    DateTimeOffset? TimestampUtc,
    string? Source,
    string? ClientPlatform,
    string? SessionId,
    string? CorrelationId,
    string? Category,
    string Action,
    string? Screen,
    string? Target,
    string? HttpMethod,
    string? Path,
    int? StatusCode,
    string? Outcome,
    long? DurationMs,
    Dictionary<string, string>? Metadata);

public sealed record UserActionLogBatchRequest(
    List<UserActionLogIngestItem>? Events,
    string? Action = null,
    string? Category = null,
    string? Source = null,
    string? ClientPlatform = null,
    string? SessionId = null,
    string? CorrelationId = null,
    string? Screen = null,
    string? Target = null,
    string? HttpMethod = null,
    string? Path = null,
    int? StatusCode = null,
    string? Outcome = null,
    long? DurationMs = null,
    Dictionary<string, string>? Metadata = null);

public sealed record UserActionLogIngestResultDto(
    int AcceptedCount,
    int RejectedCount,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc);

public sealed record UserActionLogQueryResultDto(
    IReadOnlyList<UserActionLogEntry> Items,
    int TotalBufferedCount,
    IReadOnlyDictionary<string, int> CountsBySource,
    IReadOnlyDictionary<string, int> CountsByPlatform,
    string LogFilePath);

public static class UserActionLogSanitizer
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "passwd",
        "pwd",
        "token",
        "secret",
        "authorization",
        "cookie",
        "apikey",
        "api_key",
        "signingkey",
        "passphrase",
        "credential",
        "jwt"
    ];

    public static string CleanText(string? raw, int maxLength, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var sb = new StringBuilder(Math.Min(raw.Length, maxLength));
        foreach (var ch in raw.Trim())
        {
            if (sb.Length >= maxLength)
            {
                break;
            }

            if (ch == '\r' || ch == '\n' || ch == '\t')
            {
                sb.Append(' ');
            }
            else if (!char.IsControl(ch))
            {
                sb.Append(ch);
            }
        }

        var cleaned = sb.ToString().Trim();
        return cleaned.Length == 0 ? fallback : cleaned;
    }

    public static string? CleanOptional(string? raw, int maxLength)
    {
        var cleaned = CleanText(raw, maxLength, string.Empty);
        return cleaned.Length == 0 ? null : cleaned;
    }

    public static string NormalizeSource(string? rawSource, string? clientPlatform)
    {
        var s = CleanText(rawSource, 32).ToLowerInvariant();
        if (s is "server" or "backend" or "webapi")
        {
            return "Server";
        }

        if (s is "web" or "webclient" or "webspa" or "client" or "spa")
        {
            return "WebClient";
        }

        if (s is "mobile" or "mobileapp" or "maui" or "android" or "ios" or "mobileandroid" or "mobileios")
        {
            return "MobileApp";
        }

        var p = CleanText(clientPlatform, 32).ToLowerInvariant();
        if (p.Contains("android", StringComparison.Ordinal) || p.Contains("ios", StringComparison.Ordinal) || p.Contains("mobile", StringComparison.Ordinal))
        {
            return "MobileApp";
        }

        if (p.Contains("web", StringComparison.Ordinal) || p.Contains("spa", StringComparison.Ordinal))
        {
            return "WebClient";
        }

        return "Server";
    }

    public static string NormalizePlatform(string? rawPlatform, string? userAgent)
    {
        var p = CleanText(rawPlatform, 40);
        if (p.Length > 0)
        {
            if (p.Equals("web", StringComparison.OrdinalIgnoreCase) || p.Equals("webspa", StringComparison.OrdinalIgnoreCase))
            {
                return "WebSpa";
            }

            if (p.Equals("android", StringComparison.OrdinalIgnoreCase) || p.Equals("mobileandroid", StringComparison.OrdinalIgnoreCase))
            {
                return "MobileAndroid";
            }

            if (p.Equals("ios", StringComparison.OrdinalIgnoreCase) || p.Equals("mobileios", StringComparison.OrdinalIgnoreCase))
            {
                return "MobileIos";
            }

            return p;
        }

        var ua = CleanText(userAgent, 256).ToLowerInvariant();
        if (ua.Contains("mozilla", StringComparison.Ordinal) || ua.Contains("chrome", StringComparison.Ordinal) || ua.Contains("safari", StringComparison.Ordinal))
        {
            return "WebSpa";
        }

        return "ApiClient";
    }

    public static string SanitizeQueryString(QueryString queryString)
    {
        if (!queryString.HasValue || string.IsNullOrWhiteSpace(queryString.Value))
        {
            return string.Empty;
        }

        var trimmed = queryString.Value.TrimStart('?');
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var segments = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
        var sanitized = new List<string>(segments.Length);
        foreach (var seg in segments)
        {
            var eqIdx = seg.IndexOf('=');
            var key = eqIdx >= 0 ? seg[..eqIdx] : seg;
            if (IsSensitiveKey(key))
            {
                sanitized.Add($"{CleanText(key, 48)}=[REDACTED]");
            }
            else
            {
                sanitized.Add(CleanText(seg, 128));
            }
        }

        return "?" + string.Join('&', sanitized);
    }

    public static IReadOnlyDictionary<string, string>? SanitizeMetadata(IReadOnlyDictionary<string, string>? raw)
    {
        if (raw is null || raw.Count == 0)
        {
            return null;
        }

        var safe = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in raw.Take(20))
        {
            var key = CleanText(pair.Key, 64);
            if (key.Length == 0)
            {
                continue;
            }

            safe[key] = IsSensitiveKey(key)
                ? "[REDACTED]"
                : CleanText(pair.Value, 256);
        }

        return safe.Count == 0 ? null : safe;
    }

    public static bool IsSensitiveKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        foreach (var fragment in SensitiveKeyFragments)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
