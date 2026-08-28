using System.Globalization;
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
using MotorcycleDealer.Application.Features.Analytics;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.SalesOrders;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Application.Features.Vouchers;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using MotorcycleDealer.Infrastructure.Services.Analytics;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.Analytics;

public sealed class LocalDbAnalyticsWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbAnalyticsWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_Forensic_P7Anl_{Guid.NewGuid():N}";

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

public sealed class AdminAnalyticsIntegrationTests : IClassFixture<LocalDbAnalyticsWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] AllAnalyticsRoutes =
    [
        "/api/v1/admin/analytics/overview",
        "/api/v1/admin/analytics/kpis",
        "/api/v1/admin/analytics/revenue-by-dealer",
        "/api/v1/admin/analytics/bookings-by-dealer",
        "/api/v1/admin/analytics/monthly-trends",
        "/api/v1/admin/analytics/loyalty-distribution",
        "/api/v1/admin/analytics/quality-warranty",
        "/api/v1/admin/analytics/export",
        "/api/v1/admin/analytics/export-csv"
    ];

    private readonly LocalDbAnalyticsWebAppFixture _fixture;

    public AdminAnalyticsIntegrationTests(LocalDbAnalyticsWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AnalyticsEndpoints_ShouldEnforce401Anonymous_403CustomerAndDealerStaff_AndExportPermissionSeparation()
    {
        using var client = _fixture.CreateClient();

        // 1. Anonymous calls -> 401 UNAUTHORIZED RFC 7807 ProblemDetails on all 8 routes
        foreach (var route in AllAnalyticsRoutes)
        {
            using var res = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
            Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("UNAUTHORIZED", doc.RootElement.GetProperty("errorCode").GetString());
            Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("traceId").GetString()));
        }

        // 2. Customer & DealerStaff -> 403 FORBIDDEN_PERMISSION on all 8 routes (RISK-05)
        var customerAuth = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var hanoiStaffAuth = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");

        foreach (var token in new[] { customerAuth.AccessToken, hanoiStaffAuth.AccessToken })
        {
            foreach (var route in AllAnalyticsRoutes)
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, route, token);
                using var res = await client.SendAsync(req);
                Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
                Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                Assert.Equal("FORBIDDEN_PERMISSION", doc.RootElement.GetProperty("errorCode").GetString());
            }
        }

        // 3. Admin token carrying ViewAllDealers WITHOUT ExportReports can read /overview & /kpis, but gets 403 on /export & /export-csv
        IssuedAccessToken readOnlyAdminJwt;
        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            readOnlyAdminJwt = jwtService.GenerateAccessToken(new JwtTokenSubject(
                UserId: Guid.CreateVersion7(),
                Email: "readonly.admin@motorcycledealer.vn",
                FullName: "Read Only Executive",
                Role: RoleNames.Admin,
                DealerId: null,
                Permissions: [Permissions.Analytics.ViewAllDealers]));
        }

        using (var overviewReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/analytics/overview",
            readOnlyAdminJwt.AccessToken))
        using (var overviewRes = await client.SendAsync(overviewReq))
        {
            Assert.Equal(HttpStatusCode.OK, overviewRes.StatusCode);
        }

        foreach (var exportRoute in new[] { "/api/v1/admin/analytics/export", "/api/v1/admin/analytics/export-csv" })
        {
            using var exportReq = CreateAuthorizedRequest(HttpMethod.Get, exportRoute, readOnlyAdminJwt.AccessToken);
            using var exportRes = await client.SendAsync(exportReq);
            Assert.Equal(HttpStatusCode.Forbidden, exportRes.StatusCode);
            using var doc = JsonDocument.Parse(await exportRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_PERMISSION", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task AnalyticsFilters_ShouldReturn400OnInvalidDateWindows_And404OnUnknownOrSoftDeletedDealerId()
    {
        using var client = _fixture.CreateClient();
        var adminAuth = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");

        // 1. Inverted window (fromUtc > toUtc) -> 400 VALIDATION_FAILED across /overview & /quality-warranty
        foreach (var invertedUrl in new[]
        {
            "/api/v1/admin/analytics/overview?fromUtc=2026-08-10T00:00:00Z&toUtc=2026-08-01T00:00:00Z",
            "/api/v1/admin/analytics/quality-warranty?fromUtc=2026-08-10T00:00:00Z&toUtc=2026-08-01T00:00:00Z"
        })
        {
            using var invertedReq = CreateAuthorizedRequest(HttpMethod.Get, invertedUrl, adminAuth.AccessToken);
            using var invertedRes = await client.SendAsync(invertedReq);
            Assert.Equal(HttpStatusCode.BadRequest, invertedRes.StatusCode);
            using var doc = JsonDocument.Parse(await invertedRes.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_FAILED", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 2. Window exceeding 731 days -> 400 VALIDATION_FAILED
        using (var wideReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/analytics/monthly-trends?fromUtc=2023-01-01T00:00:00Z&toUtc=2026-06-01T00:00:00Z",
            adminAuth.AccessToken))
        using (var wideRes = await client.SendAsync(wideReq))
        {
            Assert.Equal(HttpStatusCode.BadRequest, wideRes.StatusCode);
            using var doc = JsonDocument.Parse(await wideRes.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_FAILED", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 3. Empty GUID dealerId -> 400 VALIDATION_FAILED
        using (var emptyDealerReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/analytics/overview?dealerId={Guid.Empty:D}",
            adminAuth.AccessToken))
        using (var emptyDealerRes = await client.SendAsync(emptyDealerReq))
        {
            Assert.Equal(HttpStatusCode.BadRequest, emptyDealerRes.StatusCode);
        }

        // 4. Unknown dealerId -> 404 RESOURCE_NOT_FOUND across all branch-filterable endpoints
        var missingDealerId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        string[] dealerScopedRoutes =
        [
            $"/api/v1/admin/analytics/overview?dealerId={missingDealerId:D}",
            $"/api/v1/admin/analytics/revenue-by-dealer?dealerId={missingDealerId:D}",
            $"/api/v1/admin/analytics/bookings-by-dealer?dealerId={missingDealerId:D}",
            $"/api/v1/admin/analytics/monthly-trends?dealerId={missingDealerId:D}",
            $"/api/v1/admin/analytics/quality-warranty?dealerId={missingDealerId:D}",
            $"/api/v1/admin/analytics/export?dealerId={missingDealerId:D}"
        ];

        foreach (var route in dealerScopedRoutes)
        {
            using var req = CreateAuthorizedRequest(HttpMethod.Get, route, adminAuth.AccessToken);
            using var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("RESOURCE_NOT_FOUND", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task ClosedLoopTransactions_ShouldReconcileExactFinancialsZeroFillAndUtf8BomCsvAcrossAllSixEndpoints()
    {
        using var client = _fixture.CreateClient();

        var adminAuth = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        var hanoiStaffAuth = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var hcmStaffAuth = await LoginAsync(client, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        var silverCustomerAuth = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");

        // 1. Baseline snapshot across /overview, /kpis alias, /monthly-trends, and /loyalty-distribution
        var baselineOverview = await GetEnvelopeDataAsync<AnalyticsOverviewDto>(
            client,
            "/api/v1/admin/analytics/overview",
            adminAuth.AccessToken);

        var baselineKpisAlias = await GetEnvelopeDataAsync<AnalyticsOverviewDto>(
            client,
            "/api/v1/admin/analytics/kpis",
            adminAuth.AccessToken);

        Assert.Equal(baselineOverview.TotalRevenueVnd, baselineKpisAlias.TotalRevenueVnd);
        Assert.Equal(baselineOverview.ShowroomRevenueVnd, baselineKpisAlias.ShowroomRevenueVnd);
        Assert.Equal(baselineOverview.ServiceRevenueVnd, baselineKpisAlias.ServiceRevenueVnd);
        Assert.Equal(baselineOverview.TotalBookings, baselineKpisAlias.TotalBookings);
        Assert.Equal(baselineOverview.UnredeemedPointsPool, baselineKpisAlias.UnredeemedPointsPool);
        Assert.Equal(baselineOverview.UnredeemedPointsPool * 500.00m, baselineOverview.PointsLiabilityVnd);

        var baselineTrends = await GetEnvelopeDataAsync<MonthlyTrendsAnalyticsDto>(
            client,
            "/api/v1/admin/analytics/monthly-trends",
            adminAuth.AccessToken);
        Assert.Equal(12, baselineTrends.MonthLabels.Count);
        Assert.Equal(12, baselineTrends.Months.Count);
        Assert.Equal(12, baselineTrends.CombinedRevenueVnd.Count);
        Assert.Equal(12, baselineTrends.BookingCounts.Count);

        var baselineLoyalty = await GetEnvelopeDataAsync<LoyaltyDistributionAnalyticsDto>(
            client,
            "/api/v1/admin/analytics/loyalty-distribution",
            adminAuth.AccessToken);
        Assert.Equal(["Bronze", "Silver", "Gold", "Platinum"], baselineLoyalty.TierLabels);
        Assert.Equal(4, baselineLoyalty.Tiers.Count);
        Assert.Equal(baselineOverview.TotalMembersCount, baselineLoyalty.TotalMembers);
        Assert.Equal(baselineOverview.UnredeemedPointsPool, baselineLoyalty.UnredeemedPointsPool);
        Assert.Equal(baselineOverview.PointsLiabilityVnd, baselineLoyalty.PointsLiabilityVnd);

        // 2. Provision a brand-new active 4th dealer (zero transactions -> proves zero-fill)
        //    AND a soft-deleted dealer in SQL (proves !IsDeleted predicate alongside .IgnoreQueryFilters())
        DealerDto zeroFillDealer;
        using (var createDealerReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/dealers",
            adminAuth.AccessToken,
            new UpsertDealerRequest
            {
                Code = "DLR-CT-01",
                Name = "Apex Moto Can Tho Mekong Hub",
                City = "Can Tho",
                Address = "18 Hoa Binh Avenue, Ninh Kieu District, Can Tho",
                PhoneNumber = "02923819900",
                ContactEmail = "cantho@motorcycledealer.vn",
                OpeningTime = "08:00:00",
                ClosingTime = "18:00:00",
                DefaultSlotCapacity = 4,
                IsActive = true
            }))
        using (var createDealerRes = await client.SendAsync(createDealerReq))
        {
            Assert.Equal(HttpStatusCode.Created, createDealerRes.StatusCode);
            var env = await createDealerRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerDto>>(JsonOpts);
            Assert.NotNull(env);
            zeroFillDealer = env.Data;
        }

        await using (var db = _fixture.CreateScopedDbContext())
        {
            var deletedDealer = new Dealer(Guid.CreateVersion7())
            {
                Code = "DLR-DEL-99",
                Name = "Retired Soft Deleted Branch",
                City = "Hue",
                Address = "99 Le Loi Street, Hue",
                PhoneNumber = "02343889999",
                ContactEmail = "deleted@motorcycledealer.vn",
                OpeningTime = new TimeOnly(8, 0),
                ClosingTime = new TimeOnly(17, 0),
                DefaultSlotCapacity = 2,
                IsActive = false,
                IsDeleted = true,
                DeletedAtUtc = DateTimeOffset.UtcNow
            };
            db.Dealers.Add(deletedDealer);
            await db.SaveChangesAsync();
        }

        // 3. Closed-Loop Showroom Sale at Hanoi (DLR-HN-01):
        //    Intake a fresh ISO 3779 VIN unit -> complete SalesOrder with 1,500,000 VND discount
        Guid modelId;
        using (var modelsRes = await client.GetAsync("/api/v1/motorcycles?pageSize=5"))
        {
            Assert.Equal(HttpStatusCode.OK, modelsRes.StatusCode);
            var modelsEnv = await modelsRes.Content.ReadFromJsonAsync<MotorcycleDealer.Application.Features.Showroom.PagedApiEnvelope<MotorcycleModelDto>>(JsonOpts);
            Assert.NotNull(modelsEnv);
            Assert.NotEmpty(modelsEnv.Data);
            modelId = modelsEnv.Data[0].Id;
        }

        DealerInventoryDto hanoiUnit;
        using (var intakeReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/inventories",
            hanoiStaffAuth.AccessToken,
            new CreateDealerInventoryRequest
            {
                MotorcycleModelId = modelId,
                Vin = "RLHJF5109RZ778899",
                EngineNumber = "ENG-ANL-778899",
                ColorName = "Matte Racing Red",
                ManufactureYear = 2026,
                ListedPrice = 96500000.00m
            }))
        using (var intakeRes = await client.SendAsync(intakeReq))
        {
            Assert.Equal(HttpStatusCode.Created, intakeRes.StatusCode);
            var env = await intakeRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerInventoryDto>>(JsonOpts);
            Assert.NotNull(env);
            hanoiUnit = env.Data;
        }

        SalesOrderDto completedSale;
        using (var saleReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/sales-orders",
            hanoiStaffAuth.AccessToken,
            new CreateSalesOrderRequest
            {
                CustomerEmail = "customer.silver@motorcycledealer.vn",
                DealerInventoryId = hanoiUnit.Id,
                InventoryRowVersion = hanoiUnit.RowVersion,
                DiscountAmount = 1500000.00m,
                LicensePlate = "29A1-77889",
                PaymentMethod = "BankTransfer",
                Notes = "Phase 7 analytics reconciliation sale"
            }))
        using (var saleRes = await client.SendAsync(saleReq))
        {
            Assert.Equal(HttpStatusCode.Created, saleRes.StatusCode);
            var env = await saleRes.Content.ReadFromJsonAsync<ApiEnvelope<SalesOrderDto>>(JsonOpts);
            Assert.NotNull(env);
            completedSale = env.Data;
            Assert.Equal(95000000.00m, completedSale.FinalAmount);
            Assert.Equal(1500000.00m, completedSale.DiscountAmount);
        }

        // Also insert a Cancelled SalesOrder on a separate inventory row to verify Status == 'Completed' filter
        await using (var db = _fixture.CreateScopedDbContext())
        {
            var cancelledInv = new DealerInventory(Guid.CreateVersion7())
            {
                DealerId = DatabaseSeeder.HanoiDealerId,
                MotorcycleModelId = modelId,
                VIN = "RLHJF5109RZ778890",
                EngineNumber = "ENG-ANL-778890",
                ColorName = "Pearl White",
                ManufactureYear = 2026,
                ListedPrice = 50000000.00m,
                StockStatus = "Available",
                ReceivedAtUtc = DateTimeOffset.UtcNow
            };
            db.DealerInventories.Add(cancelledInv);

            var cancelledOrder = new SalesOrder(Guid.CreateVersion7())
            {
                OrderNumber = $"SO-CANC-{Guid.NewGuid():N}"[..24],
                DealerId = DatabaseSeeder.HanoiDealerId,
                CustomerId = silverCustomerAuth.User.UserId,
                DealerInventoryId = cancelledInv.Id,
                SoldByStaffId = hanoiStaffAuth.User.UserId,
                TotalAmount = 50000000.00m,
                DiscountAmount = 0.00m,
                FinalAmount = 50000000.00m,
                PointsEarned = 0,
                PaymentMethod = "Cash",
                Status = "Cancelled",
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
            db.SalesOrders.Add(cancelledOrder);
            await db.SaveChangesAsync();
        }

        // 4. Closed-Loop Voucher Redeem + ServiceBooking + ServiceInvoice Settlement at HCMC (DLR-HCM-01)
        LoyaltyAccountSummaryDto silverLoyalty;
        using (var loyReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/loyalty/me", silverCustomerAuth.AccessToken))
        using (var loyRes = await client.SendAsync(loyReq))
        {
            Assert.Equal(HttpStatusCode.OK, loyRes.StatusCode);
            var loyEnv = await loyRes.Content.ReadFromJsonAsync<ApiEnvelope<LoyaltyAccountSummaryDto>>(JsonOpts);
            Assert.NotNull(loyEnv);
            silverLoyalty = loyEnv.Data;
        }

        Guid serviceVoucherCatalogId;
        using (var catReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/vouchers/catalog?applicableScope=ServiceOnly",
            silverCustomerAuth.AccessToken))
        using (var catRes = await client.SendAsync(catReq))
        {
            Assert.Equal(HttpStatusCode.OK, catRes.StatusCode);
            var catEnv = await catRes.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<VoucherCatalogDto>>>(JsonOpts);
            Assert.NotNull(catEnv);
            serviceVoucherCatalogId = catEnv.Data.First(c => c.IsEligibleForCaller && c.DiscountType == "FixedAmount").Id;
        }

        CustomerVoucherDto redeemedVoucher;
        using (var redeemReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/vouchers/redeem",
            silverCustomerAuth.AccessToken,
            new RedeemVoucherRequest
            {
                VoucherCatalogId = serviceVoucherCatalogId,
                AccountRowVersion = silverLoyalty.RowVersion
            }))
        using (var redeemRes = await client.SendAsync(redeemReq))
        {
            Assert.Equal(HttpStatusCode.Created, redeemRes.StatusCode);
            var env = await redeemRes.Content.ReadFromJsonAsync<ApiEnvelope<CustomerVoucherDto>>(JsonOpts);
            Assert.NotNull(env);
            redeemedVoucher = env.Data;
        }

        // Generate/pick an open slot at HCMC and book the newly onboarded vehicle with the redeemed voucher
        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        using (var genReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/time-slots/generate",
            hcmStaffAuth.AccessToken,
            new GenerateTimeSlotsRequest
            {
                FromDate = slotDate,
                ToDate = slotDate,
                SlotDurationMinutes = 60,
                MaxCapacity = 5
            }))
        using (var genRes = await client.SendAsync(genReq))
        {
            Assert.True(genRes.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK);
        }

        ServiceTimeSlotDto openSlot;
        using (var slotRes = await client.GetAsync(
            $"/api/v1/time-slots?dealerId={DatabaseSeeder.HcmDealerId:D}&fromDate={slotDate:yyyy-MM-dd}&toDate={slotDate:yyyy-MM-dd}"))
        {
            Assert.Equal(HttpStatusCode.OK, slotRes.StatusCode);
            var slotEnv = await slotRes.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>>(JsonOpts);
            Assert.NotNull(slotEnv);
            openSlot = slotEnv.Data.First(s => s.AvailableBays > 0 && !s.IsBlocked);
        }

        BookingDto booking;
        using (var bookReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/bookings",
            silverCustomerAuth.AccessToken,
            new CreateServiceBookingRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                ServiceTimeSlotId = openSlot.Id,
                SlotRowVersion = openSlot.RowVersion,
                CustomerVehicleId = completedSale.OnboardedCustomerVehicleId!.Value,
                AppliedCustomerVoucherId = redeemedVoucher.Id,
                VoucherRowVersion = redeemedVoucher.RowVersion,
                ServiceType = "PeriodicMaintenance",
                OdometerAtBookingKm = 1000,
                ReportedNotes = "1,000 km break-in service"
            }))
        using (var bookRes = await client.SendAsync(bookReq))
        {
            Assert.Equal(HttpStatusCode.Created, bookRes.StatusCode);
            var env = await bookRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts);
            Assert.NotNull(env);
            booking = env.Data;
        }

        foreach (var nextStatus in new[] { "Confirmed", "CheckedIn", "InProgress" })
        {
            using var statusReq = CreateAuthorizedJsonRequest(
                HttpMethod.Patch,
                $"/api/v1/bookings/{booking.Id:D}/status",
                hcmStaffAuth.AccessToken,
                new UpdateBookingStatusRequest
                {
                    NewStatus = nextStatus,
                    RowVersion = booking.RowVersion
                });
            using var statusRes = await client.SendAsync(statusReq);
            Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);
            var env = await statusRes.Content.ReadFromJsonAsync<ApiEnvelope<BookingDto>>(JsonOpts);
            Assert.NotNull(env);
            booking = env.Data;
        }

        ServiceInvoiceDto settledInvoice;
        using (var invReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/service-invoices",
            hcmStaffAuth.AccessToken,
            new CreateServiceInvoiceRequest
            {
                ServiceBookingId = booking.Id,
                BookingRowVersion = booking.RowVersion,
                LaborAmount = 350000.00m,
                PartsAmount = 650000.00m,
                OdometerAtCompletionKm = 1025,
                UseBookingReservedVoucher = true,
                PaymentMethod = "Card",
                WorkSummaryNotes = "Completed 1,000 km oil & filter service"
            }))
        using (var invRes = await client.SendAsync(invReq))
        {
            Assert.Equal(HttpStatusCode.Created, invRes.StatusCode);
            var env = await invRes.Content.ReadFromJsonAsync<ApiEnvelope<ServiceInvoiceDto>>(JsonOpts);
            Assert.NotNull(env);
            settledInvoice = env.Data;
            Assert.Equal(1000000.00m, settledInvoice.GrossAmount);
            Assert.True(settledInvoice.VoucherDiscountAmount > 0.00m);
            Assert.Equal(1000000.00m - settledInvoice.VoucherDiscountAmount, settledInvoice.NetPaidAmount);
        }

        // 5. Re-query all 6 Admin Analytics endpoints and reconcile exact deltas!
        var updatedOverview = await GetEnvelopeDataAsync<AnalyticsOverviewDto>(
            client,
            "/api/v1/admin/analytics/overview",
            adminAuth.AccessToken);

        // Excluded Cancelled SalesOrder (50,000,000 VND) + used NetPaidAmount (NOT GrossAmount)!
        Assert.Equal(baselineOverview.UnitsSoldCount + 1, updatedOverview.UnitsSoldCount);
        Assert.Equal(baselineOverview.ShowroomRevenueVnd + completedSale.FinalAmount, updatedOverview.ShowroomRevenueVnd);
        Assert.Equal(baselineOverview.InvoicesIssuedCount + 1, updatedOverview.InvoicesIssuedCount);
        Assert.Equal(baselineOverview.ServiceRevenueVnd + settledInvoice.NetPaidAmount, updatedOverview.ServiceRevenueVnd);
        Assert.Equal(
            baselineOverview.TotalVoucherDiscountVnd + completedSale.DiscountAmount + settledInvoice.VoucherDiscountAmount,
            updatedOverview.TotalVoucherDiscountVnd);
        Assert.Equal(
            updatedOverview.ShowroomRevenueVnd + updatedOverview.ServiceRevenueVnd,
            updatedOverview.TotalRevenueVnd);
        Assert.Equal(baselineOverview.TotalBookings + 1, updatedOverview.TotalBookings);
        Assert.Equal(baselineOverview.CompletedBookings + 1, updatedOverview.CompletedBookings);

        // Verify active 4th dealer (DLR-CT-01) is zero-filled, while soft-deleted DLR-DEL-99 is excluded!
        Assert.Equal(4, updatedOverview.Dealers.Count);
        Assert.DoesNotContain(updatedOverview.Dealers, d => d.DealerCode == "DLR-DEL-99");
        var canThoRow = Assert.Single(updatedOverview.Dealers, d => d.DealerId == zeroFillDealer.Id);
        Assert.Equal(0, canThoRow.UnitsSoldCount);
        Assert.Equal(0.00m, canThoRow.ShowroomRevenueVnd);
        Assert.Equal(0.00m, canThoRow.ServiceRevenueVnd);
        Assert.Equal(0, canThoRow.TotalBookings);
        Assert.Equal(0.00m, canThoRow.CompletionRatePct);
        Assert.Equal(0.00m, canThoRow.CancellationRatePct);

        // Revenue-by-dealer alignment & deterministic DESC sort
        var revenueChart = await GetEnvelopeDataAsync<RevenueByDealerAnalyticsDto>(
            client,
            "/api/v1/admin/analytics/revenue-by-dealer",
            adminAuth.AccessToken);
        Assert.Equal(4, revenueChart.Labels.Count);
        Assert.Equal(4, revenueChart.ShowroomSalesVnd.Count);
        Assert.Equal(4, revenueChart.WorkshopServiceVnd.Count);
        Assert.Equal(updatedOverview.ShowroomRevenueVnd, revenueChart.ShowroomRevenueVnd);
        Assert.Equal(updatedOverview.ServiceRevenueVnd, revenueChart.ServiceRevenueVnd);
        Assert.Equal(updatedOverview.TotalRevenueVnd, revenueChart.TotalRevenueVnd);
        for (var i = 1; i < revenueChart.Dealers.Count; i++)
        {
            Assert.True(revenueChart.Dealers[i - 1].TotalRevenueVnd >= revenueChart.Dealers[i].TotalRevenueVnd);
        }

        // Bookings-by-dealer alignment across 7 statuses & 4 dealers
        var bookingsChart = await GetEnvelopeDataAsync<BookingsByDealerAnalyticsDto>(
            client,
            "/api/v1/admin/analytics/bookings-by-dealer",
            adminAuth.AccessToken);
        Assert.Equal(7, bookingsChart.StatusLabels.Count);
        Assert.Equal(7, bookingsChart.StatusCounts.Count);
        Assert.Equal(7, bookingsChart.StatusBreakdown.Count);
        Assert.Equal(updatedOverview.TotalBookings, bookingsChart.StatusCounts.Sum());
        Assert.Equal(4, bookingsChart.Dealers.Count);

        // Monthly-trends current UTC month bucket reflects exact new revenue & booking
        var updatedTrends = await GetEnvelopeDataAsync<MonthlyTrendsAnalyticsDto>(
            client,
            "/api/v1/admin/analytics/monthly-trends",
            adminAuth.AccessToken);
        Assert.Equal(
            baselineTrends.Months[^1].ShowroomRevenueVnd + completedSale.FinalAmount,
            updatedTrends.Months[^1].ShowroomRevenueVnd);
        Assert.Equal(
            baselineTrends.Months[^1].ServiceRevenueVnd + settledInvoice.NetPaidAmount,
            updatedTrends.Months[^1].ServiceRevenueVnd);
        Assert.Equal(
            baselineTrends.Months[^1].TotalBookings + 1,
            updatedTrends.Months[^1].TotalBookings);

        // ?dealerId= single-branch scoping check
        var hanoiScopedOverview = await GetEnvelopeDataAsync<AnalyticsOverviewDto>(
            client,
            $"/api/v1/admin/analytics/overview?dealerId={DatabaseSeeder.HanoiDealerId:D}",
            adminAuth.AccessToken);
        var expectedHanoiRow = Assert.Single(updatedOverview.Dealers, d => d.DealerId == DatabaseSeeder.HanoiDealerId);
        Assert.Single(hanoiScopedOverview.Dealers);
        Assert.Equal(expectedHanoiRow.TotalRevenueVnd, hanoiScopedOverview.TotalRevenueVnd);
        Assert.Equal(expectedHanoiRow.TotalBookings, hanoiScopedOverview.TotalBookings);

        // Quality & Warranty dedicated endpoint reconciliation + ?dealerId= branch scoping
        var qualityWarranty = await GetEnvelopeDataAsync<QualityWarrantyAnalyticsDto>(
            client,
            "/api/v1/admin/analytics/quality-warranty",
            adminAuth.AccessToken);
        Assert.Equal(4, qualityWarranty.Dealers.Count);
        Assert.DoesNotContain(qualityWarranty.Dealers, d => d.DealerCode == "DLR-DEL-99");
        Assert.Equal(6, qualityWarranty.DefectBreakdown.Count);
        Assert.Equal(updatedOverview.AverageCsatRating, qualityWarranty.AverageCsatRating);
        Assert.Equal(updatedOverview.TotalServiceReviewsCount, qualityWarranty.TotalReviewsCount);
        Assert.Equal(updatedOverview.FiveStarReviewsCount, qualityWarranty.FiveStarReviewsCount);
        Assert.Equal(updatedOverview.UnansweredReviewsCount, qualityWarranty.UnansweredReviewsCount);
        Assert.Equal(updatedOverview.OpenWarrantyClaimsCount, qualityWarranty.OpenWarrantyClaimsCount);
        Assert.Equal(updatedOverview.CompletedWarrantyClaimsCount, qualityWarranty.CompletedWarrantyClaimsCount);
        Assert.Equal(updatedOverview.ApprovedWarrantyCoveredVnd, qualityWarranty.ApprovedWarrantyCoveredVnd);
        Assert.Equal(qualityWarranty.TotalWarrantyClaimsCount, qualityWarranty.DefectBreakdown.Sum(b => b.ClaimsCount));

        var hanoiQuality = await GetEnvelopeDataAsync<QualityWarrantyAnalyticsDto>(
            client,
            $"/api/v1/admin/analytics/quality-warranty?dealerId={DatabaseSeeder.HanoiDealerId:D}",
            adminAuth.AccessToken);
        Assert.Single(hanoiQuality.Dealers);
        Assert.Equal(expectedHanoiRow.AverageCsatRating, hanoiQuality.AverageCsatRating);
        Assert.Equal(expectedHanoiRow.ApprovedWarrantyCoveredVnd, hanoiQuality.ApprovedWarrantyCoveredVnd);

        // 6. Export CSV (/export and /export-csv alias) UTF-8 BOM + penny-for-penny 17-column cell reconciliation
        foreach (var exportPath in new[] { "/api/v1/admin/analytics/export", "/api/v1/admin/analytics/export-csv" })
        {
            using var csvReq = CreateAuthorizedRequest(HttpMethod.Get, exportPath, adminAuth.AccessToken);
            using var csvRes = await client.SendAsync(csvReq);
            Assert.Equal(HttpStatusCode.OK, csvRes.StatusCode);
            Assert.Equal("text/csv", csvRes.Content.Headers.ContentType?.MediaType);
            Assert.StartsWith(
                "dealer-analytics-",
                csvRes.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty,
                StringComparison.Ordinal);

            var csvBytes = await csvRes.Content.ReadAsByteArrayAsync();
            Assert.Equal(0xEF, csvBytes[0]);
            Assert.Equal(0xBB, csvBytes[1]);
            Assert.Equal(0xBF, csvBytes[2]);

            var parsedCsv = ParseRfc4180Csv(Encoding.UTF8.GetString(csvBytes, 3, csvBytes.Length - 3));
            Assert.Equal(updatedOverview.Dealers.Count + 1, parsedCsv.Count);
            Assert.Equal(AnalyticsCsvExporter.HeaderRow.Split(','), parsedCsv[0]);

            for (var r = 0; r < updatedOverview.Dealers.Count; r++)
            {
                var dto = updatedOverview.Dealers[r];
                var cells = parsedCsv[r + 1];
                Assert.Equal(17, cells.Length);
                Assert.Equal(dto.DealerCode, cells[0]);
                Assert.Equal(dto.DealerName, cells[1]);
                Assert.Equal(dto.City, cells[2]);
                Assert.Equal(dto.UnitsSoldCount, int.Parse(cells[3], CultureInfo.InvariantCulture));
                Assert.Equal(dto.ShowroomRevenueVnd, decimal.Parse(cells[4], CultureInfo.InvariantCulture));
                Assert.Equal(dto.InvoicesIssuedCount, int.Parse(cells[5], CultureInfo.InvariantCulture));
                Assert.Equal(dto.ServiceRevenueVnd, decimal.Parse(cells[6], CultureInfo.InvariantCulture));
                Assert.Equal(dto.TotalRevenueVnd, decimal.Parse(cells[7], CultureInfo.InvariantCulture));
                Assert.Equal(dto.TotalBookings, int.Parse(cells[8], CultureInfo.InvariantCulture));
                Assert.Equal(dto.CompletedBookings, int.Parse(cells[9], CultureInfo.InvariantCulture));
                Assert.Equal(dto.CancelledOrNoShowBookings, int.Parse(cells[10], CultureInfo.InvariantCulture));
                Assert.Equal(dto.CompletionRatePct, decimal.Parse(cells[11], CultureInfo.InvariantCulture));
                Assert.Equal(dto.CancellationRatePct, decimal.Parse(cells[12], CultureInfo.InvariantCulture));
                Assert.Equal(dto.AverageCsatRating, decimal.Parse(cells[13], CultureInfo.InvariantCulture));
                Assert.Equal(dto.TotalServiceReviewsCount, int.Parse(cells[14], CultureInfo.InvariantCulture));
                Assert.Equal(dto.OpenWarrantyClaimsCount, int.Parse(cells[15], CultureInfo.InvariantCulture));
                Assert.Equal(dto.ApprovedWarrantyCoveredVnd, decimal.Parse(cells[16], CultureInfo.InvariantCulture));
            }
        }
    }

    [Fact]
    public async Task GetMonthlyTrends_WithPlusSevenOffsetTimestampCrossingLocalMidnight_ShouldBucketStrictlyInUtcMonth()
    {
        using var client = _fixture.CreateClient();
        var adminAuth = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        var hanoiStaffAuth = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var silverCustomerAuth = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");

        const string windowQuery = "fromUtc=2026-04-01T00:00:00Z&toUtc=2026-04-30T23:59:59Z";
        var beforeApril = await GetEnvelopeDataAsync<MonthlyTrendsAnalyticsDto>(
            client,
            $"/api/v1/admin/analytics/monthly-trends?{windowQuery}&dealerId={DatabaseSeeder.HanoiDealerId:D}",
            adminAuth.AccessToken);

        var aprilBefore = Assert.Single(beforeApril.Months);
        Assert.Equal("2026-04", aprilBefore.Month);

        // Seed a Completed SalesOrder stamped at 2026-05-01T02:00:00+07:00 (which is 2026-04-30T19:00:00Z in UTC!)
        var boundaryPlusSeven = new DateTimeOffset(2026, 5, 1, 2, 0, 0, TimeSpan.FromHours(7));
        const decimal boundarySaleAmount = 72500000.00m;

        await using (var db = _fixture.CreateScopedDbContext())
        {
            var modelId = await db.MotorcycleModels.IgnoreQueryFilters().Select(m => m.Id).FirstAsync();
            var boundaryInventory = new DealerInventory(Guid.CreateVersion7())
            {
                DealerId = DatabaseSeeder.HanoiDealerId,
                MotorcycleModelId = modelId,
                VIN = "RLHJF5109RZ403019",
                EngineNumber = "ENG-UTC-403019",
                ColorName = "Obsidian Blue",
                ManufactureYear = 2026,
                ListedPrice = boundarySaleAmount,
                StockStatus = "Sold",
                ReceivedAtUtc = boundaryPlusSeven.AddDays(-5),
                SoldAtUtc = boundaryPlusSeven
            };
            db.DealerInventories.Add(boundaryInventory);

            var boundaryOrder = new SalesOrder(Guid.CreateVersion7())
            {
                OrderNumber = $"SO-UTC-{Guid.NewGuid():N}"[..24],
                DealerId = DatabaseSeeder.HanoiDealerId,
                CustomerId = silverCustomerAuth.User.UserId,
                DealerInventoryId = boundaryInventory.Id,
                SoldByStaffId = hanoiStaffAuth.User.UserId,
                TotalAmount = boundarySaleAmount,
                DiscountAmount = 0.00m,
                FinalAmount = boundarySaleAmount,
                PointsEarned = 0,
                PaymentMethod = "BankTransfer",
                Status = "Completed",
                CompletedAtUtc = boundaryPlusSeven,
                CreatedAtUtc = boundaryPlusSeven
            };
            db.SalesOrders.Add(boundaryOrder);
            await db.SaveChangesAsync();
        }

        var afterApril = await GetEnvelopeDataAsync<MonthlyTrendsAnalyticsDto>(
            client,
            $"/api/v1/admin/analytics/monthly-trends?{windowQuery}&dealerId={DatabaseSeeder.HanoiDealerId:D}",
            adminAuth.AccessToken);

        var aprilAfter = Assert.Single(afterApril.Months);
        Assert.Equal("2026-04", aprilAfter.Month);
        Assert.Equal(aprilBefore.UnitsSoldCount + 1, aprilAfter.UnitsSoldCount);
        Assert.Equal(aprilBefore.ShowroomRevenueVnd + boundarySaleAmount, aprilAfter.ShowroomRevenueVnd);
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

    private static async Task<T> GetEnvelopeDataAsync<T>(HttpClient client, string path, string accessToken)
    {
        using var req = CreateAuthorizedRequest(HttpMethod.Get, path, accessToken);
        using var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOpts);
        Assert.NotNull(env);
        Assert.True(env.Success);
        return env.Data;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path, string accessToken)
    {
        var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return req;
    }

    private static HttpRequestMessage CreateAuthorizedJsonRequest<TBody>(
        HttpMethod method,
        string path,
        string accessToken,
        TBody body)
    {
        var req = CreateAuthorizedRequest(method, path, accessToken);
        req.Content = JsonContent.Create(body);
        return req;
    }

    internal static IReadOnlyList<string[]> ParseRfc4180Csv(string csvContent)
    {
        var rows = new List<string[]>();
        var currentRow = new List<string>();
        var currentCell = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csvContent.Length; i++)
        {
            var c = csvContent[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvContent.Length && csvContent[i + 1] == '"')
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentCell.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
            }
            else if (c == '\r')
            {
                if (i + 1 < csvContent.Length && csvContent[i + 1] == '\n')
                {
                    i++;
                }

                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                rows.Add([.. currentRow]);
                currentRow.Clear();
            }
            else if (c == '\n')
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                rows.Add([.. currentRow]);
                currentRow.Clear();
            }
            else
            {
                currentCell.Append(c);
            }
        }

        if (currentCell.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentCell.ToString());
            rows.Add([.. currentRow]);
        }

        return rows;
    }
}
