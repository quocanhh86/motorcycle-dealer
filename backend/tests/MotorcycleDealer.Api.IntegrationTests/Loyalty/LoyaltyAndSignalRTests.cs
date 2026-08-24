using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
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
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Application.Features.Vouchers;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.Loyalty;

public sealed class LocalDbLoyaltyAndSignalRWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbLoyaltyAndSignalRWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_Forensic_{Guid.NewGuid():N}";

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

public sealed class LoyaltyAndSignalRTests : IClassFixture<LocalDbLoyaltyAndSignalRWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly LocalDbLoyaltyAndSignalRWebAppFixture _fixture;

    public LoyaltyAndSignalRTests(LocalDbLoyaltyAndSignalRWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LoyaltyTiers_GetAndAdminPut_ShouldEnforceMonotonicThresholds_BronzeZeroInvariant_AndRbac()
    {
        using var anonClient = _fixture.CreateClient();
        using var customerClient = _fixture.CreateClient();
        using var adminClient = _fixture.CreateClient();

        var customerAuth = await LoginAsync(customerClient, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var adminAuth = await LoginAsync(adminClient, "admin@motorcycledealer.vn", "Admin@123!");

        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerAuth.AccessToken);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        var tiersRes = await anonClient.GetAsync("/api/v1/loyalty/tiers");
        Assert.Equal(HttpStatusCode.OK, tiersRes.StatusCode);

        var tiersEnv = await tiersRes.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<LoyaltyTierDto>>>(JsonOpts);
        Assert.NotNull(tiersEnv);
        Assert.Equal(4, tiersEnv.Data.Count);
        Assert.Equal(["Bronze", "Silver", "Gold", "Platinum"], tiersEnv.Data.Select(t => t.TierCode).ToArray());
        Assert.Equal([0, 500, 2000, 5000], tiersEnv.Data.Select(t => t.MinQualifyingPoints).ToArray());
        Assert.Equal([1.00m, 1.20m, 1.50m, 2.00m], tiersEnv.Data.Select(t => t.PointsMultiplier).ToArray());

        var bronzeTier = tiersEnv.Data[0];
        var silverTier = tiersEnv.Data[1];

        // Customer cannot mutate LoyaltyTiers (403 FORBIDDEN_PERMISSION)
        var forbiddenRes = await customerClient.PutAsJsonAsync($"/api/v1/loyalty/tiers/{silverTier.TierId}", new UpdateLoyaltyTierRequest
        {
            DisplayName = "Unauthorized Edit",
            BenefitsJson = "[\"Illegal\"]"
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenRes.StatusCode);

        // Bronze MinQualifyingPoints MUST remain 0 (422 INVALID_TIER_THRESHOLD)
        var bronzeThresholdRes = await adminClient.PutAsJsonAsync($"/api/v1/loyalty/tiers/{bronzeTier.TierId}", new UpdateLoyaltyTierRequest
        {
            DisplayName = bronzeTier.DisplayName,
            MinQualifyingPoints = 50,
            PointsMultiplier = bronzeTier.PointsMultiplier,
            BenefitsJson = bronzeTier.BenefitsJson
        });
        await AssertProblemErrorCodeAsync(bronzeThresholdRes, HttpStatusCode.UnprocessableEntity, "INVALID_TIER_THRESHOLD");

        // Monotonic ordering violation: Silver >= Gold (2000) -> 422 INVALID_TIER_THRESHOLD
        var nonMonotonicRes = await adminClient.PutAsJsonAsync($"/api/v1/loyalty/tiers/{silverTier.TierId}", new UpdateLoyaltyTierRequest
        {
            DisplayName = silverTier.DisplayName,
            MinQualifyingPoints = 2000,
            PointsMultiplier = silverTier.PointsMultiplier,
            BenefitsJson = silverTier.BenefitsJson
        });
        await AssertProblemErrorCodeAsync(nonMonotonicRes, HttpStatusCode.UnprocessableEntity, "INVALID_TIER_THRESHOLD");

        // Valid Admin update and clean restore
        var validUpdateRes = await adminClient.PutAsJsonAsync($"/api/v1/loyalty/tiers/{silverTier.TierId}", new UpdateLoyaltyTierRequest
        {
            DisplayName = "Silver Rider Club",
            MinQualifyingPoints = 500,
            PointsMultiplier = 1.20m,
            BenefitsJson = "[\"1.20x accelerated accrual\",\"Priority wash\"]"
        });
        Assert.Equal(HttpStatusCode.OK, validUpdateRes.StatusCode);

        var restoreRes = await adminClient.PutAsJsonAsync($"/api/v1/loyalty/tiers/{silverTier.TierId}", new UpdateLoyaltyTierRequest
        {
            DisplayName = silverTier.DisplayName,
            MinQualifyingPoints = silverTier.MinQualifyingPoints,
            PointsMultiplier = silverTier.PointsMultiplier,
            BenefitsJson = silverTier.BenefitsJson
        });
        Assert.Equal(HttpStatusCode.OK, restoreRes.StatusCode);
    }

    [Fact]
    public async Task FourTierProgression_TwoStepFloorMath_AndAdminAdjustmentIdempotency_ShouldMaintainZeroLedgerDrift()
    {
        using var customerClient = _fixture.CreateClient();
        using var staffClient = _fixture.CreateClient();
        using var adminClient = _fixture.CreateClient();

        var staffAuth = await LoginAsync(staffClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var adminAuth = await LoginAsync(adminClient, "admin@motorcycledealer.vn", "Admin@123!");
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffAuth.AccessToken);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        var rider = await RegisterCustomerWithVehicleAsync(customerClient, "TIERPROG");
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rider.AccessToken);

        // Initial Bronze wallet verification
        var initialWallet = await GetJsonDataAsync<LoyaltyAccountSummaryDto>(customerClient, "/api/v1/loyalty/me");
        Assert.Equal("Bronze", initialWallet.TierCode);
        Assert.Equal(1.00m, initialWallet.PointsMultiplier);
        Assert.Equal(0, initialWallet.AvailablePoints);
        Assert.Equal(0, initialWallet.LifetimeQualifyingPoints);
        Assert.Equal("Silver", initialWallet.NextTierCode);
        Assert.Equal(500, initialWallet.PointsToNextTier);

        // Step 1: Admin adjustment +455 qualifying pts + duplicate IdempotencyKey 409 check
        var step1IdempKey = $"TEST-ADJ-STEP1-{Guid.NewGuid():N}";
        var adj1Res = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = rider.CustomerId,
            PointsDelta = 455,
            QualifiesForTier = true,
            Notes = "Initial goodwill qualifying boost toward Silver threshold",
            IdempotencyKey = step1IdempKey
        });
        Assert.Equal(HttpStatusCode.Created, adj1Res.StatusCode);
        var adj1Data = (await adj1Res.Content.ReadFromJsonAsync<ApiEnvelope<LoyaltyAdjustmentResultDto>>(JsonOpts))!.Data;
        Assert.Equal(455, adj1Data.AvailablePointsAfter);
        Assert.Equal(455, adj1Data.LifetimeQualifyingPointsAfter);
        Assert.Equal("Bronze", adj1Data.NewTierCode);
        Assert.False(adj1Data.TierUpgraded);

        // Duplicate IdempotencyKey replay -> 409 DUPLICATE_IDEMPOTENCY_KEY
        var duplicateAdjRes = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = rider.CustomerId,
            PointsDelta = 455,
            QualifiesForTier = true,
            Notes = "Duplicate replay of adjustment request",
            IdempotencyKey = step1IdempKey
        });
        await AssertProblemErrorCodeAsync(duplicateAdjRes, HttpStatusCode.Conflict, "DUPLICATE_IDEMPOTENCY_KEY");

        // Step 2: ServiceInvoice #1 at Bronze (1.00x) for 450,000 VND -> +45 pts -> 455 + 45 = 500 -> Bronze -> Silver!
        var inv1 = await BookConfirmAndSettleInvoiceAsync(
            customerClient,
            staffClient,
            rider.VehicleId,
            slotDayOffset: 31,
            odometerBookingKm: 1_100,
            odometerCompletionKm: 1_110,
            laborAmount: 150_000m,
            partsAmount: 300_000m);

        Assert.Equal("Bronze", inv1.AppliedTierCode);
        Assert.Equal(1.00m, inv1.AppliedMultiplier);
        Assert.Equal(45, inv1.PointsEarned);
        Assert.Equal(500, inv1.LifetimeQualifyingPointsAfter);
        Assert.Equal("Silver", inv1.NewTierCode);
        Assert.True(inv1.TierUpgraded);

        // Step 3: Admin adjustment +1,450 qualifying pts (500 + 1,450 = 1,950, still Silver)
        var adj2Res = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = rider.CustomerId,
            PointsDelta = 1_450,
            QualifiesForTier = true,
            Notes = "Verified historical showroom campaign points",
            IdempotencyKey = $"TEST-ADJ-STEP3-{Guid.NewGuid():N}"
        });
        Assert.Equal(HttpStatusCode.Created, adj2Res.StatusCode);

        // Step 4: ServiceInvoice #2 at Silver (1.20x) for 455,000 VND -> Floor(Floor(455,000/10,000)*1.20) = Floor(45*1.20) = 54 pts -> 1,950 + 54 = 2,004 -> Silver -> Gold!
        var inv2 = await BookConfirmAndSettleInvoiceAsync(
            customerClient,
            staffClient,
            rider.VehicleId,
            slotDayOffset: 32,
            odometerBookingKm: 1_200,
            odometerCompletionKm: 1_215,
            laborAmount: 155_000m,
            partsAmount: 300_000m);

        Assert.Equal("Silver", inv2.AppliedTierCode);
        Assert.Equal(1.20m, inv2.AppliedMultiplier);
        Assert.Equal(54, inv2.PointsEarned);
        Assert.Equal(2_004, inv2.LifetimeQualifyingPointsAfter);
        Assert.Equal("Gold", inv2.NewTierCode);
        Assert.True(inv2.TierUpgraded);

        // Step 5: ServiceInvoice #3 at Gold (1.50x) for 455,000 VND -> two-step floor proof: Floor(45 * 1.50) == 67 (NOT 68!)
        var inv3 = await BookConfirmAndSettleInvoiceAsync(
            customerClient,
            staffClient,
            rider.VehicleId,
            slotDayOffset: 33,
            odometerBookingKm: 1_300,
            odometerCompletionKm: 1_320,
            laborAmount: 155_000m,
            partsAmount: 300_000m);

        Assert.Equal("Gold", inv3.AppliedTierCode);
        Assert.Equal(1.50m, inv3.AppliedMultiplier);
        Assert.Equal(67, inv3.PointsEarned);
        Assert.Equal(2_071, inv3.LifetimeQualifyingPointsAfter);
        Assert.Equal("Gold", inv3.NewTierCode);
        Assert.False(inv3.TierUpgraded);

        // Step 6: Admin adjustment +2,929 qualifying pts -> 2,071 + 2,929 = 5,000 -> Gold -> Platinum (2.00x)!
        var adj3Res = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = rider.CustomerId,
            PointsDelta = 2_929,
            QualifiesForTier = true,
            Notes = "VIP fleet milestone qualifying adjustment to Platinum",
            IdempotencyKey = $"TEST-ADJ-STEP6-{Guid.NewGuid():N}"
        });
        Assert.Equal(HttpStatusCode.Created, adj3Res.StatusCode);
        var adj3Data = (await adj3Res.Content.ReadFromJsonAsync<ApiEnvelope<LoyaltyAdjustmentResultDto>>(JsonOpts))!.Data;
        Assert.True(adj3Data.TierUpgraded);
        Assert.Equal("Gold", adj3Data.PreviousTierCode);
        Assert.Equal("Platinum", adj3Data.NewTierCode);
        Assert.Equal(2.00m, adj3Data.PointsMultiplier);
        Assert.Equal(5_000, adj3Data.LifetimeQualifyingPointsAfter);

        // Step 7: Non-qualifying goodwill credit (+100, QualifiesForTier = false) & excessive negative deduction -> 422 INSUFFICIENT_POINTS
        var nonQualRes = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = rider.CustomerId,
            PointsDelta = 100,
            QualifiesForTier = false,
            Notes = "Complimentary survey bonus (non-qualifying)",
            IdempotencyKey = $"TEST-ADJ-NONQUAL-{Guid.NewGuid():N}"
        });
        Assert.Equal(HttpStatusCode.Created, nonQualRes.StatusCode);

        var insufficientRes = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = rider.CustomerId,
            PointsDelta = -999_999,
            Notes = "Excessive reversal should be rejected cleanly"
        });
        await AssertProblemErrorCodeAsync(insufficientRes, HttpStatusCode.UnprocessableEntity, "INSUFFICIENT_POINTS");

        // Verify final Platinum wallet, paginated ledger, and DealerStaff checkout inspection
        var finalWallet = await GetJsonDataAsync<LoyaltyAccountSummaryDto>(customerClient, "/api/v1/loyalty/me");
        Assert.Equal("Platinum", finalWallet.TierCode);
        Assert.Equal(2.00m, finalWallet.PointsMultiplier);
        Assert.Equal(5_100, finalWallet.AvailablePoints);
        Assert.Equal(5_000, finalWallet.LifetimeQualifyingPoints);
        Assert.Null(finalWallet.NextTierCode);
        Assert.Equal(0, finalWallet.PointsToNextTier);
        Assert.Equal(100.00m, finalWallet.ProgressPercent);

        var ledgerRes = await customerClient.GetAsync("/api/v1/loyalty/ledger?transactionType=ServiceInvoiceEarn&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, ledgerRes.StatusCode);
        var ledgerPage = await ledgerRes.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<PointLedgerEntryDto>>(JsonOpts);
        Assert.NotNull(ledgerPage);
        Assert.Equal(3, ledgerPage.Data.Count);
        Assert.All(ledgerPage.Data, entry => Assert.Equal("ServiceInvoiceEarn", entry.TransactionType));

        var staffInspection = await GetJsonDataAsync<CustomerLoyaltyInspectionDto>(
            staffClient,
            $"/api/v1/loyalty/customers/{rider.CustomerId}");
        Assert.Equal("Platinum", staffInspection.Account.TierCode);
        Assert.Equal(5_100, staffInspection.Account.AvailablePoints);
        Assert.NotEmpty(staffInspection.RecentLedgerEntries);

        await AssertZeroLedgerDriftForCustomerAsync(rider.CustomerId);
    }

    [Fact]
    public async Task VoucherCatalogCrud_TierGate_InactiveGate_AndQuotaExhaustion_ShouldEnforceAllContracts()
    {
        using var customerClient = _fixture.CreateClient();
        using var customer2Client = _fixture.CreateClient();
        using var adminClient = _fixture.CreateClient();

        var adminAuth = await LoginAsync(adminClient, "admin@motorcycledealer.vn", "Admin@123!");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        var bronzeRider1 = await RegisterCustomerWithVehicleAsync(customerClient, "VCHGATE1");
        var bronzeRider2 = await RegisterCustomerWithVehicleAsync(customer2Client, "VCHGATE2");
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bronzeRider1.AccessToken);
        customer2Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bronzeRider2.AccessToken);

        // Credit both Bronze riders with 150 spendable non-qualifying points so they stay Bronze (0 qualifying pts)
        foreach (var custId in new[] { bronzeRider1.CustomerId, bronzeRider2.CustomerId })
        {
            var adj = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
            {
                CustomerId = custId,
                PointsDelta = 150,
                QualifiesForTier = false,
                Notes = "Promo points for Bronze voucher gate test"
            });
            Assert.Equal(HttpStatusCode.Created, adj.StatusCode);
        }

        // Create Gold-gated catalog item + verify duplicate prefix 409
        var goldPrefix = $"GLD{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var createGoldCatRes = await adminClient.PostAsJsonAsync("/api/v1/vouchers/catalog", new UpsertVoucherCatalogRequest
        {
            CodePrefix = goldPrefix,
            Title = "Gold Exclusive 20% Detailing",
            Description = "Gold-tier workshop reward",
            ApplicableScope = "ServiceOnly",
            PointsCost = 100,
            DiscountType = "Percentage",
            DiscountValue = 20m,
            MaxDiscountAmount = 250_000m,
            MinOrderAmount = 500_000m,
            ValidDays = 60,
            QuotaTotal = 10,
            MinTierCode = "Gold",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, createGoldCatRes.StatusCode);
        var goldCatalog = (await createGoldCatRes.Content.ReadFromJsonAsync<ApiEnvelope<VoucherCatalogDto>>(JsonOpts))!.Data;

        var dupPrefixRes = await adminClient.PostAsJsonAsync("/api/v1/vouchers/catalog", new UpsertVoucherCatalogRequest
        {
            CodePrefix = goldPrefix,
            Title = "Duplicate Prefix Attempt",
            ApplicableScope = "ServiceOnly",
            PointsCost = 80,
            DiscountType = "FixedAmount",
            DiscountValue = 40_000m,
            MinOrderAmount = 200_000m,
            ValidDays = 30,
            MinTierCode = "Bronze",
            IsActive = true
        });
        await AssertProblemErrorCodeAsync(dupPrefixRes, HttpStatusCode.Conflict, "DUPLICATE_VOUCHER_PREFIX");

        // Bronze rider attempting to redeem Gold-gated catalog -> 422 TIER_REQUIREMENT_NOT_MET
        var tierBlockedRes = await customerClient.PostAsJsonAsync("/api/v1/vouchers/redeem", new RedeemVoucherRequest
        {
            VoucherCatalogId = goldCatalog.Id
        });
        await AssertProblemErrorCodeAsync(tierBlockedRes, HttpStatusCode.UnprocessableEntity, "TIER_REQUIREMENT_NOT_MET");

        // Create single-unit Bronze catalog (QuotaTotal = 1), test inactive gate -> 422 VOUCHER_INACTIVE, then quota exhaustion -> 409 VOUCHER_QUOTA_EXHAUSTED
        var limPrefix = $"LIM{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var createLimRes = await adminClient.PostAsJsonAsync("/api/v1/vouchers/catalog", new UpsertVoucherCatalogRequest
        {
            CodePrefix = limPrefix,
            Title = "Flash Single-Unit Oil Voucher",
            Description = "Quota=1 test voucher",
            ApplicableScope = "ServiceOnly",
            PointsCost = 100,
            DiscountType = "FixedAmount",
            DiscountValue = 50_000m,
            MinOrderAmount = 200_000m,
            ValidDays = 45,
            QuotaTotal = 1,
            MinTierCode = "Bronze",
            IsActive = false
        });
        Assert.Equal(HttpStatusCode.Created, createLimRes.StatusCode);
        var limCatalog = (await createLimRes.Content.ReadFromJsonAsync<ApiEnvelope<VoucherCatalogDto>>(JsonOpts))!.Data;

        var inactiveRedeemRes = await customerClient.PostAsJsonAsync("/api/v1/vouchers/redeem", new RedeemVoucherRequest
        {
            VoucherCatalogId = limCatalog.Id
        });
        await AssertProblemErrorCodeAsync(inactiveRedeemRes, HttpStatusCode.UnprocessableEntity, "VOUCHER_INACTIVE");

        // Activate limited catalog
        var activateRes = await adminClient.PutAsJsonAsync($"/api/v1/vouchers/catalog/{limCatalog.Id}", new UpsertVoucherCatalogRequest
        {
            CodePrefix = limPrefix,
            Title = limCatalog.Title,
            Description = limCatalog.Description,
            ApplicableScope = limCatalog.ApplicableScope,
            PointsCost = limCatalog.PointsCost,
            DiscountType = limCatalog.DiscountType,
            DiscountValue = limCatalog.DiscountValue,
            MinOrderAmount = limCatalog.MinOrderAmount,
            ValidDays = limCatalog.ValidDays,
            QuotaTotal = 1,
            MinTierCode = "Bronze",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.OK, activateRes.StatusCode);

        // Rider 1 claims the sole unit -> 201 Created
        var claim1Res = await customerClient.PostAsJsonAsync("/api/v1/vouchers/redeem", new RedeemVoucherRequest
        {
            VoucherCatalogId = limCatalog.Id
        });
        Assert.Equal(HttpStatusCode.Created, claim1Res.StatusCode);

        // Rider 2 attempts to claim exhausted catalog -> 409 VOUCHER_QUOTA_EXHAUSTED
        var claim2Res = await customer2Client.PostAsJsonAsync("/api/v1/vouchers/redeem", new RedeemVoucherRequest
        {
            VoucherCatalogId = limCatalog.Id
        });
        await AssertProblemErrorCodeAsync(claim2Res, HttpStatusCode.Conflict, "VOUCHER_QUOTA_EXHAUSTED");
    }

    [Fact]
    public async Task TenWayParallelVoucherRedemptionRace_WhenCustomerHas200Points_ShouldAllowExactSingleWinner_PreserveLifetimePoints_AndZeroLedgerDrift()
    {
        using var adminClient = _fixture.CreateClient();
        var adminAuth = await LoginAsync(adminClient, "admin@motorcycledealer.vn", "Admin@123!");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        using var setupClient = _fixture.CreateClient();
        var raceCustomer = await RegisterCustomerWithVehicleAsync(setupClient, "RACE200");

        // Grant +600 qualifying pts (promotes to Silver) then deduct -400 -> AvailablePoints = 200, LifetimeQualifyingPoints = 600 (Silver)
        var creditRes = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = raceCustomer.CustomerId,
            PointsDelta = 600,
            QualifiesForTier = true,
            Notes = "Seed 600 qualifying Silver points for concurrency race"
        });
        Assert.Equal(HttpStatusCode.Created, creditRes.StatusCode);

        var debitRes = await adminClient.PostAsJsonAsync("/api/v1/loyalty/adjustments", new AdminPointAdjustmentRequest
        {
            CustomerId = raceCustomer.CustomerId,
            PointsDelta = -400,
            QualifiesForTier = false,
            Notes = "Leave exact 200 spendable points for 10-way voucher race"
        });
        Assert.Equal(HttpStatusCode.Created, debitRes.StatusCode);

        // Create dedicated active catalog item costing 200 points (QuotaTotal = 100) so quota is plentiful while wallet has exact 200 pts
        var raceCatalogPrefix = $"RCE{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var createCatRes = await adminClient.PostAsJsonAsync("/api/v1/vouchers/catalog", new UpsertVoucherCatalogRequest
        {
            CodePrefix = raceCatalogPrefix,
            Title = "100,000 VND Concurrency Race Maintenance Voucher",
            Description = "200-point reward for 10-way over-redemption race test",
            ApplicableScope = "ServiceOnly",
            PointsCost = 200,
            DiscountType = "FixedAmount",
            DiscountValue = 100_000m,
            MinOrderAmount = 400_000m,
            ValidDays = 90,
            QuotaTotal = 100,
            MinTierCode = "Bronze",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, createCatRes.StatusCode);
        var raceCatalog = (await createCatRes.Content.ReadFromJsonAsync<ApiEnvelope<VoucherCatalogDto>>(JsonOpts))!.Data;
        Assert.Equal(0, raceCatalog.QuotaRedeemed);

        var clients = Enumerable.Range(0, 10)
            .Select(_ =>
            {
                var client = _fixture.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", raceCustomer.AccessToken);
                return client;
            })
            .ToArray();

        try
        {
            using var startGate = new ManualResetEventSlim(false);
            var tasks = clients.Select(async client =>
            {
                await Task.Yield();
                startGate.Wait(TimeSpan.FromSeconds(10));
                return await client.PostAsJsonAsync("/api/v1/vouchers/redeem", new RedeemVoucherRequest
                {
                    VoucherCatalogId = raceCatalog.Id
                });
            }).ToArray();

            startGate.Set();
            var responses = await Task.WhenAll(tasks);

            var createdResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToArray();
            var rejectedResponses = responses
                .Where(r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
                .ToArray();

            Assert.Single(createdResponses);
            Assert.Equal(9, rejectedResponses.Length);

            var winnerVoucher = (await createdResponses[0].Content.ReadFromJsonAsync<ApiEnvelope<CustomerVoucherDto>>(JsonOpts))!.Data;
            Assert.StartsWith($"{raceCatalogPrefix}-", winnerVoucher.VoucherCode, StringComparison.Ordinal);
            var suffix = winnerVoucher.VoucherCode[(raceCatalogPrefix.Length + 1)..];
            Assert.Equal(8, suffix.Length);
            Assert.DoesNotContain('I', suffix);
            Assert.DoesNotContain('O', suffix);
            Assert.DoesNotContain('Q', suffix);
            Assert.Equal(0, winnerVoucher.AvailablePointsAfter);
            Assert.Equal(600, winnerVoucher.LifetimeQualifyingPoints);

            foreach (var r in responses)
            {
                r.Dispose();
            }

            // Reconcile relational invariants directly in SQL Server LocalDB
            await using var db = _fixture.CreateScopedDbContext();
            var reloadedAccount = await db.LoyaltyAccounts
                .AsNoTracking()
                .Include(a => a.CurrentTier)
                .SingleAsync(a => a.CustomerId == raceCustomer.CustomerId);

            Assert.Equal(0, reloadedAccount.AvailablePoints);
            Assert.Equal(600, reloadedAccount.LifetimeQualifyingPoints);
            Assert.Equal("Silver", reloadedAccount.CurrentTier!.TierCode);

            var ledgerSum = await db.PointLedgerEntries
                .AsNoTracking()
                .Where(e => e.LoyaltyAccountId == reloadedAccount.Id)
                .SumAsync(e => e.PointsDelta);

            Assert.Equal(reloadedAccount.AvailablePoints, ledgerSum);

            var reloadedCatalog = await db.VoucherCatalogs
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(c => c.Id == raceCatalog.Id);

            var actualVoucherRows = await db.CustomerVouchers
                .AsNoTracking()
                .CountAsync(v => v.VoucherCatalogId == raceCatalog.Id);

            Assert.Equal(1, reloadedCatalog.QuotaRedeemed);
            Assert.Equal(1, actualVoucherRows);
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }
    }

    [Fact]
    public async Task RealSignalRHubConnections_ShouldIsolateDealerGroups_AndDeliverBookingInvoicePointsAndTierUpgradeLive()
    {
        // 1. Unauthenticated connection must be rejected with 401
        await using (var unauthHub = CreateHubConnection(accessToken: null, useQueryStringToken: false))
        {
            await Assert.ThrowsAnyAsync<Exception>(() => unauthHub.StartAsync());
        }

        using var staffHcmClient = _fixture.CreateClient();
        using var staffHnClient = _fixture.CreateClient();
        using var custSilverClient = _fixture.CreateClient();

        var staffHcmAuth = await LoginAsync(staffHcmClient, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var staffHnAuth = await LoginAsync(staffHnClient, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var custSilverAuth = await LoginAsync(custSilverClient, "customer.silver@motorcycledealer.vn", "Customer@123!");

        staffHcmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffHcmAuth.AccessToken);
        staffHnClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffHnAuth.AccessToken);
        custSilverClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", custSilverAuth.AccessToken);

        await using var staffHcmHub = CreateHubConnection(staffHcmAuth.AccessToken, useQueryStringToken: false);
        await using var staffHnHub = CreateHubConnection(staffHnAuth.AccessToken, useQueryStringToken: true);
        await using var custSilverHub = CreateHubConnection(custSilverAuth.AccessToken, useQueryStringToken: true);

        var hcmBookingTcs = new TaskCompletionSource<BookingCreatedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hnEvents = new ConcurrentQueue<string>();

        var custStatusEvents = new ConcurrentQueue<BookingStatusChangedRealtimeEvent>();
        var custCompletedStatusTcs = new TaskCompletionSource<BookingStatusChangedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custPointsTcs = new TaskCompletionSource<LoyaltyPointsUpdatedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custTierUpgradedTcs = new TaskCompletionSource<TierUpgradedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        staffHcmHub.On<BookingCreatedRealtimeEvent>("BookingCreated", evt => hcmBookingTcs.TrySetResult(evt));

        staffHnHub.On<BookingCreatedRealtimeEvent>("BookingCreated", evt => hnEvents.Enqueue($"BookingCreated:{evt.BookingNumber}"));
        staffHnHub.On<BookingStatusChangedRealtimeEvent>("BookingStatusChanged", evt => hnEvents.Enqueue($"BookingStatusChanged:{evt.BookingNumber}"));
        staffHnHub.On<LoyaltyPointsUpdatedRealtimeEvent>("LoyaltyPointsUpdated", evt => hnEvents.Enqueue($"LoyaltyPointsUpdated:{evt.LoyaltyAccountId}"));
        staffHnHub.On<TierUpgradedRealtimeEvent>("TierUpgraded", evt => hnEvents.Enqueue($"TierUpgraded:{evt.NewTierCode}"));

        custSilverHub.On<BookingStatusChangedRealtimeEvent>("BookingStatusChanged", evt =>
        {
            custStatusEvents.Enqueue(evt);
            if (string.Equals(evt.NewStatus, "Completed", StringComparison.Ordinal))
            {
                custCompletedStatusTcs.TrySetResult(evt);
            }
        });
        custSilverHub.On<LoyaltyPointsUpdatedRealtimeEvent>("LoyaltyPointsUpdated", evt =>
        {
            if (string.Equals(evt.TransactionType, "ServiceInvoiceEarn", StringComparison.Ordinal))
            {
                custPointsTcs.TrySetResult(evt);
            }
        });
        custSilverHub.On<TierUpgradedRealtimeEvent>("TierUpgraded", evt => custTierUpgradedTcs.TrySetResult(evt));

        await Task.WhenAll(
            staffHcmHub.StartAsync(),
            staffHnHub.StartAsync(),
            custSilverHub.StartAsync());

        Assert.Equal(HubConnectionState.Connected, staffHcmHub.State);
        Assert.Equal(HubConnectionState.Connected, staffHnHub.State);
        Assert.Equal(HubConnectionState.Connected, custSilverHub.State);

        // Create open slot at Saigon (DLR-HCM-01)
        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(38));
        var createSlotRes = await staffHcmClient.PostAsJsonAsync("/api/v1/time-slots", new CreateTimeSlotRequest
        {
            SlotDate = slotDate,
            StartTime = "09:00:00",
            EndTime = "10:00:00",
            MaxCapacity = 4,
            IsBlocked = false
        });
        Assert.Equal(HttpStatusCode.Created, createSlotRes.StatusCode);
        var slotDto = (await createSlotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts))!.Data;

        // Lookup customer.silver's seeded SH160i vehicle & ready 100k voucher (SVC100K-7K9P2M4X)
        var vehicles = await GetJsonDataAsync<IReadOnlyList<CustomerVehicleDto>>(custSilverClient, "/api/v1/vehicles");
        var silverShVehicle = Assert.Single(vehicles, v => v.Vin == "RLHJF5109RZ104829");

        var myVouchers = await GetJsonDataAsync<IReadOnlyList<CustomerVoucherDto>>(custSilverClient, "/api/v1/vouchers/my?status=Available");
        var readyVoucher = Assert.Single(myVouchers, v => v.VoucherCode == "SVC100K-7K9P2M4X");

        // Act 1: Customer creates booking at DLR-HCM-01 -> staff.hcm MUST receive BookingCreated live over WebSocket; staff.hn MUST receive 0 events!
        var bookingRes = await custSilverClient.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            ServiceTimeSlotId = slotDto.SlotId,
            SlotRowVersion = slotDto.RowVersion,
            CustomerVehicleId = silverShVehicle.Id,
            AppliedCustomerVoucherId = readyVoucher.CustomerVoucherId,
            VoucherRowVersion = readyVoucher.RowVersion,
            ServiceType = "PeriodicMaintenance",
            OdometerAtBookingKm = Math.Max(silverShVehicle.CurrentMileageKm, 3_950),
            ReportedNotes = "4,000 km scheduled periodic service with SignalR live test"
        });
        Assert.Equal(HttpStatusCode.Created, bookingRes.StatusCode);
        var swPush = Stopwatch.StartNew();
        var createdBooking = (await bookingRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;

        var receivedBookingEvt = await hcmBookingTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        swPush.Stop();

        Assert.True(swPush.ElapsedMilliseconds < 3_000, $"Expected BookingCreated push latency < 3000ms, got {swPush.ElapsedMilliseconds}ms.");
        Assert.Equal(createdBooking.BookingId, receivedBookingEvt.BookingId);
        Assert.Equal(DatabaseSeeder.HcmDealerId, receivedBookingEvt.DealerId);
        Assert.Equal("RLHJF5109RZ104829", receivedBookingEvt.Vin);
        Assert.True(receivedBookingEvt.HasReservedVoucher);

        // Negative isolation proof for Hanoi staff (DLR-HN-01)
        await Task.Delay(250);
        Assert.Empty(hnEvents);

        // Act 2: Saigon staff confirms booking, then issues ServiceInvoice (Gross=700k - Voucher=100k = Net=600k -> +72 pts @ 1.20x -> 1960 + 72 = 2032 -> Silver -> Gold!)
        var confirmRes = await staffHcmClient.PatchAsJsonAsync($"/api/v1/bookings/{createdBooking.BookingId}/status", new UpdateBookingStatusRequest
        {
            NewStatus = "Confirmed",
            RowVersion = createdBooking.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);
        var confirmedBooking = (await confirmRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;

        var invoiceRes = await staffHcmClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmedBooking.BookingId,
            BookingRowVersion = confirmedBooking.RowVersion,
            LaborAmount = 250_000m,
            PartsAmount = 450_000m,
            OdometerAtCompletionKm = 4_010,
            PaymentMethod = "BankTransfer",
            WorkSummaryNotes = "Completed 4,000 km synthetic oil, filter & CVT inspection."
        });
        Assert.Equal(HttpStatusCode.Created, invoiceRes.StatusCode);
        var issuedInvoice = (await invoiceRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!.Data;
        Assert.Equal(700_000m, issuedInvoice.GrossAmount);
        Assert.Equal(100_000m, issuedInvoice.VoucherDiscountAmount);
        Assert.Equal(600_000m, issuedInvoice.NetPaidAmount);
        Assert.Equal(72, issuedInvoice.PointsEarned);
        Assert.True(issuedInvoice.TierUpgraded);
        Assert.Equal("Gold", issuedInvoice.NewTierCode);

        var completedEvt = await custCompletedStatusTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var pointsEvt = await custPointsTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var tierEvt = await custTierUpgradedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(confirmedBooking.BookingId, completedEvt.BookingId);
        Assert.Equal("Completed", completedEvt.NewStatus);

        Assert.Equal(72, pointsEvt.PointsDelta);
        Assert.Equal(2_032, pointsEvt.LifetimeQualifyingPointsAfter);

        Assert.Equal("Silver", tierEvt.PreviousTierCode);
        Assert.Equal("Gold", tierEvt.NewTierCode);
        Assert.Equal(1.50m, tierEvt.NewPointsMultiplier);
        Assert.Equal(2_032, tierEvt.LifetimeQualifyingPoints);

        // Hanoi staff still received ZERO cross-dealer SignalR frames across the entire lifecycle
        await Task.Delay(200);
        Assert.Empty(hnEvents);

        await AssertZeroLedgerDriftForCustomerAsync(custSilverAuth.User.UserId);

        await Task.WhenAll(
            staffHcmHub.StopAsync(),
            staffHnHub.StopAsync(),
            custSilverHub.StopAsync());
    }

    private HubConnection CreateHubConnection(string? accessToken, bool useQueryStringToken)
    {
        var path = useQueryStringToken && !string.IsNullOrWhiteSpace(accessToken)
            ? $"/hubs/notifications?access_token={Uri.EscapeDataString(accessToken)}"
            : "/hubs/notifications";

        var hubUri = new Uri(_fixture.Server.BaseAddress, path);

        return new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.HttpMessageHandlerFactory = _ => _fixture.Server.CreateHandler();
                options.WebSocketFactory = async (ctx, ct) =>
                {
                    var wsClient = _fixture.Server.CreateWebSocketClient();
                    if (!useQueryStringToken && !string.IsNullOrWhiteSpace(accessToken))
                    {
                        wsClient.ConfigureRequest = req =>
                            req.Headers.Authorization = $"Bearer {accessToken}";
                    }

                    return await wsClient.ConnectAsync(ctx.Uri, ct);
                };

                if (!useQueryStringToken && !string.IsNullOrWhiteSpace(accessToken))
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            })
            .Build();
    }

    private async Task<ServiceInvoiceDto> BookConfirmAndSettleInvoiceAsync(
        HttpClient customerClient,
        HttpClient staffClient,
        Guid vehicleId,
        int slotDayOffset,
        int odometerBookingKm,
        int odometerCompletionKm,
        decimal laborAmount,
        decimal partsAmount)
    {
        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(slotDayOffset));
        var slotRes = await staffClient.PostAsJsonAsync("/api/v1/time-slots", new CreateTimeSlotRequest
        {
            SlotDate = slotDate,
            StartTime = "10:00:00",
            EndTime = "11:00:00",
            MaxCapacity = 4,
            IsBlocked = false
        });
        Assert.Equal(HttpStatusCode.Created, slotRes.StatusCode);
        var slot = (await slotRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceTimeSlotDto>>(JsonOpts))!.Data;

        var bookRes = await customerClient.PostAsJsonAsync("/api/v1/bookings", new CreateServiceBookingRequest
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            ServiceTimeSlotId = slot.SlotId,
            SlotRowVersion = slot.RowVersion,
            CustomerVehicleId = vehicleId,
            ServiceType = "PeriodicMaintenance",
            OdometerAtBookingKm = odometerBookingKm
        });
        Assert.Equal(HttpStatusCode.Created, bookRes.StatusCode);
        var booking = (await bookRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;

        var confirmRes = await staffClient.PatchAsJsonAsync($"/api/v1/bookings/{booking.BookingId}/status", new UpdateBookingStatusRequest
        {
            NewStatus = "Confirmed",
            RowVersion = booking.RowVersion
        });
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);
        var confirmed = (await confirmRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts))!.Data;

        var invRes = await staffClient.PostAsJsonAsync("/api/v1/service-invoices", new CreateServiceInvoiceRequest
        {
            ServiceBookingId = confirmed.BookingId,
            BookingRowVersion = confirmed.RowVersion,
            LaborAmount = laborAmount,
            PartsAmount = partsAmount,
            OdometerAtCompletionKm = odometerCompletionKm,
            PaymentMethod = "BankTransfer",
            WorkSummaryNotes = "Automated tier progression verification invoice"
        });
        Assert.Equal(HttpStatusCode.Created, invRes.StatusCode);
        return (await invRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts))!.Data;
    }

    private async Task<(Guid CustomerId, string AccessToken, Guid VehicleId)> RegisterCustomerWithVehicleAsync(
        HttpClient client,
        string prefix)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var registerRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
        {
            Email = $"{prefix.ToLowerInvariant()}.{uniqueSuffix}@customer.vn",
            Password = "Customer@123!",
            FullName = $"{prefix} Rider {uniqueSuffix}",
            PhoneNumber = $"09{Random.Shared.Next(10000000, 99999999)}"
        });
        Assert.Equal(HttpStatusCode.Created, registerRes.StatusCode);
        var auth = (await registerRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts))!.Data;

        var modelsRes = await client.GetAsync("/api/v1/motorcycles");
        Assert.Equal(HttpStatusCode.OK, modelsRes.StatusCode);
        var modelsEnv = await modelsRes.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<MotorcycleModelDto>>(JsonOpts);
        Assert.NotNull(modelsEnv);
        Assert.NotEmpty(modelsEnv.Data);
        var modelId = modelsEnv.Data[0].Id;

        var validVin = GenerateValidIso3779Vin();
        using var vehReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/vehicles")
        {
            Content = JsonContent.Create(new RegisterExternalVehicleRequest
            {
                Vin = validVin,
                MotorcycleModelId = modelId,
                CurrentMileageKm = 1_000,
                ColorName = "Matte Silver"
            })
        };
        vehReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        using var vehRes = await client.SendAsync(vehReq);
        Assert.Equal(HttpStatusCode.Created, vehRes.StatusCode);
        var vehEnv = await vehRes.Content.ReadFromJsonAsync<ApiEnvelope<CustomerVehicleDto>>(JsonOpts);
        Assert.NotNull(vehEnv);

        return (auth.User.UserId, auth.AccessToken, vehEnv.Data.Id);
    }

    private static string GenerateValidIso3779Vin()
    {
        const string alphabet = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        Span<char> chars = stackalloc char[17];
        "RLH".AsSpan().CopyTo(chars);
        for (var i = 3; i < 17; i++)
        {
            chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
        }

        return new string(chars);
    }

    private async Task AssertZeroLedgerDriftForCustomerAsync(Guid customerId)
    {
        await using var db = _fixture.CreateScopedDbContext();
        var account = await db.LoyaltyAccounts
            .AsNoTracking()
            .SingleAsync(a => a.CustomerId == customerId);

        var sum = await db.PointLedgerEntries
            .AsNoTracking()
            .Where(p => p.LoyaltyAccountId == account.Id)
            .SumAsync(p => p.PointsDelta);

        Assert.Equal(account.AvailablePoints, sum);
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

    private static async Task<T> GetJsonDataAsync<T>(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOpts);
        Assert.NotNull(env);
        return env.Data;
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
