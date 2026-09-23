using System.Net;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.DependencyInjection;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.Core.Validation;
using MotorcycleDealer.Mobile.Core.ViewModels;
using MotorcycleDealer.Mobile.Core.ViewModels.Customer;
using MotorcycleDealer.Mobile.Core.ViewModels.Public;
using MotorcycleDealer.Mobile.Core.ViewModels.Staff;
using MotorcycleDealer.Mobile.UnitTests.Fakes;
using Xunit;

namespace MotorcycleDealer.Mobile.UnitTests.Realtime;

public sealed class SignalRAndViewModelTests
{
    [Fact]
    public void SignalRReconnectPolicy_MatchesCanonicalExponentialSequence_0_2_5_10_30()
    {
        var policy = new SignalRReconnectPolicy();
        Assert.Equal(TimeSpan.Zero, policy.NextRetryDelay(new RetryContext { PreviousRetryCount = 0 }));
        Assert.Equal(TimeSpan.FromSeconds(2), policy.NextRetryDelay(new RetryContext { PreviousRetryCount = 1 }));
        Assert.Equal(TimeSpan.FromSeconds(5), policy.NextRetryDelay(new RetryContext { PreviousRetryCount = 2 }));
        Assert.Equal(TimeSpan.FromSeconds(10), policy.NextRetryDelay(new RetryContext { PreviousRetryCount = 3 }));
        Assert.Equal(TimeSpan.FromSeconds(30), policy.NextRetryDelay(new RetryContext { PreviousRetryCount = 4 }));
        Assert.Equal(TimeSpan.FromSeconds(30), policy.NextRetryDelay(new RetryContext { PreviousRetryCount = 9 }));
        Assert.Equal(10, SignalRHubEventNames.All.Count);
    }

    [Fact]
    public async Task SignalRMobileService_HandlesAll10Events_UpdatesWatermark_SessionAndChannels()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        var endpoint = new DefaultApiEndpointProvider(tokenStore);
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser());

        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var localNotif = new FakeLocalNotificationService();
        var dispatcher = new FakeMainThreadDispatcher();
        var recoveredWatermarkCaptured = string.Empty;

        var apiClient = CreateMockApiClient((req, _) =>
        {
            recoveredWatermarkCaptured = req.RequestUri!.Query;
            var recovered = new NotificationDto(
                Guid.NewGuid(), Guid.NewGuid(), session.UserId, null, null,
                SignalRHubEventNames.BookingStatusChanged, "Recovered", "Missed while asleep",
                null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            return Task.FromResult(TestJsonResponses.JsonOk(
                new NotificationFeedEnvelope(true, [recovered], 11, new PaginationMeta(1, 50, 1, 1, false, false), new NotificationFeedMeta(DateTimeOffset.UtcNow, "tr", 11)),
                MobileJsonSerializerContext.Default.NotificationFeedEnvelope));
        });

        await using var signalR = new SignalRMobileService(endpoint, tokenStore, session, apiClient, db, localNotif, dispatcher);

        var t = DateTimeOffset.UtcNow;
        await signalR.HandleIncomingBookingCreatedAsync(new(Guid.NewGuid(), SignalRHubEventNames.BookingCreated, Guid.NewGuid(), "BK-1", Guid.NewGuid(), "Nam", "RLHJC7920NY123456", "SH160i", new DateOnly(2026, 11, 25), new TimeOnly(9, 0), new TimeOnly(10, 0), "PeriodicMaintenance", false, "Pending", t.AddSeconds(1)));
        await signalR.HandleIncomingBookingStatusChangedAsync(new(Guid.NewGuid(), SignalRHubEventNames.BookingStatusChanged, Guid.NewGuid(), "BK-1", "Pending", "Confirmed", "Hanoi", t.AddSeconds(2)));
        await signalR.HandleIncomingMaintenanceReminderAsync(new(Guid.NewGuid(), SignalRHubEventNames.MaintenanceReminderTriggered, Guid.NewGuid(), Guid.NewGuid(), "RLHJC7920NY123456", "SH160i", "6,000 km", 5800, 6000, t.AddDays(3), "DEDUP-1", t.AddSeconds(3)));
        await signalR.HandleIncomingLoyaltyPointsUpdatedAsync(new(Guid.NewGuid(), SignalRHubEventNames.LoyaltyPointsUpdated, Guid.NewGuid(), "ServiceInvoiceEarn", 120, 1970, 2570, "ServiceInvoice", "INV-1", t.AddSeconds(4)));
        await signalR.HandleIncomingTierUpgradedAsync(new(Guid.NewGuid(), SignalRHubEventNames.TierUpgraded, Guid.NewGuid(), "Gold", "Platinum", 2.00m, 5100, t.AddSeconds(5)));
        await signalR.HandleIncomingTestDriveCreatedAsync(new(Guid.NewGuid(), SignalRHubEventNames.TestDriveCreated, Guid.NewGuid(), "TD-1", Guid.NewGuid(), "Hanoi", "Nam", "CB650R", new DateOnly(2026, 11, 26), new TimeOnly(10, 0), new TimeOnly(11, 0), "Requested", t.AddSeconds(6)));
        await signalR.HandleIncomingTestDriveStatusChangedAsync(new(Guid.NewGuid(), SignalRHubEventNames.TestDriveStatusChanged, Guid.NewGuid(), "TD-1", "Requested", "Confirmed", "Hanoi", "CB650R", "RLHJC7920NY123456", t.AddSeconds(7)));
        await signalR.HandleIncomingServiceReviewSubmittedAsync(new(Guid.NewGuid(), SignalRHubEventNames.ServiceReviewSubmitted, Guid.NewGuid(), "REV-1", Guid.NewGuid(), "INV-1", Guid.NewGuid(), "Hanoi", "Nam", "SH160i", 5, 25, t.AddSeconds(8)));
        await signalR.HandleIncomingWarrantyClaimCreatedAsync(new(Guid.NewGuid(), SignalRHubEventNames.WarrantyClaimCreated, Guid.NewGuid(), "WC-1", Guid.NewGuid(), "Hanoi", "Nam", "RLHJC7920NY123456", "SH160i", "Engine", 4200, "Submitted", t.AddSeconds(9)));
        await signalR.HandleIncomingWarrantyClaimStatusChangedAsync(new(Guid.NewGuid(), SignalRHubEventNames.WarrantyClaimStatusChanged, Guid.NewGuid(), "WC-1", "Submitted", "Approved", "Hanoi", "RLHJC7920NY123456", 1_200_000m, t.AddSeconds(10)));

        Assert.Equal(10, localNotif.SentNotifications.Count);
        Assert.Equal(10, signalR.UnreadCount);
        Assert.Equal(t.AddSeconds(10), signalR.LastEventWatermarkUtc);
        Assert.Equal("Platinum", session.TierCode);
        Assert.Equal(2.00m, session.TierMultiplier);
        Assert.Equal(1970, session.AvailablePoints);
        Assert.Contains(localNotif.SentNotifications, n => n.ChannelId == NotificationChannels.BookingAlerts);
        Assert.Contains(localNotif.SentNotifications, n => n.ChannelId == NotificationChannels.LoyaltyRewards);
        Assert.Contains(localNotif.SentNotifications, n => n.ChannelId == NotificationChannels.WarrantyUpdates);

        // Background pause + watermark catch-up call
        await signalR.OnAppBackgroundedAsync();
        var recoveredList = await signalR.SyncMissedNotificationsSinceWatermarkAsync();
        Assert.Single(recoveredList);
        Assert.Contains("sinceUtc=", recoveredWatermarkCaptured);
        Assert.Equal(11, signalR.UnreadCount);
    }

    [Fact]
    public async Task LoginRegisterViewModel_CustomerLogin_SavesTokensAndNavigatesToCustomerHome()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        var endpoint = new DefaultApiEndpointProvider(tokenStore, "http://127.0.0.1:1");
        var session = new MobileSessionState();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        var nav = new FakeNavigationService();
        var haptic = new FakeHapticService();
        var dispatcher = new FakeMainThreadDispatcher();

        var apiClient = CreateMockApiClient((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath == "/api/v1/auth/login")
            {
                var auth = new AuthTokenData("jwt-cus", "rt-cus", DateTimeOffset.UtcNow.AddMinutes(15), 900, TestDataFactory.CustomerUser());
                return Task.FromResult(TestJsonResponses.JsonOk(
                    new ApiEnvelope<AuthTokenData>(true, auth, TestDataFactory.Meta()),
                    MobileJsonSerializerContext.Default.ApiEnvelopeAuthTokenData));
            }

            return Task.FromResult(TestJsonResponses.JsonOk(
                new NotificationFeedEnvelope(true, [], 0, new PaginationMeta(1, 50, 0, 0, false, false), new NotificationFeedMeta(DateTimeOffset.UtcNow, "t", 0)),
                MobileJsonSerializerContext.Default.NotificationFeedEnvelope));
        });

        var logout = new LogoutCoordinator(tokenStore, new AuthRefreshClient(new StubHttpClientFactory(_ => new HttpClient()), endpoint), db, session);
        await using var signalR = new SignalRMobileService(endpoint, tokenStore, session, apiClient, db, new FakeLocalNotificationService(), dispatcher, logout);

        var vm = new LoginRegisterViewModel(
            apiClient, tokenStore, endpoint, session, signalR, logout, nav, haptic,
            new LoginInputValidator(), new RegisterInputValidator(), new WeakReferenceMessenger(), new FakeUserFeedbackService());
        vm.Email = "customer.gold@motorcycledealer.vn";
        vm.Password = "TestCredential#1";
        await vm.SubmitAsync();

        Assert.Equal("jwt-cus", await tokenStore.GetAccessTokenAsync());
        Assert.True(session.IsCustomer);
        Assert.Contains(nav.Navigations, n => n.Route == MobileRoutes.CustomerHome);
        Assert.Contains(HapticKind.Success, haptic.Performed);
    }

    [Fact]
    public async Task LoginRegisterViewModel_AdminLogin_RedirectsToAdminWebConsoleNotice()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        var endpoint = new DefaultApiEndpointProvider(tokenStore);
        var session = new MobileSessionState();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        var nav = new FakeNavigationService();
        var haptic = new FakeHapticService();

        var apiClient = CreateMockApiClient((_, _) =>
        {
            var auth = new AuthTokenData("jwt-adm", "rt-adm", DateTimeOffset.UtcNow.AddMinutes(15), 900, TestDataFactory.AdminUser());
            return Task.FromResult(TestJsonResponses.JsonOk(
                new ApiEnvelope<AuthTokenData>(true, auth, TestDataFactory.Meta()),
                MobileJsonSerializerContext.Default.ApiEnvelopeAuthTokenData));
        });

        var logout = new LogoutCoordinator(tokenStore, new AuthRefreshClient(new StubHttpClientFactory(_ => new HttpClient()), endpoint), db, session);
        await using var signalR = new SignalRMobileService(endpoint, tokenStore, session, apiClient, db, new FakeLocalNotificationService(), new FakeMainThreadDispatcher(), logout);

        var vm = new LoginRegisterViewModel(
            apiClient, tokenStore, endpoint, session, signalR, logout, nav, haptic,
            new LoginInputValidator(), new RegisterInputValidator(), new WeakReferenceMessenger(), new FakeUserFeedbackService());
        vm.Email = "admin@motorcycledealer.vn";
        vm.Password = "Admin@123!";
        await vm.SubmitAsync();

        Assert.True(session.IsAdminWebOnly);
        Assert.Single(nav.Navigations);
        Assert.Equal(MobileRoutes.AdminWebConsoleNotice, nav.Navigations[0].Route);
    }

    [Fact]
    public async Task BookingWizardViewModel_On409SlotConflict_SetsConflictStateAndReloadsLiveSlots()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser());
        var connectivity = new FakeConnectivityMonitor(true);
        var haptic = new FakeHapticService();

        var slotFetchCount = 0;
        var freshSlot = TestDataFactory.SampleSlot(remaining: 3, rv: "AAAAAAAAC09=");

        var apiClient = CreateMockApiClient((req, _) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/api/v1/bookings")
            {
                return Task.FromResult(TestJsonResponses.Problem(
                    HttpStatusCode.Conflict,
                    "SLOT_CAPACITY_CONFLICT",
                    "The selected service bay slot was just filled by another booking."));
            }

            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/api/v1/time-slots")
            {
                slotFetchCount++;
                return Task.FromResult(TestJsonResponses.JsonOk(
                    new ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>(true, [freshSlot], TestDataFactory.Meta()),
                    MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListServiceTimeSlotDto));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var outbox = new OfflineOutboxSyncService(db, apiClient, connectivity);
        using var vm = new BookingWizardViewModel(
            apiClient, db, session, connectivity, outbox, new FakeShareService(), haptic, new FakeMainThreadDispatcher(),
            new WeakReferenceMessenger(), new FakeUserFeedbackService())
        {
            SelectedDealer = TestDataFactory.SampleDealer(),
            SelectedVehicle = TestDataFactory.SampleVehicle(),
            SelectedSlot = TestDataFactory.SampleSlot(remaining: 1, rv: "STALE_RV=="),
            CurrentStep = 3
        };

        await vm.ConfirmBookingAsync();

        Assert.True(vm.HasSlotConflict);
        Assert.Equal(2, vm.CurrentStep);
        Assert.Equal(1, slotFetchCount);
        Assert.Single(vm.AvailableSlots);
        Assert.Equal("AAAAAAAAC09=", vm.SelectedSlot?.RowVersion);
        Assert.Contains(HapticKind.Warning, haptic.Performed);
    }

    [Fact]
    public async Task LoyaltyPassbookViewModel_WhenOffline_FallsBackToEncryptedSqlCipherAndGeneratesQrPayload()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        await db.InitializeAsync();

        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser());
        var voucher = TestDataFactory.SampleVoucher(code: "OFFLINE-QR-99");
        await db.SaveLoyaltyWalletAsync(session.OwnerScopeKey, TestDataFactory.SampleWallet("Gold", 1850, 2450));
        await db.SaveCustomerVouchersAsync(session.OwnerScopeKey, [voucher]);

        // Simulate complete network failure
        var apiClient = CreateMockApiClient((_, _) => throw new HttpRequestException("Device is offline"));
        using var vm = new LoyaltyPassbookViewModel(
            apiClient, db, session, new FakeConnectivityMonitor(false), new FakeShareService(), new FakeHapticService(), new FakeMainThreadDispatcher(),
            new WeakReferenceMessenger(), new FakeUserFeedbackService());

        await vm.LoadPassbookAsync();

        Assert.True(vm.IsOfflineBannerVisible);
        Assert.NotNull(vm.Wallet);
        Assert.Equal("Gold", vm.TierCode);
        Assert.Equal("1.850 pts", vm.FormattedAvailablePoints);
        Assert.Single(vm.MyVouchers);
        Assert.StartsWith("dealer://voucher/v1?code=OFFLINE-QR-99", vm.SelectedVoucherQrPayload);
    }

    [Fact]
    public async Task StaffWorkbenchViewModel_SwipeCheckInAndScannedVinIntake_WorkEndToEnd()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        await using var db = new MobileOfflineDb(tempPath, tokenStore);
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.StaffUser());

        var initialBooking = TestDataFactory.SampleBooking(status: "Confirmed");
        var checkedInBooking = initialBooking with { Status = "CheckedIn", RowVersion = "AAAAAAAAD02=" };
        var vehicle = TestDataFactory.SampleVehicle(vin: initialBooking.Vin);

        var apiClient = CreateMockApiClient((req, _) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/api/v1/bookings/dealer")
            {
                return Task.FromResult(TestJsonResponses.JsonOk(
                    new PagedApiEnvelope<BookingDto>(true, [initialBooking], new PaginationMeta(1, 50, 1, 1, false, false), TestDataFactory.Meta()),
                    MobileJsonSerializerContext.Default.PagedApiEnvelopeBookingDto));
            }

            if (req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath.EndsWith("/status", StringComparison.Ordinal))
            {
                return Task.FromResult(TestJsonResponses.JsonOk(
                    new ApiEnvelope<BookingDto>(true, checkedInBooking, TestDataFactory.Meta()),
                    MobileJsonSerializerContext.Default.ApiEnvelopeBookingDto));
            }

            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/api/v1/vehicles/lookup")
            {
                return Task.FromResult(TestJsonResponses.JsonOk(
                    new ApiEnvelope<IReadOnlyList<CustomerVehicleDto>>(true, [vehicle], TestDataFactory.Meta()),
                    MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListCustomerVehicleDto));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        await using var signalR = new SignalRMobileService(
            new DefaultApiEndpointProvider(tokenStore), tokenStore, session, apiClient, db, new FakeLocalNotificationService(), new FakeMainThreadDispatcher());

        using var vm = new StaffWorkbenchViewModel(
            apiClient, db, session, signalR, new FakeNavigationService(), new FakeHapticService(), new FakeMainThreadDispatcher(),
            new WeakReferenceMessenger(), new FakeUserFeedbackService());

        await vm.LoadWorkbenchAsync();
        Assert.Single(vm.FilteredBookings);

        await vm.SwipeCheckInBookingAsync(vm.FilteredBookings[0]);
        Assert.Equal("CheckedIn", vm.SelectedBooking?.Status);
        Assert.Equal(1, vm.CheckedInOrInProgressCount);

        await vm.LookupByScannedCodeAsync(initialBooking.Vin);
        Assert.NotNull(vm.ScannedIntakeVehicle);
        Assert.Equal(initialBooking.Vin, vm.ScannedIntakeVehicle.Vin);
    }

    [Fact]
    public void MobileInvoiceBuilderViewModel_PartsPlusVoucherQr_ComputesLiveTwoStepFloorPreviewAndTierUpgrade()
    {
        using var tempPath = new TempDatabasePathProvider();
        var tokenStore = new InMemorySecureTokenStore();
        using var dbHolder = tempPath;
        var db = new MobileOfflineDb(tempPath, tokenStore);
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.StaffUser());

        var apiClient = CreateMockApiClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var vm = new MobileInvoiceBuilderViewModel(
            apiClient, db, session, new FakeNavigationService(), new FakeShareService(), new FakeHapticService(), new FakeMainThreadDispatcher(),
            new WeakReferenceMessenger(), new FakeUserFeedbackService())
        {
            CustomerTierCode = "Silver",
            CustomerTierMultiplier = 1.20m,
            CustomerCurrentAvailablePoints = 500,
            CustomerCurrentQualifyingPoints = 1950, // Only 50 pts away from Gold (2000)!
            BaseLaborAmountVnd = 350_000m
        };

        var oilPart = new SparePartDto(
            Guid.NewGuid(), Guid.NewGuid(), "OEM-OIL-1L", "Honda Synthetic Oil 1L", "EngineOil",
            null, null, null, true, "Bottle", 249_999m, true, DateTimeOffset.UtcNow, null);
        var filterPart = new SparePartDto(
            Guid.NewGuid(), Guid.NewGuid(), "OEM-FLT-AIR", "Air Filter", "Filter",
            null, null, null, true, "Piece", 150_000m, true, DateTimeOffset.UtcNow, null);

        vm.AddSparePart(oilPart);
        vm.AddSparePart(filterPart);
        // Gross = 350,000 labor + 399,999 parts = 749,999 VND

        var voucher = TestDataFactory.SampleVoucher(code: "SVC150-QR"); // 150,000 VND FixedAmount, min 500,000 VND
        vm.EligibleCustomerVouchers.Add(voucher);

        var qr = DealerQrPayloadParser.BuildVoucherQrPayload(voucher.VoucherCode, voucher.Id, voucher.RowVersion);
        Assert.True(vm.ApplyScannedVoucherQr(qr));

        // Net = 749,999 - 150,000 = 599,999 VND
        // Two-step floor: Floor(Floor(599,999 / 10,000) * 1.20) = Floor(59 * 1.20) = Floor(70.8) = 70 pts!
        // Qualifying after = 1950 + 70 = 2020 >= 2000 -> Tier upgrade to Gold!
        Assert.Equal(749_999m, vm.GrossAmountVnd);
        Assert.Equal(150_000m, vm.VoucherDiscountVnd);
        Assert.Equal(599_999m, vm.NetPaidAmountVnd);
        Assert.Equal(59, vm.PreviewBasePoints);
        Assert.Equal(70, vm.PreviewPointsEarned);
        Assert.Equal("Gold", vm.PreviewResultingTierCode);
        Assert.True(vm.PreviewTierUpgraded);
    }

    [Fact]
    public async Task ServiceCollectionExtensions_ResolvesAll17ViewModelsAndCoreServicesCleanly()
    {
        using var tempPath = new TempDatabasePathProvider();
        var services = new ServiceCollection();
        services.AddSingleton<ISecureTokenStore>(new InMemorySecureTokenStore());
        services.AddSingleton<IDatabasePathProvider>(tempPath);
        services.AddSingleton<IMainThreadDispatcher>(new FakeMainThreadDispatcher());
        services.AddSingleton<ILocalNotificationService>(new FakeLocalNotificationService());
        services.AddSingleton<IConnectivityMonitor>(new FakeConnectivityMonitor());
        services.AddSingleton<INavigationService>(new FakeNavigationService());
        services.AddSingleton<IHapticService>(new FakeHapticService());
        services.AddSingleton<IShareService>(new FakeShareService());

        services.AddMotorcycleDealerMobileCore();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        Assert.NotNull(provider.GetRequiredService<AppShellViewModel>());
        Assert.NotNull(provider.GetRequiredService<NotificationsFeedViewModel>());
        Assert.NotNull(provider.GetRequiredService<ShowroomCatalogViewModel>());
        Assert.NotNull(provider.GetRequiredService<DealerLocatorViewModel>());
        Assert.NotNull(provider.GetRequiredService<LoginRegisterViewModel>());
        Assert.NotNull(provider.GetRequiredService<AdminWebConsoleNoticeViewModel>());
        Assert.NotNull(provider.GetRequiredService<CustomerHomeViewModel>());
        Assert.NotNull(provider.GetRequiredService<VinGarageViewModel>());
        Assert.NotNull(provider.GetRequiredService<BookingWizardViewModel>());
        Assert.NotNull(provider.GetRequiredService<LoyaltyPassbookViewModel>());
        Assert.NotNull(provider.GetRequiredService<CustomerWarrantyViewModel>());
        Assert.NotNull(provider.GetRequiredService<StaffWorkbenchViewModel>());
        Assert.NotNull(provider.GetRequiredService<MobileInvoiceBuilderViewModel>());
        Assert.NotNull(provider.GetRequiredService<ShowroomVinSalesViewModel>());
        Assert.NotNull(provider.GetRequiredService<StaffTestDriveViewModel>());
        Assert.NotNull(provider.GetRequiredService<StaffWarrantyCsatViewModel>());
        Assert.NotNull(provider.GetRequiredService<SlotCapacityViewModel>());
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
