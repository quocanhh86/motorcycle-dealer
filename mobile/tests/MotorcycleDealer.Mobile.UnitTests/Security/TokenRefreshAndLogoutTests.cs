using System.Net;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.UnitTests.Fakes;
using Xunit;

namespace MotorcycleDealer.Mobile.UnitTests.Security;

public sealed class TokenRefreshAndLogoutTests
{
    private sealed class StubAuthRefreshClient : IAuthRefreshClient
    {
        private int _refreshCalls;
        public int RefreshCallCount => Volatile.Read(ref _refreshCalls);
        public List<LogoutRequest> LogoutRequests { get; } = [];
        public Func<string, Task<ApiResult<AuthTokenData>>>? OnRefresh { get; set; }
        public Func<string, LogoutRequest, Task<ApiResult<LogoutResultDto>>>? OnLogout { get; set; }

        public Task<ApiResult<AuthTokenData>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _refreshCalls);
            return OnRefresh is not null
                ? OnRefresh(refreshToken)
                : Task.FromResult(ApiResult<AuthTokenData>.Fail(401));
        }

        public Task<ApiResult<LogoutResultDto>> LogoutAsync(string accessToken, LogoutRequest request, CancellationToken cancellationToken = default)
        {
            LogoutRequests.Add(request);
            return OnLogout is not null
                ? OnLogout(accessToken, request)
                : Task.FromResult(ApiResult<LogoutResultDto>.Ok(new LogoutResultDto(true, 1)));
        }
    }

    private sealed class RecordingSessionResetCoordinator : ISessionResetCoordinator
    {
        public List<(string ReasonCode, string? Message)> Calls { get; } = [];
        public event EventHandler<SessionResetEventArgs>? SessionReset;

        public Task ForceSignOutAsync(string reasonCode, string? message = null, CancellationToken cancellationToken = default)
        {
            Calls.Add((reasonCode, message));
            SessionReset?.Invoke(this, new SessionResetEventArgs(reasonCode, message, true));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SingleFlightHandler_10Concurrent401Calls_TriggersExactlyOneRefreshAndRetriesAll10()
    {
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("expired-jwt-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(-5)));

        var session = new MobileSessionState();
        var resetCoordinator = new RecordingSessionResetCoordinator();
        using var refreshLock = new SingleFlightRefreshLock();

        var refreshClient = new StubAuthRefreshClient
        {
            OnRefresh = async rt =>
            {
                Assert.Equal("refresh-1", rt);
                await Task.Delay(40); // Keep lock contended so all 10 tasks pile onto SemaphoreSlim
                return ApiResult<AuthTokenData>.Ok(new AuthTokenData(
                    AccessToken: "fresh-jwt-2",
                    RefreshToken: "refresh-2",
                    ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(15),
                    ExpiresInSeconds: 900,
                    User: TestDataFactory.CustomerUser()));
            }
        };

        var inner = new DelegateHttpMessageHandler((req, _) =>
        {
            var bearer = req.Headers.Authorization?.Parameter;
            if (bearer == "fresh-jwt-2")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\":true}")
                });
            }

            return Task.FromResult(TestJsonResponses.Problem(HttpStatusCode.Unauthorized, "TOKEN_EXPIRED", "Access token expired."));
        });

        using var handler = new SingleFlightTokenRefreshHandler(tokenStore, refreshClient, resetCoordinator, session, refreshLock)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        var tasks = Enumerable.Range(0, 10)
            .Select(i => client.GetAsync($"http://10.0.2.2:5080/api/v1/vehicles/my?i={i}"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        Assert.Equal(1, refreshClient.RefreshCallCount);
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal("fresh-jwt-2", await tokenStore.GetAccessTokenAsync());
        Assert.Equal("refresh-2", await tokenStore.GetRefreshTokenAsync());
        Assert.Empty(resetCoordinator.Calls);
        Assert.True(session.IsAuthenticated);
    }

    [Fact]
    public async Task SingleFlightHandler_ClonesPostBodyAndHeadersOn401Retry()
    {
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("stale-jwt", "rt-1", DateTimeOffset.UtcNow.AddMinutes(-1)));

        var session = new MobileSessionState();
        var resetCoordinator = new RecordingSessionResetCoordinator();
        using var refreshLock = new SingleFlightRefreshLock();
        var refreshClient = new StubAuthRefreshClient
        {
            OnRefresh = _ => Task.FromResult(ApiResult<AuthTokenData>.Ok(new AuthTokenData(
                "new-jwt", "rt-2", DateTimeOffset.UtcNow.AddMinutes(15), 900, TestDataFactory.CustomerUser())))
        };

        string? receivedBodyOnRetry = null;
        string? receivedCustomHeaderOnRetry = null;

        var inner = new DelegateHttpMessageHandler(async (req, ct) =>
        {
            var body = req.Content is not null ? await req.Content.ReadAsStringAsync(ct) : null;
            if (req.Headers.Authorization?.Parameter == "new-jwt")
            {
                receivedBodyOnRetry = body;
                receivedCustomHeaderOnRetry = req.Headers.TryGetValues("X-Client-Trace", out var vals) ? vals.FirstOrDefault() : null;
                return new HttpResponseMessage(HttpStatusCode.Created);
            }

            return TestJsonResponses.Problem(HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Expired.");
        });

        using var handler = new SingleFlightTokenRefreshHandler(tokenStore, refreshClient, resetCoordinator, session, refreshLock)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        using var req = new HttpRequestMessage(HttpMethod.Post, "http://10.0.2.2:5080/api/v1/bookings")
        {
            Content = new StringContent("{\"serviceType\":\"PeriodicMaintenance\"}")
        };
        req.Headers.Add("X-Client-Trace", "mobile-trace-99");

        var response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("{\"serviceType\":\"PeriodicMaintenance\"}", receivedBodyOnRetry);
        Assert.Equal("mobile-trace-99", receivedCustomHeaderOnRetry);
    }

    [Fact]
    public async Task SingleFlightHandler_ClonesPatchBodyContentHeadersAndIdempotencyKeyOn401Retry()
    {
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("stale-jwt", "rt-patch-1", DateTimeOffset.UtcNow.AddMinutes(-1)));

        var session = new MobileSessionState();
        var resetCoordinator = new RecordingSessionResetCoordinator();
        using var refreshLock = new SingleFlightRefreshLock();
        var refreshClient = new StubAuthRefreshClient
        {
            OnRefresh = _ => Task.FromResult(ApiResult<AuthTokenData>.Ok(new AuthTokenData(
                "fresh-patch-jwt", "rt-patch-2", DateTimeOffset.UtcNow.AddMinutes(15), 900, TestDataFactory.StaffUser())))
        };

        string? retryBody = null;
        string? retryIdempotencyKey = null;
        string? retryContentType = null;

        var inner = new DelegateHttpMessageHandler(async (req, ct) =>
        {
            var body = req.Content is not null ? await req.Content.ReadAsStringAsync(ct) : null;
            if (req.Headers.Authorization?.Parameter == "fresh-patch-jwt")
            {
                retryBody = body;
                retryIdempotencyKey = req.Headers.TryGetValues("Idempotency-Key", out var vals) ? vals.FirstOrDefault() : null;
                retryContentType = req.Content?.Headers.ContentType?.MediaType;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return TestJsonResponses.Problem(HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Expired.");
        });

        using var handler = new SingleFlightTokenRefreshHandler(tokenStore, refreshClient, resetCoordinator, session, refreshLock)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        using var req = new HttpRequestMessage(HttpMethod.Patch, "http://10.0.2.2:5080/api/v1/bookings/11111111-1111-1111-1111-111111111111/status")
        {
            Content = new StringContent("{\"newStatus\":\"CheckedIn\",\"rowVersion\":\"AAAAAAAAD01=\"}", System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Idempotency-Key", "MOB-PATCH-KEY-01");

        var response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"newStatus\":\"CheckedIn\",\"rowVersion\":\"AAAAAAAAD01=\"}", retryBody);
        Assert.Equal("MOB-PATCH-KEY-01", retryIdempotencyKey);
        Assert.Equal("application/json", retryContentType);
    }

    [Theory]
    [InlineData("ACCOUNT_LOCKED")]
    [InlineData("STAFF_DEALER_UNASSIGNED")]
    public async Task SingleFlightHandler_403RevocationErrorCode_TriggersImmediateForceSignOut(string errorCode)
    {
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("active-jwt", "rt-1", DateTimeOffset.UtcNow.AddMinutes(10)));

        var session = new MobileSessionState();
        var resetCoordinator = new RecordingSessionResetCoordinator();
        using var refreshLock = new SingleFlightRefreshLock();
        var refreshClient = new StubAuthRefreshClient();

        var inner = new DelegateHttpMessageHandler((_, _) =>
            Task.FromResult(TestJsonResponses.Problem(HttpStatusCode.Forbidden, errorCode, "Account revoked.")));

        using var handler = new SingleFlightTokenRefreshHandler(tokenStore, refreshClient, resetCoordinator, session, refreshLock)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://10.0.2.2:5080/api/v1/bookings/dealer");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(errorCode, body);
        Assert.Single(resetCoordinator.Calls);
        Assert.Equal(errorCode, resetCoordinator.Calls[0].ReasonCode);
        Assert.Equal(0, refreshClient.RefreshCallCount);
    }

    [Fact]
    public async Task SingleFlightHandler_FailedRefresh_TriggersForceSignOutAndReturns401()
    {
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("stale-jwt", "revoked-rt", DateTimeOffset.UtcNow.AddMinutes(-2)));

        var session = new MobileSessionState();
        var resetCoordinator = new RecordingSessionResetCoordinator();
        using var refreshLock = new SingleFlightRefreshLock();
        var refreshClient = new StubAuthRefreshClient
        {
            OnRefresh = _ => Task.FromResult(ApiResult<AuthTokenData>.Fail(
                401,
                new ProblemDetailsPayload { ErrorCode = "REFRESH_TOKEN_REUSE", Detail = "Refresh token reused." }))
        };

        var inner = new DelegateHttpMessageHandler((_, _) =>
            Task.FromResult(TestJsonResponses.Problem(HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Expired")));

        using var handler = new SingleFlightTokenRefreshHandler(tokenStore, refreshClient, resetCoordinator, session, refreshLock)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://10.0.2.2:5080/api/v1/loyalty/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, refreshClient.RefreshCallCount);
        Assert.Single(resetCoordinator.Calls);
        Assert.Equal("REFRESH_TOKEN_REUSE", resetCoordinator.Calls[0].ReasonCode);
    }

    [Fact]
    public async Task SingleFlightHandler_MissingRefreshToken_TriggersForceSignOutWithoutCallingRefresh()
    {
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("stale-jwt", string.Empty, DateTimeOffset.UtcNow.AddMinutes(-2)));

        var session = new MobileSessionState();
        var resetCoordinator = new RecordingSessionResetCoordinator();
        using var refreshLock = new SingleFlightRefreshLock();
        var refreshClient = new StubAuthRefreshClient();

        var inner = new DelegateHttpMessageHandler((_, _) =>
            Task.FromResult(TestJsonResponses.Problem(HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Expired")));

        using var handler = new SingleFlightTokenRefreshHandler(tokenStore, refreshClient, resetCoordinator, session, refreshLock)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://10.0.2.2:5080/api/v1/loyalty/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, refreshClient.RefreshCallCount);
        Assert.Single(resetCoordinator.Calls);
        Assert.Equal("REFRESH_TOKEN_MISSING", resetCoordinator.Calls[0].ReasonCode);
    }

    [Fact]
    public async Task SingleFlightHandler_AnonymousLoginEndpoint_DoesNotAttachBearerOrRefresh()
    {
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("old-jwt", "old-rt", DateTimeOffset.UtcNow.AddMinutes(5)));

        var session = new MobileSessionState();
        var resetCoordinator = new RecordingSessionResetCoordinator();
        using var refreshLock = new SingleFlightRefreshLock();
        var refreshClient = new StubAuthRefreshClient();

        string? capturedAuth = "SENTINEL";
        var inner = new DelegateHttpMessageHandler((req, _) =>
        {
            capturedAuth = req.Headers.Authorization?.ToString();
            return Task.FromResult(TestJsonResponses.Problem(HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS", "Bad password"));
        });

        using var handler = new SingleFlightTokenRefreshHandler(tokenStore, refreshClient, resetCoordinator, session, refreshLock)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        var res = await client.PostAsync("http://10.0.2.2:5080/api/v1/auth/login", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Null(capturedAuth);
        Assert.Equal(0, refreshClient.RefreshCallCount);
        Assert.Empty(resetCoordinator.Calls);
    }

    [Fact]
    public async Task LogoutCoordinator_DefaultLogout_SendsRevokeAllFalse_WipesUserSqlCipher_PreservesPublicCatalog()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("jwt-1", "rt-device-only", DateTimeOffset.UtcNow.AddMinutes(15)));

        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser());

        await db.SaveDealersAsync([TestDataFactory.SampleDealer()]);
        await db.SaveMotorcycleModelsAsync([TestDataFactory.SampleModel()]);
        await db.SaveLoyaltyWalletAsync(session.OwnerScopeKey, TestDataFactory.SampleWallet());
        await db.SaveCustomerVehiclesAsync(session.OwnerScopeKey, [TestDataFactory.SampleVehicle()]);

        var refreshClient = new StubAuthRefreshClient();
        var realtimeStopped = false;
        var coordinator = new LogoutCoordinator(tokenStore, refreshClient, db, session);
        coordinator.RegisterRealtimeStopHook(_ =>
        {
            realtimeStopped = true;
            return Task.CompletedTask;
        });

        var result = await coordinator.LogoutAsync(); // Default revokeAllDevices: false!

        Assert.True(result.Revoked);
        Assert.Single(refreshClient.LogoutRequests);
        Assert.False(refreshClient.LogoutRequests[0].RevokeAllDevices);
        Assert.Equal("rt-device-only", refreshClient.LogoutRequests[0].RefreshToken);
        Assert.True(realtimeStopped);
        Assert.Null(await tokenStore.GetAccessTokenAsync());
        Assert.Null(await tokenStore.GetRefreshTokenAsync());
        Assert.False(session.IsAuthenticated);

        // User-scoped SQLCipher data wiped
        Assert.Null(await db.GetCachedLoyaltyWalletAsync(TestDataFactory.CustomerUser().UserId + ":Customer:global"));
        Assert.Empty(await db.GetCachedCustomerVehiclesAsync(TestDataFactory.CustomerUser().UserId + ":Customer:global"));

        // Public showroom data preserved!
        Assert.Single(await db.GetCachedDealersAsync());
        Assert.Single(await db.GetCachedMotorcycleModelsAsync());
    }

    [Fact]
    public async Task LogoutCoordinator_RevokeAllDevicesTrue_ForwardsFlagToBackend()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await tokenStore.SaveTokensAsync(new AuthTokens("jwt-1", "rt-1", DateTimeOffset.UtcNow.AddMinutes(15)));
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser());

        var refreshClient = new StubAuthRefreshClient();
        var coordinator = new LogoutCoordinator(tokenStore, refreshClient, db, session);

        await coordinator.LogoutAsync(revokeAllDevices: true);

        Assert.Single(refreshClient.LogoutRequests);
        Assert.True(refreshClient.LogoutRequests[0].RevokeAllDevices);
    }

    [Fact]
    public async Task DefaultApiEndpointProvider_DefaultsToAndroidEmulatorAndSwitchesCleanly()
    {
        var store = new InMemorySecureTokenStore();
        var provider = new DefaultApiEndpointProvider(store);

        Assert.Equal("http://10.0.2.2:5080", provider.CurrentBaseUrl);
        Assert.Equal("http://10.0.2.2:5080/hubs/notifications", provider.BuildNotificationHubUri().AbsoluteUri);

        await provider.SetBaseUrlAsync("http://localhost:5080/");
        Assert.Equal("http://localhost:5080", provider.CurrentBaseUrl);
        Assert.Equal("http://localhost:5080/api/v1/health", provider.BuildApiUri("/api/v1/health").AbsoluteUri);

        var provider2 = new DefaultApiEndpointProvider(store);
        await provider2.InitializeAsync();
        Assert.Equal("http://localhost:5080", provider2.CurrentBaseUrl);

        await Assert.ThrowsAsync<ArgumentException>(() => provider.SetBaseUrlAsync("ftp://invalid-scheme"));
    }
}
