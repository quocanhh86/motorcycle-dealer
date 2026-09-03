using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
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
using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.SalesOrders;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.TestDrives;

public sealed class LocalDbTestDriveAndSparePartsWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbTestDriveAndSparePartsWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_Forensic_TestDriveParts_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={TestDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

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
        using (var poolConn = new SqlConnection(ConnectionString))
        {
            SqlConnection.ClearPool(poolConn);
        }

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

public sealed class TestDriveAndSparePartsIntegrationTests : IClassFixture<LocalDbTestDriveAndSparePartsWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid SilverVehicleSh160Id = Guid.Parse("99999999-9999-9999-9999-999999999901");
    private static readonly Guid SeededHcmInvoiceId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01");

    private readonly LocalDbTestDriveAndSparePartsWebAppFixture _fixture;

    public TestDriveAndSparePartsIntegrationTests(LocalDbTestDriveAndSparePartsWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TestDriveLifecycle_WithLiveSignalRWebSockets_ShouldIsolateDealerGroups_AssignDemoVin_EnforceRowVersion_AndCancelCleanly()
    {
        using var custSilverClient = _fixture.CreateClient();
        using var custBronzeClient = _fixture.CreateClient();
        using var staffHcmClient = _fixture.CreateClient();
        using var staffHnClient = _fixture.CreateClient();

        var custSilverAuth = await LoginAsync(custSilverClient, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var custBronzeAuth = await LoginAsync(custBronzeClient, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var staffHcmAuth = await LoginAsync(staffHcmClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var staffHnAuth = await LoginAsync(staffHnClient, "staff.hn@motorcycledealer.vn", "Staff@123!");

        custSilverClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", custSilverAuth.AccessToken);
        custBronzeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", custBronzeAuth.AccessToken);
        staffHcmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffHcmAuth.AccessToken);
        staffHnClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffHnAuth.AccessToken);

        // 1. Verify RBAC & IDealerScoped queue isolation on GET /api/v1/test-drives
        var custDealerQueueRes = await custSilverClient.GetAsync("/api/v1/test-drives");
        await AssertProblemErrorCodeAsync(custDealerQueueRes, HttpStatusCode.Forbidden, "FORBIDDEN_PERMISSION");

        var hnQueueRes = await staffHnClient.GetAsync("/api/v1/test-drives");
        Assert.Equal(HttpStatusCode.OK, hnQueueRes.StatusCode);
        var hnQueueEnv = await hnQueueRes.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<TestDriveBookingDto>>(JsonOpts);
        Assert.NotNull(hnQueueEnv);
        Assert.NotEmpty(hnQueueEnv.Data);
        Assert.All(hnQueueEnv.Data, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));

        // 2. Lookup Honda SH160i model and available demo inventory at Saigon (DLR-HCM-01) plus mismatched Hanoi inventory
        var modelsEnv = await GetPagedDataAsync<MotorcycleModelDto>(custSilverClient, "/api/v1/motorcycles");
        var sh160Model = Assert.Single(modelsEnv.Data, m => m.ModelCode == "HND-SH160I-2026");

        var hcmInventories = await GetPagedDataAsync<DealerInventoryDto>(
            staffHcmClient,
            $"/api/v1/inventories?modelId={sh160Model.Id}&status=Available");
        var hcmDemoUnit = Assert.Single(hcmInventories.Data.Take(1));

        var hnInventories = await GetPagedDataAsync<DealerInventoryDto>(staffHnClient, "/api/v1/inventories?status=Available");
        var foreignDealerUnit = hnInventories.Data[0];

        // 3. Past date rejected with 422 PAST_TEST_DRIVE_DATE
        var vietnamToday = TestDriveValidationRules.GetVietnamToday(DateTimeOffset.UtcNow);
        var pastRes = await custSilverClient.PostAsJsonAsync("/api/v1/test-drives", new CreateTestDriveRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            MotorcycleModelId = sh160Model.Id,
            PreferredDate = vietnamToday.AddDays(-1),
            StartTime = "09:00",
            EndTime = "09:45",
            DriverLicenseNumber = "A1-791234567",
            ContactPhone = "0913000201"
        });
        await AssertProblemErrorCodeAsync(pastRes, HttpStatusCode.UnprocessableEntity, "PAST_TEST_DRIVE_DATE");

        // 4. Connect live SignalR WebSockets for staff.hcm, staff.hn, and customer.silver
        await using var staffHcmHub = CreateHubConnection(staffHcmAuth.AccessToken);
        await using var staffHnHub = CreateHubConnection(staffHnAuth.AccessToken);
        await using var custSilverHub = CreateHubConnection(custSilverAuth.AccessToken);

        var hcmCreatedTcs = new TaskCompletionSource<TestDriveCreatedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custConfirmedTcs = new TaskCompletionSource<TestDriveStatusChangedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custCompletedTcs = new TaskCompletionSource<TestDriveStatusChangedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hnCrossTenantEvents = new ConcurrentQueue<string>();

        staffHcmHub.On<TestDriveCreatedRealtimeEvent>("TestDriveCreated", evt => hcmCreatedTcs.TrySetResult(evt));
        staffHnHub.On<TestDriveCreatedRealtimeEvent>("TestDriveCreated", evt => hnCrossTenantEvents.Enqueue($"Created:{evt.BookingNumber}"));
        staffHnHub.On<TestDriveStatusChangedRealtimeEvent>("TestDriveStatusChanged", evt => hnCrossTenantEvents.Enqueue($"Status:{evt.BookingNumber}:{evt.NewStatus}"));

        custSilverHub.On<TestDriveStatusChangedRealtimeEvent>("TestDriveStatusChanged", evt =>
        {
            if (string.Equals(evt.NewStatus, "Confirmed", StringComparison.Ordinal))
            {
                custConfirmedTcs.TrySetResult(evt);
            }
            else if (string.Equals(evt.NewStatus, "Completed", StringComparison.Ordinal))
            {
                custCompletedTcs.TrySetResult(evt);
            }
        });

        await Task.WhenAll(
            StartAndWaitGroupEnrollmentAsync(staffHcmHub),
            StartAndWaitGroupEnrollmentAsync(staffHnHub),
            StartAndWaitGroupEnrollmentAsync(custSilverHub));

        // 5. Customer schedules test-drive at DLR-HCM-01 -> staff.hcm receives TestDriveCreated; staff.hn receives 0!
        var targetDate = vietnamToday.AddDays(12);
        var createRes = await custSilverClient.PostAsJsonAsync("/api/v1/test-drives", new CreateTestDriveRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            MotorcycleModelId = sh160Model.Id,
            PreferredDate = targetDate,
            StartTime = "09:00",
            EndTime = "09:45",
            DriverLicenseNumber = "A1-791234567",
            ContactPhone = "0913000201",
            CustomerNotes = "Testing SH160i ABS braking response."
        });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createRes.Headers.Location);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiEnvelope<TestDriveBookingDto>>(JsonOpts))!.Data;
        Assert.StartsWith("TD-", created.BookingNumber, StringComparison.Ordinal);
        Assert.Equal("Requested", created.Status);
        Assert.NotEmpty(created.RowVersion);

        var createdWsEvt = await hcmCreatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(created.TestDriveId, createdWsEvt.TestDriveId);
        Assert.Equal(DatabaseSeeder.HcmDealerId, createdWsEvt.DealerId);

        await Task.Delay(200);
        Assert.Empty(hnCrossTenantEvents);

        // 6. Foreign staff (staff.hn) & foreign customer (customer.bronze) blocked by IDealerScoped / ownership
        var foreignStaffPatch = await staffHnClient.PatchAsJsonAsync($"/api/v1/test-drives/{created.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Confirmed",
            RowVersion = created.RowVersion
        });
        await AssertProblemErrorCodeAsync(foreignStaffPatch, HttpStatusCode.Forbidden, "FORBIDDEN_DEALER_SCOPE");

        var foreignCustGet = await custBronzeClient.GetAsync($"/api/v1/test-drives/{created.TestDriveId}");
        await AssertProblemErrorCodeAsync(foreignCustGet, HttpStatusCode.Forbidden, "FORBIDDEN_RESOURCE_OWNER");

        // 7. Assigning foreign dealer's inventory unit OR a Reserved inventory unit rejected with 422 INVALID_DEMO_VEHICLE
        var invalidDemoRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/test-drives/{created.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Confirmed",
            AssignedInventoryId = foreignDealerUnit.Id,
            RowVersion = created.RowVersion
        });
        await AssertProblemErrorCodeAsync(invalidDemoRes, HttpStatusCode.UnprocessableEntity, "INVALID_DEMO_VEHICLE");

        var intakeReservedRes = await staffHcmClient.PostAsJsonAsync("/api/v1/inventories", new CreateDealerInventoryRequest
        {
            MotorcycleModelId = sh160Model.Id,
            Vin = "RLHJF5109RZ884411",
            EngineNumber = "ENG-TD-884411",
            ColorName = "Matte Titanium",
            ManufactureYear = 2026,
            ListedPrice = 102_500_000m
        });
        Assert.Equal(HttpStatusCode.Created, intakeReservedRes.StatusCode);
        var intakenUnit = (await intakeReservedRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerInventoryDto>>(JsonOpts))!.Data;

        var markReservedRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/inventories/{intakenUnit.Id}", new UpdateDealerInventoryRequest
        {
            StockStatus = "Reserved",
            RowVersion = intakenUnit.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, markReservedRes.StatusCode);

        var reservedDemoRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/test-drives/{created.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Confirmed",
            AssignedInventoryId = intakenUnit.Id,
            RowVersion = created.RowVersion
        });
        await AssertProblemErrorCodeAsync(reservedDemoRes, HttpStatusCode.UnprocessableEntity, "INVALID_DEMO_VEHICLE");

        // 8. Saigon staff confirms with valid demo VIN -> Customer receives TestDriveStatusChanged(Confirmed) over WebSocket
        var confirmRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/test-drives/{created.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Confirmed",
            AssignedInventoryId = hcmDemoUnit.Id,
            StaffNotes = "Demo SH160i prepped at showroom bay.",
            RowVersion = created.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);
        var confirmed = (await confirmRes.Content.ReadFromJsonAsync<ApiEnvelope<TestDriveBookingDto>>(JsonOpts))!.Data;
        Assert.Equal("Confirmed", confirmed.Status);
        Assert.Equal(hcmDemoUnit.Vin, confirmed.AssignedVin);

        var confirmedWsEvt = await custConfirmedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(created.TestDriveId, confirmedWsEvt.TestDriveId);
        Assert.Equal("Confirmed", confirmedWsEvt.NewStatus);
        Assert.Equal(hcmDemoUnit.Vin, confirmedWsEvt.AssignedVin);

        // 8b. Active Confirmed Demo VIN guards (F-XP-02 & F-XP-03):
        //     - Transitioning assigned demo unit to Reserved -> 409 INVENTORY_ASSIGNED_TO_TEST_DRIVE
        var reserveAssignedDemoRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/inventories/{hcmDemoUnit.Id}", new UpdateDealerInventoryRequest
        {
            StockStatus = "Reserved",
            RowVersion = hcmDemoUnit.RowVersion
        });
        await AssertProblemErrorCodeAsync(reserveAssignedDemoRes, HttpStatusCode.Conflict, "INVENTORY_ASSIGNED_TO_TEST_DRIVE");

        //     - Selling assigned demo unit to a DIFFERENT customer (customer.bronze) -> 409 INVENTORY_ASSIGNED_TO_TEST_DRIVE
        var otherCustomerSaleRes = await staffHcmClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest
        {
            CustomerId = custBronzeAuth.User.UserId,
            DealerInventoryId = hcmDemoUnit.Id,
            InventoryRowVersion = hcmDemoUnit.RowVersion,
            PaymentMethod = "BankTransfer",
            Notes = "Attempted sale of active demo bike to another rider."
        });
        await AssertProblemErrorCodeAsync(otherCustomerSaleRes, HttpStatusCode.Conflict, "INVENTORY_ASSIGNED_TO_TEST_DRIVE");

        //     - Assigning same demo VIN to another overlapping Confirmed test-drive (09:00-09:45 vs 09:30-10:15) -> 409 DEMO_VIN_TIME_CONFLICT,
        //       whereas a non-overlapping window (10:00-10:45) succeeds with 200 OK!
        var secondBookingRes = await custBronzeClient.PostAsJsonAsync("/api/v1/test-drives", new CreateTestDriveRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            MotorcycleModelId = sh160Model.Id,
            PreferredDate = targetDate,
            StartTime = "09:30",
            EndTime = "10:15",
            DriverLicenseNumber = "A1-790099881",
            ContactPhone = "0913000299"
        });
        Assert.Equal(HttpStatusCode.Created, secondBookingRes.StatusCode);
        var secondBooking = (await secondBookingRes.Content.ReadFromJsonAsync<ApiEnvelope<TestDriveBookingDto>>(JsonOpts))!.Data;

        var overlapDemoVinRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/test-drives/{secondBooking.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Confirmed",
            AssignedInventoryId = hcmDemoUnit.Id,
            RowVersion = secondBooking.RowVersion
        });
        await AssertProblemErrorCodeAsync(overlapDemoVinRes, HttpStatusCode.Conflict, "DEMO_VIN_TIME_CONFLICT");

        var cleanupSecondRes = await custBronzeClient.PostAsJsonAsync($"/api/v1/test-drives/{secondBooking.TestDriveId}/cancel", new CancelTestDriveRequest
        {
            Reason = "Cancelling overlapping demo VIN contention test booking",
            RowVersion = secondBooking.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, cleanupSecondRes.StatusCode);

        var nonOverlapBookingRes = await custBronzeClient.PostAsJsonAsync("/api/v1/test-drives", new CreateTestDriveRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            MotorcycleModelId = sh160Model.Id,
            PreferredDate = targetDate,
            StartTime = "10:00",
            EndTime = "10:45",
            DriverLicenseNumber = "A1-790099882",
            ContactPhone = "0913000299"
        });
        Assert.Equal(HttpStatusCode.Created, nonOverlapBookingRes.StatusCode);
        var nonOverlapBooking = (await nonOverlapBookingRes.Content.ReadFromJsonAsync<ApiEnvelope<TestDriveBookingDto>>(JsonOpts))!.Data;

        var nonOverlapConfirmRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/test-drives/{nonOverlapBooking.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Confirmed",
            AssignedInventoryId = hcmDemoUnit.Id,
            RowVersion = nonOverlapBooking.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, nonOverlapConfirmRes.StatusCode);
        var confirmedNonOverlap = (await nonOverlapConfirmRes.Content.ReadFromJsonAsync<ApiEnvelope<TestDriveBookingDto>>(JsonOpts))!.Data;
        Assert.Equal("Confirmed", confirmedNonOverlap.Status);
        Assert.Equal(hcmDemoUnit.Id, confirmedNonOverlap.AssignedInventoryId);

        var cleanupNonOverlapRes = await custBronzeClient.PostAsJsonAsync($"/api/v1/test-drives/{confirmedNonOverlap.TestDriveId}/cancel", new CancelTestDriveRequest
        {
            Reason = "Releasing non-overlapping demo slot so primary rider can purchase demo bike",
            RowVersion = confirmedNonOverlap.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, cleanupNonOverlapRes.StatusCode);

        // Stale RowVersion rejected with 409 CONCURRENCY_CONFLICT
        var stalePatchRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/test-drives/{created.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Completed",
            RowVersion = created.RowVersion
        });
        await AssertProblemErrorCodeAsync(stalePatchRes, HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT");

        // 9. Customer purchases the assigned demo unit (StockStatus -> Sold), then Saigon staff completes test-drive
        // preserving AssignedInventoryId -> must succeed 200 OK (F-P8-API-02) and push TestDriveStatusChanged(Completed)!
        var demoSaleRes = await staffHcmClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest
        {
            CustomerId = custSilverAuth.User.UserId,
            DealerInventoryId = hcmDemoUnit.Id,
            InventoryRowVersion = hcmDemoUnit.RowVersion,
            PaymentMethod = "BankTransfer",
            Notes = "Converted test-drive demo unit into showroom sale."
        });
        Assert.Equal(HttpStatusCode.Created, demoSaleRes.StatusCode);

        var completeRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/test-drives/{created.TestDriveId}/status", new UpdateTestDriveStatusRequest
        {
            Status = "Completed",
            AssignedInventoryId = hcmDemoUnit.Id,
            StaffNotes = "Customer purchased assigned demo unit and completed test-drive log.",
            RowVersion = confirmed.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, completeRes.StatusCode);
        var completed = (await completeRes.Content.ReadFromJsonAsync<ApiEnvelope<TestDriveBookingDto>>(JsonOpts))!.Data;
        Assert.Equal("Completed", completed.Status);
        Assert.Equal(hcmDemoUnit.Id, completed.AssignedInventoryId);
        Assert.Equal(hcmDemoUnit.Vin, completed.AssignedVin);
        Assert.NotNull(completed.CompletedAtUtc);

        var completedWsEvt = await custCompletedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Completed", completedWsEvt.NewStatus);

        // Cancelling a Completed test-drive rejected with 422 INVALID_TEST_DRIVE_STATUS
        var cancelCompletedRes = await custSilverClient.PostAsJsonAsync($"/api/v1/test-drives/{created.TestDriveId}/cancel", new CancelTestDriveRequest
        {
            Reason = "Attempting to cancel already completed test-drive",
            RowVersion = completed.RowVersion
        });
        await AssertProblemErrorCodeAsync(cancelCompletedRes, HttpStatusCode.UnprocessableEntity, "INVALID_TEST_DRIVE_STATUS");

        // Hanoi staff received 0 cross-dealer frames throughout entire lifecycle
        await Task.Delay(200);
        Assert.Empty(hnCrossTenantEvents);

        await Task.WhenAll(staffHcmHub.StopAsync(), staffHnHub.StopAsync(), custSilverHub.StopAsync());
    }

    [Fact]
    public async Task SixWayParallelTestDriveSlotRace_ShouldAllowMaxTwoActiveBookings_AndRejectCustomerOverlapWith409()
    {
        using var setupClient = _fixture.CreateClient();
        var modelsEnv = await GetPagedDataAsync<MotorcycleModelDto>(setupClient, "/api/v1/motorcycles");
        var targetModel = Assert.Single(modelsEnv.Data, m => m.ModelCode == "HND-AB160-2026");

        var vietnamToday = TestDriveValidationRules.GetVietnamToday(DateTimeOffset.UtcNow);
        var raceDate = vietnamToday.AddDays(21);

        var riders = new List<(Guid CustomerId, HttpClient Client)>(6);
        try
        {
            for (var i = 0; i < 6; i++)
            {
                var riderClient = _fixture.CreateClient();
                var suffix = $"{i}_{Guid.NewGuid():N}"[..10];
                var regRes = await riderClient.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
                {
                    Email = $"td.race.{suffix}@customer.vn",
                    Password = "Customer@123!",
                    FullName = $"TestDrive Contender {i + 1}",
                    PhoneNumber = $"0918{100000 + i}"
                });
                Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
                var auth = (await regRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts))!.Data;
                riderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
                riders.Add((auth.User.UserId, riderClient));
            }

            using var startGate = new ManualResetEventSlim(false);
            var tasks = riders.Select(async (rider, idx) =>
            {
                await Task.Yield();
                startGate.Wait(TimeSpan.FromSeconds(10));
                return (
                    Rider: rider,
                    Response: await rider.Client.PostAsJsonAsync("/api/v1/test-drives", new CreateTestDriveRequest
                    {
                        DealerId = DatabaseSeeder.HcmDealerId,
                        MotorcycleModelId = targetModel.Id,
                        PreferredDate = raceDate,
                        StartTime = "10:00",
                        EndTime = "10:45",
                        DriverLicenseNumber = $"A1-88000{idx + 10}",
                        ContactPhone = $"0918{100000 + idx}",
                        CustomerNotes = "6-way parallel test-drive slot contention"
                    }));
            }).ToArray();

            startGate.Set();
            var outcomes = await Task.WhenAll(tasks);

            var createdWinners = outcomes.Where(o => o.Response.StatusCode == HttpStatusCode.Created).ToArray();
            var rejectedContenders = outcomes.Where(o => o.Response.StatusCode == HttpStatusCode.Conflict).ToArray();

            Assert.Equal(2, createdWinners.Length);
            Assert.Equal(4, rejectedContenders.Length);

            foreach (var rejected in rejectedContenders)
            {
                await AssertProblemErrorCodeAsync(rejected.Response, HttpStatusCode.Conflict, "TEST_DRIVE_SLOT_FULL");
            }

            // Winning rider attempting overlapping window (10:15-11:00) on same PreferredDate rejected with 409 TEST_DRIVE_OVERLAP
            var winningClient = createdWinners[0].Rider.Client;
            var overlapRes = await winningClient.PostAsJsonAsync("/api/v1/test-drives", new CreateTestDriveRequest
            {
                DealerId = DatabaseSeeder.HanoiDealerId,
                MotorcycleModelId = targetModel.Id,
                PreferredDate = raceDate,
                StartTime = "10:15",
                EndTime = "11:00",
                DriverLicenseNumber = "A1-8800099",
                ContactPhone = "0918100099"
            });
            await AssertProblemErrorCodeAsync(overlapRes, HttpStatusCode.Conflict, "TEST_DRIVE_OVERLAP");

            foreach (var o in outcomes)
            {
                o.Response.Dispose();
            }
        }
        finally
        {
            foreach (var (_, client) in riders)
            {
                client.Dispose();
            }
        }
    }

    [Fact]
    public async Task SparePartsCatalog_ItemizedServiceInvoices_AppendOnlyLineItems_AndPrintableReceipts_ShouldEnforceRbacAndExactTotals()
    {
        using var anonClient = _fixture.CreateClient();
        using var custSilverClient = _fixture.CreateClient();
        using var custBronzeClient = _fixture.CreateClient();
        using var staffHcmClient = _fixture.CreateClient();
        using var staffHnClient = _fixture.CreateClient();
        using var adminClient = _fixture.CreateClient();

        var custSilverAuth = await LoginAsync(custSilverClient, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var custBronzeAuth = await LoginAsync(custBronzeClient, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var staffHcmAuth = await LoginAsync(staffHcmClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var staffHnAuth = await LoginAsync(staffHnClient, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var adminAuth = await LoginAsync(adminClient, "admin@motorcycledealer.vn", "Admin@123!");

        custSilverClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", custSilverAuth.AccessToken);
        custBronzeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", custBronzeAuth.AccessToken);
        staffHcmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffHcmAuth.AccessToken);
        staffHnClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffHnAuth.AccessToken);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        // 1. Public & filtered SpareParts catalog + Admin CRUD & RBAC
        var allPublicParts = await GetPagedDataAsync<SparePartDto>(anonClient, "/api/v1/spare-parts?pageSize=50");
        Assert.True(allPublicParts.Data.Count >= 8);

        var models = await GetPagedDataAsync<MotorcycleModelDto>(anonClient, "/api/v1/motorcycles");
        var sh160Model = Assert.Single(models.Data, m => m.ModelCode == "HND-SH160I-2026");

        var shCompatibleParts = await GetPagedDataAsync<SparePartDto>(
            anonClient,
            $"/api/v1/spare-parts?compatibleModelId={sh160Model.Id}&includeUniversal=true&pageSize=50");
        Assert.Contains(shCompatibleParts.Data, p => p.IsUniversalFit);
        Assert.All(shCompatibleParts.Data, p =>
            Assert.True(p.CompatibleModelId is null || p.CompatibleModelId == sh160Model.Id));

        // Customer & DealerStaff forbidden from creating spare parts (403)
        var custPartCreate = await custSilverClient.PostAsJsonAsync("/api/v1/spare-parts", new UpsertSparePartRequest
        {
            PartCode = "PRT-FORBID-01",
            Name = "Unauthorized Part",
            Category = "Filter",
            UnitLabel = "pc",
            UnitPriceVnd = 120_000m
        });
        await AssertProblemErrorCodeAsync(custPartCreate, HttpStatusCode.Forbidden, "FORBIDDEN_PERMISSION");

        var staffPartCreate = await staffHcmClient.PostAsJsonAsync("/api/v1/spare-parts", new UpsertSparePartRequest
        {
            PartCode = "PRT-FORBID-02",
            Name = "Unauthorized Staff Part",
            Category = "Filter",
            UnitLabel = "pc",
            UnitPriceVnd = 120_000m
        });
        await AssertProblemErrorCodeAsync(staffPartCreate, HttpStatusCode.Forbidden, "FORBIDDEN_PERMISSION");

        // Admin creates new OEM part, rejects 409 DUPLICATE_PART_CODE, toggles inactive & restores active
        var newPartCode = $"PRT-PAD-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var adminCreateRes = await adminClient.PostAsJsonAsync("/api/v1/spare-parts", new UpsertSparePartRequest
        {
            PartCode = newPartCode,
            Name = "Nissin Sintered Rear Brake Pad Set",
            Category = "Brakes",
            CompatibleModelId = sh160Model.Id,
            UnitLabel = "Set",
            UnitPriceVnd = 420_000m,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, adminCreateRes.StatusCode);
        var createdPart = (await adminCreateRes.Content.ReadFromJsonAsync<ApiEnvelope<SparePartDto>>(JsonOpts))!.Data;
        Assert.Equal(newPartCode, createdPart.PartCode);

        var dupPartRes = await adminClient.PostAsJsonAsync("/api/v1/spare-parts", new UpsertSparePartRequest
        {
            PartCode = newPartCode.ToLowerInvariant(),
            Name = "Duplicate Code Attempt",
            Category = "Brakes",
            UnitLabel = "Set",
            UnitPriceVnd = 420_000m
        });
        await AssertProblemErrorCodeAsync(dupPartRes, HttpStatusCode.Conflict, "DUPLICATE_PART_CODE");

        var deactivateRes = await adminClient.PutAsJsonAsync($"/api/v1/spare-parts/{createdPart.PartId}", new UpsertSparePartRequest
        {
            PartCode = newPartCode,
            Name = "Nissin Sintered Rear Brake Pad Set",
            Category = "Brakes",
            CompatibleModelId = sh160Model.Id,
            UnitLabel = "Set",
            UnitPriceVnd = 430_000m,
            IsActive = false
        });
        Assert.Equal(HttpStatusCode.OK, deactivateRes.StatusCode);

        var anonInactiveGet = await anonClient.GetAsync($"/api/v1/spare-parts/{createdPart.PartId}");
        Assert.Equal(HttpStatusCode.NotFound, anonInactiveGet.StatusCode);

        var adminInactiveGet = await adminClient.GetAsync($"/api/v1/spare-parts/{createdPart.PartId}");
        Assert.Equal(HttpStatusCode.OK, adminInactiveGet.StatusCode);

        var reactivateRes = await adminClient.PutAsJsonAsync($"/api/v1/spare-parts/{createdPart.PartId}", new UpsertSparePartRequest
        {
            PartCode = newPartCode,
            Name = "Nissin Sintered Rear Brake Pad Set",
            Category = "Brakes",
            CompatibleModelId = sh160Model.Id,
            UnitLabel = "Set",
            UnitPriceVnd = 430_000m,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.OK, reactivateRes.StatusCode);

        // 2. Itemized ServiceInvoice: reject 422 SPARE_PART_INCOMPATIBLE, then settle valid itemized invoice
        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(34));
        var slotRes = await staffHcmClient.PostAsJsonAsync("/api/v1/time-slots", new CreateTimeSlotRequest
        {
            SlotDate = slotDate,
            StartTime = "14:00:00",
            EndTime = "15:00:00",
            MaxCapacity = 4,
            IsBlocked = false
        });
        Assert.Equal(HttpStatusCode.Created, slotRes.StatusCode);
        var slot = (await slotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts))!.Data;

        var bookRes = await custSilverClient.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            ServiceTimeSlotId = slot.SlotId,
            SlotRowVersion = slot.RowVersion,
            CustomerVehicleId = SilverVehicleSh160Id,
            ServiceType = "PeriodicMaintenance",
            OdometerAtBookingKm = 4_100,
            ReportedNotes = "Itemized spare-parts settlement test"
        });
        Assert.Equal(HttpStatusCode.Created, bookRes.StatusCode);
        var booking = (await bookRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
        Assert.Equal(sh160Model.Id, booking.MotorcycleModelId);

        var confirmBkRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/bookings/{booking.BookingId}/status", new UpdateBookingStatusRequest
        {
            NewStatus = "Confirmed",
            RowVersion = booking.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, confirmBkRes.StatusCode);
        var confirmedBooking = (await confirmBkRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
        Assert.Equal(sh160Model.Id, confirmedBooking.MotorcycleModelId);

        // Incompatible CB300R chain kit (PRT-CHN-520VX) on SH160i scooter -> 422 SPARE_PART_INCOMPATIBLE
        var cb300ChainPart = Assert.Single(allPublicParts.Data, p => p.PartCode == "PRT-CHN-520VX");
        var oil10W30Part = Assert.Single(allPublicParts.Data, p => p.PartCode == "PRT-OIL-10W30");

        var incompatibleInvRes = await staffHcmClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            BookingRowVersion = confirmedBooking.RowVersion,
            OdometerAtCompletionKm = 4_120,
            LineItems =
            [
                new CreateServiceInvoiceLineItemRequest
                {
                    SparePartId = cb300ChainPart.PartId,
                    ItemType = "Part",
                    Quantity = 1
                }
            ]
        });
        await AssertProblemErrorCodeAsync(incompatibleInvRes, HttpStatusCode.UnprocessableEntity, "SPARE_PART_INCOMPATIBLE");

        // Valid itemized settlement: 2x Oil (500,000) + 1x Rear Pad (430,000) + LaborAmount (270,000) = 1,200,000 Gross
        var settleRes = await staffHcmClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            BookingRowVersion = confirmedBooking.RowVersion,
            LaborAmount = 270_000m,
            PartsAmount = 0m,
            OdometerAtCompletionKm = 4_120,
            PaymentMethod = "Card",
            WorkSummaryNotes = "Replaced engine oil and rear sintered brake pads.",
            LineItems =
            [
                new CreateServiceInvoiceLineItemRequest
                {
                    SparePartId = oil10W30Part.PartId,
                    ItemType = "Part",
                    Quantity = 2
                },
                new CreateServiceInvoiceLineItemRequest
                {
                    SparePartId = createdPart.PartId,
                    ItemType = "Part",
                    Quantity = 1
                }
            ]
        });
        Assert.Equal(HttpStatusCode.Created, settleRes.StatusCode);
        var settledInvoice = (await settleRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!.Data;

        Assert.Equal(930_000m, settledInvoice.PartsAmount);
        Assert.Equal(270_000m, settledInvoice.LaborAmount);
        Assert.Equal(1_200_000m, settledInvoice.GrossAmount);
        Assert.NotNull(settledInvoice.LineItems);
        Assert.Equal(3, settledInvoice.LineItems!.Count);
        Assert.Equal(settledInvoice.GrossAmount, settledInvoice.LineItems.Sum(li => li.LineTotalVnd));

        // Verify AppendOnlyLedgerInterceptor blocks mutation/deletion of ServiceInvoiceLineItems
        await using (var db = _fixture.CreateScopedDbContext())
        {
            var persistedLine = await db.ServiceInvoiceLineItems
                .FirstAsync(li => li.ServiceInvoiceId == settledInvoice.InvoiceId && li.LineNumber == 1);
            persistedLine.Quantity = 99;
            await Assert.ThrowsAsync<DomainException>(() => db.SaveChangesAsync());
        }

        await using (var dbDelete = _fixture.CreateScopedDbContext())
        {
            var persistedLine = await dbDelete.ServiceInvoiceLineItems
                .FirstAsync(li => li.ServiceInvoiceId == settledInvoice.InvoiceId && li.LineNumber == 1);
            dbDelete.Entry(persistedLine).State = EntityState.Deleted;
            await Assert.ThrowsAsync<DomainException>(() => dbDelete.SaveChangesAsync());
        }

        // 3. Printable Service Invoice Receipt & Printable Sales Order Receipt + Tenant/Owner RBAC
        var unauthReceiptRes = await anonClient.GetAsync($"/api/v1/service-invoices/{settledInvoice.InvoiceId}/receipt");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthReceiptRes.StatusCode);

        var ownerSvcReceiptRes = await custSilverClient.GetAsync($"/api/v1/service-invoices/{settledInvoice.InvoiceId}/receipt");
        Assert.Equal(HttpStatusCode.OK, ownerSvcReceiptRes.StatusCode);
        var svcReceipt = (await ownerSvcReceiptRes.Content.ReadFromJsonAsync<ApiEnvelope<PrintableServiceReceiptDto>>(JsonOpts))!.Data;
        Assert.Equal($"RCP-SVC-{settledInvoice.InvoiceNumber}", svcReceipt.ReceiptNumber);
        Assert.Equal(16, svcReceipt.VerificationCode.Length);
        Assert.Equal(3, svcReceipt.LineItems.Count);
        Assert.Equal(settledInvoice.GrossAmount, svcReceipt.LineItems.Sum(li => li.LineTotalVnd));
        Assert.Equal("DLR-HCM-01", svcReceipt.Dealer.Code);

        var scopedStaffSvcReceipt = await staffHcmClient.GetAsync($"/api/v1/service-invoices/{SeededHcmInvoiceId}/receipt");
        Assert.Equal(HttpStatusCode.OK, scopedStaffSvcReceipt.StatusCode);

        var foreignCustSvcReceipt = await custBronzeClient.GetAsync($"/api/v1/service-invoices/{settledInvoice.InvoiceId}/receipt");
        await AssertProblemErrorCodeAsync(foreignCustSvcReceipt, HttpStatusCode.Forbidden, "FORBIDDEN_RESOURCE_OWNER");

        var foreignStaffSvcReceipt = await staffHnClient.GetAsync($"/api/v1/service-invoices/{settledInvoice.InvoiceId}/receipt");
        await AssertProblemErrorCodeAsync(foreignStaffSvcReceipt, HttpStatusCode.Forbidden, "FORBIDDEN_DEALER_SCOPE");

        // SalesOrder receipt RBAC (customer.silver owns seeded SH160i order at DLR-HCM-01)
        var myOrders = await GetPagedDataAsync<SalesOrderDto>(custSilverClient, "/api/v1/sales-orders/my");
        var silverOrder = Assert.Single(myOrders.Data.Take(1));

        var ownerSalesReceiptRes = await custSilverClient.GetAsync($"/api/v1/sales-orders/{silverOrder.SalesOrderId}/receipt");
        Assert.Equal(HttpStatusCode.OK, ownerSalesReceiptRes.StatusCode);
        var salesReceipt = (await ownerSalesReceiptRes.Content.ReadFromJsonAsync<ApiEnvelope<PrintableSalesOrderReceiptDto>>(JsonOpts))!.Data;
        Assert.Equal($"RCP-SLS-{silverOrder.OrderNumber}", salesReceipt.ReceiptNumber);
        Assert.Equal(16, salesReceipt.VerificationCode.Length);
        Assert.Equal(silverOrder.Vin, salesReceipt.Vehicle.Vin);
        Assert.NotEmpty(salesReceipt.Vehicle.EngineNumber);

        var scopedStaffSalesReceipt = await staffHcmClient.GetAsync($"/api/v1/sales-orders/{silverOrder.SalesOrderId}/receipt");
        Assert.Equal(HttpStatusCode.OK, scopedStaffSalesReceipt.StatusCode);

        var foreignCustSalesReceipt = await custBronzeClient.GetAsync($"/api/v1/sales-orders/{silverOrder.SalesOrderId}/receipt");
        await AssertProblemErrorCodeAsync(foreignCustSalesReceipt, HttpStatusCode.Forbidden, "FORBIDDEN_RESOURCE_OWNER");

        var foreignStaffSalesReceipt = await staffHnClient.GetAsync($"/api/v1/sales-orders/{silverOrder.SalesOrderId}/receipt");
        await AssertProblemErrorCodeAsync(foreignStaffSalesReceipt, HttpStatusCode.Forbidden, "FORBIDDEN_DEALER_SCOPE");
    }

    private static async Task StartAndWaitGroupEnrollmentAsync(HubConnection hub)
    {
        await hub.StartAsync();
        try
        {
            // SignalR HubConnectionHandler enters DispatchMessagesAsync strictly AFTER NotificationHub.OnConnectedAsync()
            // and all Groups.AddToGroupAsync(...) calls complete, establishing a 100% causal group-enrollment barrier.
            await hub.InvokeAsync("__sync_onconnected");
        }
        catch (HubException)
        {
            // Expected because NotificationHub intentionally exposes zero client-callable methods.
        }

        Assert.Equal(HubConnectionState.Connected, hub.State);
    }

    private HubConnection CreateHubConnection(string accessToken)
    {
        var hubUri = new Uri(
            _fixture.Server.BaseAddress,
            $"/hubs/notifications?access_token={Uri.EscapeDataString(accessToken)}");

        return new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.HttpMessageHandlerFactory = _ => _fixture.Server.CreateHandler();
                options.WebSocketFactory = async (ctx, ct) =>
                {
                    var wsClient = _fixture.Server.CreateWebSocketClient();
                    return await wsClient.ConnectAsync(ctx.Uri, ct);
                };
            })
            .Build();
    }

    private static async Task<AuthTokenData> LoginAsync(HttpClient client, string email, string password)
    {
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
        Assert.NotNull(env);
        return env.Data;
    }

    private static async Task<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<T>> GetPagedDataAsync<T>(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<T>>(JsonOpts);
        Assert.NotNull(env);
        return env;
    }

    private static async Task AssertProblemErrorCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedErrorCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType ?? string.Empty);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actualCode = doc.RootElement.TryGetProperty("errorCode", out var codeProp)
            ? codeProp.GetString()
            : null;
        Assert.Equal(expectedErrorCode, actualCode);
    }
}
