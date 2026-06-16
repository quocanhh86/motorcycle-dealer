using Microsoft.Extensions.Hosting;

namespace MotorcycleDealer.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string TestingEnvironmentName = "Testing";
    public const string DevPlaceholderPrefix = "SET_VIA_USER_SECRETS";
    public const string DeterministicDevFallbackSigningKey =
        "MotorcycleDealer-LocalDev-Only-Ephemeral-Key-Min32Bytes-2026!";

    public string Issuer { get; set; } = "MotorcycleDealer.WebApi";
    public string Audience { get; set; } = "MotorcycleDealer.Spa";
    public string SigningKey { get; set; } = "SET_VIA_USER_SECRETS_OR_ENV_JWT_SIGNING_KEY";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;

    public static bool AllowsDevelopmentFallback(IHostEnvironment? environment) =>
        environment is not null &&
        (environment.IsDevelopment() || environment.IsEnvironment(TestingEnvironmentName));

    public string ResolveEffectiveSigningKey(bool allowDevelopmentFallback)
    {
        var candidate = SigningKey?.Trim() ?? string.Empty;
        var isPlaceholder = string.IsNullOrWhiteSpace(candidate) ||
                            candidate.StartsWith(DevPlaceholderPrefix, StringComparison.OrdinalIgnoreCase) ||
                            candidate.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase) ||
                            candidate.StartsWith("DEV_PLACEHOLDER", StringComparison.OrdinalIgnoreCase) ||
                            candidate.StartsWith('<');

        if (isPlaceholder)
        {
            if (allowDevelopmentFallback)
            {
                return DeterministicDevFallbackSigningKey;
            }

            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured via user-secrets or environment variable Jwt__SigningKey (>= 32 UTF-8 bytes) outside Development and Testing environments.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(candidate) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 UTF-8 bytes (256 bits) long.");
        }

        return candidate;
    }
}
