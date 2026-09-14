using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.Core.Security;

public interface IApiEndpointProvider
{
    string CurrentBaseUrl { get; }
    Uri CurrentBaseUri { get; }
    event EventHandler<string>? EndpointChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SetBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default);
    Uri BuildApiUri(string relativePathAndQuery);
    Uri BuildNotificationHubUri();
}

public sealed class DefaultApiEndpointProvider : IApiEndpointProvider
{
    private readonly ISecureTokenStore _tokenStore;
    private readonly object _sync = new();
    private Uri _currentBaseUri;

    public DefaultApiEndpointProvider(ISecureTokenStore tokenStore, string? initialBaseUrl = null)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _currentBaseUri = ParseAndNormalize(initialBaseUrl ?? MobileEndpointDefaults.EmulatorBaseUrl);
    }

    public string CurrentBaseUrl
    {
        get
        {
            lock (_sync)
            {
                return _currentBaseUri.AbsoluteUri.TrimEnd('/');
            }
        }
    }

    public Uri CurrentBaseUri
    {
        get
        {
            lock (_sync)
            {
                return _currentBaseUri;
            }
        }
    }

    public event EventHandler<string>? EndpointChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await _tokenStore.GetApiBaseUrlAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(persisted) && TryNormalize(persisted, out var parsed))
        {
            lock (_sync)
            {
                _currentBaseUri = parsed;
            }
        }
    }

    public async Task SetBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var normalized = ParseAndNormalize(baseUrl);
        var formatted = normalized.AbsoluteUri.TrimEnd('/');

        lock (_sync)
        {
            _currentBaseUri = normalized;
        }

        await _tokenStore.SaveApiBaseUrlAsync(formatted, cancellationToken).ConfigureAwait(false);
        EndpointChanged?.Invoke(this, formatted);
    }

    public Uri BuildApiUri(string relativePathAndQuery)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePathAndQuery);
        var trimmed = relativePathAndQuery.TrimStart('/');
        return new Uri(CurrentBaseUri, trimmed);
    }

    public Uri BuildNotificationHubUri() => BuildApiUri(SignalRHubEventNames.HubPath);

    public static bool TryNormalize(string? rawUrl, out Uri normalizedUri)
    {
        normalizedUri = default!;
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return false;
        }

        var trimmed = rawUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.TrimEnd('/') + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };

        normalizedUri = builder.Uri;
        return true;
    }

    private static Uri ParseAndNormalize(string rawUrl)
    {
        if (!TryNormalize(rawUrl, out var parsed))
        {
            throw new ArgumentException($"Invalid API base URL '{rawUrl}'. Expected an http:// or https:// absolute URI.", nameof(rawUrl));
        }

        return parsed;
    }
}
