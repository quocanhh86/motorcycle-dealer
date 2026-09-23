using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MotorcycleDealer.Mobile.Core.Contracts;

namespace MotorcycleDealer.Mobile.Core.Security;

public interface IAuthRefreshClient
{
    Task<ApiResult<AuthTokenData>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApiResult<LogoutResultDto>> LogoutAsync(string accessToken, LogoutRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dedicated handler-free auth client (named "AuthRefreshClient") that bypasses
/// SingleFlightTokenRefreshHandler to prevent recursive 401 refresh deadlocks.
/// </summary>
public sealed class AuthRefreshClient : IAuthRefreshClient
{
    public const string HttpClientName = "AuthRefreshClient";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiEndpointProvider _endpointProvider;

    public AuthRefreshClient(IHttpClientFactory httpClientFactory, IApiEndpointProvider endpointProvider)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
    }

    public async Task<ApiResult<AuthTokenData>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpointProvider.BuildApiUri("/api/v1/auth/refresh"))
            {
                Content = JsonContent.Create(
                    new RefreshTokenRequest { RefreshToken = refreshToken },
                    MobileJsonSerializerContext.Default.RefreshTokenRequest)
            };

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var envelope = await response.Content.ReadFromJsonAsync(
                    MobileJsonSerializerContext.Default.ApiEnvelopeAuthTokenData,
                    cancellationToken).ConfigureAwait(false);

                return envelope is not null
                    ? ApiResult<AuthTokenData>.Ok(envelope.Data, (int)response.StatusCode)
                    : ApiResult<AuthTokenData>.Fail((int)response.StatusCode);
            }

            var problem = await TryReadProblemAsync(response, cancellationToken).ConfigureAwait(false);
            return ApiResult<AuthTokenData>.Fail((int)response.StatusCode, problem);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<AuthTokenData>.Fail(
                0,
                new ProblemDetailsPayload { Title = "Network Error", Detail = ex.Message, ErrorCode = "NETWORK_UNREACHABLE" },
                isNetworkError: true);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<AuthTokenData>.Fail(
                0,
                new ProblemDetailsPayload { Title = "Request Timeout", Detail = "Refresh request timed out.", ErrorCode = "REQUEST_TIMEOUT" },
                isNetworkError: true);
        }
    }

    public async Task<ApiResult<LogoutResultDto>> LogoutAsync(
        string accessToken,
        LogoutRequest requestPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpointProvider.BuildApiUri("/api/v1/auth/logout"))
            {
                Content = JsonContent.Create(requestPayload, MobileJsonSerializerContext.Default.LogoutRequest)
            };

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var envelope = await response.Content.ReadFromJsonAsync(
                    MobileJsonSerializerContext.Default.ApiEnvelopeLogoutResultDto,
                    cancellationToken).ConfigureAwait(false);

                return envelope is not null
                    ? ApiResult<LogoutResultDto>.Ok(envelope.Data, (int)response.StatusCode)
                    : ApiResult<LogoutResultDto>.Ok(new LogoutResultDto(true, 0), (int)response.StatusCode);
            }

            var problem = await TryReadProblemAsync(response, cancellationToken).ConfigureAwait(false);
            return ApiResult<LogoutResultDto>.Fail((int)response.StatusCode, problem);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<LogoutResultDto>.Fail(
                0,
                new ProblemDetailsPayload { Title = "Network Error", Detail = ex.Message, ErrorCode = "NETWORK_UNREACHABLE" },
                isNetworkError: true);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<LogoutResultDto>.Fail(
                0,
                new ProblemDetailsPayload { Title = "Request Timeout", Detail = "Logout request timed out.", ErrorCode = "REQUEST_TIMEOUT" },
                isNetworkError: true);
        }
    }

    private static async Task<ProblemDetailsPayload?> TryReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return JsonSerializer.Deserialize(body, MobileJsonSerializerContext.Default.ProblemDetailsPayload);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
