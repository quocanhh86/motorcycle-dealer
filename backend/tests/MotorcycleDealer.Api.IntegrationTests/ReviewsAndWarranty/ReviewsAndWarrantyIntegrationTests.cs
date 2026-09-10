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
using MotorcycleDealer.Application.Features.Analytics;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.Reviews;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.ReviewsAndWarranty;

public sealed class LocalDbReviewsAndWarrantyWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbReviewsAndWarrantyWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_Forensic_RevWar_{Guid.NewGuid():N}";

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

public sealed class ReviewsAndWarrantyIntegrationTests : IClassFixture<LocalDbReviewsAndWarrantyWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid SilverVehicleSh160Id = Guid.Parse("99999999-9999-9999-9999-999999999901");
    private static readonly Guid PlatinumVehicleSh160Id = Guid.Parse("99999999-9999-9999-9999-999999999904");
    private static readonly Guid SeededHcmInvoiceId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01");

    private readonly LocalDbReviewsAndWarrantyWebAppFixture _fixture;

    public ReviewsAndWarrantyIntegrationTests(LocalDbReviewsAndWarrantyWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SixWayParallelServiceReviewRace_ShouldAwardExact25BonusAvailablePointsOnly_IsolateDealerSignalR_AndEnforceReplyRowVersion()
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

        // 1. RBAC & Separation of Duties guards + pre-review /by-invoice lookup
        var unauthRes = await anonClient.GetAsync("/api/v1/service-reviews/my");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthRes.StatusCode);

        var custDealerQueueForbidden = await custSilverClient.GetAsync("/api/v1/service-reviews");
        await AssertProblemErrorCodeAsync(custDealerQueueForbidden, HttpStatusCode.Forbidden, "FORBIDDEN_PERMISSION");

        var staffCreateForbidden = await staffHcmClient.PostAsJsonAsync("/api/v1/service-reviews", new CreateServiceReviewRequest
        {
            ServiceInvoiceId = SeededHcmInvoiceId,
            Rating = 5,
            Comment = "Staff must never create customer CSAT reviews."
        });
        await AssertProblemErrorCodeAsync(staffCreateForbidden, HttpStatusCode.Forbidden, "FORBIDDEN_PERMISSION");

        // Foreign customer attempting to inspect or review customer.silver's invoice -> 403 FORBIDDEN_RESOURCE_OWNER
        var foreignCustByInvoice = await custBronzeClient.GetAsync($"/api/v1/service-reviews/by-invoice/{SeededHcmInvoiceId}");
        await AssertProblemErrorCodeAsync(foreignCustByInvoice, HttpStatusCode.Forbidden, "FORBIDDEN_RESOURCE_OWNER");

        var foreignCustPost = await custBronzeClient.PostAsJsonAsync("/api/v1/service-reviews", new CreateServiceReviewRequest
        {
            ServiceInvoiceId = SeededHcmInvoiceId,
            Rating = 5,
            Comment = "Foreign customer review attempt."
        });
        await AssertProblemErrorCodeAsync(foreignCustPost, HttpStatusCode.Forbidden, "FORBIDDEN_RESOURCE_OWNER");

        // Foreign DealerStaff (Hanoi) attempting to inspect Saigon invoice review -> 403 FORBIDDEN_DEALER_SCOPE
        var foreignStaffByInvoice = await staffHnClient.GetAsync($"/api/v1/service-reviews/by-invoice/{SeededHcmInvoiceId}");
        await AssertProblemErrorCodeAsync(foreignStaffByInvoice, HttpStatusCode.Forbidden, "FORBIDDEN_DEALER_SCOPE");

        // Owning customer checking unreviewed invoice -> 200 OK with Data == null
        var unreviewedLookupRes = await custSilverClient.GetAsync($"/api/v1/service-reviews/by-invoice/{SeededHcmInvoiceId}");
        Assert.Equal(HttpStatusCode.OK, unreviewedLookupRes.StatusCode);
        var unreviewedEnv = await unreviewedLookupRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceReviewDto?>>(JsonOpts);
        Assert.NotNull(unreviewedEnv);
        Assert.Null(unreviewedEnv.Data);

        // Snapshot customer.silver loyalty wallet before 6-way parallel review race
        int baselineAvailablePoints;
        int baselineLifetimeQualifyingPoints;
        await using (var dbBefore = _fixture.CreateScopedDbContext())
        {
            var accBefore = await dbBefore.LoyaltyAccounts
                .AsNoTracking()
                .SingleAsync(a => a.CustomerId == custSilverAuth.User.UserId);
            baselineAvailablePoints = accBefore.AvailablePoints;
            baselineLifetimeQualifyingPoints = accBefore.LifetimeQualifyingPoints;
        }

        // 2. Connect live SignalR WebSockets for staff.hcm, staff.hn, admin, and customer.silver
        await using var staffHcmHub = CreateHubConnection(staffHcmAuth.AccessToken);
        await using var staffHnHub = CreateHubConnection(staffHnAuth.AccessToken);
        await using var adminHub = CreateHubConnection(adminAuth.AccessToken);
        await using var custSilverHub = CreateHubConnection(custSilverAuth.AccessToken);

        var hcmReviewSubmittedTcs = new TaskCompletionSource<ServiceReviewSubmittedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var adminReviewSubmittedTcs = new TaskCompletionSource<ServiceReviewSubmittedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custBonusPointsTcs = new TaskCompletionSource<LoyaltyPointsUpdatedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hnCrossTenantEvents = new ConcurrentQueue<string>();

        staffHcmHub.On<ServiceReviewSubmittedRealtimeEvent>("ServiceReviewSubmitted", evt => hcmReviewSubmittedTcs.TrySetResult(evt));
        adminHub.On<ServiceReviewSubmittedRealtimeEvent>("ServiceReviewSubmitted", evt => adminReviewSubmittedTcs.TrySetResult(evt));
        staffHnHub.On<ServiceReviewSubmittedRealtimeEvent>("ServiceReviewSubmitted", evt => hnCrossTenantEvents.Enqueue(evt.ReviewNumber));
        custSilverHub.On<LoyaltyPointsUpdatedRealtimeEvent>("LoyaltyPointsUpdated", evt =>
        {
            if (string.Equals(evt.TransactionType, nameof(LedgerTransactionType.ServiceReviewBonus), StringComparison.Ordinal))
            {
                custBonusPointsTcs.TrySetResult(evt);
            }
        });

        await Task.WhenAll(
            StartAndWaitGroupEnrollmentAsync(staffHcmHub),
            StartAndWaitGroupEnrollmentAsync(staffHnHub),
            StartAndWaitGroupEnrollmentAsync(adminHub),
            StartAndWaitGroupEnrollmentAsync(custSilverHub));

        // 3. 6-way parallel POST /api/v1/service-reviews race on the same ServiceInvoiceId
        var contenderClients = new List<HttpClient>(6);
        ServiceReviewDto winningReview;
        try
        {
            for (var i = 0; i < 6; i++)
            {
                var c = _fixture.CreateClient();
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", custSilverAuth.AccessToken);
                contenderClients.Add(c);
            }

            using var startGate = new ManualResetEventSlim(false);
            var raceTasks = contenderClients.Select(async (contender, idx) =>
            {
                await Task.Yield();
                startGate.Wait(TimeSpan.FromSeconds(10));
                return await contender.PostAsJsonAsync("/api/v1/service-reviews", new CreateServiceReviewRequest
                {
                    ServiceInvoiceId = SeededHcmInvoiceId,
                    Rating = 5,
                    QualityTags = "FastTurnaround,GenuineParts,ExpertTechnician",
                    Comment = $"Outstanding 1,000 km SH160i workshop care at Saigon Center (contender #{idx + 1})."
                });
            }).ToArray();

            startGate.Set();
            var responses = await Task.WhenAll(raceTasks);

            var createdWinners = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToArray();
            var conflictLosers = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToArray();

            Assert.Single(createdWinners);
            Assert.Equal(5, conflictLosers.Length);

            foreach (var loser in conflictLosers)
            {
                await AssertProblemErrorCodeAsync(loser, HttpStatusCode.Conflict, "REVIEW_ALREADY_SUBMITTED");
            }

            Assert.NotNull(createdWinners[0].Headers.Location);
            winningReview = (await createdWinners[0].Content.ReadFromJsonAsync<ApiEnvelope<ServiceReviewDto>>(JsonOpts))!.Data;

            foreach (var r in responses)
            {
                r.Dispose();
            }
        }
        finally
        {
            foreach (var c in contenderClients)
            {
                c.Dispose();
            }
        }

        // 4. Verify ADR-003 Bonus Invariant (+25 AvailablePoints ONLY; LifetimeQualifyingPoints strictly unchanged!) & SQL Ledger Drift == 0
        Assert.StartsWith("REV-", winningReview.ReviewNumber, StringComparison.Ordinal);
        Assert.Equal(5, winningReview.Rating);
        Assert.Equal(25, winningReview.BonusPointsAwarded);
        Assert.Equal(baselineAvailablePoints + 25, winningReview.AvailablePointsAfter);
        Assert.Equal(baselineLifetimeQualifyingPoints, winningReview.LifetimeQualifyingPointsAfter);
        Assert.Equal(["FastTurnaround", "GenuineParts", "ExpertTechnician"], winningReview.QualityTagList);
        Assert.False(winningReview.IsAnswered);
        Assert.NotEmpty(winningReview.RowVersion);

        await using (var dbAfter = _fixture.CreateScopedDbContext())
        {
            var accAfter = await dbAfter.LoyaltyAccounts
                .AsNoTracking()
                .SingleAsync(a => a.CustomerId == custSilverAuth.User.UserId);
            Assert.Equal(baselineAvailablePoints + 25, accAfter.AvailablePoints);
            Assert.Equal(baselineLifetimeQualifyingPoints, accAfter.LifetimeQualifyingPoints);

            var ledgerSum = await dbAfter.PointLedgerEntries
                .AsNoTracking()
                .Where(p => p.LoyaltyAccountId == accAfter.Id)
                .SumAsync(p => p.PointsDelta);
            Assert.Equal(accAfter.AvailablePoints, ledgerSum);

            var bonusEntry = await dbAfter.PointLedgerEntries
                .AsNoTracking()
                .SingleAsync(p => p.IdempotencyKey == $"SERVICE_REVIEW:{SeededHcmInvoiceId}:BONUS");
            Assert.Equal(25, bonusEntry.PointsDelta);
            Assert.Equal(nameof(LedgerTransactionType.ServiceReviewBonus), bonusEntry.TransactionType);
            Assert.Equal("ServiceReview", bonusEntry.ReferenceType);
            Assert.Equal(winningReview.ReviewId, bonusEntry.ReferenceId);
        }

        // 5. Verify live SignalR WebSocket broadcasts & zero cross-dealer leakage to Hanoi staff
        var hcmWsEvt = await hcmReviewSubmittedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var adminWsEvt = await adminReviewSubmittedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var custPointsWsEvt = await custBonusPointsTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(winningReview.ReviewId, hcmWsEvt.ReviewId);
        Assert.Equal(DatabaseSeeder.HcmDealerId, hcmWsEvt.DealerId);
        Assert.Equal(5, hcmWsEvt.Rating);
        Assert.Equal(25, hcmWsEvt.BonusPointsAwarded);
        Assert.Equal(winningReview.ReviewId, adminWsEvt.ReviewId);

        Assert.Equal(25, custPointsWsEvt.PointsDelta);
        Assert.Equal(baselineAvailablePoints + 25, custPointsWsEvt.AvailablePointsAfter);
        Assert.Equal(baselineLifetimeQualifyingPoints, custPointsWsEvt.LifetimeQualifyingPointsAfter);

        await Task.Delay(200);
        Assert.Empty(hnCrossTenantEvents);

        // 6. Verify dealer queue summary, cross-dealer isolation, and RowVersion-guarded StaffResponse PATCH
        var crossDealerQueueRes = await staffHnClient.GetAsync($"/api/v1/service-reviews?dealerId={DatabaseSeeder.HcmDealerId}");
        await AssertProblemErrorCodeAsync(crossDealerQueueRes, HttpStatusCode.Forbidden, "FORBIDDEN_DEALER_SCOPE");

        var hcmQueueRes = await staffHcmClient.GetAsync("/api/v1/service-reviews?unansweredOnly=true");
        Assert.Equal(HttpStatusCode.OK, hcmQueueRes.StatusCode);
        var hcmPage = (await hcmQueueRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceReviewPageDto>>(JsonOpts))!.Data;
        Assert.Contains(hcmPage.Items, r => r.ReviewId == winningReview.ReviewId);
        Assert.All(hcmPage.Items, r => Assert.Equal(DatabaseSeeder.HcmDealerId, r.DealerId));
        Assert.True(hcmPage.UnansweredCount >= 1);
        Assert.True(hcmPage.TotalBonusPointsAwarded >= 25);

        // Foreign Hanoi staff cannot reply to Saigon review -> 403 FORBIDDEN_DEALER_SCOPE
        var foreignReplyRes = await staffHnClient.PatchAsJsonAsync(
            $"/api/v1/service-reviews/{winningReview.ReviewId}/response",
            new RespondToServiceReviewRequest
            {
                StaffResponse = "Cross-dealer reply attempt should fail.",
                RowVersion = winningReview.RowVersion
            });
        await AssertProblemErrorCodeAsync(foreignReplyRes, HttpStatusCode.Forbidden, "FORBIDDEN_DEALER_SCOPE");

        // Saigon staff posts official reply -> 200 OK
        var validReplyRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/service-reviews/{winningReview.ReviewId}/response",
            new RespondToServiceReviewRequest
            {
                StaffResponse = "Thank you for rating Saigon Center 5 stars! We look forward to seeing you at 4,000 km.",
                RowVersion = winningReview.RowVersion
            });
        Assert.Equal(HttpStatusCode.OK, validReplyRes.StatusCode);
        var repliedDto = (await validReplyRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceReviewDto>>(JsonOpts))!.Data;
        Assert.True(repliedDto.IsAnswered);
        Assert.NotNull(repliedDto.RespondedAtUtc);
        Assert.Equal(staffHcmAuth.User.UserId, repliedDto.RespondedByUserId);
        Assert.NotEqual(winningReview.RowVersion, repliedDto.RowVersion);

        // Replaying stale RowVersion -> 409 CONCURRENCY_CONFLICT
        var staleReplyRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/service-reviews/{winningReview.ReviewId}/response",
            new RespondToServiceReviewRequest
            {
                StaffResponse = "Stale concurrency token reply attempt.",
                RowVersion = winningReview.RowVersion
            });
        await AssertProblemErrorCodeAsync(staleReplyRes, HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT");

        // Customer /by-invoice, /my?minRating=5, and /loyalty/ledger?transactionType=ServiceReviewBonus reflect review & +25 bonus
        var reviewedLookupRes = await custSilverClient.GetAsync($"/api/v1/service-reviews/by-invoice/{SeededHcmInvoiceId}");
        Assert.Equal(HttpStatusCode.OK, reviewedLookupRes.StatusCode);
        var reviewedLookup = (await reviewedLookupRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceReviewDto?>>(JsonOpts))!.Data;
        Assert.NotNull(reviewedLookup);
        Assert.Equal(repliedDto.StaffResponse, reviewedLookup!.StaffResponse);

        var myMinRatingRes = await GetPagedDataAsync<ServiceReviewDto>(custSilverClient, "/api/v1/service-reviews/my?minRating=5");
        Assert.Contains(myMinRatingRes.Data, r => r.ReviewId == winningReview.ReviewId);
        Assert.All(myMinRatingRes.Data, r => Assert.True(r.Rating >= 5));

        var dealerMinRatingRes = await staffHcmClient.GetAsync("/api/v1/service-reviews?minRating=4");
        Assert.Equal(HttpStatusCode.OK, dealerMinRatingRes.StatusCode);
        var dealerMinRatingPage = (await dealerMinRatingRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceReviewPageDto>>(JsonOpts))!.Data;
        Assert.Contains(dealerMinRatingPage.Items, r => r.ReviewId == winningReview.ReviewId);
        Assert.All(dealerMinRatingPage.Items, r => Assert.True(r.Rating >= 4));

        var adminMinRating4Res = await adminClient.GetAsync("/api/v1/service-reviews?minRating=4");
        Assert.Equal(HttpStatusCode.OK, adminMinRating4Res.StatusCode);
        var adminMinRating4Page = (await adminMinRating4Res.Content.ReadFromJsonAsync<ApiEnvelope<ServiceReviewPageDto>>(JsonOpts))!.Data;
        Assert.True(adminMinRating4Page.Items.Count >= 3);
        Assert.All(adminMinRating4Page.Items, r => Assert.True(r.Rating >= 4));

        var invalidMinRatingRes = await staffHcmClient.GetAsync("/api/v1/service-reviews?minRating=6");
        await AssertProblemErrorCodeAsync(invalidMinRatingRes, HttpStatusCode.BadRequest, "VALIDATION_FAILED");

        var bonusLedgerPage = await GetPagedDataAsync<PointLedgerEntryDto>(
            custSilverClient,
            "/api/v1/loyalty/ledger?transactionType=ServiceReviewBonus");
        Assert.Contains(bonusLedgerPage.Data, p => p.ReferenceId == winningReview.ReviewId && p.PointsDelta == 25);
        Assert.All(bonusLedgerPage.Data, p => Assert.Equal("ServiceReviewBonus", p.TransactionType));

        await Task.WhenAll(
            staffHcmHub.StopAsync(),
            staffHnHub.StopAsync(),
            adminHub.StopAsync(),
            custSilverHub.StopAsync());
    }

    [Fact]
    public async Task WarrantyClaimLifecycle_ShouldEnforce36m30000KmCoverage_MonotonicOdometer_ActiveDuplicateGuard_CompatibleSparePart_LiveSignalR_AndAdminAnalytics()
    {
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

        // 1. RBAC & Dealer queue isolation checks
        var custDealerWarrantyForbidden = await custSilverClient.GetAsync("/api/v1/warranty-claims");
        await AssertProblemErrorCodeAsync(custDealerWarrantyForbidden, HttpStatusCode.Forbidden, "FORBIDDEN_PERMISSION");

        var staffCreateWarrantyForbidden = await staffHcmClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SilverVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 3_900,
            DefectCategory = "Electrical",
            SymptomDescription = "Staff accounts cannot file customer warranty claims."
        });
        await AssertProblemErrorCodeAsync(staffCreateWarrantyForbidden, HttpStatusCode.Forbidden, "FORBIDDEN_PERMISSION");

        var hnWarrantyQueue = await GetPagedDataAsync<WarrantyClaimDto>(staffHnClient, "/api/v1/warranty-claims");
        Assert.NotEmpty(hnWarrantyQueue.Data);
        Assert.All(hnWarrantyQueue.Data, c => Assert.Equal(DatabaseSeeder.HanoiDealerId, c.DealerId));

        // Baseline AdminAnalytics /overview snapshot
        var baselineOverviewRes = await adminClient.GetAsync("/api/v1/admin/analytics/overview");
        Assert.Equal(HttpStatusCode.OK, baselineOverviewRes.StatusCode);
        var baselineOverview = (await baselineOverviewRes.Content.ReadFromJsonAsync<ApiEnvelope<AnalyticsOverviewDto>>(JsonOpts))!.Data;

        // Lookup OEM SpareParts (incompatible CB300R chain kit vs compatible universal NGK Spark Plug)
        var allParts = await GetPagedDataAsync<SparePartDto>(custSilverClient, "/api/v1/spare-parts?pageSize=50");
        var cb300ChainPart = Assert.Single(allParts.Data, p => p.PartCode == "PRT-CHN-520VX");
        var iridiumSparkPlugPart = Assert.Single(allParts.Data, p => p.PartCode == "PRT-SPK-IR01");
        var shCvtBeltPart = Assert.Single(allParts.Data, p => p.PartCode == "PRT-CVT-BLT01");

        // 2. Negative guards on POST /api/v1/warranty-claims:
        //    a) Foreign customer's vehicle -> 404 RESOURCE_NOT_FOUND
        var foreignVehicleRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = PlatinumVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 6_000,
            DefectCategory = "Engine",
            SymptomDescription = "Attempting claim on another customer's motorcycle VIN."
        });
        await AssertProblemErrorCodeAsync(foreignVehicleRes, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");

        //    b) Odometer regression (< 3,850 km seeded on SilverVehicleSh160Id) -> 422 ODOMETER_REGRESSION
        var regressionRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SilverVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 3_500,
            DefectCategory = "Electrical",
            SymptomDescription = "Claim odometer lower than recorded garage mileage."
        });
        await AssertProblemErrorCodeAsync(regressionRes, HttpStatusCode.UnprocessableEntity, "ODOMETER_REGRESSION");

        //    c) > 30,000 km -> 422 WARRANTY_COVERAGE_EXPIRED (and must NOT mutate vehicle.CurrentMileageKm!)
        var overMileageRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SilverVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 30_001,
            DefectCategory = "Electrical",
            SymptomDescription = "Mileage exceeds 30,000 km factory warranty cap."
        });
        await AssertProblemErrorCodeAsync(overMileageRes, HttpStatusCode.UnprocessableEntity, "WARRANTY_COVERAGE_EXPIRED");

        //    d) > 36 months -> 422 WARRANTY_COVERAGE_EXPIRED
        Guid expiredVehicleId;
        await using (var dbExpired = _fixture.CreateScopedDbContext())
        {
            var modelId = await dbExpired.MotorcycleModels.IgnoreQueryFilters().Select(m => m.Id).FirstAsync();
            var expiredStart = DateTimeOffset.UtcNow.AddMonths(-37);
            var expiredVehicle = new CustomerVehicle(Guid.CreateVersion7())
            {
                VIN = "RLHJF5109RZ993737",
                CustomerId = custSilverAuth.User.UserId,
                MotorcycleModelId = modelId,
                RegistrationSource = "ExternalManual",
                OwnershipStartDateUtc = expiredStart,
                CurrentMileageKm = 12_000,
                LastServiceMileageKm = 8_000,
                NextDueMileageKm = 12_000,
                NextDueDateUtc = DateTimeOffset.UtcNow.AddMonths(1),
                IsActive = true,
                CreatedAtUtc = expiredStart
            };
            dbExpired.CustomerVehicles.Add(expiredVehicle);
            await dbExpired.SaveChangesAsync();
            expiredVehicleId = expiredVehicle.Id;
        }

        var overAgeRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = expiredVehicleId,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 12_100,
            DefectCategory = "Engine",
            SymptomDescription = "Vehicle ownership started 37 months ago beyond 36-month OEM limit."
        });
        await AssertProblemErrorCodeAsync(overAgeRes, HttpStatusCode.UnprocessableEntity, "WARRANTY_COVERAGE_EXPIRED");

        // 3. Connect live SignalR WebSockets for staff.hcm, staff.hn, admin, and customer.silver
        await using var staffHcmHub = CreateHubConnection(staffHcmAuth.AccessToken);
        await using var staffHnHub = CreateHubConnection(staffHnAuth.AccessToken);
        await using var adminHub = CreateHubConnection(adminAuth.AccessToken);
        await using var custSilverHub = CreateHubConnection(custSilverAuth.AccessToken);

        var hcmClaimCreatedTcs = new TaskCompletionSource<WarrantyClaimCreatedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var adminClaimCreatedTcs = new TaskCompletionSource<WarrantyClaimCreatedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custUnderReviewTcs = new TaskCompletionSource<WarrantyClaimStatusChangedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custApprovedTcs = new TaskCompletionSource<WarrantyClaimStatusChangedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custCompletedTcs = new TaskCompletionSource<WarrantyClaimStatusChangedRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hnCrossTenantEvents = new ConcurrentQueue<string>();

        staffHcmHub.On<WarrantyClaimCreatedRealtimeEvent>("WarrantyClaimCreated", evt => hcmClaimCreatedTcs.TrySetResult(evt));
        adminHub.On<WarrantyClaimCreatedRealtimeEvent>("WarrantyClaimCreated", evt => adminClaimCreatedTcs.TrySetResult(evt));
        staffHnHub.On<WarrantyClaimCreatedRealtimeEvent>("WarrantyClaimCreated", evt => hnCrossTenantEvents.Enqueue($"Created:{evt.ClaimNumber}"));
        staffHnHub.On<WarrantyClaimStatusChangedRealtimeEvent>("WarrantyClaimStatusChanged", evt => hnCrossTenantEvents.Enqueue($"Status:{evt.ClaimNumber}:{evt.NewStatus}"));

        custSilverHub.On<WarrantyClaimStatusChangedRealtimeEvent>("WarrantyClaimStatusChanged", evt =>
        {
            switch (evt.NewStatus)
            {
                case "UnderReview":
                    custUnderReviewTcs.TrySetResult(evt);
                    break;
                case "Approved":
                    custApprovedTcs.TrySetResult(evt);
                    break;
                case "Completed":
                    custCompletedTcs.TrySetResult(evt);
                    break;
            }
        });

        await Task.WhenAll(
            StartAndWaitGroupEnrollmentAsync(staffHcmHub),
            StartAndWaitGroupEnrollmentAsync(staffHnHub),
            StartAndWaitGroupEnrollmentAsync(adminHub),
            StartAndWaitGroupEnrollmentAsync(custSilverHub));

        // 4. Submit valid WarrantyClaim (Odometer advances 3,850 -> 3,920 km) -> live WarrantyClaimCreated push to staff.hcm & admin!
        var createRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SilverVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 3_920,
            DefectCategory = "Electrical",
            SymptomDescription = "Intermittent LED headlamp voltage drop and ignition misfire on cold morning starts."
        });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createRes.Headers.Location);
        var createdClaim = (await createRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;
        Assert.StartsWith("WC-", createdClaim.ClaimNumber, StringComparison.Ordinal);
        Assert.Equal("Submitted", createdClaim.Status);
        Assert.Equal(3_920, createdClaim.OdometerAtClaimKm);
        Assert.NotEmpty(createdClaim.RowVersion);

        var createdWsEvt = await hcmClaimCreatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var adminCreatedWsEvt = await adminClaimCreatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(createdClaim.ClaimId, createdWsEvt.ClaimId);
        Assert.Equal(DatabaseSeeder.HcmDealerId, createdWsEvt.DealerId);
        Assert.Equal(createdClaim.ClaimId, adminCreatedWsEvt.ClaimId);

        // Verify vehicle CurrentMileageKm advanced to 3,920 km
        await using (var dbCheckOdo = _fixture.CreateScopedDbContext())
        {
            var updatedVeh = await dbCheckOdo.CustomerVehicles.AsNoTracking().SingleAsync(v => v.Id == SilverVehicleSh160Id);
            Assert.Equal(3_920, updatedVeh.CurrentMileageKm);
        }

        // 5. Duplicate active claim on same (Vehicle, DefectCategory) -> 409 ACTIVE_WARRANTY_CLAIM_EXISTS
        var dupActiveRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SilverVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 3_925,
            DefectCategory = "Electrical",
            SymptomDescription = "Duplicate active electrical claim on same motorcycle."
        });
        await AssertProblemErrorCodeAsync(dupActiveRes, HttpStatusCode.Conflict, "ACTIVE_WARRANTY_CLAIM_EXISTS");

        // Foreign customer & foreign staff isolation on GET /{claimId} and PATCH /{claimId}/status
        var foreignCustGet = await custBronzeClient.GetAsync($"/api/v1/warranty-claims/{createdClaim.ClaimId}");
        await AssertProblemErrorCodeAsync(foreignCustGet, HttpStatusCode.Forbidden, "FORBIDDEN_RESOURCE_OWNER");

        var foreignStaffPatch = await staffHnClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{createdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "UnderReview",
                RowVersion = createdClaim.RowVersion
            });
        await AssertProblemErrorCodeAsync(foreignStaffPatch, HttpStatusCode.Forbidden, "FORBIDDEN_DEALER_SCOPE");

        // Illegal skip transition Submitted -> Completed -> 422 INVALID_WARRANTY_STATUS_TRANSITION
        var invalidSkipRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{createdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Completed",
                ResolutionNotes = "Cannot jump directly from Submitted to Completed.",
                RowVersion = createdClaim.RowVersion
            });
        await AssertProblemErrorCodeAsync(invalidSkipRes, HttpStatusCode.UnprocessableEntity, "INVALID_WARRANTY_STATUS_TRANSITION");

        // 6. Transition Submitted -> UnderReview -> Approved (rejecting incompatible SparePart first) -> Completed
        var underReviewRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{createdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "UnderReview",
                ResolutionNotes = "Technician bay 2 running ignition coil and spark plug diagnostics.",
                RowVersion = createdClaim.RowVersion
            });
        Assert.Equal(HttpStatusCode.OK, underReviewRes.StatusCode);
        var underReviewDto = (await underReviewRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;
        Assert.Equal("UnderReview", underReviewDto.Status);

        var underReviewWsEvt = await custUnderReviewTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("UnderReview", underReviewWsEvt.NewStatus);

        // Incompatible CB300R part on SH160i rejected with 422 SPARE_PART_INCOMPATIBLE
        var incompatiblePartRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{createdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Approved",
                CoveredSparePartId = cb300ChainPart.PartId,
                CoveredAmountVnd = 2_800_000m,
                RowVersion = underReviewDto.RowVersion
            });
        await AssertProblemErrorCodeAsync(incompatiblePartRes, HttpStatusCode.UnprocessableEntity, "SPARE_PART_INCOMPATIBLE");

        // Valid Approved transition with compatible OEM part & 480,000 VND covered cost
        const decimal coveredAmountVnd = 480_000.00m;
        var approveRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{createdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Approved",
                CoveredSparePartId = iridiumSparkPlugPart.PartId,
                CoveredAmountVnd = coveredAmountVnd,
                ResolutionNotes = "Approved OEM spark plug & resistor cap replacement under 36m warranty.",
                RowVersion = underReviewDto.RowVersion
            });
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);
        var approvedDto = (await approveRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;
        Assert.Equal("Approved", approvedDto.Status);
        Assert.Equal(iridiumSparkPlugPart.PartId, approvedDto.CoveredSparePartId);
        Assert.Equal(coveredAmountVnd, approvedDto.CoveredAmountVnd);

        var approvedWsEvt = await custApprovedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Approved", approvedWsEvt.NewStatus);
        Assert.Equal(coveredAmountVnd, approvedWsEvt.CoveredAmountVnd);

        // Stale RowVersion rejected with 409 CONCURRENCY_CONFLICT
        var staleStatusRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{createdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Completed",
                ResolutionNotes = "Stale RowVersion attempt.",
                RowVersion = underReviewDto.RowVersion
            });
        await AssertProblemErrorCodeAsync(staleStatusRes, HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT");

        // Transition Approved -> Completed
        var completeRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{createdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Completed",
                ResolutionNotes = "Installed OEM NGK Laser Iridium spark plug assembly and verified cold-start voltage stability.",
                RowVersion = approvedDto.RowVersion
            });
        Assert.Equal(HttpStatusCode.OK, completeRes.StatusCode);
        var completedDto = (await completeRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;
        Assert.Equal("Completed", completedDto.Status);
        Assert.Equal(coveredAmountVnd, completedDto.CoveredAmountVnd);
        Assert.NotNull(completedDto.CompletedAtUtc);

        var completedWsEvt = await custCompletedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Completed", completedWsEvt.NewStatus);
        Assert.Equal(coveredAmountVnd, completedWsEvt.CoveredAmountVnd);

        // 7. Also verify Rejected flow forces CoveredAmountVnd == 0m & Hanoi staff received 0 cross-dealer events
        var secondClaimRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SilverVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 3_930,
            DefectCategory = "FrameAndPaint",
            SymptomDescription = "Minor parking scuff on side fairing panel reported for inspection."
        });
        Assert.Equal(HttpStatusCode.Created, secondClaimRes.StatusCode);
        var secondClaim = (await secondClaimRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;

        var rejectRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{secondClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Rejected",
                CoveredAmountVnd = 999_000m,
                RejectionReason = "External parking abrasion is excluded from OEM factory manufacturing defect warranty.",
                RowVersion = secondClaim.RowVersion
            });
        Assert.Equal(HttpStatusCode.OK, rejectRes.StatusCode);
        var rejectedDto = (await rejectRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;
        Assert.Equal("Rejected", rejectedDto.Status);
        Assert.Equal(0.00m, rejectedDto.CoveredAmountVnd);

        // 7b. Verify F-P9-API-01: Approved with CoveredAmountVnd = null defaults to partA.UnitPriceVnd (350,000),
        //     and swapping partA -> partB (PRT-CVT-BLT01, 850,000) on Completed with CoveredAmountVnd = null updates to partB.UnitPriceVnd!
        var thirdClaimRes = await custSilverClient.PostAsJsonAsync("/api/v1/warranty-claims", new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SilverVehicleSh160Id,
            DealerId = DatabaseSeeder.HcmDealerId,
            OdometerAtClaimKm = 3_940,
            DefectCategory = "Transmission",
            SymptomDescription = "Noticeable CVT drive belt chirp under sustained uphill load."
        });
        Assert.Equal(HttpStatusCode.Created, thirdClaimRes.StatusCode);
        var thirdClaim = (await thirdClaimRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;

        var thirdApproveNullAmtRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{thirdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Approved",
                CoveredSparePartId = iridiumSparkPlugPart.PartId,
                CoveredAmountVnd = null,
                RowVersion = thirdClaim.RowVersion
            });
        Assert.Equal(HttpStatusCode.OK, thirdApproveNullAmtRes.StatusCode);
        var thirdApproved = (await thirdApproveNullAmtRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;
        Assert.Equal(iridiumSparkPlugPart.UnitPriceVnd, thirdApproved.CoveredAmountVnd);

        var thirdCompleteSwapPartRes = await staffHcmClient.PatchAsJsonAsync(
            $"/api/v1/warranty-claims/{thirdClaim.ClaimId}/status",
            new UpdateWarrantyClaimStatusRequest
            {
                Status = "Completed",
                CoveredSparePartId = shCvtBeltPart.PartId,
                CoveredAmountVnd = null,
                ResolutionNotes = "Swapped covered OEM part to SH160i Bando Kevlar CVT belt PRT-CVT-BLT01 on final bench inspection.",
                RowVersion = thirdApproved.RowVersion
            });
        Assert.Equal(HttpStatusCode.OK, thirdCompleteSwapPartRes.StatusCode);
        var thirdCompleted = (await thirdCompleteSwapPartRes.Content.ReadFromJsonAsync<ApiEnvelope<WarrantyClaimDto>>(JsonOpts))!.Data;
        Assert.Equal(shCvtBeltPart.PartId, thirdCompleted.CoveredSparePartId);
        Assert.Equal(shCvtBeltPart.UnitPriceVnd, thirdCompleted.CoveredAmountVnd);

        await Task.Delay(200);
        Assert.Empty(hnCrossTenantEvents);

        // 8. Re-query AdminAnalytics /overview and verify exact CSAT & ApprovedWarrantyCoveredVnd reconciliation!
        var updatedOverviewRes = await adminClient.GetAsync("/api/v1/admin/analytics/overview");
        Assert.Equal(HttpStatusCode.OK, updatedOverviewRes.StatusCode);
        var updatedOverview = (await updatedOverviewRes.Content.ReadFromJsonAsync<ApiEnvelope<AnalyticsOverviewDto>>(JsonOpts))!.Data;

        await using (var dbReconcile = _fixture.CreateScopedDbContext())
        {
            var expectedReviewCount = await dbReconcile.ServiceReviews.IgnoreQueryFilters().CountAsync();
            var expectedRatingSum = await dbReconcile.ServiceReviews.IgnoreQueryFilters().SumAsync(r => r.Rating);
            var expectedAvgCsat = Math.Round((decimal)expectedRatingSum / expectedReviewCount, 2, MidpointRounding.AwayFromZero);

            Assert.Equal(expectedReviewCount, updatedOverview.TotalServiceReviewsCount);
            Assert.Equal(expectedAvgCsat, updatedOverview.AverageCsatRating);
        }

        Assert.Equal(baselineOverview.OpenWarrantyClaimsCount, updatedOverview.OpenWarrantyClaimsCount);
        Assert.Equal(
            baselineOverview.ApprovedWarrantyCoveredVnd + coveredAmountVnd + shCvtBeltPart.UnitPriceVnd,
            updatedOverview.ApprovedWarrantyCoveredVnd);

        var hcmAnalyticsRow = Assert.Single(updatedOverview.Dealers, d => d.DealerId == DatabaseSeeder.HcmDealerId);
        Assert.True(hcmAnalyticsRow.ApprovedWarrantyCoveredVnd >= coveredAmountVnd);

        var qualityWarrantyRes = await adminClient.GetAsync("/api/v1/admin/analytics/quality-warranty");
        Assert.Equal(HttpStatusCode.OK, qualityWarrantyRes.StatusCode);
        var qualityWarranty = (await qualityWarrantyRes.Content.ReadFromJsonAsync<ApiEnvelope<QualityWarrantyAnalyticsDto>>(JsonOpts))!.Data;
        Assert.Equal(updatedOverview.TotalServiceReviewsCount, qualityWarranty.TotalReviewsCount);
        Assert.Equal(updatedOverview.AverageCsatRating, qualityWarranty.AverageCsatRating);
        Assert.Equal(updatedOverview.ApprovedWarrantyCoveredVnd, qualityWarranty.ApprovedWarrantyCoveredVnd);
        Assert.Equal(6, qualityWarranty.DefectBreakdown.Count);
        Assert.Equal(qualityWarranty.TotalWarrantyClaimsCount, qualityWarranty.DefectBreakdown.Sum(b => b.ClaimsCount));

        await Task.WhenAll(
            staffHcmHub.StopAsync(),
            staffHnHub.StopAsync(),
            adminHub.StopAsync(),
            custSilverHub.StopAsync());
    }

    private static async Task StartAndWaitGroupEnrollmentAsync(HubConnection hub)
    {
        await hub.StartAsync();
        try
        {
            await hub.InvokeAsync("__sync_onconnected");
        }
        catch (HubException)
        {
            // Expected: NotificationHub exposes zero client-callable methods; proves OnConnectedAsync group enrollment completed.
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

    private static async Task<PagedApiEnvelope<T>> GetPagedDataAsync<T>(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<PagedApiEnvelope<T>>(JsonOpts);
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
