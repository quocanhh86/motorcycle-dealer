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
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.BackgroundJobs;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using MotorcycleDealer.WebApi.Hubs;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.Booking;

public sealed class RecordingNotificationPublisher : INotificationPublisher
{
    private readonly SignalRNotificationPublisher _inner;

    public RecordingNotificationPublisher(SignalRNotificationPublisher inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public ConcurrentQueue<(BookingCreatedRealtimeEvent Payload, Guid TargetDealerId)> BookingCreatedEvents { get; } = new();
    public ConcurrentQueue<(BookingStatusChangedRealtimeEvent Payload, Guid CustomerId, Guid DealerId)> BookingStatusEvents { get; } = new();
    public ConcurrentQueue<(MaintenanceReminderRealtimeEvent Payload, Guid CustomerId)> ReminderEvents { get; } = new();

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

    public async Task PublishMaintenanceReminderAsync(
        MaintenanceReminderRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await _inner.PublishMaintenanceReminderAsync(payload, customerId, cancellationToken);
        ReminderEvents.Enqueue((payload, customerId));
    }

    public Task PublishLoyaltyPointsUpdatedAsync(
        LoyaltyPointsUpdatedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        _inner.PublishLoyaltyPointsUpdatedAsync(payload, customerId, cancellationToken);

    public Task PublishTierUpgradedAsync(
        TierUpgradedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        _inner.PublishTierUpgradedAsync(payload, customerId, cancellationToken);

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

public sealed class LocalDbPhase5BookingWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbPhase5BookingWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_P5Book_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={TestDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public RecordingNotificationPublisher NotificationSpy =>
        Services.GetRequiredService<RecordingNotificationPublisher>();

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
                ["MaintenanceReminderWorker:MileageLeadKm"] = "300",
                ["MaintenanceReminderWorker:DaysLeadWindow"] = "14",
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

            services.RemoveAll<INotificationPublisher>();
            services.AddSingleton<SignalRNotificationPublisher>();
            services.AddSingleton<RecordingNotificationPublisher>();
            services.AddScoped<INotificationPublisher>(sp => sp.GetRequiredService<RecordingNotificationPublisher>());
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

public sealed class SlotConcurrencyAndReminderTests : IClassFixture<LocalDbPhase5BookingWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly LocalDbPhase5BookingWebAppFixture _fixture;

    public SlotConcurrencyAndReminderTests(LocalDbPhase5BookingWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TenWayParallelDoubleBookingRace_WhenMaxCapacityIsOne_ShouldAllowExactSingleWinner_AndRejectNineContendersWith409ProblemDetails()
    {
        using var staffClient = _fixture.CreateClient();
        var hcmStaff = await LoginAsync(staffClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");

        var raceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25));
        ServiceTimeSlotDto targetSlot;

        using (var createSlotReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/time-slots",
            hcmStaff.AccessToken,
            new CreateTimeSlotRequest
            {
                SlotDate = raceDate,
                StartTime = "08:00:00",
                EndTime = "09:00:00",
                MaxCapacity = 1,
                IsBlocked = false
            }))
        using (var createSlotRes = await staffClient.SendAsync(createSlotReq))
        {
            Assert.Equal(HttpStatusCode.Created, createSlotRes.StatusCode);
            var env = await createSlotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts);
            Assert.NotNull(env);
            targetSlot = env.Data;
            Assert.Equal(1, targetSlot.MaxCapacity);
            Assert.Equal(0, targetSlot.BookedCount);
            Assert.Equal(1, targetSlot.RemainingCapacity);
            Assert.Equal(1, targetSlot.AvailableBays);
        }

        // Provision 10 distinct Customer accounts + 10 distinct owned CustomerVehicles
        var contenders = await ProvisionDistinctCustomerRidersAsync(riderCount: 10, seedPrefix: "RACE1");
        var initialBookingCreatedEvents = _fixture.NotificationSpy.BookingCreatedEvents.Count;

        try
        {
            using var startGate = new ManualResetEventSlim(false);
            var bookingTasks = contenders.Select(async contender =>
            {
                await Task.Yield();
                startGate.Wait(TimeSpan.FromSeconds(10));
                return await contender.Client.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
                {
                    DealerId = DatabaseSeeder.HcmDealerId,
                    ServiceTimeSlotId = targetSlot.SlotId,
                    SlotRowVersion = targetSlot.RowVersion,
                    CustomerVehicleId = contender.VehicleId,
                    ServiceType = "PeriodicMaintenance",
                    OdometerAtBookingKm = 1_500,
                    ReportedNotes = "10-way parallel slot contention test"
                });
            }).ToArray();

            startGate.Set();
            var responses = await Task.WhenAll(bookingTasks);

            try
            {
                var createdResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
                var conflictResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();

                Assert.Single(createdResponses);
                Assert.Equal(9, conflictResponses.Count);

                foreach (var conflictRes in conflictResponses)
                {
                    Assert.Equal("application/problem+json", conflictRes.Content.Headers.ContentType?.MediaType);
                    using var problemDoc = JsonDocument.Parse(await conflictRes.Content.ReadAsStringAsync());
                    Assert.Equal(409, problemDoc.RootElement.GetProperty("status").GetInt32());
                    Assert.Equal("SLOT_CAPACITY_EXHAUSTED", problemDoc.RootElement.GetProperty("errorCode").GetString());
                    Assert.False(string.IsNullOrWhiteSpace(problemDoc.RootElement.GetProperty("traceId").GetString()));
                }

                var winnerEnv = await createdResponses[0].Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts);
                Assert.NotNull(winnerEnv);
                Assert.Equal("Pending", winnerEnv.Data.Status);

                // Verify SQL Server invariants: BookedCount == 1 and exactly 1 ServiceBooking row
                await using var verifyDb = _fixture.CreateScopedDbContext();
                var dbSlot = await verifyDb.ServiceTimeSlots
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstAsync(s => s.Id == targetSlot.SlotId);

                var dbBookingCount = await verifyDb.ServiceBookings
                    .IgnoreQueryFilters()
                    .CountAsync(b => b.ServiceTimeSlotId == targetSlot.SlotId && b.Status != "Cancelled");

                Assert.Equal(1, dbSlot.BookedCount);
                Assert.Equal(1, dbBookingCount);

                // Verify post-commit SignalR BookingCreated fired ONLY once for the single winning commit
                var matchingRealtimeEvents = _fixture.NotificationSpy.BookingCreatedEvents
                    .Skip(initialBookingCreatedEvents)
                    .Where(e => e.Payload.BookingId == winnerEnv.Data.BookingId)
                    .ToList();

                Assert.Single(matchingRealtimeEvents);
                Assert.Equal(DatabaseSeeder.HcmDealerId, matchingRealtimeEvents[0].TargetDealerId);

                // Cleanly cancel the winning booking so test vehicles/slots leave clean state
                var winningContender = contenders.First(c => c.CustomerId == winnerEnv.Data.CustomerId);
                using var cleanupCancelRes = await winningContender.Client.PostAsJsonAsync(
                    $"/api/v1/bookings/{winnerEnv.Data.BookingId}/cancel",
                    new CancelBookingRequest { CancellationReason = "Cleanup race winner" });
                Assert.Equal(HttpStatusCode.OK, cleanupCancelRes.StatusCode);
            }
            finally
            {
                foreach (var res in responses)
                {
                    res.Dispose();
                }
            }
        }
        finally
        {
            foreach (var contender in contenders)
            {
                contender.Client.Dispose();
            }
        }
    }

    [Fact]
    public async Task MultiBayParallelContention_WhenMaxCapacityIsThree_ShouldRetryStaleRowVersionAndFillExactThreeBays()
    {
        using var staffClient = _fixture.CreateClient();
        var hcmStaff = await LoginAsync(staffClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");

        var multiBayDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(26));
        ServiceTimeSlotDto multiBaySlot;

        using (var createSlotReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/time-slots",
            hcmStaff.AccessToken,
            new CreateTimeSlotRequest
            {
                SlotDate = multiBayDate,
                StartTime = "10:00:00",
                EndTime = "11:00:00",
                MaxCapacity = 3,
                IsBlocked = false
            }))
        using (var createSlotRes = await staffClient.SendAsync(createSlotReq))
        {
            Assert.Equal(HttpStatusCode.Created, createSlotRes.StatusCode);
            multiBaySlot = (await createSlotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts))!.Data;
        }

        var contenders = await ProvisionDistinctCustomerRidersAsync(riderCount: 6, seedPrefix: "BAY3");
        try
        {
            var responses = await Task.WhenAll(contenders.Select(c =>
                c.Client.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
                {
                    DealerId = DatabaseSeeder.HcmDealerId,
                    ServiceTimeSlotId = multiBaySlot.SlotId,
                    SlotRowVersion = multiBaySlot.RowVersion,
                    CustomerVehicleId = c.VehicleId,
                    ServiceType = "OilAndTire",
                    OdometerAtBookingKm = 2_000
                })));

            try
            {
                Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
                Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

                await using var verifyDb = _fixture.CreateScopedDbContext();
                var reloadedSlot = await verifyDb.ServiceTimeSlots
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstAsync(s => s.Id == multiBaySlot.SlotId);

                Assert.Equal(3, reloadedSlot.BookedCount);
            }
            finally
            {
                foreach (var res in responses)
                {
                    res.Dispose();
                }
            }
        }
        finally
        {
            foreach (var c in contenders)
            {
                c.Client.Dispose();
            }
        }
    }

    [Fact]
    public async Task VoucherLockOdometerGuardAndCancellationRelease_ShouldReserveAndRestoreSvc100KVoucherAtomically()
    {
        using var client = _fixture.CreateClient();
        var silverAuth = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var hcmStaffAuth = await LoginAsync(client, "staff.hcm@motorcycledealer.vn", "Staff@123!");

        var silverVehicleId = Guid.Parse("99999999-9999-9999-9999-999999999901");
        var svc100kVoucherId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");

        // Create dedicated open slot at DLR-HCM-01
        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(27));
        ServiceTimeSlotDto slot;
        using (var slotReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/time-slots",
            hcmStaffAuth.AccessToken,
            new CreateTimeSlotRequest
            {
                SlotDate = slotDate,
                StartTime = "09:00:00",
                EndTime = "10:00:00",
                MaxCapacity = 4
            }))
        using (var slotRes = await client.SendAsync(slotReq))
        {
            Assert.Equal(HttpStatusCode.Created, slotRes.StatusCode);
            slot = (await slotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts))!.Data;
        }

        // 1a. Malformed/short 3-byte Base64 slotRowVersion ("AAAA") -> 400 VALIDATION_FAILED
        using (var badRvReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            silverAuth.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                ServiceTimeSlotId = slot.SlotId,
                SlotRowVersion = "AAAA",
                CustomerVehicleId = silverVehicleId,
                ServiceType = "PeriodicMaintenance",
                OdometerAtBookingKm = 3_860
            }))
        using (var badRvRes = await client.SendAsync(badRvReq))
        {
            Assert.Equal(HttpStatusCode.BadRequest, badRvRes.StatusCode);
            using var badRvDoc = JsonDocument.Parse(await badRvRes.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_FAILED", badRvDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // 1b. Odometer regression (< 3,850 km) -> 422 ODOMETER_REGRESSION
        using (var regressReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            silverAuth.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                ServiceTimeSlotId = slot.SlotId,
                CustomerVehicleId = silverVehicleId,
                ServiceType = "PeriodicMaintenance",
                OdometerAtBookingKm = 3_200
            }))
        using (var regressRes = await client.SendAsync(regressReq))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, regressRes.StatusCode);
            using var doc = JsonDocument.Parse(await regressRes.Content.ReadAsStringAsync());
            Assert.Equal("ODOMETER_REGRESSION", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 2. Book with SVC100K-7K9P2M4X -> Available -> ReservedOnBooking + BookedCount == 1
        BookingDto createdBooking;
        using (var bookReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            silverAuth.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                ServiceTimeSlotId = slot.SlotId,
                SlotRowVersion = slot.RowVersion,
                CustomerVehicleId = silverVehicleId,
                AppliedCustomerVoucherId = svc100kVoucherId,
                ServiceType = "PeriodicMaintenance",
                OdometerAtBookingKm = 3_860,
                ReportedNotes = "4,000 km periodic service with SVC100K voucher"
            }))
        using (var bookRes = await client.SendAsync(bookReq))
        {
            Assert.Equal(HttpStatusCode.Created, bookRes.StatusCode);
            createdBooking = (await bookRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
            Assert.Equal("SVC100K-7K9P2M4X", createdBooking.ReservedVoucherCode);
            Assert.Equal("Pending", createdBooking.Status);
        }

        await using (var dbAfterBook = _fixture.CreateScopedDbContext())
        {
            var voucherAfterBook = await dbAfterBook.CustomerVouchers
                .AsNoTracking()
                .FirstAsync(v => v.Id == svc100kVoucherId);
            var slotAfterBook = await dbAfterBook.ServiceTimeSlots
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstAsync(s => s.Id == slot.SlotId);
            var vehicleAfterBook = await dbAfterBook.CustomerVehicles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstAsync(v => v.Id == silverVehicleId);

            Assert.Equal("ReservedOnBooking", voucherAfterBook.Status);
            Assert.Equal(1, slotAfterBook.BookedCount);
            Assert.Equal(3_860, vehicleAfterBook.CurrentMileageKm);
        }

        // 3. Duplicate active booking for same VIN in same slot -> 409 DUPLICATE_VEHICLE_BOOKING
        using (var dupBookReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            silverAuth.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                ServiceTimeSlotId = slot.SlotId,
                CustomerVehicleId = silverVehicleId,
                ServiceType = "Inspection",
                OdometerAtBookingKm = 3_860
            }))
        using (var dupBookRes = await client.SendAsync(dupBookReq))
        {
            Assert.Equal(HttpStatusCode.Conflict, dupBookRes.StatusCode);
            using var dupDoc = JsonDocument.Parse(await dupBookRes.Content.ReadAsStringAsync());
            Assert.Equal("DUPLICATE_VEHICLE_BOOKING", dupDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // 4a. Too-short cancellationReason (< 3 chars) -> 400 VALIDATION_FAILED
        using (var shortReasonReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/bookings/{createdBooking.BookingId}/cancel",
            silverAuth.AccessToken,
            new CancelBookingRequest
            {
                CancellationReason = "ab",
                RowVersion = createdBooking.RowVersion
            }))
        using (var shortReasonRes = await client.SendAsync(shortReasonReq))
        {
            Assert.Equal(HttpStatusCode.BadRequest, shortReasonRes.StatusCode);
            using var shortDoc = JsonDocument.Parse(await shortReasonRes.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_FAILED", shortDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // 4b. Cancel booking -> BookedCount decremented to 0 & voucher restored to Available
        using (var cancelReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/bookings/{createdBooking.BookingId}/cancel",
            silverAuth.AccessToken,
            new CancelBookingRequest
            {
                CancellationReason = "Rescheduling appointment to next week",
                RowVersion = createdBooking.RowVersion
            }))
        using (var cancelRes = await client.SendAsync(cancelReq))
        {
            Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
            var cancelled = (await cancelRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
            Assert.Equal("Cancelled", cancelled.Status);
            Assert.NotNull(cancelled.CancelledAtUtc);
        }

        await using (var dbAfterCancel = _fixture.CreateScopedDbContext())
        {
            var voucherAfterCancel = await dbAfterCancel.CustomerVouchers
                .AsNoTracking()
                .FirstAsync(v => v.Id == svc100kVoucherId);
            var slotAfterCancel = await dbAfterCancel.ServiceTimeSlots
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstAsync(s => s.Id == slot.SlotId);

            Assert.Equal("Available", voucherAfterCancel.Status);
            Assert.Equal(0, slotAfterCancel.BookedCount);
        }
    }

    [Fact]
    public async Task CrossDealerIsolationAndBookingLifecycle_ShouldBlockHanoiStaffFromSaigonSlotsAndBookings()
    {
        using var client = _fixture.CreateClient();
        var hnStaffAuth = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var hcmStaffAuth = await LoginAsync(client, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var bronzeAuth = await LoginAsync(client, "customer.bronze@motorcycledealer.vn", "Customer@123!");

        var genDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(28));

        // 1. Saigon staff bulk-generates slots idempotently (run twice -> zero duplicates)
        IReadOnlyList<ServiceTimeSlotDto> hcmSlots;
        var generatePayload = new GenerateTimeSlotsRequest
        {
            FromDate = genDate,
            ToDate = genDate,
            SlotDurationMinutes = 120,
            MaxCapacityPerSlot = 2,
            OpeningTime = "08:00:00",
            ClosingTime = "12:00:00"
        };

        using (var gen1Req = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/time-slots/generate", hcmStaffAuth.AccessToken, generatePayload))
        using (var gen1Res = await client.SendAsync(gen1Req))
        {
            Assert.Equal(HttpStatusCode.Created, gen1Res.StatusCode);
            hcmSlots = (await gen1Res.Content.ReadFromJsonAsync<ApiEnvelope<List<ServiceTimeSlotDto>>>(JsonOpts))!.Data;
            Assert.Equal(2, hcmSlots.Count);
        }

        using (var gen2Req = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/time-slots/generate", hcmStaffAuth.AccessToken, generatePayload))
        using (var gen2Res = await client.SendAsync(gen2Req))
        {
            Assert.Equal(HttpStatusCode.Created, gen2Res.StatusCode);
            var hcmSlotsSecondRun = (await gen2Res.Content.ReadFromJsonAsync<ApiEnvelope<List<ServiceTimeSlotDto>>>(JsonOpts))!.Data;
            Assert.Equal(2, hcmSlotsSecondRun.Count);
        }

        var hcmTargetSlot = hcmSlots[0];

        // 2. Customer books Saigon slot
        var bronzeVehicleId = Guid.Parse("99999999-9999-9999-9999-999999999903");
        BookingDto hcmBooking;
        using (var bookReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            bronzeAuth.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                ServiceTimeSlotId = hcmTargetSlot.SlotId,
                CustomerVehicleId = bronzeVehicleId,
                ServiceType = "Inspection",
                OdometerAtBookingKm = 920
            }))
        using (var bookRes = await client.SendAsync(bookReq))
        {
            Assert.Equal(HttpStatusCode.Created, bookRes.StatusCode);
            hcmBooking = (await bookRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
        }

        // 3. Hanoi staff (DLR-HN-01) CANNOT view or update DLR-HCM-01 slots or bookings (403 / 404)
        using (var hnViewHcmSlotsReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/time-slots?dealerId={DatabaseSeeder.HcmDealerId}&fromDate={genDate:yyyy-MM-dd}&toDate={genDate:yyyy-MM-dd}",
            hnStaffAuth.AccessToken))
        using (var hnViewHcmSlotsRes = await client.SendAsync(hnViewHcmSlotsReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, hnViewHcmSlotsRes.StatusCode);
        }

        using (var hnPatchHcmSlotReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/time-slots/{hcmTargetSlot.SlotId}",
            hnStaffAuth.AccessToken,
            new UpdateTimeSlotRequest { IsBlocked = true }))
        using (var hnPatchHcmSlotRes = await client.SendAsync(hnPatchHcmSlotReq))
        {
            Assert.Equal(HttpStatusCode.NotFound, hnPatchHcmSlotRes.StatusCode);
        }

        using (var hnListHcmBookingsReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/bookings?dealerId={DatabaseSeeder.HcmDealerId}",
            hnStaffAuth.AccessToken))
        using (var hnListHcmBookingsRes = await client.SendAsync(hnListHcmBookingsReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, hnListHcmBookingsRes.StatusCode);
        }

        using (var hnDefaultQueueReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/bookings",
            hnStaffAuth.AccessToken))
        using (var hnDefaultQueueRes = await client.SendAsync(hnDefaultQueueReq))
        {
            Assert.Equal(HttpStatusCode.OK, hnDefaultQueueRes.StatusCode);
            var hnQueue = await hnDefaultQueueRes.Content.ReadFromJsonAsync<PagedApiEnvelope<BookingDto>>(JsonOpts);
            Assert.NotNull(hnQueue);
            Assert.All(hnQueue.Data, b => Assert.Equal(DatabaseSeeder.HanoiDealerId, b.DealerId));
            Assert.DoesNotContain(hnQueue.Data, b => b.BookingId == hcmBooking.BookingId);
        }

        using (var hnGetHcmBookingReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/bookings/{hcmBooking.BookingId}",
            hnStaffAuth.AccessToken))
        using (var hnGetHcmBookingRes = await client.SendAsync(hnGetHcmBookingReq))
        {
            Assert.Equal(HttpStatusCode.NotFound, hnGetHcmBookingRes.StatusCode);
        }

        using (var hnPatchHcmBookingReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/bookings/{hcmBooking.BookingId}/status",
            hnStaffAuth.AccessToken,
            new UpdateBookingStatusRequest { NewStatus = "Confirmed" }))
        using (var hnPatchHcmBookingRes = await client.SendAsync(hnPatchHcmBookingReq))
        {
            Assert.Equal(HttpStatusCode.NotFound, hnPatchHcmBookingRes.StatusCode);
        }

        // 4. Saigon staff capacity guard (MaxCapacity < BookedCount -> 409 CAPACITY_BELOW_BOOKED & stale RowVersion -> 409 CONCURRENCY_CONFLICT)
        // Book a second vehicle into hcmTargetSlot so BookedCount == 2, then attempt MaxCapacity = 1 -> 409 CAPACITY_BELOW_BOOKED
        var minhAuth = await LoginAsync(client, "minh.nguyen@customer.vn", "Customer@123!");
        var minhVehicleId = Guid.Parse("99999999-9999-9999-9999-999999999902");
        using (var secondBookReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            minhAuth.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                ServiceTimeSlotId = hcmTargetSlot.SlotId,
                CustomerVehicleId = minhVehicleId,
                ServiceType = "OilAndTire",
                OdometerAtBookingKm = 910
            }))
        using (var secondBookRes = await client.SendAsync(secondBookReq))
        {
            Assert.Equal(HttpStatusCode.Created, secondBookRes.StatusCode);
        }

        // Stale RowVersion (from before the 2 bookings!) -> 409 CONCURRENCY_CONFLICT
        using (var stalePatchReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/time-slots/{hcmTargetSlot.SlotId}",
            hcmStaffAuth.AccessToken,
            new UpdateTimeSlotRequest { MaxCapacity = 4, RowVersion = hcmTargetSlot.RowVersion }))
        using (var stalePatchRes = await client.SendAsync(stalePatchReq))
        {
            Assert.Equal(HttpStatusCode.Conflict, stalePatchRes.StatusCode);
            using var staleDoc = JsonDocument.Parse(await stalePatchRes.Content.ReadAsStringAsync());
            Assert.Equal("CONCURRENCY_CONFLICT", staleDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // Attempt to shrink MaxCapacity to 1 while BookedCount == 2 -> 409 CAPACITY_BELOW_BOOKED
        using (var shrinkPatchReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/time-slots/{hcmTargetSlot.SlotId}",
            hcmStaffAuth.AccessToken,
            new UpdateTimeSlotRequest { MaxCapacity = 1 }))
        using (var shrinkPatchRes = await client.SendAsync(shrinkPatchReq))
        {
            Assert.Equal(HttpStatusCode.Conflict, shrinkPatchRes.StatusCode);
            using var shrinkDoc = JsonDocument.Parse(await shrinkPatchRes.Content.ReadAsStringAsync());
            Assert.Equal("CAPACITY_BELOW_BOOKED", shrinkDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // Illegal jump Pending -> Completed -> 422 INVALID_STATUS_TRANSITION
        using (var illegalJumpReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/bookings/{hcmBooking.BookingId}/status",
            hcmStaffAuth.AccessToken,
            new UpdateBookingStatusRequest { NewStatus = "Completed" }))
        using (var illegalJumpRes = await client.SendAsync(illegalJumpReq))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, illegalJumpRes.StatusCode);
            using var jumpDoc = JsonDocument.Parse(await illegalJumpRes.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_STATUS_TRANSITION", jumpDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // Valid workshop progression: Pending -> Confirmed -> CheckedIn -> InProgress -> Completed
        foreach (var nextStatus in new[] { "Confirmed", "CheckedIn", "InProgress", "Completed" })
        {
            using var statusReq = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/v1/bookings/{hcmBooking.BookingId}/status",
                hcmStaffAuth.AccessToken,
                new UpdateBookingStatusRequest { NewStatus = nextStatus });
            using var statusRes = await client.SendAsync(statusReq);
            Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);
            var statusEnv = await statusRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts);
            Assert.NotNull(statusEnv);
            Assert.Equal(nextStatus, statusEnv.Data.Status);
        }
    }

    [Fact]
    public async Task VinMaintenanceReminderScanner_RunThreeTimesConsecutively_ShouldCreateExactSingleReminderPerDueRule_AndZeroDuplicates()
    {
        const string targetVin = "RLHJF5109RZ104829";
        Guid dueVehicleId;
        Guid periodic4000RuleId;

        // Ensure clean baseline for RLHJF5109RZ104829 (3,850+ km approaching 4,000 km with 0 existing reminders and 0 active bookings)
        await using (var setupDb = _fixture.CreateScopedDbContext())
        {
            var vehicle = await setupDb.CustomerVehicles
                .IgnoreQueryFilters()
                .FirstAsync(v => v.VIN == targetVin);

            dueVehicleId = vehicle.Id;
            if (vehicle.CurrentMileageKm < 3_850 || vehicle.CurrentMileageKm >= 4_000)
            {
                vehicle.CurrentMileageKm = 3_850;
            }

            periodic4000RuleId = await setupDb.MaintenanceRules
                .IgnoreQueryFilters()
                .Where(r => r.MotorcycleModelId == vehicle.MotorcycleModelId && r.MilestoneCode == "PERIODIC_4000")
                .Select(r => r.Id)
                .FirstAsync();

            var activeVehicleBookings = await setupDb.ServiceBookings
                .IgnoreQueryFilters()
                .Where(b => b.CustomerVehicleId == dueVehicleId &&
                            b.Status != "Completed" &&
                            b.Status != "Cancelled" &&
                            b.Status != "NoShow")
                .ToListAsync();

            foreach (var openBooking in activeVehicleBookings)
            {
                openBooking.Status = "Cancelled";
            }

            var existingVehicleReminders = await setupDb.MaintenanceReminders
                .IgnoreQueryFilters()
                .Where(r => r.CustomerVehicleId == dueVehicleId)
                .ToListAsync();

            if (existingVehicleReminders.Count > 0)
            {
                setupDb.MaintenanceReminders.RemoveRange(existingVehicleReminders);
            }

            await setupDb.SaveChangesAsync();
        }

        var expectedDedupKey = $"{targetVin}:{periodic4000RuleId}:4000";
        var initialSpyCount = _fixture.NotificationSpy.ReminderEvents.Count;

        // Run 1: Should create exact 1 MaintenanceReminder for RLHJF5109RZ104829
        VinMaintenanceReminderScanResult run1;
        await using (var scope1 = _fixture.Services.CreateAsyncScope())
        {
            var scanner1 = scope1.ServiceProvider.GetRequiredService<IVinMaintenanceReminderScanner>();
            run1 = await scanner1.ScanAndDispatchDueRemindersAsync();
        }

        Assert.True(run1.RemindersCreated >= 1);

        await using (var verifyRun1Db = _fixture.CreateScopedDbContext())
        {
            var remindersAfterRun1 = await verifyRun1Db.MaintenanceReminders
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.CustomerVehicleId == dueVehicleId)
                .ToListAsync();

            Assert.Single(remindersAfterRun1);
            Assert.Equal(expectedDedupKey, remindersAfterRun1[0].DeduplicationKey);
            Assert.Equal(4_000, remindersAfterRun1[0].TargetMileageKm);
            Assert.Equal("Sent", remindersAfterRun1[0].Status);
            Assert.NotNull(remindersAfterRun1[0].SentAtUtc);
        }

        // Run 2 & Run 3 consecutively: MUST produce 0 duplicate reminders!
        VinMaintenanceReminderScanResult run2;
        await using (var scope2 = _fixture.Services.CreateAsyncScope())
        {
            var scanner2 = scope2.ServiceProvider.GetRequiredService<IVinMaintenanceReminderScanner>();
            run2 = await scanner2.ScanAndDispatchDueRemindersAsync();
        }

        VinMaintenanceReminderScanResult run3;
        await using (var scope3 = _fixture.Services.CreateAsyncScope())
        {
            var scanner3 = scope3.ServiceProvider.GetRequiredService<IVinMaintenanceReminderScanner>();
            run3 = await scanner3.ScanAndDispatchDueRemindersAsync();
        }

        Assert.Equal(0, run2.RemindersCreated);
        Assert.Equal(0, run3.RemindersCreated);

        await using (var verifyFinalDb = _fixture.CreateScopedDbContext())
        {
            var finalRemindersForVin = await verifyFinalDb.MaintenanceReminders
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.CustomerVehicleId == dueVehicleId)
                .ToListAsync();

            Assert.Single(finalRemindersForVin);
            Assert.Equal(expectedDedupKey, finalRemindersForVin[0].DeduplicationKey);
        }

        var spyEventsForTargetVin = _fixture.NotificationSpy.ReminderEvents
            .Skip(initialSpyCount)
            .Where(e => e.Payload.DeduplicationKey == expectedDedupKey)
            .ToList();

        Assert.Single(spyEventsForTargetVin);
    }

    [Fact]
    public async Task DeactivatedDealer_ShouldRejectTimeSlotMutationsAndCustomerBookingsWith422DealerInactive_WhileMissingDealerReturns404()
    {
        using var client = _fixture.CreateClient();
        var admin = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        var bronzeCustomer = await LoginAsync(client, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var bronzeVehicleId = Guid.Parse("99999999-9999-9999-9999-999999999903");
        var suffix = Random.Shared.Next(100, 999);

        var dealerPayload = new UpsertDealerRequest
        {
            Code = $"DLR-BH-{suffix}",
            Name = $"Apex Moto Bien Hoa {suffix}",
            City = "Bien Hoa",
            Address = "210 Pham Van Thuan, Bien Hoa",
            PhoneNumber = $"0251380{suffix:D4}"[..11],
            ContactEmail = $"bh.{suffix}@motorcycledealer.vn",
            OpeningTime = "08:00:00",
            ClosingTime = "18:00:00",
            DefaultSlotCapacity = 3,
            IsActive = true
        };

        Guid branchId;
        using (var createDealerReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/dealers", admin.AccessToken, dealerPayload))
        using (var createDealerRes = await client.SendAsync(createDealerReq))
        {
            Assert.Equal(HttpStatusCode.Created, createDealerRes.StatusCode);
            branchId = (await createDealerRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerDto>>(JsonOpts))!.Data.Id;
        }

        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(29));
        ServiceTimeSlotDto existingSlot;
        using (var createSlotReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/time-slots",
            admin.AccessToken,
            new CreateTimeSlotRequest
            {
                DealerId = branchId,
                SlotDate = slotDate,
                StartTime = "08:00:00",
                EndTime = "09:00:00",
                MaxCapacity = 3
            }))
        using (var createSlotRes = await client.SendAsync(createSlotReq))
        {
            Assert.Equal(HttpStatusCode.Created, createSlotRes.StatusCode);
            existingSlot = (await createSlotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts))!.Data;
        }

        // Provision DealerStaff on active branch and obtain unexpired 15m JWT prior to branch deactivation
        var branchStaffEmail = $"staff.bh.{suffix}@motorcycledealer.vn";
        using (var createStaffReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/dealers/{branchId}/staff",
            admin.AccessToken,
            new CreateDealerStaffRequest
            {
                Email = branchStaffEmail,
                Password = "Staff@123!",
                FullName = "Le Van Bien",
                PhoneNumber = $"0918444{suffix:D3}",
                StaffEmployeeCode = $"EMP-BH-{suffix}"
            }))
        using (var createStaffRes = await client.SendAsync(createStaffReq))
        {
            Assert.Equal(HttpStatusCode.Created, createStaffRes.StatusCode);
        }

        var branchStaffJwt = (await LoginAsync(client, branchStaffEmail, "Staff@123!")).AccessToken;

        // Deactivate dealership via Phase 4 Admin endpoint
        using (var deactReq = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/dealers/{branchId}",
            admin.AccessToken,
            dealerPayload with { IsActive = false }))
        using (var deactRes = await client.SendAsync(deactReq))
        {
            Assert.Equal(HttpStatusCode.OK, deactRes.StatusCode);
        }

        // 1. POST /api/v1/time-slots -> 422 DEALER_INACTIVE
        using (var createInactiveSlotReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/time-slots",
            admin.AccessToken,
            new CreateTimeSlotRequest
            {
                DealerId = branchId,
                SlotDate = slotDate,
                StartTime = "10:00:00",
                EndTime = "11:00:00",
                MaxCapacity = 3
            }))
        using (var createInactiveSlotRes = await client.SendAsync(createInactiveSlotReq))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, createInactiveSlotRes.StatusCode);
            using var doc = JsonDocument.Parse(await createInactiveSlotRes.Content.ReadAsStringAsync());
            Assert.Equal("DEALER_INACTIVE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 2. POST /api/v1/time-slots/generate -> 422 DEALER_INACTIVE
        using (var genInactiveReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/time-slots/generate",
            admin.AccessToken,
            new GenerateTimeSlotsRequest
            {
                DealerId = branchId,
                FromDate = slotDate.AddDays(1),
                ToDate = slotDate.AddDays(1),
                SlotDurationMinutes = 60
            }))
        using (var genInactiveRes = await client.SendAsync(genInactiveReq))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, genInactiveRes.StatusCode);
            using var doc = JsonDocument.Parse(await genInactiveRes.Content.ReadAsStringAsync());
            Assert.Equal("DEALER_INACTIVE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 3. PATCH /api/v1/time-slots/{id} -> 422 DEALER_INACTIVE
        using (var patchInactiveSlotReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/time-slots/{existingSlot.SlotId}",
            admin.AccessToken,
            new UpdateTimeSlotRequest { IsBlocked = true, RowVersion = existingSlot.RowVersion }))
        using (var patchInactiveSlotRes = await client.SendAsync(patchInactiveSlotReq))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, patchInactiveSlotRes.StatusCode);
            using var doc = JsonDocument.Parse(await patchInactiveSlotRes.Content.ReadAsStringAsync());
            Assert.Equal("DEALER_INACTIVE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 4. Customer POST /api/v1/bookings against inactive dealer -> 422 DEALER_INACTIVE (distinct from 404 on missing dealer!)
        using (var bookInactiveReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            bronzeCustomer.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = branchId,
                ServiceTimeSlotId = existingSlot.SlotId,
                CustomerVehicleId = bronzeVehicleId,
                ServiceType = "Inspection",
                OdometerAtBookingKm = 950
            }))
        using (var bookInactiveRes = await client.SendAsync(bookInactiveReq))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, bookInactiveRes.StatusCode);
            using var doc = JsonDocument.Parse(await bookInactiveRes.Content.ReadAsStringAsync());
            Assert.Equal("DEALER_INACTIVE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var bookMissingDealerReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            bronzeCustomer.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = Guid.CreateVersion7(),
                ServiceTimeSlotId = existingSlot.SlotId,
                CustomerVehicleId = bronzeVehicleId,
                ServiceType = "Inspection",
                OdometerAtBookingKm = 950
            }))
        using (var bookMissingDealerRes = await client.SendAsync(bookMissingDealerReq))
        {
            Assert.Equal(HttpStatusCode.NotFound, bookMissingDealerRes.StatusCode);
            using var doc = JsonDocument.Parse(await bookMissingDealerRes.Content.ReadAsStringAsync());
            Assert.Equal("RESOURCE_NOT_FOUND", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 5. Unexpired DealerStaff JWT of deactivated branch receives 403 STAFF_DEALER_UNASSIGNED on dual-mode [AllowAnonymous] endpoints
        foreach (var dualModeUrl in new[]
                 {
                     "/api/v1/inventories",
                     "/api/v1/inventories?publicOnly=true",
                     $"/api/v1/time-slots?dealerId={branchId}"
                 })
        {
            using var staffDualModeReq = CreateAuthorizedRequest(HttpMethod.Get, dualModeUrl, branchStaffJwt);
            using var staffDualModeRes = await client.SendAsync(staffDualModeReq);
            Assert.Equal(HttpStatusCode.Forbidden, staffDualModeRes.StatusCode);
            using var doc = JsonDocument.Parse(await staffDualModeRes.Content.ReadAsStringAsync());
            Assert.Equal("STAFF_DEALER_UNASSIGNED", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task VinMaintenanceReminderScanner_WhenPeriodicAndMajorRulesOverlapAt12000Km_ShouldDispatchSingleMajor12000Reminder()
    {
        const string overlapVin = "RLHPC6405RY000612";
        Guid overlapVehicleId;
        Guid major12000RuleId;

        await using (var setupDb = _fixture.CreateScopedDbContext())
        {
            var vehicle = await setupDb.CustomerVehicles
                .IgnoreQueryFilters()
                .FirstAsync(v => v.VIN == overlapVin);

            overlapVehicleId = vehicle.Id;
            vehicle.LastServiceMileageKm = 8_000;
            vehicle.CurrentMileageKm = 11_850;
            vehicle.NextDueMileageKm = 12_000;

            major12000RuleId = await setupDb.MaintenanceRules
                .IgnoreQueryFilters()
                .Where(r => r.MotorcycleModelId == vehicle.MotorcycleModelId && r.MilestoneCode == "MAJOR_12000")
                .Select(r => r.Id)
                .FirstAsync();

            var existingReminders = await setupDb.MaintenanceReminders
                .IgnoreQueryFilters()
                .Where(r => r.CustomerVehicleId == overlapVehicleId)
                .ToListAsync();

            if (existingReminders.Count > 0)
            {
                setupDb.MaintenanceReminders.RemoveRange(existingReminders);
            }

            await setupDb.SaveChangesAsync();
        }

        var expectedMajorDedupKey = $"{overlapVin}:{major12000RuleId}:12000";

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var scanner = scope.ServiceProvider.GetRequiredService<IVinMaintenanceReminderScanner>();
            _ = await scanner.ScanAndDispatchDueRemindersAsync();
        }

        await using (var verifyDb = _fixture.CreateScopedDbContext())
        {
            var reminders = await verifyDb.MaintenanceReminders
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.CustomerVehicleId == overlapVehicleId)
                .ToListAsync();

            // Exactly 1 reminder created (MAJOR_12000), NOT 2 simultaneous reminders with PERIODIC_4000!
            Assert.Single(reminders);
            Assert.Equal(major12000RuleId, reminders[0].MaintenanceRuleId);
            Assert.Equal(12_000, reminders[0].TargetMileageKm);
            Assert.Equal(expectedMajorDedupKey, reminders[0].DeduplicationKey);
        }
    }

    [Theory]
    [InlineData(0, 800, 20, "BREAKIN_1000", 1_000, "MileageThreshold")]          // Exact break-in lead boundary (1000 - max(200,300)=700..1000)
    [InlineData(0, 300, 10, "BREAKIN_1000", 1_000, "DateThreshold")]             // Date lead window (within 14 days of 1-month break-in)
    [InlineData(0, 1_050, 20, "BREAKIN_1000", 1_000, "Overdue")]                 // Mileage overdue on break-in
    [InlineData(1_000, 3_699, 90, null, 0, null)]                                // 1 km below 3,700 km lead threshold & 90 days out -> not due
    [InlineData(1_000, 3_700, 90, "PERIODIC_4000", 4_000, "MileageThreshold")]   // Exact 300 km lead boundary -> due
    [InlineData(1_000, 2_500, 14, "PERIODIC_4000", 4_000, "DateThreshold")]      // Exact 14-day calendar lead boundary -> due
    [InlineData(1_000, 2_500, -1, "PERIODIC_4000", 4_000, "Overdue")]            // Calendar overdue -> Overdue
    [InlineData(8_000, 11_600, 120, "MAJOR_12000", 12_000, "MileageThreshold")]  // 12,000 km tie-break (MAJOR_12000 wins + 400 km rule lead)
    [InlineData(8_000, 11_599, 120, null, 0, null)]                              // 1 km below MAJOR_12000 400 km lead -> not due
    public void SelectNextDueMilestoneForVehicle_ShouldEvaluateMilestonesTieBreaksAndLeadBoundariesDeterministically(
        int lastServiceMileageKm,
        int currentMileageKm,
        int daysUntilNextDueDate,
        string? expectedMilestoneCode,
        int expectedTargetKm,
        string? expectedTriggerReason)
    {
        var utcNow = new DateTimeOffset(2026, 11, 22, 8, 0, 0, TimeSpan.Zero);
        var modelId = Guid.Parse("55555555-5555-5555-5555-555555555502");
        var breakInRule = new MaintenanceRule(Guid.Parse("10000000-0000-0000-0000-000000000001"))
        {
            MotorcycleModelId = modelId,
            MilestoneCode = "BREAKIN_1000",
            MilestoneName = "Initial 1,000 km Break-In",
            IntervalKm = 1_000,
            IntervalMonths = 1,
            IsRecurring = false,
            LeadMileageKm = 200,
            LeadTimeDays = 7,
            IsActive = true
        };
        var periodicRule = new MaintenanceRule(Guid.Parse("10000000-0000-0000-0000-000000000002"))
        {
            MotorcycleModelId = modelId,
            MilestoneCode = "PERIODIC_4000",
            MilestoneName = "4,000 km Periodic Maintenance",
            IntervalKm = 4_000,
            IntervalMonths = 6,
            IsRecurring = true,
            LeadMileageKm = 250,
            LeadTimeDays = 7,
            IsActive = true
        };
        var majorRule = new MaintenanceRule(Guid.Parse("10000000-0000-0000-0000-000000000003"))
        {
            MotorcycleModelId = modelId,
            MilestoneCode = "MAJOR_12000",
            MilestoneName = "12,000 km Major Overhaul",
            IntervalKm = 12_000,
            IntervalMonths = 12,
            IsRecurring = true,
            LeadMileageKm = 400,
            LeadTimeDays = 14,
            IsActive = true
        };

        var rules = new[] { breakInRule, periodicRule, majorRule };
        var computedTargetForDueOverride = lastServiceMileageKm == 0
            ? 1_000
            : ((lastServiceMileageKm / 4_000) + 1) * 4_000;
        var intervalMonthsForTarget = lastServiceMileageKm == 0 ? 1 : (lastServiceMileageKm >= 8_000 ? 12 : 6);
        var anchorDateUtc = utcNow.AddDays(daysUntilNextDueDate).AddMonths(-intervalMonthsForTarget);

        var vehicle = new CustomerVehicle
        {
            VIN = "RLHJF5109RZ104829",
            CustomerId = Guid.CreateVersion7(),
            MotorcycleModelId = modelId,
            OwnershipStartDateUtc = lastServiceMileageKm == 0 ? anchorDateUtc : utcNow.AddMonths(-12),
            LastServiceDateUtc = lastServiceMileageKm == 0 ? null : anchorDateUtc,
            LastServiceMileageKm = lastServiceMileageKm,
            CurrentMileageKm = currentMileageKm,
            NextDueMileageKm = computedTargetForDueOverride,
            NextDueDateUtc = utcNow.AddDays(daysUntilNextDueDate),
            IsActive = true
        };

        var evaluation = VinMaintenanceReminderScanner.SelectNextDueMilestoneForVehicle(
            vehicle,
            rules,
            utcNow,
            defaultMileageLeadKm: 300,
            defaultDaysLeadWindow: 14);

        if (expectedMilestoneCode is null)
        {
            Assert.Null(evaluation);
            return;
        }

        Assert.NotNull(evaluation);
        Assert.Equal(expectedMilestoneCode, evaluation.Rule.MilestoneCode);
        Assert.Equal(expectedTargetKm, evaluation.TargetMileageKm);
        Assert.Equal(expectedTriggerReason, evaluation.TriggerReason);
        Assert.Equal($"{vehicle.VIN}:{evaluation.Rule.Id}:{expectedTargetKm}", evaluation.DeduplicationKey);
    }

    private async Task<List<(HttpClient Client, Guid CustomerId, Guid VehicleId, string Vin)>> ProvisionDistinctCustomerRidersAsync(
        int riderCount,
        string seedPrefix)
    {
        Guid modelId;
        await using (var db = _fixture.CreateScopedDbContext())
        {
            modelId = await db.MotorcycleModels
                .IgnoreQueryFilters()
                .Select(m => m.Id)
                .FirstAsync();
        }

        var riders = new List<(HttpClient Client, Guid CustomerId, Guid VehicleId, string Vin)>(riderCount);
        var utcNow = DateTimeOffset.UtcNow;

        await using var writeDb = _fixture.CreateScopedDbContext();
        for (var i = 0; i < riderCount; i++)
        {
            var client = _fixture.CreateClient();
            var suffix = Random.Shared.Next(100000, 999999);
            var registerRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
            {
                Email = $"rider.{seedPrefix.ToLowerInvariant()}.{i}.{suffix}@customer.vn",
                Password = "Customer@123!",
                FullName = $"Concurrent Rider {seedPrefix}-{i}",
                PhoneNumber = $"096{i:D1}{suffix:D6}"
            });

            Assert.Equal(HttpStatusCode.Created, registerRes.StatusCode);
            var authEnv = await registerRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
            Assert.NotNull(authEnv);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authEnv.Data.AccessToken);

            // Generate valid 17-char ISO 3779 VIN (excluding I, O, Q)
            var vinTail = $"{i:D2}{suffix:D6}"; // 8 digits
            var prefixClean = seedPrefix.PadRight(4, 'X')[..4].Replace('I', 'A').Replace('O', 'B').Replace('Q', 'C');
            var vin = $"RLH{prefixClean}9R{vinTail}"; // 3 + 4 + 2 + 8 = 17 chars

            var vehicle = new CustomerVehicle
            {
                VIN = vin,
                CustomerId = authEnv.Data.User.UserId,
                MotorcycleModelId = modelId,
                LicensePlate = $"59-Z{i} {suffix.ToString()[..3]}.{suffix.ToString()[3..5]}",
                ColorName = "Matte Black",
                RegistrationSource = "ExternalManual",
                OwnershipStartDateUtc = utcNow.AddMonths(-2),
                CurrentMileageKm = 1_200,
                LastServiceMileageKm = 1_000,
                LastServiceDateUtc = utcNow.AddMonths(-1),
                NextDueMileageKm = 4_000,
                NextDueDateUtc = utcNow.AddMonths(5),
                IsActive = true,
                CreatedAtUtc = utcNow
            };

            writeDb.CustomerVehicles.Add(vehicle);
            riders.Add((client, authEnv.Data.User.UserId, vehicle.Id, vin));
        }

        await writeDb.SaveChangesAsync();
        return riders;
    }

    private static async Task<AuthTokenData> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
        Assert.NotNull(envelope);
        return envelope.Data;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? jsonBody = null)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (jsonBody is not null)
        {
            request.Content = JsonContent.Create(jsonBody);
        }

        return request;
    }
}
