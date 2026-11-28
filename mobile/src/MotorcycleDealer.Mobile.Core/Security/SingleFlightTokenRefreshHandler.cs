using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MotorcycleDealer.Mobile.Core.Contracts;

namespace MotorcycleDealer.Mobile.Core.Security;

/// <summary>
/// Singleton synchronization lock shared across all rotated DelegatingHandler instances created by IHttpClientFactory.
/// </summary>
public sealed class SingleFlightRefreshLock : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public void Dispose() => Gate.Dispose();
}

/// <summary>
/// HTTP DelegatingHandler implementing a single-flight SemaphoreSlim(1, 1) refresh gate with double-check
/// token rotation, full request cloning (headers + buffered content bytes), and immediate session purge on
/// 403 ACCOUNT_LOCKED / STAFF_DEALER_UNASSIGNED or failed refresh.
/// </summary>
public sealed class SingleFlightTokenRefreshHandler : DelegatingHandler
{
    private static readonly HashSet<string> ForcedSignOutErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACCOUNT_LOCKED",
        "STAFF_DEALER_UNASSIGNED"
    };

    private static readonly HashSet<string> AnonymousAuthPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/auth/refresh"
    };

    private readonly ISecureTokenStore _tokenStore;
    private readonly IAuthRefreshClient _authRefreshClient;
    private readonly ISessionResetCoordinator _sessionResetCoordinator;
    private readonly MobileSessionState _sessionState;
    private readonly SingleFlightRefreshLock _refreshLock;

    public SingleFlightTokenRefreshHandler(
        ISecureTokenStore tokenStore,
        IAuthRefreshClient authRefreshClient,
        ISessionResetCoordinator sessionResetCoordinator,
        MobileSessionState sessionState,
        SingleFlightRefreshLock refreshLock)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _authRefreshClient = authRefreshClient ?? throw new ArgumentNullException(nameof(authRefreshClient));
        _sessionResetCoordinator = sessionResetCoordinator ?? throw new ArgumentNullException(nameof(sessionResetCoordinator));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _refreshLock = refreshLock ?? throw new ArgumentNullException(nameof(refreshLock));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var isAnonymousAuthPath = IsAnonymousAuthEndpoint(request.RequestUri);

        // Buffer content upfront so we can safely retry on 401 without ObjectDisposedException.
        byte[]? bufferedContentBytes = null;
        List<KeyValuePair<string, IEnumerable<string>>>? bufferedContentHeaders = null;
        if (request.Content is not null)
        {
            bufferedContentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            bufferedContentHeaders = request.Content.Headers
                .Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value.ToArray()))
                .ToList();
        }

        string? usedBearerToken = null;
        if (!isAnonymousAuthPath)
        {
            if (request.Headers.Authorization is { Scheme: "Bearer", Parameter: { Length: > 0 } existingToken })
            {
                usedBearerToken = existingToken;
            }
            else
            {
                var storedToken = await _tokenStore.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(storedToken))
                {
                    usedBearerToken = storedToken;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", storedToken);
                }
            }
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Forbidden && !isAnonymousAuthPath)
        {
            await InspectForbiddenRevocationAsync(response, cancellationToken).ConfigureAwait(false);
            return response;
        }

        if (response.StatusCode != HttpStatusCode.Unauthorized ||
            isAnonymousAuthPath ||
            string.IsNullOrWhiteSpace(usedBearerToken))
        {
            return response;
        }

        var (newAccessToken, isTransientNetworkError) = await AcquireFreshAccessTokenSingleFlightAsync(usedBearerToken, cancellationToken)
            .ConfigureAwait(false);

        if (isTransientNetworkError)
        {
            response.Dispose();
            throw new HttpRequestException(
                "Transient network/server error during token refresh; offline session preserved.",
                null,
                HttpStatusCode.ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            return response;
        }

        response.Dispose();

        using var clonedRequest = CloneRequest(request, bufferedContentBytes, bufferedContentHeaders, newAccessToken);
        var retriedResponse = await base.SendAsync(clonedRequest, cancellationToken).ConfigureAwait(false);

        if (retriedResponse.StatusCode == HttpStatusCode.Forbidden)
        {
            await InspectForbiddenRevocationAsync(retriedResponse, cancellationToken).ConfigureAwait(false);
        }

        return retriedResponse;
    }

    private async Task<(string? AccessToken, bool IsTransientNetworkError)> AcquireFreshAccessTokenSingleFlightAsync(
        string failedBearerToken,
        CancellationToken cancellationToken)
    {
        await _refreshLock.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check: did another concurrent request already rotate the token while we waited?
            var latestAccessToken = await _tokenStore.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(latestAccessToken) &&
                !string.Equals(latestAccessToken, failedBearerToken, StringComparison.Ordinal))
            {
                return (latestAccessToken, false);
            }

            var refreshToken = await _tokenStore.GetRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await _sessionResetCoordinator.ForceSignOutAsync(
                    "REFRESH_TOKEN_MISSING",
                    "Your session has expired. Please sign in again.",
                    cancellationToken).ConfigureAwait(false);
                return (null, false);
            }

            var refreshResult = await _authRefreshClient.RefreshAsync(refreshToken, cancellationToken)
                .ConfigureAwait(false);

            if (!refreshResult.IsSuccess || refreshResult.Data is null)
            {
                // Preserve tokens & encrypted SQLCipher offline cache when refresh fails due to transient offline/network or 429/5xx server error
                bool isTransientRefreshFailure =
                    (refreshResult.IsNetworkError || refreshResult.StatusCode == 429 || refreshResult.StatusCode >= 500) &&
                    !refreshResult.IsUnauthorized401 &&
                    !refreshResult.IsForbidden403;
                if (isTransientRefreshFailure)
                {
                    return (null, true);
                }

                var errorCode = refreshResult.Problem?.ErrorCode ?? "SESSION_EXPIRED";
                await _sessionResetCoordinator.ForceSignOutAsync(
                    errorCode,
                    refreshResult.ErrorMessage,
                    cancellationToken).ConfigureAwait(false);
                return (null, false);
            }

            var tokenData = refreshResult.Data;
            await _tokenStore.SaveTokensAsync(
                new AuthTokens(tokenData.AccessToken, tokenData.RefreshToken, tokenData.ExpiresAtUtc),
                cancellationToken).ConfigureAwait(false);

            _sessionState.ApplyAuthenticatedUser(tokenData.User);
            return (tokenData.AccessToken, false);
        }
        finally
        {
            _refreshLock.Gate.Release();
        }
    }

    private async Task InspectForbiddenRevocationAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content is null)
        {
            return;
        }

        var rawBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var originalHeaders = response.Content.Headers
            .Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value.ToArray()))
            .ToList();

        // Restore content so downstream callers can still deserialize the 403 ProblemDetails payload.
        var restoredContent = new ByteArrayContent(rawBytes);
        foreach (var header in originalHeaders)
        {
            restoredContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        response.Content = restoredContent;

        if (rawBytes.Length == 0)
        {
            return;
        }

        try
        {
            var problem = JsonSerializer.Deserialize(rawBytes, MobileJsonSerializerContext.Default.ProblemDetailsPayload);
            if (problem is not null &&
                !string.IsNullOrWhiteSpace(problem.ErrorCode) &&
                ForcedSignOutErrorCodes.Contains(problem.ErrorCode))
            {
                await _sessionResetCoordinator.ForceSignOutAsync(
                    problem.ErrorCode,
                    problem.Detail ?? problem.Title,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (JsonException)
        {
            // Non-JSON 403 body: ignore and return as-is.
        }
    }

    private static HttpRequestMessage CloneRequest(
        HttpRequestMessage original,
        byte[]? contentBytes,
        List<KeyValuePair<string, IEnumerable<string>>>? contentHeaders,
        string newAccessToken)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
            VersionPolicy = original.VersionPolicy
        };

        foreach (var header in original.Headers)
        {
            if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

        foreach (var option in original.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        if (contentBytes is not null)
        {
            var clonedContent = new ByteArrayContent(contentBytes);
            if (contentHeaders is not null)
            {
                foreach (var header in contentHeaders)
                {
                    clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            clone.Content = clonedContent;
        }

        return clone;
    }

    private static bool IsAnonymousAuthEndpoint(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        var normalized = uri.AbsolutePath.TrimEnd('/');
        return AnonymousAuthPaths.Contains(normalized);
    }
}
