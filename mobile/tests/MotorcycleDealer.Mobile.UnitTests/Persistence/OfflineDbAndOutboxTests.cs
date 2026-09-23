using System.Net;
using System.Text;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.UnitTests.Fakes;
using SQLite;
using Xunit;

namespace MotorcycleDealer.Mobile.UnitTests.Persistence;

public sealed class OfflineDbAndOutboxTests
{
    [Fact]
    public async Task MobileOfflineDb_InitializesAll9TablesWithWalMode()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();

        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var tables = await db.ListTableNamesAsync();
        var journalMode = await db.GetJournalModeAsync();

        Assert.Equal("wal", journalMode.ToLowerInvariant());
        Assert.Equal(9, tables.Count);
        Assert.Contains("CacheSyncState", tables);
        Assert.Contains("CachedDealer", tables);
        Assert.Contains("CachedMotorcycleModel", tables);
        Assert.Contains("CachedCustomerVehicle", tables);
        Assert.Contains("CachedLoyaltyWallet", tables);
        Assert.Contains("CachedCustomerVoucher", tables);
        Assert.Contains("CachedActiveAppointment", tables);
        Assert.Contains("CachedReceiptSnapshot", tables);
        Assert.Contains("OfflineMutationQueueItem", tables);
    }

    [Fact]
    public async Task MobileOfflineDb_SqlCipherEncryptsFileOnDiskAndRejectsWrongKey()
    {
        using var tempPath = new TempDatabasePathProvider();
        var correctStore = new InMemorySecureTokenStore("0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF");
        var wrongStore = new InMemorySecureTokenStore("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");

        var scope = "user-1:Customer:global";
        await using (var db = new MobileOfflineDb(tempPath, correctStore))
        {
            await db.InitializeAsync();
            await db.SaveLoyaltyWalletAsync(scope, TestDataFactory.SampleWallet("Platinum", 9999, 12000));
            var readBack = await db.GetCachedLoyaltyWalletAsync(scope);
            Assert.NotNull(readBack);
            Assert.Equal("Platinum", readBack.TierCode);
        }

        // Verify raw file on disk is NOT unencrypted plaintext SQLite header ("SQLite format 3\0")
        var rawHeader = new byte[16];
        await using (var fs = File.OpenRead(tempPath.GetDatabasePath()))
        {
            var bytesRead = await fs.ReadAsync(rawHeader.AsMemory(0, 16));
            Assert.Equal(16, bytesRead);
        }

        var headerAscii = Encoding.ASCII.GetString(rawHeader);
        Assert.DoesNotContain("SQLite format 3", headerAscii);

        // Attempting to open and query encrypted DB with a wrong key MUST fail with SQLiteException!
        await using var wrongDb = new MobileOfflineDb(tempPath, wrongStore);
        await Assert.ThrowsAsync<SQLiteException>(() => wrongDb.InitializeAsync());
    }

    [Fact]
    public async Task MobileOfflineDb_OwnerScopeKey_StrictlyIsolatesTenantsAndPreservesInfiniteTtlReceipts()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();

        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var scopeA = "11111111-1111-1111-1111-111111111111:Customer:global";
        var scopeB = "22222222-2222-2222-2222-222222222222:DealerStaff:33333333-3333-3333-3333-333333333333";

        var receiptA = TestDataFactory.SampleServiceReceipt();
        var salesReceiptA = TestDataFactory.SampleSalesReceipt();
        await db.SaveCustomerVehiclesAsync(scopeA, [TestDataFactory.SampleVehicle(vin: "RLHJC7920NY111111")]);
        await db.SaveLoyaltyWalletAsync(scopeA, TestDataFactory.SampleWallet("Gold", 2000, 2500));
        await db.SaveCustomerVouchersAsync(scopeA, [TestDataFactory.SampleVoucher(code: "VCH-A")]);
        await db.SaveActiveBookingsAsync(scopeA, [TestDataFactory.SampleBooking(vin: "RLHJC7920NY111111")]);
        await db.SaveServiceReceiptAsync(scopeA, receiptA);
        await db.SaveSalesReceiptAsync(scopeA, salesReceiptA);

        // Tenant B MUST see zero rows belonging to Tenant A!
        Assert.Empty(await db.GetCachedCustomerVehiclesAsync(scopeB));
        Assert.Null(await db.GetCachedLoyaltyWalletAsync(scopeB));
        Assert.Empty(await db.GetCachedCustomerVouchersAsync(scopeB));
        Assert.Empty(await db.GetCachedActiveBookingsAsync(scopeB));
        Assert.Null(await db.GetCachedServiceReceiptAsync(scopeB, receiptA.InvoiceId));
        Assert.Null(await db.GetCachedSalesReceiptAsync(scopeB, salesReceiptA.SalesOrderId));

        // Tenant A sees its exact infinite-TTL receipts intact!
        var loadedSvc = await db.GetCachedServiceReceiptAsync(scopeA, receiptA.InvoiceId);
        var loadedSales = await db.GetCachedSalesReceiptAsync(scopeA, salesReceiptA.SalesOrderId);
        Assert.NotNull(loadedSvc);
        Assert.Equal(receiptA.VerificationCode, loadedSvc.VerificationCode);
        Assert.NotNull(loadedSales);
        Assert.Equal(salesReceiptA.ReceiptNumber, loadedSales.ReceiptNumber);
        Assert.Equal(2, (await db.ListCachedReceiptSnapshotsAsync(scopeA)).Count);
    }

    [Theory]
    [InlineData(OfflineMutationOperations.SalesOrder)]
    [InlineData(OfflineMutationOperations.ServiceInvoice)]
    [InlineData(OfflineMutationOperations.PointLedger)]
    [InlineData(OfflineMutationOperations.ServiceBooking)]
    [InlineData(OfflineMutationOperations.TestDrive)]
    [InlineData(OfflineMutationOperations.VoucherRedeem)]
    [InlineData("UnknownMutation")]
    public async Task OfflineOutbox_RejectsOnlineOnlyImmutableAndConcurrencyOperations(string forbiddenOp)
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        var connectivity = new FakeConnectivityMonitor(isConnected: false);
        var apiClient = CreateMockApiClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var outbox = new OfflineOutboxSyncService(db, apiClient, connectivity);

        var ex = await Assert.ThrowsAsync<OnlineOnlyViolationException>(() =>
            outbox.EnqueueRawAsync("user:Customer:global", forbiddenOp, Guid.NewGuid().ToString("D"), "{}"));

        Assert.Equal("OnlineOnlyViolation", ex.ErrorCode);
        Assert.Equal(0, await outbox.GetPendingCountAsync("user:Customer:global"));
    }

    [Fact]
    public async Task OfflineOutbox_FlushesAllowedOperationsInStrictFifoOrder()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var connectivity = new FakeConnectivityMonitor(isConnected: false);
        var executedOrder = new List<string>();
        var apiClient = CreateMockApiClient((req, _) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            executedOrder.Add($"{req.Method}:{path}");

            if (path.EndsWith("/read", StringComparison.Ordinal))
            {
                return Task.FromResult(TestJsonResponses.JsonOk(
                    new ApiEnvelope<MarkNotificationReadResultDto>(true, new(Guid.NewGuid(), Guid.NewGuid(), true, DateTimeOffset.UtcNow, 0), TestDataFactory.Meta()),
                    MobileJsonSerializerContext.Default.ApiEnvelopeMarkNotificationReadResultDto));
            }

            if (path.EndsWith("/mileage", StringComparison.Ordinal))
            {
                return Task.FromResult(TestJsonResponses.JsonOk(
                    new ApiEnvelope<CustomerVehicleDto>(true, TestDataFactory.SampleVehicle(), TestDataFactory.Meta()),
                    MobileJsonSerializerContext.Default.ApiEnvelopeCustomerVehicleDto));
            }

            return Task.FromResult(TestJsonResponses.JsonOk(
                new ApiEnvelope<MarkAllNotificationsReadResultDto>(true, new(1, 0, DateTimeOffset.UtcNow), TestDataFactory.Meta()),
                MobileJsonSerializerContext.Default.ApiEnvelopeMarkAllNotificationsReadResultDto));
        });

        var outbox = new OfflineOutboxSyncService(db, apiClient, connectivity);
        var scope = "user:Customer:global";
        var notifId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);

        await outbox.EnqueueRawAsync(scope, OfflineMutationOperations.NotificationRead, notifId.ToString("D"), "{}", t0);
        await outbox.EnqueueRawAsync(scope, OfflineMutationOperations.VehicleMileageUpdate, vehicleId.ToString("D"), "{\"currentMileageKm\":5500}", t0.AddSeconds(10));
        await outbox.EnqueueRawAsync(scope, OfflineMutationOperations.NotificationReadAll, string.Empty, "{}", t0.AddSeconds(20));

        // While offline, flush skips cleanly
        var skipped = await outbox.FlushAsync(scope);
        Assert.True(skipped.SkippedOffline);
        Assert.Equal(3, skipped.RemainingCount);

        // Connectivity restored -> FIFO flush!
        connectivity.SetConnected(true);
        var summary = await outbox.FlushAsync(scope);

        Assert.False(summary.SkippedOffline);
        Assert.Equal(3, summary.ProcessedCount);
        Assert.Equal(3, summary.SucceededCount);
        Assert.Equal(0, summary.RemainingCount);
        Assert.Equal(
            [
                $"PATCH:/api/v1/notifications/{notifId:D}/read",
                $"PATCH:/api/v1/vehicles/{vehicleId:D}/mileage",
                "POST:/api/v1/notifications/read-all"
            ],
            executedOrder);
    }

    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity, "ODOMETER_CANNOT_DECREASE")]
    [InlineData(HttpStatusCode.Conflict, "DUPLICATE_SERVICE_REVIEW")]
    public async Task OfflineOutbox_DropsItemCleanlyOnPermanent409Or422Rejection(HttpStatusCode status, string errorCode)
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        var connectivity = new FakeConnectivityMonitor(isConnected: true);
        var apiClient = CreateMockApiClient((_, _) =>
            Task.FromResult(TestJsonResponses.Problem(status, errorCode, "Permanent business rule rejection.")));

        var outbox = new OfflineOutboxSyncService(db, apiClient, connectivity);
        var scope = "user:Customer:global";
        await outbox.EnqueueVehicleMileageUpdateAsync(scope, Guid.NewGuid(), 1000);

        var summary = await outbox.FlushAsync(scope);

        Assert.Equal(1, summary.ProcessedCount);
        Assert.Equal(0, summary.SucceededCount);
        Assert.Equal(1, summary.DroppedPermanentCount);
        Assert.Equal(0, summary.RemainingCount);
    }

    [Fact]
    public async Task OfflineOutbox_RetriesOnServer503WithExponentialBackoffCap()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        var connectivity = new FakeConnectivityMonitor(isConnected: true);
        var apiClient = CreateMockApiClient((_, _) =>
            Task.FromResult(TestJsonResponses.Problem(HttpStatusCode.ServiceUnavailable, "DATABASE_UNAVAILABLE", "Try again later.")));

        var outbox = new OfflineOutboxSyncService(db, apiClient, connectivity);
        var scope = "user:Customer:global";
        await outbox.EnqueueWarrantyClaimSubmitAsync(scope, new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = Guid.NewGuid(),
            DealerId = Guid.NewGuid(),
            OdometerAtClaimKm = 3200,
            DefectCategory = "Engine",
            SymptomDescription = "Intermittent knocking sound at cold idle."
        });

        var t0 = DateTimeOffset.UtcNow;
        var summary1 = await outbox.FlushAsync(scope, t0);
        Assert.Equal(1, summary1.RetriedCount);
        Assert.Equal(1, summary1.RemainingCount);

        // Immediately flushing 1s later skips because NextRetryAfterUtc is +5s away
        var summaryImmediate = await outbox.FlushAsync(scope, t0.AddSeconds(1));
        Assert.Equal(0, summaryImmediate.ProcessedCount);
        Assert.Equal(1, summaryImmediate.RemainingCount);

        // Exponential cap bounds check
        Assert.Equal(5, OfflineOutboxSyncService.ComputeExponentialBackoffSeconds(1));
        Assert.Equal(10, OfflineOutboxSyncService.ComputeExponentialBackoffSeconds(2));
        Assert.Equal(20, OfflineOutboxSyncService.ComputeExponentialBackoffSeconds(3));
        Assert.Equal(300, OfflineOutboxSyncService.ComputeExponentialBackoffSeconds(10));
    }

    private static IDealerMobileApiClient CreateMockApiClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var store = new InMemorySecureTokenStore();
        var endpoint = new DefaultApiEndpointProvider(store);
        var httpClient = new HttpClient(new DelegateHttpMessageHandler(handler));
        return new DealerMobileApiClient(new StubHttpClientFactory(_ => httpClient), endpoint);
    }
}
