using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Domain.Constants;

namespace MotorcycleDealer.Infrastructure.Identity;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly bool _allowDevelopmentFallback;

    public JwtTokenService(
        IOptions<JwtOptions> options,
        IDateTimeProvider dateTimeProvider,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _allowDevelopmentFallback = JwtOptions.AllowsDevelopmentFallback(hostEnvironment);
    }

    public IssuedAccessToken GenerateAccessToken(JwtTokenSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var signingKey = _options.ResolveEffectiveSigningKey(_allowDevelopmentFallback);
        var issuedAt = _dateTimeProvider.UtcNow;
        var expiresAt = issuedAt.AddMinutes(Math.Max(1, _options.AccessTokenMinutes));
        var jwtId = Guid.CreateVersion7().ToString("D");

        const string headerJson = """{"alg":"HS256","typ":"JWT"}""";
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [JwtClaimNames.Sub] = subject.UserId.ToString("D"),
            [JwtClaimNames.Email] = subject.Email,
            [JwtClaimNames.FullName] = subject.FullName,
            [JwtClaimNames.Role] = subject.Role,
            [JwtClaimNames.Permissions] = subject.Permissions,
            [JwtClaimNames.JwtId] = jwtId,
            [JwtClaimNames.IssuedAt] = issuedAt.ToUnixTimeSeconds(),
            [JwtClaimNames.NotBefore] = issuedAt.ToUnixTimeSeconds(),
            [JwtClaimNames.ExpiresAt] = expiresAt.ToUnixTimeSeconds(),
            [JwtClaimNames.Issuer] = _options.Issuer,
            [JwtClaimNames.Audience] = _options.Audience
        };

        if (subject.DealerId is { } dealerId && dealerId != Guid.Empty)
        {
            payload[JwtClaimNames.DealerId] = dealerId.ToString("D");
        }

        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = $"{encodedHeader}.{encodedPayload}";

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        var signatureBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(signingInput));
        var encodedSignature = Base64UrlEncode(signatureBytes);

        return new IssuedAccessToken(
            AccessToken: $"{signingInput}.{encodedSignature}",
            ExpiresAtUtc: expiresAt,
            JwtId: jwtId);
    }

    public string GenerateRefreshToken()
    {
        Span<byte> randomBytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        return Base64UrlEncode(randomBytes);
    }

    public string ComputeRefreshTokenHash(string rawRefreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawRefreshToken);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken.Trim()));
        return Convert.ToHexStringLower(digest);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
