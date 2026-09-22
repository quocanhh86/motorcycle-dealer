using System.Net;
using System.Text;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.UnitTests.Fakes;
using Xunit;

namespace MotorcycleDealer.Mobile.UnitTests.Api;

public sealed class DealerMobileApiClientAndIsolationTests
{
    [Fact]
    public async Task RedeemVoucherAsync_AttachesIdempotencyKeyHttpHeaderAndJsonBody()
    {
        string? capturedHeader = null;
        string? capturedBody = null;

        var apiClient = CreateMockApiClient(async (req, ct) =>
        {
            capturedHeader = req.Headers.TryGetValues("Idempotency-Key", out var vals)
                ? vals.FirstOrDefault()
                : null;
            capturedBody = req.Content is not null ? await req.Content.ReadAsStringAsync(ct) : null;

            return TestJsonResponses.JsonOk(
                new ApiEnvelope<CustomerVoucherDto>(true, TestDataFactory.SampleVoucher(), TestDataFactory.Meta()),
                MobileJsonSerializerContext.Default.ApiEnvelopeCustomerVoucherDto,
                HttpStatusCode.Created);
        });

        var result = await apiClient.RedeemVoucherAsync(new RedeemVoucherRequest
        {
            VoucherCatalogId = Guid.NewGuid(),
            AccountRowVersion = "AAAAAAAAB9E=",
            IdempotencyKey = "  MOB-REDEEM-IDEMP-001  "
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("MOB-REDEEM-IDEMP-001", capturedHeader);
        Assert.NotNull(capturedBody);
        Assert.Contains("MOB-REDEEM-IDEMP-001", capturedBody);
    }

    [Fact]
    public async Task RedeemVoucherAsync_OmitsIdempotencyKeyHeaderWhenNotProvided()
    {
        var hasHeader = true;
        var apiClient = CreateMockApiClient((req, _) =>
        {
            hasHeader = req.Headers.Contains("Idempotency-Key");
            return Task.FromResult(TestJsonResponses.JsonOk(
                new ApiEnvelope<CustomerVoucherDto>(true, TestDataFactory.SampleVoucher(), TestDataFactory.Meta()),
                MobileJsonSerializerContext.Default.ApiEnvelopeCustomerVoucherDto,
                HttpStatusCode.Created));
        });

        var result = await apiClient.RedeemVoucherAsync(new RedeemVoucherRequest
        {
            VoucherCatalogId = Guid.NewGuid()
        });

        Assert.True(result.IsSuccess);
        Assert.False(hasHeader);
    }

    [Theory]
    [InlineData("<html><body>502 Bad Gateway Proxy</body></html>")]
    [InlineData("{truncated json")]
    [InlineData("not-json-at-all")]
    public async Task ApiClient_OnMalformed200Response_CatchesJsonExceptionGracefully(string corruptBody)
    {
        var apiClient = CreateMockApiClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(corruptBody, Encoding.UTF8, "application/json")
            }));

        var rawHealth = await apiClient.GetHealthAsync();
        var dealersEnvelope = await apiClient.ListDealersAsync();
        var pagedModels = await apiClient.ListMotorcyclesAsync();
        var notificationsFeed = await apiClient.ListNotificationsAsync();

        Assert.False(rawHealth.IsSuccess);
        Assert.Equal(200, rawHealth.StatusCode);
        Assert.Equal("INVALID_JSON_PAYLOAD", rawHealth.Problem?.ErrorCode);

        Assert.False(dealersEnvelope.IsSuccess);
        Assert.Equal("INVALID_JSON_PAYLOAD", dealersEnvelope.Problem?.ErrorCode);

        Assert.False(pagedModels.IsSuccess);
        Assert.Equal("INVALID_JSON_PAYLOAD", pagedModels.Problem?.ErrorCode);

        Assert.False(notificationsFeed.IsSuccess);
        Assert.Equal("INVALID_JSON_PAYLOAD", notificationsFeed.Problem?.ErrorCode);
    }

    [Fact]
    public async Task SignalRMobileService_StopAsync_ResetsWatermarkAndUnreadCountPreventingCrossUserLeak()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        var endpoint = new DefaultApiEndpointProvider(tokenStore, "http://127.0.0.1:1");
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var queriesCaptured = new List<string>();
        var apiClient = CreateMockApiClient((req, _) =>
        {
            queriesCaptured.Add(req.RequestUri!.Query);
            return Task.FromResult(TestJsonResponses.JsonOk(
                new NotificationFeedEnvelope(true, [], 0, new PaginationMeta(1, 50, 0, 0, false, false), new NotificationFeedMeta(DateTimeOffset.UtcNow, "t", 0)),
                MobileJsonSerializerContext.Default.NotificationFeedEnvelope));
        });

        await using var signalR = new SignalRMobileService(
            endpoint, tokenStore, session, apiClient, db, new FakeLocalNotificationService(), new FakeMainThreadDispatcher());

        // User A receives live events
        var userAEventTime = DateTimeOffset.UtcNow;
        await signalR.HandleIncomingBookingStatusChangedAsync(new(
            Guid.NewGuid(), SignalRHubEventNames.BookingStatusChanged, Guid.NewGuid(), "BK-A",
            "Pending", "Confirmed", "Hanoi", userAEventTime));

        Assert.Equal(1, signalR.UnreadCount);
        Assert.Equal(userAEventTime, signalR.LastEventWatermarkUtc);

        // User A signs out -> StopAsync MUST reset in-memory watermark & unread count
        await signalR.StopAsync();
        await db.PurgeUserScopedDataAsync();

        Assert.Equal(0, signalR.UnreadCount);
        Assert.Null(signalR.LastEventWatermarkUtc);

        // User B signs in and syncs notifications -> query MUST NOT carry User A's sinceUtc watermark!
        session.ApplyAuthenticatedUser(TestDataFactory.StaffUser(userId: Guid.Parse("22222222-2222-2222-2222-222222222222")));
        await signalR.SyncMissedNotificationsSinceWatermarkAsync();

        Assert.Single(queriesCaptured);
        Assert.DoesNotContain("sinceUtc=", queriesCaptured[0]);
    }

    private static IDealerMobileApiClient CreateMockApiClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var store = new InMemorySecureTokenStore();
        var endpoint = new DefaultApiEndpointProvider(store, "http://127.0.0.1:5080");
        var httpClient = new HttpClient(new DelegateHttpMessageHandler(handler));
        return new DealerMobileApiClient(new StubHttpClientFactory(_ => httpClient), endpoint);
    }
}
