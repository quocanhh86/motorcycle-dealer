using MotorcycleDealer.Infrastructure.Identity;

namespace MotorcycleDealer.WebApi.Security;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";
    public const string PolicyName = "AuthEndpointRateLimit";
    public const string TestPartitionHeaderName = "X-Test-RateLimit-Partition";
    public const int MaxTestPartitionKeyLength = 80;

    public const int DefaultPermitLimit = 30;
    public const int DefaultWindowSeconds = 60;
    public const int MinWindowSeconds = 1;
    public const int MaxWindowSeconds = 3600;
    public const int InMemoryPipeFallbackPermitLimit = 300;

    public const string ContentDispositionHeaderName = "Content-Disposition";
    public const string RetryAfterHeaderName = "Retry-After";
    public const string LocationHeaderName = "Location";
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    public const string ContentTypeOptionsHeaderName = "X-Content-Type-Options";
    public const string ContentTypeOptionsNoSniffValue = "nosniff";
    public const string FrameOptionsHeaderName = "X-Frame-Options";
    public const string FrameOptionsDenyValue = "DENY";
    public const string ReferrerPolicyHeaderName = "Referrer-Policy";
    public const string ReferrerPolicyStrictOriginValue = "strict-origin-when-cross-origin";

    public static IReadOnlyList<string> ExposedCorsHeaders { get; } =
    [
        ContentDispositionHeaderName,
        RetryAfterHeaderName,
        LocationHeaderName,
        CorrelationIdHeaderName
    ];

    public int PermitLimit { get; set; } = DefaultPermitLimit;
    public int WindowSeconds { get; set; } = DefaultWindowSeconds;
    public int QueueLimit { get; set; }

    public int ResolveEffectivePermitLimit(bool hasExplicitPermitLimitConfiguration, bool isInMemoryPipeConnection)
    {
        if (hasExplicitPermitLimitConfiguration)
        {
            return Math.Max(1, PermitLimit);
        }

        if (isInMemoryPipeConnection)
        {
            return InMemoryPipeFallbackPermitLimit;
        }

        return Math.Max(1, PermitLimit);
    }

    public int ResolveEffectiveWindowSeconds() =>
        Math.Clamp(WindowSeconds, MinWindowSeconds, MaxWindowSeconds);

    public static void ApplySecurityResponseHeaders(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        headers[ContentTypeOptionsHeaderName] = ContentTypeOptionsNoSniffValue;
        headers[FrameOptionsHeaderName] = FrameOptionsDenyValue;
        headers[ReferrerPolicyHeaderName] = ReferrerPolicyStrictOriginValue;
    }

    public static string ResolvePartitionKey(HttpContext httpContext, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(environment);

        if (JwtOptions.AllowsDevelopmentFallback(environment) &&
            httpContext.Request.Headers.TryGetValue(TestPartitionHeaderName, out var testPartition) &&
            !string.IsNullOrWhiteSpace(testPartition))
        {
            var trimmed = testPartition.ToString().Trim();
            if (trimmed.Length is > 0 and <= MaxTestPartitionKeyLength)
            {
                return $"test:{trimmed}";
            }
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIp) ? "local" : remoteIp;
    }
}
