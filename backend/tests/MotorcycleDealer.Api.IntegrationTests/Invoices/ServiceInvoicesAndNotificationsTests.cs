using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.Notifications;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using MotorcycleDealer.Infrastructure.Services.Invoices;
using MotorcycleDealer.Infrastructure.Services.Notifications;
using MotorcycleDealer.WebApi.Hubs;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.Invoices;

public sealed class RecordingInvoiceNotificationPublisher : INotificationPublisher
{
    private readonly SignalRNotificationPublisher _inner;

    public RecordingInvoiceNotificationPublisher(SignalRNotificationPublisher inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public ConcurrentQueue<(BookingCreatedRealtimeEvent Payload, Guid TargetDealerId)> BookingCreatedEvents { get; } = new();
    public ConcurrentQueue<(BookingStatusChangedRealtimeEvent Payload, Guid CustomerId, Guid DealerId)> BookingStatusEvents { get; } = new();
    public ConcurrentQueue<(LoyaltyPointsUpdatedRealtimeEvent Payload, Guid CustomerId)> LoyaltyPointsEvents { get; } = new();
    public ConcurrentQueue<(TierUpgradedRealtimeEvent Payload, Guid CustomerId)> TierUpgradedEvents { get; } = new();

    public async Task PublishBookingCreatedAsync(
        BookingCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default)
    {
        await _inner.PublishBookingCreatedAsync(payload, targetDealerId, cancellationToken);
        BookingCreatedEvents.Enqueue((payload, targetDealerId));
    }

    public async Task PublishBookingStatusChangedAsync(
        BookingStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        await _inner.PublishBookingStatusChangedAsync(payload, customerId, dealerId, cancellationToken);
        BookingStatusEvents.Enqueue((payload, customerId, dealerId));
    }

    public Task PublishMaintenanceReminderAsync(
        MaintenanceReminderRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        _inner.PublishMaintenanceReminderAsync(payload, customerId, cancellationToken);

    public async Task PublishLoyaltyPointsUpdatedAsync(
        LoyaltyPointsUpdatedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await _inner.PublishLoyaltyPointsUpdatedAsync(payload, customerId, cancellationToken);
        LoyaltyPointsEvents.Enqueue((payload, customerId));
    }

    public async Task PublishTierUpgradedAsync(
        TierUpgradedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await _inner.PublishTierUpgradedAsync(payload, customerId, cancellationToken);
        TierUpgradedEvents.Enqueue((payload, customerId));
    }

    public Task PublishTestDriveCreatedAsync(
        TestDriveCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default) =>
        _inner.PublishTestDriveCreatedAsync(payload, targetDealerId, cancellationToken);

    public Task PublishTestDriveStatusChangedAsync(
        TestDriveStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default) =>
        _inner.PublishTestDriveStatusChangedAsync(payload, customerId, dealerId, cancellationToken);
}

public sealed class LocalDbInvoicesAndNotificationsWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbInvoicesAndNotificationsWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_Forensic_InvNotif_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={TestDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public RecordingInvoiceNotificationPublisher NotificationSpy =>
        Services.GetRequiredService<RecordingInvoiceNotificationPublisher>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Database:AutoMigrateAndSeed"] = "true",
                ["MaintenanceReminderWorker:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
                ["Cors:AllowedOrigins:1"] = "http://127.0.0.1:4200"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MotorcycleDealerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<MotorcycleDealerDbContext>>();
            services.RemoveAll<MotorcycleDealerDbContext>();

            services.AddDbContext<MotorcycleDealerDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(ConnectionString, sqlOptions =>
                {
                    sqlOptions.UseCompatibilityLevel(DependencyInjection.SqlServerCompatibilityLevel);
                    sqlOptions.MigrationsAssembly(typeof(MotorcycleDealerDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });

                options.ConfigureWarnings(warnings => warnings.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning,
                    SqlServerEventId.SavepointsDisabledBecauseOfMARS));

                options.AddInterceptors(
                    serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                    serviceProvider.GetRequiredService<AppendOnlyLedgerInterceptor>());
            });

            services.TryAddScoped<IServiceInvoiceService, ServiceInvoiceService>();
            services.TryAddScoped<INotificationInboxService, NotificationInboxService>();

            services.RemoveAll<INotificationPublisher>();
            services.AddSingleton<SignalRNotificationPublisher>();
            services.AddSingleton<RecordingInvoiceNotificationPublisher>();
            services.AddScoped<INotificationPublisher>(sp => sp.GetRequiredService<RecordingInvoiceNotificationPublisher>());
        });
    }

    public Task InitializeAsync()
    {
        using var client = CreateClient();
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        SqlConnection.ClearAllPools();
        await LocalDbTestDatabaseCleaner.DropFixtureAndOrphanTestDatabasesAsync(TestDatabaseName);
    }

    public MotorcycleDealerDbContext CreateScopedDbContext(ICurrentUserContext? currentUser = null)
    {
        var dateTimeProvider = Services.GetRequiredService<IDateTimeProvider>();
        var options = new DbContextOptionsBuilder<MotorcycleDealerDbContext>()
            .UseSqlServer(ConnectionString, sql =>
            {
                sql.UseCompatibilityLevel(DependencyInjection.SqlServerCompatibilityLevel);
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            })
            .AddInterceptors(
                new AuditableEntityInterceptor(dateTimeProvider),
                new AppendOnlyLedgerInterceptor())
            .Options;

        return new MotorcycleDealerDbContext(options, currentUser);
    }
}

public sealed class ServiceInvoicesAndNotificationsTests : IClassFixture<LocalDbInvoicesAndNotificationsWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid SilverVehicleId = Guid.Parse("99999999-9999-9999-9999-999999999901");
    private static readonly Guid SilverReadyVoucherId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private static readonly Guid SilverReminderId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeee01");
    private static readonly Guid GoldVehicleCb300Id = Guid.Parse("99999999-9999-9999-9999-999999999905");
    private static readonly Guid GoldPercentageVoucherId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03");
    private static readonly Guid SeededHcmInvoiceId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01");
    private static readonly Guid SeededHnInvoiceId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd02");
    private static readonly Guid SeededHcmDealerNotifId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffff02");

    private readonly LocalDbInvoicesAndNotificationsWebAppFixture _fixture;

    public ServiceInvoicesAndNotificationsTests(LocalDbInvoicesAndNotificationsWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ClosedLoopWorkshopSettlement_ShouldConsumeReservedVoucher_RollForwardOdometer_AppendLedger_PromoteSilverToGold_AndRejectDuplicateInvoice()
    {
        using var silverClient = _fixture.CreateClient();
        using var hcmStaffClient = _fixture.CreateClient();
        using var hnStaffClient = _fixture.CreateClient();

        var silverAuth = await LoginAsync(silverClient, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var hcmStaffAuth = await LoginAsync(hcmStaffClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var hnStaffAuth = await LoginAsync(hnStaffClient, "staff.hn@motorcycledealer.vn", "Staff@123!");

        silverClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", silverAuth.AccessToken);
        hcmStaffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hcmStaffAuth.AccessToken);
        hnStaffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hnStaffAuth.AccessToken);

        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var openSlot = await CreateStaffTimeSlotAsync(hcmStaffClient, slotDate, "09:00:00", "10:00:00");

        // 1. Customer books service at DLR-HCM-01 reserving SVC100K-7K9P2M4X (Available -> ReservedOnBooking)
        BookingDto createdBooking;
        using (var createBookingRes = await silverClient.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            ServiceTimeSlotId = openSlot.SlotId,
            SlotRowVersion = openSlot.RowVersion,
            CustomerVehicleId = SilverVehicleId,
            AppliedCustomerVoucherId = SilverReadyVoucherId,
            ServiceType = "PeriodicMaintenance",
            OdometerAtBookingKm = 3_900,
            ReportedNotes = "4,000 km periodic service with reserved 100k voucher"
        }))
        {
            Assert.Equal(HttpStatusCode.Created, createBookingRes.StatusCode);
            var env = await createBookingRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts);
            Assert.NotNull(env);
            createdBooking = env.Data;
            Assert.Equal("Pending", createdBooking.Status);
            Assert.Equal(SilverReadyVoucherId, createdBooking.AppliedCustomerVoucherId);
            Assert.Equal("SVC100K-7K9P2M4X", createdBooking.ReservedVoucherCode);
        }

        // 2. Reject invoice issuance while booking is still Pending -> 422 INVALID_BOOKING_STATUS
        using (var pendingInvRes = await hcmStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = createdBooking.BookingId,
            LaborAmount = 220_000m,
            PartsAmount = 480_000m,
            OdometerAtCompletionKm = 4_020,
            PaymentMethod = "BankTransfer"
        }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, pendingInvRes.StatusCode);
            Assert.Equal("INVALID_BOOKING_STATUS", await ReadErrorCodeAsync(pendingInvRes));
        }

        // 3. Cross-dealer staff (Hanoi) cannot settle Saigon booking -> 403 FORBIDDEN_DEALER_SCOPE
        BookingDto confirmedBooking;
        using (var confirmRes = await hcmStaffClient.PatchAsJsonAsync(
            $"/api/v1/bookings/{createdBooking.BookingId}/status",
            new UpdateBookingStatusRequest
            {
                NewStatus = "Confirmed",
                RowVersion = createdBooking.RowVersion
            }))
        {
            Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);
            var env = await confirmRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts);
            Assert.NotNull(env);
            confirmedBooking = env.Data;
            Assert.Equal("Confirmed", confirmedBooking.Status);
        }

        using (var crossDealerPostRes = await hnStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            LaborAmount = 220_000m,
            PartsAmount = 480_000m,
            OdometerAtCompletionKm = 4_020,
            PaymentMethod = "BankTransfer"
        }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, crossDealerPostRes.StatusCode);
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", await ReadErrorCodeAsync(crossDealerPostRes));
        }

        // 4. Reject odometer regression (< 3,900 km) -> 422 ODOMETER_REGRESSION
        using (var regressionRes = await hcmStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            LaborAmount = 220_000m,
            PartsAmount = 480_000m,
            OdometerAtCompletionKm = 3_880,
            PaymentMethod = "BankTransfer"
        }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, regressionRes.StatusCode);
            Assert.Equal("ODOMETER_REGRESSION", await ReadErrorCodeAsync(regressionRes));
        }

        // 5. Reject gross amount below SVC100K-7K9P2M4X MinOrderAmount (500,000 VND) -> 422 VOUCHER_INELIGIBLE
        using (var lowGrossRes = await hcmStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            LaborAmount = 150_000m,
            PartsAmount = 200_000m,
            OdometerAtCompletionKm = 4_020,
            PaymentMethod = "BankTransfer"
        }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, lowGrossRes.StatusCode);
            Assert.Equal("VOUCHER_INELIGIBLE", await ReadErrorCodeAsync(lowGrossRes));
        }

        // 6. Settle workshop invoice: Gross 700k - 100k voucher = 600k Net -> +72 pts @ 1.20x -> Lifetime 1,960 + 72 = 2,032 -> Silver -> Gold!
        ServiceInvoiceDto settledInvoice;
        using (var settleRes = await hcmStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            BookingRowVersion = confirmedBooking.RowVersion,
            LaborAmount = 220_000m,
            PartsAmount = 480_000m,
            OdometerAtCompletionKm = 4_020,
            PaymentMethod = "EWallet",
            WorkSummaryNotes = "Completed 4,000 km periodic synthetic oil, air filter, and CVT belt inspection.",
            LineItemsJson = "[{\"description\":\"Synthetic Oil & Filter\",\"amount\":480000},{\"description\":\"4000km Labor\",\"amount\":220000}]"
        }))
        {
            Assert.Equal(HttpStatusCode.Created, settleRes.StatusCode);
            var env = await settleRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts);
            Assert.NotNull(env);
            settledInvoice = env.Data;

            Assert.StartsWith("INV-", settledInvoice.InvoiceNumber, StringComparison.Ordinal);
            Assert.Equal(700_000m, settledInvoice.GrossAmount);
            Assert.Equal(100_000m, settledInvoice.VoucherDiscountAmount);
            Assert.Equal(600_000m, settledInvoice.NetPaidAmount);
            Assert.Equal(SilverReadyVoucherId, settledInvoice.AppliedCustomerVoucherId);
            Assert.Equal("SVC100K-7K9P2M4X", settledInvoice.AppliedVoucherCode);
            Assert.Equal(4_020, settledInvoice.OdometerAtCompletionKm);
            Assert.Equal(8_000, settledInvoice.NextDueMileageKm);
            Assert.Equal("Silver", settledInvoice.AppliedTierCode);
            Assert.Equal(1.20m, settledInvoice.AppliedMultiplier);
            Assert.Equal(72, settledInvoice.PointsEarned);
            Assert.Equal(721, settledInvoice.AvailablePointsAfter);
            Assert.Equal(2_032, settledInvoice.LifetimeQualifyingPointsAfter);
            Assert.Equal("Gold", settledInvoice.CurrentTierAfter);
            Assert.Equal("Gold", settledInvoice.NewTierCode);
            Assert.True(settledInvoice.TierUpgraded);
        }

        // 7. Verify persisted SQL Server state, append-only ledger reconciliation, and post-commit SignalR events
        await using (var db = _fixture.CreateScopedDbContext())
        {
            var bookingInDb = await db.ServiceBookings.AsNoTracking().SingleAsync(b => b.Id == confirmedBooking.BookingId);
            Assert.Equal("Completed", bookingInDb.Status);
            Assert.NotNull(bookingInDb.CompletedAtUtc);

            var voucherInDb = await db.CustomerVouchers.AsNoTracking().SingleAsync(v => v.Id == SilverReadyVoucherId);
            Assert.Equal("Used", voucherInDb.Status);
            Assert.NotNull(voucherInDb.UsedAtUtc);

            var vehicleInDb = await db.CustomerVehicles.AsNoTracking().SingleAsync(v => v.Id == SilverVehicleId);
            Assert.Equal(4_020, vehicleInDb.CurrentMileageKm);
            Assert.Equal(4_020, vehicleInDb.LastServiceMileageKm);
            Assert.NotNull(vehicleInDb.LastServiceDateUtc);
            Assert.Equal(8_000, vehicleInDb.NextDueMileageKm);

            var reminderInDb = await db.MaintenanceReminders.AsNoTracking().SingleAsync(r => r.Id == SilverReminderId);
            Assert.Equal("Completed", reminderInDb.Status);
            Assert.Equal(confirmedBooking.BookingId, reminderInDb.ResolvedBookingId);

            var expectedIdempotencyKey = $"SERVICE_INVOICE:{settledInvoice.InvoiceId}:EARN";
            var ledgerRow = await db.PointLedgerEntries.AsNoTracking()
                .SingleOrDefaultAsync(p => p.IdempotencyKey == expectedIdempotencyKey);
            Assert.NotNull(ledgerRow);
            Assert.Equal("ServiceInvoiceEarn", ledgerRow.TransactionType);
            Assert.Equal("ServiceInvoice", ledgerRow.ReferenceType);
            Assert.Equal(72, ledgerRow.PointsDelta);
            Assert.Equal(721, ledgerRow.BalanceAfter);

            var accountInDb = await db.LoyaltyAccounts.AsNoTracking()
                .Include(a => a.CurrentTier)
                .SingleAsync(a => a.CustomerId == silverAuth.User.UserId);
            Assert.Equal(721, accountInDb.AvailablePoints);
            Assert.Equal(2_032, accountInDb.LifetimeQualifyingPoints);
            Assert.Equal("Gold", accountInDb.CurrentTier!.TierCode);

            var computedLedgerSum = await db.PointLedgerEntries.AsNoTracking()
                .Where(p => p.LoyaltyAccountId == accountInDb.Id)
                .SumAsync(p => p.PointsDelta);
            Assert.Equal(accountInDb.AvailablePoints, computedLedgerSum);
        }

        Assert.Contains(
            _fixture.NotificationSpy.BookingStatusEvents,
            e => e.Payload.BookingId == confirmedBooking.BookingId && e.Payload.NewStatus == "Completed");
        Assert.Contains(
            _fixture.NotificationSpy.LoyaltyPointsEvents,
            e => e.CustomerId == silverAuth.User.UserId &&
                 e.Payload.ReferenceNumber == settledInvoice.InvoiceNumber &&
                 e.Payload.PointsDelta == 72);
        Assert.Contains(
            _fixture.NotificationSpy.TierUpgradedEvents,
            e => e.CustomerId == silverAuth.User.UserId &&
                 e.Payload.PreviousTierCode == "Silver" &&
                 e.Payload.NewTierCode == "Gold" &&
                 e.Payload.NewPointsMultiplier == 1.50m);

        // 8. Duplicate invoice submission on the same ServiceBookingId MUST return 409 INVOICE_ALREADY_ISSUED
        using (var duplicateRes = await hcmStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            LaborAmount = 220_000m,
            PartsAmount = 480_000m,
            OdometerAtCompletionKm = 4_020,
            PaymentMethod = "EWallet"
        }))
        {
            Assert.Equal(HttpStatusCode.Conflict, duplicateRes.StatusCode);
            Assert.Equal("INVOICE_ALREADY_ISSUED", await ReadErrorCodeAsync(duplicateRes));
        }

        // 9. Customer can query settled invoice by ?bookingId= and GET /api/v1/service-invoices/{id}
        using (var byBookingRes = await silverClient.GetAsync($"/api/v1/service-invoices?bookingId={confirmedBooking.BookingId}"))
        {
            Assert.Equal(HttpStatusCode.OK, byBookingRes.StatusCode);
            var pageEnv = await byBookingRes.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<ServiceInvoiceDto>>(JsonOpts);
            Assert.NotNull(pageEnv);
            var single = Assert.Single(pageEnv.Data);
            Assert.Equal(settledInvoice.InvoiceId, single.InvoiceId);
            Assert.Equal("Gold", single.NewTierCode);
            Assert.True(single.TierUpgraded);
        }
    }

    [Fact]
    public async Task ReservedVoucherReleaseAndPercentageCap_WhenRemovedOnFirstInvoice_ShouldRestoreAvailableAndCapSecondInvoiceDiscount()
    {
        using var goldClient = _fixture.CreateClient();
        using var hnStaffClient = _fixture.CreateClient();

        var goldAuth = await LoginAsync(goldClient, "customer.gold@motorcycledealer.vn", "Customer@123!");
        var hnStaffAuth = await LoginAsync(hnStaffClient, "staff.hn@motorcycledealer.vn", "Staff@123!");

        goldClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", goldAuth.AccessToken);
        hnStaffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hnStaffAuth.AccessToken);

        var date1 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(31));
        var slot1 = await CreateStaffTimeSlotAsync(hnStaffClient, date1, "10:00:00", "11:00:00");

        // Book reserving SVC15PCT-4R8N2W9Q (Available -> ReservedOnBooking)
        BookingDto booking1;
        using (var b1Res = await goldClient.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
        {
            DealerId = DatabaseSeeder.HanoiDealerId,
            ServiceTimeSlotId = slot1.SlotId,
            CustomerVehicleId = GoldVehicleCb300Id,
            AppliedCustomerVoucherId = GoldPercentageVoucherId,
            ServiceType = "Inspection",
            OdometerAtBookingKm = 6_450
        }))
        {
            Assert.Equal(HttpStatusCode.Created, b1Res.StatusCode);
            booking1 = (await b1Res.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
        }

        using (var c1Res = await hnStaffClient.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking1.BookingId}/status",
            new UpdateBookingStatusRequest { NewStatus = "CheckedIn" }))
        {
            Assert.Equal(HttpStatusCode.OK, c1Res.StatusCode);
        }

        // Settle booking1 with RemoveReservedVoucher = true -> voucher returns to Available!
        using (var inv1Res = await hnStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = booking1.BookingId,
            LaborAmount = 150_000m,
            PartsAmount = 150_000m,
            OdometerAtCompletionKm = 6_460,
            RemoveReservedVoucher = true,
            PaymentMethod = "Cash"
        }))
        {
            Assert.Equal(HttpStatusCode.Created, inv1Res.StatusCode);
            var inv1 = (await inv1Res.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!.Data;
            Assert.Equal(0m, inv1.VoucherDiscountAmount);
            Assert.Null(inv1.AppliedCustomerVoucherId);
        }

        await using (var db = _fixture.CreateScopedDbContext())
        {
            var releasedVoucher = await db.CustomerVouchers.AsNoTracking().SingleAsync(v => v.Id == GoldPercentageVoucherId);
            Assert.Equal("Available", releasedVoucher.Status);
        }

        // Now apply the restored Available percentage voucher (15% capped at 200,000 VND) directly on a 2,000,000 VND invoice
        var slot2 = await CreateStaffTimeSlotAsync(hnStaffClient, date1, "14:00:00", "15:00:00");
        BookingDto booking2;
        using (var b2Res = await goldClient.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
        {
            DealerId = DatabaseSeeder.HanoiDealerId,
            ServiceTimeSlotId = slot2.SlotId,
            CustomerVehicleId = GoldVehicleCb300Id,
            ServiceType = "PeriodicMaintenance",
            OdometerAtBookingKm = 6_500
        }))
        {
            Assert.Equal(HttpStatusCode.Created, b2Res.StatusCode);
            booking2 = (await b2Res.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
        }

        using (var c2CheckInRes = await hnStaffClient.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking2.BookingId}/status",
            new UpdateBookingStatusRequest { NewStatus = "CheckedIn" }))
        {
            Assert.Equal(HttpStatusCode.OK, c2CheckInRes.StatusCode);
        }

        using (var c2Res = await hnStaffClient.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking2.BookingId}/status",
            new UpdateBookingStatusRequest { NewStatus = "InProgress" }))
        {
            Assert.Equal(HttpStatusCode.OK, c2Res.StatusCode);
        }

        using (var inv2Res = await hnStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = booking2.BookingId,
            AppliedCustomerVoucherId = GoldPercentageVoucherId,
            LaborAmount = 500_000m,
            PartsAmount = 1_500_000m,
            OdometerAtCompletionKm = 6_520,
            PaymentMethod = "Card"
        }))
        {
            Assert.Equal(HttpStatusCode.Created, inv2Res.StatusCode);
            var inv2 = (await inv2Res.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!.Data;
            Assert.Equal(2_000_000m, inv2.GrossAmount);
            Assert.Equal(200_000m, inv2.VoucherDiscountAmount);
            Assert.Equal(1_800_000m, inv2.NetPaidAmount);
            Assert.Equal(270, inv2.PointsEarned);
        }
    }

    [Fact]
    public async Task InvoiceAndNotificationTenancyIsolation_AndMarkReadLifecycle_ShouldEnforceStrictRoleAndDealerBoundaries()
    {
        using var bronzeClient = _fixture.CreateClient();
        using var silverClient = _fixture.CreateClient();
        using var hcmStaffClient = _fixture.CreateClient();
        using var hnStaffClient = _fixture.CreateClient();
        using var adminClient = _fixture.CreateClient();

        var bronzeAuth = await LoginAsync(bronzeClient, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var silverAuth = await LoginAsync(silverClient, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var hcmStaffAuth = await LoginAsync(hcmStaffClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var hnStaffAuth = await LoginAsync(hnStaffClient, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var adminAuth = await LoginAsync(adminClient, "admin@motorcycledealer.vn", "Admin@123!");

        bronzeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bronzeAuth.AccessToken);
        silverClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", silverAuth.AccessToken);
        hcmStaffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hcmStaffAuth.AccessToken);
        hnStaffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hnStaffAuth.AccessToken);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        // 1. Invoice RBAC & Cross-Dealer / Cross-Customer Isolation
        using (var hnListRes = await hnStaffClient.GetAsync("/api/v1/service-invoices"))
        {
            Assert.Equal(HttpStatusCode.OK, hnListRes.StatusCode);
            var hnPage = (await hnListRes.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!;
            Assert.NotEmpty(hnPage.Data);
            Assert.All(hnPage.Data, inv => Assert.Equal(DatabaseSeeder.HanoiDealerId, inv.DealerId));
            Assert.DoesNotContain(hnPage.Data, inv => inv.Id == SeededHcmInvoiceId);
        }

        using (var hnCrossFilterRes = await hnStaffClient.GetAsync($"/api/v1/service-invoices?dealerId={DatabaseSeeder.HcmDealerId}"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, hnCrossFilterRes.StatusCode);
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", await ReadErrorCodeAsync(hnCrossFilterRes));
        }

        using (var hnCrossDetailRes = await hnStaffClient.GetAsync($"/api/v1/service-invoices/{SeededHcmInvoiceId}"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, hnCrossDetailRes.StatusCode);
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", await ReadErrorCodeAsync(hnCrossDetailRes));
        }

        using (var bronzeCrossDetailRes = await bronzeClient.GetAsync($"/api/v1/service-invoices/{SeededHcmInvoiceId}"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, bronzeCrossDetailRes.StatusCode);
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", await ReadErrorCodeAsync(bronzeCrossDetailRes));
        }

        using (var adminAllRes = await adminClient.GetAsync($"/api/v1/service-invoices?dealerId={DatabaseSeeder.HanoiDealerId}"))
        {
            Assert.Equal(HttpStatusCode.OK, adminAllRes.StatusCode);
            var adminPage = (await adminAllRes.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!;
            Assert.Contains(adminPage.Data, inv => inv.Id == SeededHnInvoiceId);
            Assert.All(adminPage.Data, inv => Assert.Equal(DatabaseSeeder.HanoiDealerId, inv.DealerId));
        }

        // 2. Notification Inbox Scoping, UnreadCount, PATCH/POST /read, and POST /read-all
        NotificationFeedEnvelope silverFeed;
        using (var silverNotifRes = await silverClient.GetAsync("/api/v1/notifications"))
        {
            Assert.Equal(HttpStatusCode.OK, silverNotifRes.StatusCode);
            silverFeed = (await silverNotifRes.Content.ReadFromJsonAsync<NotificationFeedEnvelope>(JsonOpts))!;
            Assert.NotEmpty(silverFeed.Data);
            Assert.True(silverFeed.UnreadCount >= 1);
            Assert.Equal(silverFeed.UnreadCount, silverFeed.Meta.UnreadCount);
            Assert.All(silverFeed.Data, n => Assert.Equal(silverAuth.User.UserId, n.RecipientUserId));
        }

        NotificationFeedEnvelope hcmStaffFeed;
        using (var hcmStaffNotifRes = await hcmStaffClient.GetAsync("/api/v1/notifications"))
        {
            Assert.Equal(HttpStatusCode.OK, hcmStaffNotifRes.StatusCode);
            hcmStaffFeed = (await hcmStaffNotifRes.Content.ReadFromJsonAsync<NotificationFeedEnvelope>(JsonOpts))!;
            Assert.Contains(hcmStaffFeed.Data, n => n.Id == SeededHcmDealerNotifId);
            Assert.All(
                hcmStaffFeed.Data,
                n => Assert.True(
                    n.RecipientUserId == hcmStaffAuth.User.UserId ||
                    (n.TargetDealerId == DatabaseSeeder.HcmDealerId && n.RecipientUserId is null)));
        }

        // Cross-dealer staff & cross-customer notification read attempts MUST return 403
        using (var hnCrossNotifListRes = await hnStaffClient.GetAsync($"/api/v1/notifications?dealerId={DatabaseSeeder.HcmDealerId}"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, hnCrossNotifListRes.StatusCode);
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", await ReadErrorCodeAsync(hnCrossNotifListRes));
        }

        using (var hnCrossMarkRes = await hnStaffClient.PatchAsync($"/api/v1/notifications/{SeededHcmDealerNotifId}/read", null))
        {
            Assert.Equal(HttpStatusCode.Forbidden, hnCrossMarkRes.StatusCode);
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", await ReadErrorCodeAsync(hnCrossMarkRes));
        }

        var firstUnreadSilver = silverFeed.Data.First(n => !n.IsRead);
        using (var bronzeStealReadRes = await bronzeClient.PostAsync($"/api/v1/notifications/{firstUnreadSilver.Id}/read", null))
        {
            Assert.Equal(HttpStatusCode.Forbidden, bronzeStealReadRes.StatusCode);
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", await ReadErrorCodeAsync(bronzeStealReadRes));
        }

        // Silver marks single notification read via PATCH -> unreadCount decrements by 1
        using (var markSingleRes = await silverClient.PatchAsync($"/api/v1/notifications/{firstUnreadSilver.Id}/read", null))
        {
            Assert.Equal(HttpStatusCode.OK, markSingleRes.StatusCode);
            var ack = (await markSingleRes.Content.ReadFromJsonAsync<ApiEnvelope<MarkNotificationReadResultDto>>(JsonOpts))!;
            Assert.True(ack.Data.IsRead);
            Assert.Equal(silverFeed.UnreadCount - 1, ack.Data.UnreadCount);
        }

        // Silver marks all remaining notifications read via POST /read-all -> unreadCount == 0
        using (var markAllRes = await silverClient.PostAsync("/api/v1/notifications/read-all", null))
        {
            Assert.Equal(HttpStatusCode.OK, markAllRes.StatusCode);
            var ackAll = (await markAllRes.Content.ReadFromJsonAsync<ApiEnvelope<MarkAllNotificationsReadResultDto>>(JsonOpts))!;
            Assert.Equal(0, ackAll.Data.UnreadCount);
        }

        using (var unreadOnlyRes = await silverClient.GetAsync("/api/v1/notifications?unreadOnly=true"))
        {
            Assert.Equal(HttpStatusCode.OK, unreadOnlyRes.StatusCode);
            var afterAllFeed = (await unreadOnlyRes.Content.ReadFromJsonAsync<NotificationFeedEnvelope>(JsonOpts))!;
            Assert.Empty(afterAllFeed.Data);
            Assert.Equal(0, afterAllFeed.UnreadCount);
        }

        // Saigon dealer notification remains unread for staff.hcm (unaffected by customer read-all)
        using (var hcmCheckRes = await hcmStaffClient.GetAsync("/api/v1/notifications?unreadOnly=true"))
        {
            Assert.Equal(HttpStatusCode.OK, hcmCheckRes.StatusCode);
            var hcmUnreadFeed = (await hcmCheckRes.Content.ReadFromJsonAsync<NotificationFeedEnvelope>(JsonOpts))!;
            Assert.Contains(hcmUnreadFeed.Data, n => n.Id == SeededHcmDealerNotifId && !n.IsRead);
        }

        // Admin scoped POST /api/v1/notifications/read-all?dealerId={HcmDealerId} returns scoped UnreadCount == 0
        // even when an unread Admin-role notification exists
        var adminRoleNotifId = Guid.CreateVersion7();
        await using (var db = _fixture.CreateScopedDbContext())
        {
            db.Notifications.Add(new Notification(adminRoleNotifId)
            {
                TargetRole = RoleNames.Admin,
                EventType = "BookingCreated",
                Title = "Admin Executive Alert",
                Message = "Platform-wide unread alert for Admin role scope test.",
                PayloadJson = "{\"scope\":\"admin\"}",
                IsRead = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using (var adminScopedReadAllRes = await adminClient.PostAsync(
            $"/api/v1/notifications/read-all?dealerId={DatabaseSeeder.HcmDealerId}",
            null))
        {
            Assert.Equal(HttpStatusCode.OK, adminScopedReadAllRes.StatusCode);
            var scopedAck = (await adminScopedReadAllRes.Content.ReadFromJsonAsync<ApiEnvelope<MarkAllNotificationsReadResultDto>>(JsonOpts))!;
            Assert.True(scopedAck.Data.MarkedReadCount >= 1);
            Assert.Equal(0, scopedAck.Data.UnreadCount);
        }

        using (var adminOwnFeedRes = await adminClient.GetAsync("/api/v1/notifications?unreadOnly=true"))
        {
            Assert.Equal(HttpStatusCode.OK, adminOwnFeedRes.StatusCode);
            var adminOwnFeed = (await adminOwnFeedRes.Content.ReadFromJsonAsync<NotificationFeedEnvelope>(JsonOpts))!;
            Assert.Contains(adminOwnFeed.Data, n => n.Id == adminRoleNotifId && !n.IsRead);
        }
    }

    [Fact]
    public async Task HydrateExistingInvoice_WhenAccountHasWelcomeBonusAndNonQualifyingGoodwillAdjustment_ShouldNotInflateLifetimeQualifyingPoints()
    {
        using var bronzeClient = _fixture.CreateClient();
        using var hnStaffClient = _fixture.CreateClient();
        using var adminClient = _fixture.CreateClient();

        var bronzeAuth = await LoginAsync(bronzeClient, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var hnStaffAuth = await LoginAsync(hnStaffClient, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var adminAuth = await LoginAsync(adminClient, "admin@motorcycledealer.vn", "Admin@123!");

        bronzeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bronzeAuth.AccessToken);
        hnStaffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hnStaffAuth.AccessToken);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        var bronzeVehicleId = Guid.Parse("99999999-9999-9999-9999-999999999903");

        // Seeded Bronze customer starts at 120 AvailablePoints & 120 LifetimeQualifyingPoints.
        // Add +150 WelcomeBonus (non-qualifying) + +300 AdminAdjustment (QualifiesForTier = false):
        // AvailablePoints becomes 570 (>= 500 Silver threshold!), while LifetimeQualifyingPoints MUST stay 120!
        Guid bronzeAccountId;
        await using (var db = _fixture.CreateScopedDbContext())
        {
            var account = await db.LoyaltyAccounts.SingleAsync(a => a.CustomerId == bronzeAuth.User.UserId);
            bronzeAccountId = account.Id;
            account.AvailablePoints += 150;
            account.UpdatedAtUtc = DateTimeOffset.UtcNow;

            db.PointLedgerEntries.Add(new PointLedgerEntry(Guid.CreateVersion7())
            {
                LoyaltyAccountId = account.Id,
                TransactionType = "WelcomeBonus",
                PointsDelta = 150,
                BalanceAfter = account.AvailablePoints,
                ReferenceType = "AccountOnboarding",
                ReferenceId = account.Id,
                IdempotencyKey = $"WELCOME_BONUS:{account.Id}",
                Notes = "Promotional non-qualifying welcome bonus points.",
                CreatedByUserId = adminAuth.User.UserId,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
            });

            await db.SaveChangesAsync();
        }

        using (var goodwillRes = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            LoyaltyAccountId = bronzeAccountId,
            PointsDelta = 300,
            QualifiesForTier = false,
            Notes = "Goodwill service gesture credit (non-qualifying for tier).",
            IdempotencyKey = $"ADMIN_ADJUST:{Guid.NewGuid():D}"
        }))
        {
            Assert.Equal(HttpStatusCode.Created, goodwillRes.StatusCode);
            var adj = (await goodwillRes.Content.ReadFromJsonAsync<ApiEnvelope<LoyaltyAdjustmentResultDto>>(JsonOpts))!.Data;
            Assert.Equal(570, adj.AvailablePointsAfter);
            Assert.Equal(120, adj.LifetimeQualifyingPointsAfter);
            Assert.Equal("Bronze", adj.CurrentTierCode);
        }

        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(34));
        var slot = await CreateStaffTimeSlotAsync(hnStaffClient, slotDate, "15:00:00", "16:00:00");

        BookingDto booking;
        using (var bRes = await bronzeClient.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
        {
            DealerId = DatabaseSeeder.HanoiDealerId,
            ServiceTimeSlotId = slot.SlotId,
            CustomerVehicleId = bronzeVehicleId,
            ServiceType = "PeriodicMaintenance",
            OdometerAtBookingKm = 1_000
        }))
        {
            Assert.Equal(HttpStatusCode.Created, bRes.StatusCode);
            booking = (await bRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
        }

        using (var cRes = await hnStaffClient.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking.BookingId}/status",
            new UpdateBookingStatusRequest { NewStatus = "Confirmed" }))
        {
            Assert.Equal(HttpStatusCode.OK, cRes.StatusCode);
        }

        // Settle 200,000 VND invoice -> +20 qualifying points @ 1.00x Bronze -> Qualifying = 140 (Bronze), Available = 590
        ServiceInvoiceDto created;
        using (var invRes = await hnStaffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = booking.BookingId,
            LaborAmount = 80_000m,
            PartsAmount = 120_000m,
            OdometerAtCompletionKm = 1_010,
            PaymentMethod = "Cash"
        }))
        {
            Assert.Equal(HttpStatusCode.Created, invRes.StatusCode);
            created = (await invRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!.Data;
            Assert.Equal(20, created.PointsEarned);
            Assert.Equal(590, created.AvailablePointsAfter);
            Assert.Equal(140, created.LifetimeQualifyingPointsAfter);
            Assert.Equal("Bronze", created.AppliedTierCode);
            Assert.Equal("Bronze", created.CurrentTierAfter);
            Assert.False(created.TierUpgraded);
        }

        // GET /api/v1/service-invoices/{id} MUST report LifetimeQualifyingPointsAfter == 140 (NOT 590!) and TierUpgraded == false
        using (var getRes = await bronzeClient.GetAsync($"/api/v1/service-invoices/{created.InvoiceId}"))
        {
            Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
            var fetched = (await getRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!.Data;
            Assert.Equal(590, fetched.AvailablePointsAfter);
            Assert.Equal(140, fetched.LifetimeQualifyingPointsAfter);
            Assert.Equal("Bronze", fetched.AppliedTierCode);
            Assert.Equal("Bronze", fetched.CurrentTierAfter);
            Assert.Equal("Bronze", fetched.NewTierCode);
            Assert.False(fetched.TierUpgraded);
        }
    }

    [Theory]
    [InlineData(500, false, false, 1_000, 1)]    // (a) Non-recurring 1,000 km break-in well before 200 km lead window -> 1,000 km (+1m)
    [InlineData(799, false, false, 1_000, 1)]    // (a) Exact boundary 1 km before 200 km break-in lead window -> 1,000 km (+1m)
    [InlineData(800, false, false, 4_000, 6)]    // (a) Exact entry into 200 km break-in lead window -> skips break-in, rolls to 4,000 km (+6m)
    [InlineData(985, false, false, 4_000, 6)]    // (a) Break-in completed inside lead window -> rolls to 4,000 km (+6m)
    [InlineData(1_000, false, false, 4_000, 6)]  // (a) Exact 1,000 km break-in milestone -> rolls to 4,000 km (+6m)
    [InlineData(3_200, false, false, 4_000, 6)]  // (b) Recurring 4,000 km outside 250 km lead window -> stays 4,000 km (+6m)
    [InlineData(3_749, false, false, 4_000, 6)]  // (b) 1 km before 250 km lead window -> stays 4,000 km (+6m)
    [InlineData(3_750, false, false, 8_000, 6)]  // (b) Exact 250 km lead-window entry -> rolls forward to 8,000 km (+6m)
    [InlineData(3_950, false, false, 8_000, 6)]  // (b) Inside 4,000 km lead window -> rolls forward to 8,000 km (+6m)
    [InlineData(4_000, false, false, 8_000, 6)]  // (b) Exact 4,000 km recurring milestone -> rolls forward to 8,000 km (+6m)
    [InlineData(8_000, false, false, 12_000, 12)] // (b) Overlapping 12,000 km periodic + major -> prefers comprehensive 12,000 km (+12m)
    [InlineData(2_500, true, false, 6_500, 6)]   // (c) Empty rules fallback -> +4,000 km (+6m)
    [InlineData(1_200, false, true, 2_200, 1)]   // (c) Exhausted non-recurring-only rule fallback -> +IntervalKm (+1m)
    public void ComputeNextMaintenanceSchedule_AcrossBreakInLeadWindowRecurringAndFallbackBoundaries_ShouldReturnExpectedSchedule(
        int completionMileageKm,
        bool useEmptyRules,
        bool nonRecurringOnly,
        int expectedNextDueKm,
        int expectedMonthsDelta)
    {
        var paidAtUtc = new DateTimeOffset(2026, 5, 16, 9, 30, 0, TimeSpan.Zero);
        IReadOnlyList<MaintenanceRule> rules;

        if (useEmptyRules)
        {
            rules = [];
        }
        else if (nonRecurringOnly)
        {
            rules =
            [
                new MaintenanceRule
                {
                    MilestoneCode = "BREAKIN_1000",
                    MilestoneName = "Initial 1,000 km Break-In",
                    IntervalKm = 1_000,
                    IntervalMonths = 1,
                    IsRecurring = false,
                    LeadMileageKm = 200,
                    LeadTimeDays = 7,
                    IsActive = true
                }
            ];
        }
        else
        {
            rules =
            [
                new MaintenanceRule
                {
                    MilestoneCode = "BREAKIN_1000",
                    MilestoneName = "Initial 1,000 km Break-In",
                    IntervalKm = 1_000,
                    IntervalMonths = 1,
                    IsRecurring = false,
                    LeadMileageKm = 200,
                    LeadTimeDays = 7,
                    IsActive = true
                },
                new MaintenanceRule
                {
                    MilestoneCode = "PERIODIC_4000",
                    MilestoneName = "4,000 km Periodic Maintenance",
                    IntervalKm = 4_000,
                    IntervalMonths = 6,
                    IsRecurring = true,
                    LeadMileageKm = 250,
                    LeadTimeDays = 7,
                    IsActive = true
                },
                new MaintenanceRule
                {
                    MilestoneCode = "MAJOR_12000",
                    MilestoneName = "12,000 km Major Overhaul",
                    IntervalKm = 12_000,
                    IntervalMonths = 12,
                    IsRecurring = true,
                    LeadMileageKm = 400,
                    LeadTimeDays = 14,
                    IsActive = true
                }
            ];
        }

        var (actualNextDueKm, actualNextDueDateUtc) = ServiceInvoiceService.ComputeNextMaintenanceSchedule(
            completionMileageKm,
            paidAtUtc,
            rules);

        Assert.Equal(expectedNextDueKm, actualNextDueKm);
        Assert.Equal(paidAtUtc.AddMonths(expectedMonthsDelta), actualNextDueDateUtc);
    }

    private static async Task<AuthTokenData> LoginAsync(HttpClient client, string email, string password)
    {
        using var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
        Assert.NotNull(env);
        return env.Data;
    }

    private static async Task<ServiceTimeSlotDto> CreateStaffTimeSlotAsync(
        HttpClient staffClient,
        DateOnly slotDate,
        string startTime,
        string endTime)
    {
        using var res = await staffClient.PostAsJsonAsync("/api/v1/time-slots", new CreateTimeSlotRequest
        {
            SlotDate = slotDate,
            StartTime = startTime,
            EndTime = endTime,
            MaxCapacity = 4,
            IsBlocked = false
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts);
        Assert.NotNull(env);
        return env.Data;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("errorCode", out var codeProp)
            ? codeProp.GetString()
            : null;
    }
}
