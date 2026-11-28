using System.Globalization;
using System.Security.Cryptography;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.App.Services;

/// <summary>
/// Cross-platform hardware-backed SecureStorage implementation of ISecureTokenStore.
/// Uses Android Keystore on Android (with allowBackup="false") and Apple iOS Keychain on iOS
/// (backed by Platforms/iOS/Entitlements.plist keychain-access-groups).
/// Never stores JWTs, refresh tokens, or the 256-bit SQLCipher passphrase inside SQLite.
/// </summary>
public sealed class MauiSecureTokenStore : ISecureTokenStore
{
    private const string AccessTokenKey = "apex_auth_access_token";
    private const string RefreshTokenKey = "apex_auth_refresh_token";
    private const string ExpiresAtUtcKey = "apex_auth_expires_at_utc";
    private const string SqlCipherKeyHexKey = "apex_sqlcipher_key_hex";
    private const string ApiBaseUrlKey = "apex_api_base_url";

    private readonly SemaphoreSlim _cipherKeyLock = new(1, 1);
    private readonly Dictionary<string, string> _memoryFallback = new(StringComparer.Ordinal);

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
        => GetAsync(RefreshTokenKey);

    public async Task<DateTimeOffset?> GetAccessTokenExpiresAtUtcAsync(CancellationToken cancellationToken = default)
    {
        string? raw = await GetAsync(ExpiresAtUtcKey).ConfigureAwait(false);
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed)
            ? parsed
            : null;
    }

    public async Task SaveTokensAsync(AuthTokens tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        await SetAsync(AccessTokenKey, tokens.AccessToken).ConfigureAwait(false);
        await SetAsync(RefreshTokenKey, tokens.RefreshToken).ConfigureAwait(false);
        await SetAsync(ExpiresAtUtcKey, tokens.ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture)).ConfigureAwait(false);
    }

    public Task ClearAuthTokensAsync(CancellationToken cancellationToken = default)
    {
        Remove(AccessTokenKey);
        Remove(RefreshTokenKey);
        Remove(ExpiresAtUtcKey);
        return Task.CompletedTask;
    }

    public async Task<string> GetOrCreateSqlCipherKeyHexAsync(CancellationToken cancellationToken = default)
    {
        (bool readOk, string? existing) = await TryReadSecureValueAsync(SqlCipherKeyHexKey).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        if (!readOk)
        {
            throw new InvalidOperationException("Hardware SecureStorage/Keychain is temporarily unavailable; refusing to regenerate SQLCipher key.");
        }

        await _cipherKeyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            (readOk, existing) = await TryReadSecureValueAsync(SqlCipherKeyHexKey).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            if (!readOk)
            {
                throw new InvalidOperationException("Hardware SecureStorage/Keychain is temporarily unavailable; refusing to regenerate SQLCipher key.");
            }

            byte[] keyBytes = RandomNumberGenerator.GetBytes(32);
            string hex = Convert.ToHexString(keyBytes);
            try
            {
                await SecureStorage.Default.SetAsync(SqlCipherKeyHexKey, hex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Hardware SecureStorage/Keychain write failed; refusing to encrypt offline SQLCipher DB with an ephemeral RAM-only key.",
                    ex);
            }

            lock (_memoryFallback)
            {
                _memoryFallback[SqlCipherKeyHexKey] = hex;
            }

            return hex;
        }
        finally
        {
            _cipherKeyLock.Release();
        }
    }

    private async Task<(bool Ok, string? Value)> TryReadSecureValueAsync(string key)
    {
        try
        {
            string? persisted = await SecureStorage.Default.GetAsync(key).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(persisted))
            {
                lock (_memoryFallback)
                {
                    _memoryFallback[key] = persisted;
                }
            }

            return (true, persisted);
        }
        catch
        {
            lock (_memoryFallback)
            {
                if (_memoryFallback.TryGetValue(key, out string? cached) && !string.IsNullOrWhiteSpace(cached))
                {
                    return (true, cached);
                }
            }

            return (false, null);
        }
    }

    public Task<string?> GetApiBaseUrlAsync(CancellationToken cancellationToken = default)
        => GetAsync(ApiBaseUrlKey);

    public Task SaveApiBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default)
        => SetAsync(ApiBaseUrlKey, baseUrl);

    private async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key).ConfigureAwait(false);
        }
        catch
        {
            lock (_memoryFallback)
            {
                return _memoryFallback.TryGetValue(key, out string? value) ? value : null;
            }
        }
    }

    private async Task SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value).ConfigureAwait(false);
        }
        catch
        {
            lock (_memoryFallback)
            {
                _memoryFallback[key] = value;
            }
        }
    }

    private void Remove(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch
        {
            // Fall through to clear fallback cache
        }

        lock (_memoryFallback)
        {
            _memoryFallback.Remove(key);
        }
    }
}
