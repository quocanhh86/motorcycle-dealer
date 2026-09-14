namespace MotorcycleDealer.Mobile.Core.Security;

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Hardware-backed secure storage abstraction (Android Keystore via MAUI SecureStorage.Default in Mobile.App,
/// in-memory fake in UnitTests).
/// </summary>
public interface ISecureTokenStore
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetAccessTokenExpiresAtUtcAsync(CancellationToken cancellationToken = default);
    Task SaveTokensAsync(AuthTokens tokens, CancellationToken cancellationToken = default);
    Task ClearAuthTokensAsync(CancellationToken cancellationToken = default);

    Task<string> GetOrCreateSqlCipherKeyHexAsync(CancellationToken cancellationToken = default);

    Task<string?> GetApiBaseUrlAsync(CancellationToken cancellationToken = default);
    Task SaveApiBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default);
}
