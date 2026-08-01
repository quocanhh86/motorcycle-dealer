using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.SalesOrders;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using Xunit;
using BookingDto = MotorcycleDealer.Application.Features.Bookings.BookingDto;
using CancelBookingRequest = MotorcycleDealer.Application.Features.Bookings.CancelBookingRequest;
using CreateServiceBookingRequest = MotorcycleDealer.Application.Features.Bookings.CreateServiceBookingRequest;

namespace MotorcycleDealer.Api.IntegrationTests.Showroom;

public sealed class RecordingNotificationPublisher : INotificationPublisher
{
    public ConcurrentQueue<(Guid CustomerId, LoyaltyPointsUpdatedRealtimeEvent Payload)> LoyaltyPointsEvents { get; } = new();
    public ConcurrentQueue<(Guid CustomerId, TierUpgradedRealtimeEvent Payload)> TierUpgradedEvents { get; } = new();

    public Task PublishBookingCreatedAsync(
        BookingCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishBookingStatusChangedAsync(
        BookingStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishMaintenanceReminderAsync(
        MaintenanceReminderRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishLoyaltyPointsUpdatedAsync(
        LoyaltyPointsUpdatedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        LoyaltyPointsEvents.Enqueue((customerId, payload));
        return Task.CompletedTask;
    }

    public Task PublishTierUpgradedAsync(
        TierUpgradedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        TierUpgradedEvents.Enqueue((customerId, payload));
        return Task.CompletedTask;
    }

    public Task PublishTestDriveCreatedAsync(
        TestDriveCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishTestDriveStatusChangedAsync(
        TestDriveStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class LocalDbPhase5ShowroomWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbPhase5ShowroomWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_P5Showroom_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={TestDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public RecordingNotificationPublisher NotificationSpy { get; } = new();

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

            services.AddShowroomAndGarageServices();
            services.RemoveAll<INotificationPublisher>();
            services.AddSingleton<INotificationPublisher>(NotificationSpy);
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

public sealed class ShowroomAndGarageIntegrationTests : IClassFixture<LocalDbPhase5ShowroomWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly LocalDbPhase5ShowroomWebAppFixture _fixture;

    public ShowroomAndGarageIntegrationTests(LocalDbPhase5ShowroomWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CatalogAndMaintenanceRules_ShouldSupportPublicFiltering_AndEnforceAdminCrudWith409OnDuplicateCode()
    {
        using var client = _fixture.CreateClient();

        // 1. Anonymous GET /api/v1/motorcycles returns seeded catalog + ordered MaintenanceRules + stock summary
        Guid sh160ModelId;
        using (var listRes = await client.GetAsync("/api/v1/motorcycles"))
        {
            Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
            var env = await listRes.Content.ReadFromJsonAsync<PagedApiEnvelope<MotorcycleModelDto>>(JsonOpts);
            Assert.NotNull(env);
            Assert.True(env.Success);
            Assert.True(env.Data.Count >= 6);

            var sh160 = Assert.Single(env.Data, m => m.ModelCode == "HND-SH160I-2026");
            sh160ModelId = sh160.Id;
            Assert.Equal(3, sh160.MaintenanceRules.Count);
            Assert.Equal([1000, 4000, 12000], sh160.MaintenanceRules.Select(r => r.IntervalKm).ToArray());
            Assert.True(sh160.AvailableStockCount >= 1);
        }

        // 2. Category + dealerId filter scopes availableStockCount to that dealer
        using (var filteredRes = await client.GetAsync($"/api/v1/motorcycles?category=Scooter&dealerId={DatabaseSeeder.HcmDealerId}"))
        {
            Assert.Equal(HttpStatusCode.OK, filteredRes.StatusCode);
            var filteredEnv = await filteredRes.Content.ReadFromJsonAsync<PagedApiEnvelope<MotorcycleModelDto>>(JsonOpts);
            Assert.NotNull(filteredEnv);
            Assert.All(filteredEnv.Data, m => Assert.Equal("Scooter", m.Category));
        }

        // 3. Model detail + maintenance-rules public endpoints
        using (var detailRes = await client.GetAsync($"/api/v1/motorcycles/{sh160ModelId}"))
        {
            Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
            var detailEnv = await detailRes.Content.ReadFromJsonAsync<ApiEnvelope<MotorcycleModelDto>>(JsonOpts);
            Assert.NotNull(detailEnv);
            Assert.Equal("HND-SH160I-2026", detailEnv.Data.ModelCode);
            Assert.NotEmpty(detailEnv.Data.DealerStockBreakdown);
        }

        // 4. Customer & DealerStaff blocked from Admin catalog / rule mutations -> 403
        var customerSession = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var staffSession = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var adminSession = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");

        var newModelReq = new UpsertMotorcycleModelRequest
        {
            ModelCode = $"HND-PCX160-{Random.Shared.Next(100, 999)}",
            Brand = "Honda",
            ModelName = "Honda PCX 160 Hybrid",
            Category = "Scooter",
            EngineDisplacementCc = 157,
            BaseRetailPrice = 89_500_000m,
            WarrantyMonths = 36,
            WarrantyMileageKm = 30_000,
            SpecificationsJson = "{\"abs\":\"Single-Channel\",\"smartKey\":true}",
            IsActive = true
        };

        foreach (var nonAdminToken in new[] { customerSession.AccessToken, staffSession.AccessToken })
        {
            using var forbiddenModelReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/motorcycles", nonAdminToken, newModelReq);
            using var forbiddenModelRes = await client.SendAsync(forbiddenModelReq);
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenModelRes.StatusCode);
        }

        // 5. Admin creates model -> 201, duplicate ModelCode -> 409 DUPLICATE_MODEL_CODE, updates model & rules -> 200
        Guid createdModelId;
        using (var createRes = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/v1/motorcycles", adminSession.AccessToken, newModelReq))
        {
            Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
            var createdEnv = await createRes.Content.ReadFromJsonAsync<ApiEnvelope<MotorcycleModelDto>>(JsonOpts);
            Assert.NotNull(createdEnv);
            createdModelId = createdEnv.Data.Id;
            Assert.Equal(newModelReq.ModelCode, createdEnv.Data.ModelCode);
        }

        using (var dupRes = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/v1/motorcycles", adminSession.AccessToken, newModelReq))
        {
            Assert.Equal(HttpStatusCode.Conflict, dupRes.StatusCode);
            Assert.Equal("DUPLICATE_MODEL_CODE", await ReadErrorCodeAsync(dupRes));
        }

        using (var updateRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/motorcycles/{createdModelId}",
            adminSession.AccessToken,
            newModelReq with { BaseRetailPrice = 91_000_000m }))
        {
            Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
            var updatedEnv = await updateRes.Content.ReadFromJsonAsync<ApiEnvelope<MotorcycleModelDto>>(JsonOpts);
            Assert.NotNull(updatedEnv);
            Assert.Equal(91_000_000m, updatedEnv.Data.BaseRetailPrice);
        }

        var newRuleReq = new UpsertMaintenanceRuleRequest
        {
            MilestoneCode = "BREAKIN_1000",
            MilestoneName = "1,000 km Initial Service",
            IntervalKm = 1_000,
            IntervalMonths = 1,
            IsRecurring = false,
            ChecklistSummary = "Initial break-in oil & inspection."
        };

        Guid createdRuleId;
        using (var createRuleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/motorcycles/{createdModelId}/maintenance-rules",
            adminSession.AccessToken,
            newRuleReq))
        {
            Assert.Equal(HttpStatusCode.Created, createRuleRes.StatusCode);
            var ruleEnv = await createRuleRes.Content.ReadFromJsonAsync<ApiEnvelope<MaintenanceRuleDto>>(JsonOpts);
            Assert.NotNull(ruleEnv);
            createdRuleId = ruleEnv.Data.Id;
        }

        using (var updateRuleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/motorcycles/{createdModelId}/maintenance-rules/{createdRuleId}",
            adminSession.AccessToken,
            newRuleReq with { IntervalKm = 1_200, IntervalMonths = 2 }))
        {
            Assert.Equal(HttpStatusCode.OK, updateRuleRes.StatusCode);
            var updatedRuleEnv = await updateRuleRes.Content.ReadFromJsonAsync<ApiEnvelope<MaintenanceRuleDto>>(JsonOpts);
            Assert.NotNull(updatedRuleEnv);
            Assert.Equal(1_200, updatedRuleEnv.Data.IntervalKm);
        }
    }

    [Fact]
    public async Task DealerVinInventory_ShouldEnforceDealerIsolation_Iso3779Validation_AndRowVersionConcurrency()
    {
        using var client = _fixture.CreateClient();
        var hnStaff = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var hcmStaff = await LoginAsync(client, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var silverCustomer = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var admin = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");

        // 1a. Without ?publicOnly=true: anonymous -> 401 UNAUTHORIZED, Customer -> 403 FORBIDDEN_PERMISSION
        using (var anonNoFlagRes = await client.GetAsync("/api/v1/inventories"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, anonNoFlagRes.StatusCode);
            Assert.Equal("UNAUTHORIZED", await ReadErrorCodeAsync(anonNoFlagRes));
        }

        using (var custNoFlagRes = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/v1/inventories", silverCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.Forbidden, custNoFlagRes.StatusCode);
            Assert.Equal("FORBIDDEN_PERMISSION", await ReadErrorCodeAsync(custNoFlagRes));
        }

        // 1b. Public inventory list (?publicOnly=true) returns ONLY Available stock with masked EngineNumber
        using (var pubRes = await client.GetAsync("/api/v1/inventories?publicOnly=true"))
        {
            Assert.Equal(HttpStatusCode.OK, pubRes.StatusCode);
            var pubEnv = await pubRes.Content.ReadFromJsonAsync<PagedApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(pubEnv);
            Assert.NotEmpty(pubEnv.Data);
            Assert.All(pubEnv.Data, item =>
            {
                Assert.Equal("Available", item.StockStatus);
                Assert.Equal(string.Empty, item.EngineNumber);
            });
        }

        using (var custPubRes = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/v1/inventories?publicOnly=true", silverCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, custPubRes.StatusCode);
            var custPubEnv = await custPubRes.Content.ReadFromJsonAsync<PagedApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(custPubEnv);
            Assert.NotEmpty(custPubEnv.Data);
            Assert.All(custPubEnv.Data, item =>
            {
                Assert.Equal("Available", item.StockStatus);
                Assert.Equal(string.Empty, item.EngineNumber);
            });
        }

        // 2. Hanoi staff sees ONLY DLR-HN-01 stock even when passing ?dealerId=HcmDealerId
        using (var hnListRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            $"/api/v1/inventories?dealerId={DatabaseSeeder.HcmDealerId}",
            hnStaff.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, hnListRes.StatusCode);
            var hnEnv = await hnListRes.Content.ReadFromJsonAsync<PagedApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(hnEnv);
            Assert.NotEmpty(hnEnv.Data);
            Assert.All(hnEnv.Data, item => Assert.Equal(DatabaseSeeder.HanoiDealerId, item.DealerId));
        }

        // 3. HCM staff list has only DLR-HCM-01 items
        DealerInventoryDto hcmAvailableUnit;
        DealerInventoryDto hcmSoldUnit;
        using (var hcmListRes = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/v1/inventories", hcmStaff.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, hcmListRes.StatusCode);
            var hcmEnv = await hcmListRes.Content.ReadFromJsonAsync<PagedApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(hcmEnv);
            Assert.All(hcmEnv.Data, item => Assert.Equal(DatabaseSeeder.HcmDealerId, item.DealerId));
            hcmAvailableUnit = hcmEnv.Data.First(i => i.StockStatus == "Available");
            hcmSoldUnit = hcmEnv.Data.First(i => i.StockStatus == "Sold");
        }

        // 4. Cross-dealer mutation by Hanoi staff against HCM inventory -> 403 FORBIDDEN_DEALER_SCOPE
        var ab160ModelId = Guid.Parse("55555555-5555-5555-5555-555555555501");
        using (var crossPostRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventories",
            hnStaff.AccessToken,
            new CreateDealerInventoryRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                MotorcycleModelId = ab160ModelId,
                Vin = "RLHJF5108RY770001",
                EngineNumber = "JF51E-7700001",
                ColorName = "Matte Black",
                ManufactureYear = 2026,
                ListedPrice = 45_500_000m
            }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, crossPostRes.StatusCode);
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", await ReadErrorCodeAsync(crossPostRes));
        }

        using (var crossPatchRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/inventories/{hcmAvailableUnit.Id}",
            hnStaff.AccessToken,
            new UpdateDealerInventoryRequest
            {
                StockStatus = "Reserved",
                RowVersion = hcmAvailableUnit.RowVersion
            }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, crossPatchRes.StatusCode);
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", await ReadErrorCodeAsync(crossPatchRes));
        }

        // 5. Invalid ISO 3779 VIN (contains 'I', 'O', or 'Q') -> 400 VALIDATION_FAILED
        using (var badVinRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventories",
            hnStaff.AccessToken,
            new CreateDealerInventoryRequest
            {
                MotorcycleModelId = ab160ModelId,
                Vin = "RLHJF5108RY00I109",
                EngineNumber = "JF51E-9900109",
                ColorName = "Matte Silver",
                ManufactureYear = 2026,
                ListedPrice = 45_000_000m
            }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, badVinRes.StatusCode);
            Assert.Equal("VALIDATION_FAILED", await ReadErrorCodeAsync(badVinRes));
        }

        // 6. Valid intake by Hanoi staff -> 201 Created, duplicate VIN (either inventory or garage) -> 409 DUPLICATE_VIN
        var uniqueDigits = Random.Shared.Next(10000, 99999);
        var validVin = $"RLHJF5108RY8{uniqueDigits}";
        DealerInventoryDto createdInventory;
        using (var intakeRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventories",
            hnStaff.AccessToken,
            new CreateDealerInventoryRequest
            {
                MotorcycleModelId = ab160ModelId,
                Vin = validVin,
                EngineNumber = $"JF51E-8{uniqueDigits}",
                ColorName = "Titanium Grey",
                ManufactureYear = 2026,
                ListedPrice = 46_000_000m
            }))
        {
            Assert.Equal(HttpStatusCode.Created, intakeRes.StatusCode);
            var intakeEnv = await intakeRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(intakeEnv);
            createdInventory = intakeEnv.Data;
            Assert.Equal(DatabaseSeeder.HanoiDealerId, createdInventory.DealerId);
            Assert.Equal("Available", createdInventory.StockStatus);
            Assert.False(string.IsNullOrWhiteSpace(createdInventory.RowVersion));
        }

        // Duplicate against already-registered external CustomerVehicle VIN (RLHNC5501RY000511) -> 409 DUPLICATE_VIN
        using (var dupGarageVinRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventories",
            admin.AccessToken,
            new CreateDealerInventoryRequest
            {
                DealerId = DatabaseSeeder.HanoiDealerId,
                MotorcycleModelId = ab160ModelId,
                Vin = "RLHNC5501RY000511",
                EngineNumber = $"JF51E-9{uniqueDigits}",
                ColorName = "Crimson Red",
                ManufactureYear = 2026,
                ListedPrice = 46_000_000m
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, dupGarageVinRes.StatusCode);
            Assert.Equal("DUPLICATE_VIN", await ReadErrorCodeAsync(dupGarageVinRes));
        }

        // 7. RowVersion optimistic concurrency: valid PATCH rotates RowVersion; stale RowVersion -> 409 CONCURRENCY_CONFLICT
        var staleRowVersion = createdInventory.RowVersion;
        using (var patchOkRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/inventories/{createdInventory.Id}",
            hnStaff.AccessToken,
            new UpdateDealerInventoryRequest
            {
                StockStatus = "Reserved",
                ListedPrice = 46_500_000m,
                RowVersion = staleRowVersion
            }))
        {
            Assert.Equal(HttpStatusCode.OK, patchOkRes.StatusCode);
            var patchEnv = await patchOkRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(patchEnv);
            Assert.Equal("Reserved", patchEnv.Data.StockStatus);
            Assert.NotEqual(staleRowVersion, patchEnv.Data.RowVersion);
        }

        using (var stalePatchRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/inventories/{createdInventory.Id}",
            hnStaff.AccessToken,
            new UpdateDealerInventoryRequest
            {
                StockStatus = "Available",
                RowVersion = staleRowVersion
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, stalePatchRes.StatusCode);
            Assert.Equal("CONCURRENCY_CONFLICT", await ReadErrorCodeAsync(stalePatchRes));
        }

        // 8. Editing an already 'Sold' unit -> 409 INVENTORY_ALREADY_SOLD
        using (var soldPatchRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/inventories/{hcmSoldUnit.Id}",
            hcmStaff.AccessToken,
            new UpdateDealerInventoryRequest
            {
                ListedPrice = 99_000_000m,
                RowVersion = hcmSoldUnit.RowVersion
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, soldPatchRes.StatusCode);
            Assert.Equal("INVENTORY_ALREADY_SOLD", await ReadErrorCodeAsync(soldPatchRes));
        }
    }

    [Fact]
    public async Task AtomicSalesOrder_ShouldMarkVinSold_OnboardCustomerVehicle_PromoteBronzeToGold_MaintainLedgerSum_AndPreventDoubleSale()
    {
        using var client = _fixture.CreateClient();
        var hnStaff = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var bronzeCustomer = await LoginAsync(client, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var initialBronzeSpentVnd = bronzeCustomer.User.LifetimeSpentVnd ?? 0m;
        var minhCustomer = await LoginAsync(client, "minh.nguyen@customer.vn", "Customer@123!");

        // Fetch target Hanoi inventory RLHJF5108RY000101 (45,000,000 VND) and RLHJF5108RY000102
        DealerInventoryDto targetUnit101;
        DealerInventoryDto raceUnit102;
        using (var invRes = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/v1/inventories?stockStatus=Available", hnStaff.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);
            var invEnv = await invRes.Content.ReadFromJsonAsync<PagedApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(invEnv);
            targetUnit101 = Assert.Single(invEnv.Data, i => i.Vin == "RLHJF5108RY000101");
            raceUnit102 = Assert.Single(invEnv.Data, i => i.Vin == "RLHJF5108RY000102");
        }

        // 1. Complete atomic SalesOrder on RLHJF5108RY000101 (45,000,000 VND -> +4,500 pts -> Bronze 120 + 4,500 = 4,620 -> Gold!)
        SalesOrderDto completedOrder;
        using (var saleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            hnStaff.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = bronzeCustomer.User.UserId,
                DealerInventoryId = targetUnit101.Id,
                InventoryRowVersion = targetUnit101.RowVersion,
                LicensePlate = "29-H1 688.01",
                PaymentMethod = "BankTransfer",
                Notes = "Phase 5 integration test atomic showroom purchase."
            }))
        {
            Assert.Equal(HttpStatusCode.Created, saleRes.StatusCode);
            var saleEnv = await saleRes.Content.ReadFromJsonAsync<ApiEnvelope<SalesOrderDto>>(JsonOpts);
            Assert.NotNull(saleEnv);
            completedOrder = saleEnv.Data;

            Assert.Equal("RLHJF5108RY000101", completedOrder.Vin);
            Assert.Equal(45_000_000m, completedOrder.TotalAmount);
            Assert.Equal(0m, completedOrder.DiscountAmount);
            Assert.Equal(45_000_000m, completedOrder.FinalAmount);
            Assert.Equal(4_500, completedOrder.PointsEarned);
            Assert.Equal("Bronze", completedOrder.AppliedTierCode);
            Assert.Equal("Gold", completedOrder.NewTierCode);
            Assert.True(completedOrder.TierUpgraded);
            Assert.Equal(4_620, completedOrder.AvailablePointsAfter);
            Assert.Equal(4_620, completedOrder.LifetimeQualifyingPointsAfter);
            Assert.NotNull(completedOrder.OnboardedCustomerVehicleId);
        }

        // 2. Verify SQL Server LocalDB state & AvailablePoints == SUM(PointsDelta) across ALL LoyaltyAccounts
        await using (var db = _fixture.CreateScopedDbContext())
        {
            var invEntity = await db.DealerInventories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(i => i.Id == targetUnit101.Id);
            Assert.Equal("Sold", invEntity.StockStatus);
            Assert.NotNull(invEntity.SoldAtUtc);

            var onboardedVehicle = await db.CustomerVehicles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(v => v.VIN == "RLHJF5108RY000101");
            Assert.Equal(bronzeCustomer.User.UserId, onboardedVehicle.CustomerId);
            Assert.Equal(completedOrder.SalesOrderId, onboardedVehicle.PurchasedSalesOrderId);
            Assert.Equal("ShowroomSale", onboardedVehicle.RegistrationSource);
            Assert.Equal(0, onboardedVehicle.CurrentMileageKm);
            Assert.Equal(1_000, onboardedVehicle.NextDueMileageKm);

            var ledgerRow = await db.PointLedgerEntries
                .AsNoTracking()
                .SingleAsync(p => p.IdempotencyKey == $"SALES_ORDER:{completedOrder.SalesOrderId}:EARN");
            Assert.Equal(4_500, ledgerRow.PointsDelta);
            Assert.Equal(4_620, ledgerRow.BalanceAfter);

            await AssertGlobalLedgerBalanceInvariantAsync();
        }

        // Verify post-commit SignalR events fired for customer.bronze
        Assert.Contains(
            _fixture.NotificationSpy.LoyaltyPointsEvents,
            evt => evt.CustomerId == bronzeCustomer.User.UserId && evt.Payload.PointsDelta == 4_500);
        Assert.Contains(
            _fixture.NotificationSpy.TierUpgradedEvents,
            evt => evt.CustomerId == bronzeCustomer.User.UserId &&
                   evt.Payload.PreviousTierCode == "Bronze" &&
                   evt.Payload.NewTierCode == "Gold");

        // Verify Phase 4 GET /api/v1/auth/me AND POST /api/v1/auth/refresh reflect live Phase 5 Gold tier promotion, PointsMultiplier & LifetimeSpentVnd
        using (var bronzeMeRes = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/v1/auth/me", bronzeCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, bronzeMeRes.StatusCode);
            var bronzeMeEnv = await bronzeMeRes.Content.ReadFromJsonAsync<ApiEnvelope<CurrentUserDto>>(JsonOpts);
            Assert.NotNull(bronzeMeEnv);
            Assert.Equal("Gold", bronzeMeEnv.Data.TierCode);
            Assert.Equal(1.50m, bronzeMeEnv.Data.TierMultiplier);
            Assert.Equal(1.50m, bronzeMeEnv.Data.PointsMultiplier);
            Assert.Equal(bronzeMeEnv.Data.TierMultiplier, bronzeMeEnv.Data.PointsMultiplier);
            Assert.Equal(4_620, bronzeMeEnv.Data.AvailablePoints);
            Assert.Equal(4_620, bronzeMeEnv.Data.LifetimeQualifyingPoints);
            Assert.Equal(initialBronzeSpentVnd + 45_000_000m, bronzeMeEnv.Data.LifetimeSpentVnd);
        }

        using (var bronzeRefreshRes = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = bronzeCustomer.RefreshToken }))
        {
            Assert.Equal(HttpStatusCode.OK, bronzeRefreshRes.StatusCode);
            var refreshedEnv = await bronzeRefreshRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
            Assert.NotNull(refreshedEnv);
            bronzeCustomer = refreshedEnv.Data;

            Assert.Equal("Gold", bronzeCustomer.User.TierCode);
            Assert.Equal(1.50m, bronzeCustomer.User.TierMultiplier);
            Assert.Equal(1.50m, bronzeCustomer.User.PointsMultiplier);
            Assert.Equal(bronzeCustomer.User.TierMultiplier, bronzeCustomer.User.PointsMultiplier);
            Assert.Equal(4_620, bronzeCustomer.User.AvailablePoints);
            Assert.Equal(4_620, bronzeCustomer.User.LifetimeQualifyingPoints);
            Assert.Equal(initialBronzeSpentVnd + 45_000_000m, bronzeCustomer.User.LifetimeSpentVnd);
        }

        // 3. Re-selling an already Sold inventory unit -> 409 INVENTORY_NOT_AVAILABLE
        using (var resaleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            hnStaff.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = minhCustomer.User.UserId,
                DealerInventoryId = targetUnit101.Id,
                PaymentMethod = "Cash"
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, resaleRes.StatusCode);
            Assert.Equal("INVENTORY_NOT_AVAILABLE", await ReadErrorCodeAsync(resaleRes));
        }

        // 4. Parallel Double-Sale Race on RLHJF5108RY000102 -> 1 x 201 Created, 1 x 409 Conflict
        var racePayload1 = new CreateSalesOrderRequest
        {
            CustomerId = bronzeCustomer.User.UserId,
            DealerInventoryId = raceUnit102.Id,
            InventoryRowVersion = raceUnit102.RowVersion,
            PaymentMethod = "Card"
        };
        var racePayload2 = new CreateSalesOrderRequest
        {
            CustomerId = minhCustomer.User.UserId,
            DealerInventoryId = raceUnit102.Id,
            InventoryRowVersion = raceUnit102.RowVersion,
            PaymentMethod = "BankTransfer"
        };

        using var clientA = _fixture.CreateClient();
        using var clientB = _fixture.CreateClient();
        var raceResponses = await Task.WhenAll(
            SendAuthorizedAsync(clientA, HttpMethod.Post, "/api/v1/sales-orders", hnStaff.AccessToken, racePayload1),
            SendAuthorizedAsync(clientB, HttpMethod.Post, "/api/v1/sales-orders", hnStaff.AccessToken, racePayload2));

        try
        {
            var statusCodes = raceResponses.Select(r => r.StatusCode).ToArray();
            Assert.Equal(1, statusCodes.Count(s => s == HttpStatusCode.Created));
            Assert.Equal(1, statusCodes.Count(s => s == HttpStatusCode.Conflict));
        }
        finally
        {
            foreach (var r in raceResponses)
            {
                r.Dispose();
            }
        }

        await AssertGlobalLedgerBalanceInvariantAsync();

        // 5. SalesOrder visibility scoping & historical snapshot accuracy (F-SG-01):
        // Verify GET /api/v1/sales-orders/{id} reconstructs AppliedTierCode="Bronze", AppliedMultiplier=1.00m,
        // NewTierCode="Gold", TierUpgraded=true, and exact ledger BalanceAfter even after subsequent orders!
        using (var completedDetailRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            $"/api/v1/sales-orders/{completedOrder.SalesOrderId}",
            bronzeCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, completedDetailRes.StatusCode);
            var completedDetailEnv = await completedDetailRes.Content.ReadFromJsonAsync<ApiEnvelope<SalesOrderDto>>(JsonOpts);
            Assert.NotNull(completedDetailEnv);
            Assert.Equal("Bronze", completedDetailEnv.Data.AppliedTierCode);
            Assert.Equal(1.00m, completedDetailEnv.Data.AppliedMultiplier);
            Assert.Equal("Gold", completedDetailEnv.Data.NewTierCode);
            Assert.True(completedDetailEnv.Data.TierUpgraded);
            Assert.Equal(4_620, completedDetailEnv.Data.AvailablePointsAfter);
        }

        // Also verify fresh 0-balance Bronze customer -> 45,000,000 VND sale -> GET /sales-orders/{id} reports AvailablePointsAfter == 4,500
        var freshSuffix = Random.Shared.Next(100000, 999999);
        using var freshRegRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
        {
            Email = $"fresh.bronze.{freshSuffix}@customer.vn",
            Password = "Customer@123!",
            FullName = "Fresh Bronze Buyer",
            PhoneNumber = $"0966{freshSuffix}"
        });
        Assert.Equal(HttpStatusCode.Created, freshRegRes.StatusCode);
        var freshAuth = (await freshRegRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts))!.Data;

        var adminAuth = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        DealerInventoryDto dnUnit201;
        using (var dnInvRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            $"/api/v1/inventories?dealerId={DatabaseSeeder.DaNangDealerId}&stockStatus=Available",
            adminAuth.AccessToken))
        {
            var dnInvEnv = await dnInvRes.Content.ReadFromJsonAsync<PagedApiEnvelope<DealerInventoryDto>>(JsonOpts);
            dnUnit201 = Assert.Single(dnInvEnv!.Data, i => i.Vin == "RLHJF5108RY000201");
        }

        SalesOrderDto freshSaleDto;
        using (var freshSaleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            adminAuth.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = freshAuth.User.UserId,
                DealerInventoryId = dnUnit201.Id,
                InventoryRowVersion = dnUnit201.RowVersion,
                PaymentMethod = "BankTransfer"
            }))
        {
            Assert.Equal(HttpStatusCode.Created, freshSaleRes.StatusCode);
            freshSaleDto = (await freshSaleRes.Content.ReadFromJsonAsync<ApiEnvelope<SalesOrderDto>>(JsonOpts))!.Data;
        }

        using (var freshOrderGetRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            $"/api/v1/sales-orders/{freshSaleDto.SalesOrderId}",
            freshAuth.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, freshOrderGetRes.StatusCode);
            var freshOrderGetEnv = await freshOrderGetRes.Content.ReadFromJsonAsync<ApiEnvelope<SalesOrderDto>>(JsonOpts);
            Assert.NotNull(freshOrderGetEnv);
            Assert.Equal("Bronze", freshOrderGetEnv.Data.AppliedTierCode);
            Assert.Equal(1.00m, freshOrderGetEnv.Data.AppliedMultiplier);
            Assert.Equal("Gold", freshOrderGetEnv.Data.NewTierCode);
            Assert.True(freshOrderGetEnv.Data.TierUpgraded);
            Assert.Equal(4_500, freshOrderGetEnv.Data.AvailablePointsAfter);
        }

        await AssertGlobalLedgerBalanceInvariantAsync();

        // Customer sees own orders; cross-customer detail -> 403 FORBIDDEN_RESOURCE_OWNER
        using (var bronzeOrdersRes = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/v1/sales-orders", bronzeCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, bronzeOrdersRes.StatusCode);
            var bronzeOrdersEnv = await bronzeOrdersRes.Content.ReadFromJsonAsync<PagedApiEnvelope<SalesOrderDto>>(JsonOpts);
            Assert.NotNull(bronzeOrdersEnv);
            Assert.All(bronzeOrdersEnv.Data, o => Assert.Equal(bronzeCustomer.User.UserId, o.CustomerId));
        }

        using (var crossCustomerDetailRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            $"/api/v1/sales-orders/{completedOrder.SalesOrderId}",
            minhCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.Forbidden, crossCustomerDetailRes.StatusCode);
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", await ReadErrorCodeAsync(crossCustomerDetailRes));
        }
    }

    [Fact]
    public async Task CustomerVinGarage_ShouldRegisterExternalVin_RejectDuplicatesAndShowroomStock_AndEnforceMonotonicOdometer()
    {
        using var client = _fixture.CreateClient();
        var silverCustomer = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var goldCustomer = await LoginAsync(client, "customer.gold@motorcycledealer.vn", "Customer@123!");
        var hcmStaff = await LoginAsync(client, "staff.hcm@motorcycledealer.vn", "Staff@123!");

        // 1. Customer Silver garage lists owned RLHJF5109RZ104829 with DueSoon status
        Guid silverSeededVehicleId;
        using (var garageRes = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/v1/vehicles", silverCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, garageRes.StatusCode);
            var garageEnv = await garageRes.Content.ReadFromJsonAsync<ApiEnvelope<List<CustomerVehicleDto>>>(JsonOpts);
            Assert.NotNull(garageEnv);
            var shVehicle = Assert.Single(garageEnv.Data, v => v.Vin == "RLHJF5109RZ104829");
            silverSeededVehicleId = shVehicle.Id;
            Assert.True(shVehicle.IsDueSoonOrOverdue);
        }

        // 2. Reminders & maintenance schedule endpoint returns vehicle + reminders + rules
        using (var remRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            $"/api/v1/vehicles/{silverSeededVehicleId}/reminders",
            silverCustomer.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, remRes.StatusCode);
            var remEnv = await remRes.Content.ReadFromJsonAsync<ApiEnvelope<VehicleMaintenanceScheduleDto>>(JsonOpts);
            Assert.NotNull(remEnv);
            Assert.Equal("RLHJF5109RZ104829", remEnv.Data.Vehicle.Vin);
            Assert.NotEmpty(remEnv.Data.MaintenanceRules);
            Assert.NotEmpty(remEnv.Data.Reminders);
        }

        // 3. External VIN registration (RLCUG1010PY092341) -> 201 Created, duplicate -> 409 DUPLICATE_VIN
        var exciterModelId = Guid.Parse("55555555-5555-5555-5555-555555555504");
        CustomerVehicleDto registeredExternal;
        using (var regRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/vehicles",
            silverCustomer.AccessToken,
            new RegisterExternalVehicleRequest
            {
                Vin = "RLCUG1010PY092341",
                MotorcycleModelId = exciterModelId,
                CurrentMileageKm = 2_400,
                LastServiceMileageKm = 1_000,
                LicensePlate = "59-X3 092.34",
                ColorName = "GP Racing Blue"
            }))
        {
            Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
            var regEnv = await regRes.Content.ReadFromJsonAsync<ApiEnvelope<CustomerVehicleDto>>(JsonOpts);
            Assert.NotNull(regEnv);
            registeredExternal = regEnv.Data;
            Assert.Equal("RLCUG1010PY092341", registeredExternal.Vin);
            Assert.Equal("ExternalManual", registeredExternal.RegistrationSource);
            Assert.Null(registeredExternal.PurchasedSalesOrderId);
            Assert.Equal(4_000, registeredExternal.NextDueMileageKm);
        }

        using (var dupExtRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/vehicles",
            goldCustomer.AccessToken,
            new RegisterExternalVehicleRequest
            {
                Vin = "RLCUG1010PY092341",
                MotorcycleModelId = exciterModelId,
                CurrentMileageKm = 500
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, dupExtRes.StatusCode);
            Assert.Equal("DUPLICATE_VIN", await ReadErrorCodeAsync(dupExtRes));
        }

        // 4. Attempting to self-register an unsold showroom VIN (RLHJC9204RY000309 Available at HCM) -> 409 DUPLICATE_VIN
        using (var claimShowroomVinRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/vehicles",
            silverCustomer.AccessToken,
            new RegisterExternalVehicleRequest
            {
                Vin = "RLHJC9204RY000309",
                MotorcycleModelId = exciterModelId,
                CurrentMileageKm = 10
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, claimShowroomVinRes.StatusCode);
            Assert.Equal("DUPLICATE_VIN", await ReadErrorCodeAsync(claimShowroomVinRes));
        }

        // 5. Monotonic odometer update: forward 2,400 -> 3,820 km succeeds; regression 3,820 -> 3,100 km -> 422 ODOMETER_REGRESSION
        using (var mileageUpRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/vehicles/{registeredExternal.Id}/mileage",
            silverCustomer.AccessToken,
            new UpdateVehicleMileageRequest { CurrentMileageKm = 3_820 }))
        {
            Assert.Equal(HttpStatusCode.OK, mileageUpRes.StatusCode);
            var upEnv = await mileageUpRes.Content.ReadFromJsonAsync<ApiEnvelope<CustomerVehicleDto>>(JsonOpts);
            Assert.NotNull(upEnv);
            Assert.Equal(3_820, upEnv.Data.CurrentMileageKm);
            Assert.Equal("DueSoon", upEnv.Data.MaintenanceStatus);
        }

        using (var mileageRegressRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/vehicles/{registeredExternal.Id}/mileage",
            silverCustomer.AccessToken,
            new UpdateVehicleMileageRequest { CurrentMileageKm = 3_100 }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, mileageRegressRes.StatusCode);
            Assert.Equal("ODOMETER_REGRESSION", await ReadErrorCodeAsync(mileageRegressRes));
        }

        // Empty body {} on PATCH /mileage -> 400 VALIDATION_FAILED
        using (var emptyMileageReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/vehicles/{registeredExternal.Id}/mileage"))
        {
            emptyMileageReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", silverCustomer.AccessToken);
            emptyMileageReq.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var emptyMileageRes = await client.SendAsync(emptyMileageReq);
            Assert.Equal(HttpStatusCode.BadRequest, emptyMileageRes.StatusCode);
            Assert.Equal("VALIDATION_FAILED", await ReadErrorCodeAsync(emptyMileageRes));
        }

        // 6. Cross-customer ownership guard -> 403 FORBIDDEN_RESOURCE_OWNER; Staff VIN lookup -> 200 OK
        using (var crossOwnerRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/vehicles/{registeredExternal.Id}/mileage",
            goldCustomer.AccessToken,
            new UpdateVehicleMileageRequest { CurrentMileageKm = 5_000 }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, crossOwnerRes.StatusCode);
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", await ReadErrorCodeAsync(crossOwnerRes));
        }

        using (var staffLookupRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            "/api/v1/vehicles?vin=RLCUG1010PY092341",
            hcmStaff.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, staffLookupRes.StatusCode);
            var lookupEnv = await staffLookupRes.Content.ReadFromJsonAsync<ApiEnvelope<List<CustomerVehicleDto>>>(JsonOpts);
            Assert.NotNull(lookupEnv);
            Assert.Single(lookupEnv.Data, v => v.Vin == "RLCUG1010PY092341");
        }
    }

    [Fact]
    public async Task CrossModuleVoucherDoubleSpendGuardAndInactiveDealerShowroomEnforcement_ShouldReturn409And422Deterministically()
    {
        using var client = _fixture.CreateClient();
        var admin = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        var suffix = Random.Shared.Next(10000, 99999);

        // 1. Register dedicated customer + external bike, create dedicated active branch + 2 VINs + 1 slot + 1 'All'-scope CustomerVoucher
        using var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
        {
            Email = $"voucher.guard.{suffix}@customer.vn",
            Password = "Customer@123!",
            FullName = "Voucher Guard Buyer",
            PhoneNumber = $"09381{suffix}"
        });
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        var customer = (await regRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts))!.Data;

        var dealerPayload = new UpsertDealerRequest
        {
            Code = $"DLR-VG-{suffix}"[..12],
            Name = $"Apex Moto Voucher Lab {suffix}",
            City = "Nha Trang",
            Address = "88 Tran Phu, Nha Trang",
            PhoneNumber = $"025839{suffix}",
            ContactEmail = $"vg.{suffix}@motorcycledealer.vn",
            OpeningTime = "08:00:00",
            ClosingTime = "18:00:00",
            DefaultSlotCapacity = 4,
            IsActive = true
        };

        Guid branchId;
        using (var branchRes = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/v1/dealers", admin.AccessToken, dealerPayload))
        {
            Assert.Equal(HttpStatusCode.Created, branchRes.StatusCode);
            branchId = (await branchRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerDto>>(JsonOpts))!.Data.Id;
        }

        var ab160ModelId = Guid.Parse("55555555-5555-5555-5555-555555555501");
        DealerInventoryDto unit1;
        DealerInventoryDto unit2;
        using (var inv1Res = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventories",
            admin.AccessToken,
            new CreateDealerInventoryRequest
            {
                DealerId = branchId,
                MotorcycleModelId = ab160ModelId,
                Vin = $"RLHJF5108RY5{suffix}",
                EngineNumber = $"JF51E-5{suffix}",
                ColorName = "Pearl White",
                ManufactureYear = 2026,
                ListedPrice = 45_000_000m
            }))
        {
            Assert.Equal(HttpStatusCode.Created, inv1Res.StatusCode);
            unit1 = (await inv1Res.Content.ReadFromJsonAsync<ApiEnvelope<DealerInventoryDto>>(JsonOpts))!.Data;
        }

        using (var inv2Res = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventories",
            admin.AccessToken,
            new CreateDealerInventoryRequest
            {
                DealerId = branchId,
                MotorcycleModelId = ab160ModelId,
                Vin = $"RLHJF5108RY6{suffix}",
                EngineNumber = $"JF51E-6{suffix}",
                ColorName = "Matte Black",
                ManufactureYear = 2026,
                ListedPrice = 45_000_000m
            }))
        {
            Assert.Equal(HttpStatusCode.Created, inv2Res.StatusCode);
            unit2 = (await inv2Res.Content.ReadFromJsonAsync<ApiEnvelope<DealerInventoryDto>>(JsonOpts))!.Data;
        }

        Guid externalVehicleId;
        using (var extVehRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/vehicles",
            customer.AccessToken,
            new RegisterExternalVehicleRequest
            {
                Vin = $"RLHJF5108RY7{suffix}",
                MotorcycleModelId = ab160ModelId,
                CurrentMileageKm = 1_500
            }))
        {
            Assert.Equal(HttpStatusCode.Created, extVehRes.StatusCode);
            externalVehicleId = (await extVehRes.Content.ReadFromJsonAsync<ApiEnvelope<CustomerVehicleDto>>(JsonOpts))!.Data.Id;
        }

        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(22));
        ServiceTimeSlotDto openSlot;
        using (var slotRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/time-slots",
            admin.AccessToken,
            new CreateTimeSlotRequest
            {
                DealerId = branchId,
                SlotDate = slotDate,
                StartTime = "09:00:00",
                EndTime = "10:00:00",
                MaxCapacity = 4
            }))
        {
            Assert.Equal(HttpStatusCode.Created, slotRes.StatusCode);
            openSlot = (await slotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts))!.Data;
        }

        Guid customerVoucherId;
        string initialVoucherRowVersion;
        var voucherCode = $"ALL500K-{suffix}";
        await using (var seedVoucherDb = _fixture.CreateScopedDbContext())
        {
            var loyaltyAccount = await seedVoucherDb.LoyaltyAccounts
                .FirstAsync(la => la.CustomerId == customer.User.UserId);
            var utcNow = DateTimeOffset.UtcNow;

            var catalog = new VoucherCatalog(Guid.CreateVersion7())
            {
                CodePrefix = $"ALL{suffix}",
                Title = "500K All-Channel Promo Voucher",
                Description = "Applicable to both showroom and service orders.",
                PointsCost = 100,
                DiscountType = "FixedAmount",
                DiscountValue = 500_000m,
                MinOrderAmount = 300_000m,
                ApplicableScope = "All",
                ValidDays = 30,
                MinTierId = loyaltyAccount.CurrentTierId,
                IsActive = true,
                CreatedAtUtc = utcNow
            };
            var voucher = new CustomerVoucher(Guid.CreateVersion7())
            {
                LoyaltyAccountId = loyaltyAccount.Id,
                VoucherCatalogId = catalog.Id,
                VoucherCode = voucherCode,
                PointsSpent = 100,
                Status = "Available",
                RedeemedAtUtc = utcNow,
                ExpiresAtUtc = utcNow.AddDays(30),
                CreatedAtUtc = utcNow
            };

            seedVoucherDb.VoucherCatalogs.Add(catalog);
            seedVoucherDb.CustomerVouchers.Add(voucher);
            await seedVoucherDb.SaveChangesAsync();

            customerVoucherId = voucher.Id;
            initialVoucherRowVersion = Convert.ToBase64String(voucher.RowVersion);
        }

        // 2. Lock voucher on a Workshop Booking (Status = ReservedOnBooking), then attempt SalesOrder -> 409 VOUCHER_NOT_AVAILABLE!
        BookingDto activeBooking;
        using (var bookRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/bookings",
            customer.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = branchId,
                ServiceTimeSlotId = openSlot.SlotId,
                CustomerVehicleId = externalVehicleId,
                AppliedCustomerVoucherId = customerVoucherId,
                ServiceType = "PeriodicMaintenance",
                OdometerAtBookingKm = 1_500
            }))
        {
            Assert.Equal(HttpStatusCode.Created, bookRes.StatusCode);
            activeBooking = (await bookRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;
        }

        using (var doubleSpendSaleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            admin.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = customer.User.UserId,
                DealerInventoryId = unit1.Id,
                AppliedCustomerVoucherId = customerVoucherId,
                PaymentMethod = "BankTransfer"
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, doubleSpendSaleRes.StatusCode);
            Assert.Equal("VOUCHER_NOT_AVAILABLE", await ReadErrorCodeAsync(doubleSpendSaleRes));
        }

        // 3. Cancel booking -> voucher restored to Available, but RowVersion has rotated!
        using (var cancelRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/bookings/{activeBooking.BookingId}/cancel",
            customer.AccessToken,
            new CancelBookingRequest { CancellationReason = "Use voucher on showroom purchase instead" }))
        {
            Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        }

        // 4. Malformed CustomerVoucherRowVersion -> 400 VALIDATION_FAILED; stale CustomerVoucherRowVersion -> 409 CONCURRENCY_CONFLICT
        using (var badVoucherRvRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            admin.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = customer.User.UserId,
                DealerInventoryId = unit1.Id,
                AppliedCustomerVoucherId = customerVoucherId,
                CustomerVoucherRowVersion = "AAAA",
                PaymentMethod = "BankTransfer"
            }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, badVoucherRvRes.StatusCode);
            Assert.Equal("VALIDATION_FAILED", await ReadErrorCodeAsync(badVoucherRvRes));
        }

        using (var staleVoucherRvRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            admin.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = customer.User.UserId,
                DealerInventoryId = unit1.Id,
                AppliedCustomerVoucherId = customerVoucherId,
                CustomerVoucherRowVersion = initialVoucherRowVersion,
                PaymentMethod = "BankTransfer"
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, staleVoucherRvRes.StatusCode);
            Assert.Equal("CONCURRENCY_CONFLICT", await ReadErrorCodeAsync(staleVoucherRvRes));
        }

        string currentVoucherRowVersion;
        await using (var readVoucherDb = _fixture.CreateScopedDbContext())
        {
            var freshVoucher = await readVoucherDb.CustomerVouchers
                .AsNoTracking()
                .FirstAsync(v => v.Id == customerVoucherId);
            currentVoucherRowVersion = Convert.ToBase64String(freshVoucher.RowVersion);
        }

        // 5. Apply Available voucher with current CustomerVoucherRowVersion on SalesOrder -> 201 Created (Status = Used),
        //    then verify BOTH booking and 2nd sales order reject the Used voucher with 409 VOUCHER_NOT_AVAILABLE!
        using (var validVoucherSaleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            admin.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = customer.User.UserId,
                DealerInventoryId = unit1.Id,
                InventoryRowVersion = unit1.RowVersion,
                AppliedCustomerVoucherId = customerVoucherId,
                CustomerVoucherRowVersion = currentVoucherRowVersion,
                PaymentMethod = "BankTransfer"
            }))
        {
            Assert.Equal(HttpStatusCode.Created, validVoucherSaleRes.StatusCode);
            var saleDto = (await validVoucherSaleRes.Content.ReadFromJsonAsync<ApiEnvelope<SalesOrderDto>>(JsonOpts))!.Data;
            Assert.Equal(500_000m, saleDto.DiscountAmount);
            Assert.Equal(44_500_000m, saleDto.FinalAmount);
        }

        using (var usedOnBookingRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/bookings",
            customer.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = branchId,
                ServiceTimeSlotId = openSlot.SlotId,
                CustomerVehicleId = externalVehicleId,
                AppliedCustomerVoucherId = customerVoucherId,
                ServiceType = "Inspection",
                OdometerAtBookingKm = 1_500
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, usedOnBookingRes.StatusCode);
            Assert.Equal("VOUCHER_NOT_AVAILABLE", await ReadErrorCodeAsync(usedOnBookingRes));
        }

        using (var usedOnSecondSaleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            admin.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = customer.User.UserId,
                DealerInventoryId = unit2.Id,
                AppliedCustomerVoucherId = customerVoucherId,
                PaymentMethod = "BankTransfer"
            }))
        {
            Assert.Equal(HttpStatusCode.Conflict, usedOnSecondSaleRes.StatusCode);
            Assert.Equal("VOUCHER_NOT_AVAILABLE", await ReadErrorCodeAsync(usedOnSecondSaleRes));
        }

        // 6. Deactivate branch -> POST /inventories, PATCH /inventories/{id}, and POST /sales-orders all return 422 DEALER_INACTIVE!
        using (var deactRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/dealers/{branchId}",
            admin.AccessToken,
            dealerPayload with { IsActive = false }))
        {
            Assert.Equal(HttpStatusCode.OK, deactRes.StatusCode);
        }

        using (var inactiveIntakeRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventories",
            admin.AccessToken,
            new CreateDealerInventoryRequest
            {
                DealerId = branchId,
                MotorcycleModelId = ab160ModelId,
                Vin = $"RLHJF5108RY9{suffix}",
                EngineNumber = $"JF51E-9{suffix}",
                ColorName = "Silver",
                ManufactureYear = 2026,
                ListedPrice = 45_000_000m
            }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, inactiveIntakeRes.StatusCode);
            Assert.Equal("DEALER_INACTIVE", await ReadErrorCodeAsync(inactiveIntakeRes));
        }

        using (var inactivePatchInvRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/inventories/{unit2.Id}",
            admin.AccessToken,
            new UpdateDealerInventoryRequest
            {
                StockStatus = "Reserved",
                RowVersion = unit2.RowVersion
            }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, inactivePatchInvRes.StatusCode);
            Assert.Equal("DEALER_INACTIVE", await ReadErrorCodeAsync(inactivePatchInvRes));
        }

        using (var inactiveSaleRes = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            "/api/v1/sales-orders",
            admin.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerId = customer.User.UserId,
                DealerInventoryId = unit2.Id,
                PaymentMethod = "Cash"
            }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, inactiveSaleRes.StatusCode);
            Assert.Equal("DEALER_INACTIVE", await ReadErrorCodeAsync(inactiveSaleRes));
        }

        await AssertGlobalLedgerBalanceInvariantAsync();
    }

    private async Task AssertGlobalLedgerBalanceInvariantAsync()
    {
        await using var conn = new SqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM (
                SELECT la.Id
                FROM dbo.LoyaltyAccounts la
                LEFT JOIN dbo.PointLedgerEntries ple ON ple.LoyaltyAccountId = la.Id
                GROUP BY la.Id, la.AvailablePoints
                HAVING la.AvailablePoints <> ISNULL(SUM(ple.PointsDelta), 0)
            ) AS DriftRows;
            """;
        var driftCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(0, driftCount);
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

    private static async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? jsonBody = null)
    {
        using var req = CreateAuthorizedRequest(method, requestUri, accessToken, jsonBody);
        return await client.SendAsync(req);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.TryGetProperty("errorCode", out var codeEl)
            ? codeEl.GetString()
            : null;
    }
}
