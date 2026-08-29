using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using MotorcycleDealer.Api.IntegrationTests.Analytics;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.Reviews;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Application.Features.Vouchers;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using MotorcycleDealer.WebApi.Security;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.Security;

public sealed class LocalDbOwaspSecurityWebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbOwaspSecurityWebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_Forensic_P7Sec_{Guid.NewGuid():N}";

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

public sealed class OwaspApiSecurityAuditTests : IClassFixture<LocalDbOwaspSecurityWebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly LocalDbOwaspSecurityWebAppFixture _fixture;

    public OwaspApiSecurityAuditTests(LocalDbOwaspSecurityWebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OwaspApi1AndApi5_BolaAndBfla_ShouldBlockCrossDealerStaffIdor_CrossCustomerIdor_AndCrossRolePrivilegeEscalation()
    {
        using var client = _fixture.CreateClient();

        var hanoiStaff = await LoginAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        var bronzeCustomer = await LoginAsync(client, "customer.bronze@motorcycledealer.vn", "Customer@123!");
        var silverCustomer = await LoginAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var admin = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");

        Guid hcmInventoryId;
        Guid hcmBookingId;
        Guid hcmInvoiceId;
        Guid hcmSalesOrderId;
        Guid hcmTestDriveId;
        Guid hcmWarrantyClaimId;
        Guid dnReviewId;
        Guid silverVehicleId;
        Guid silverInvoiceId;
        Guid silverSalesOrderId;
        Guid silverTestDriveId;
        Guid silverNotificationId;
        Guid sparePartId;
        Guid modelId;

        await using (var db = _fixture.CreateScopedDbContext())
        {
            modelId = await db.MotorcycleModels.IgnoreQueryFilters().Select(m => m.Id).FirstAsync();
            sparePartId = await db.SpareParts.IgnoreQueryFilters().Select(p => p.Id).FirstAsync();

            hcmInventoryId = await db.DealerInventories
                .IgnoreQueryFilters()
                .Where(i => i.DealerId == DatabaseSeeder.HcmDealerId)
                .Select(i => i.Id)
                .FirstAsync();

            hcmBookingId = await db.ServiceBookings
                .IgnoreQueryFilters()
                .Where(b => b.DealerId == DatabaseSeeder.HcmDealerId)
                .Select(b => b.Id)
                .FirstAsync();

            hcmInvoiceId = await db.ServiceInvoices
                .IgnoreQueryFilters()
                .Where(i => i.DealerId == DatabaseSeeder.HcmDealerId)
                .Select(i => i.Id)
                .FirstAsync();

            hcmSalesOrderId = await db.SalesOrders
                .IgnoreQueryFilters()
                .Where(o => o.DealerId == DatabaseSeeder.HcmDealerId)
                .Select(o => o.Id)
                .FirstAsync();

            hcmTestDriveId = await db.TestDriveBookings
                .IgnoreQueryFilters()
                .Where(t => t.DealerId == DatabaseSeeder.HcmDealerId)
                .Select(t => t.Id)
                .FirstAsync();

            hcmWarrantyClaimId = await db.WarrantyClaims
                .IgnoreQueryFilters()
                .Where(w => w.DealerId == DatabaseSeeder.HcmDealerId)
                .Select(w => w.Id)
                .FirstAsync();

            dnReviewId = await db.ServiceReviews
                .IgnoreQueryFilters()
                .Where(r => r.DealerId == DatabaseSeeder.DaNangDealerId)
                .Select(r => r.Id)
                .FirstAsync();

            silverVehicleId = await db.CustomerVehicles
                .IgnoreQueryFilters()
                .Where(v => v.CustomerId == silverCustomer.User.UserId && !v.IsDeleted)
                .Select(v => v.Id)
                .FirstAsync();

            silverInvoiceId = await db.ServiceInvoices
                .IgnoreQueryFilters()
                .Where(i => i.CustomerId == silverCustomer.User.UserId)
                .Select(i => i.Id)
                .FirstAsync();

            silverSalesOrderId = await db.SalesOrders
                .IgnoreQueryFilters()
                .Where(o => o.CustomerId == silverCustomer.User.UserId)
                .Select(o => o.Id)
                .FirstAsync();

            silverTestDriveId = await db.TestDriveBookings
                .IgnoreQueryFilters()
                .Where(t => t.CustomerId == silverCustomer.User.UserId)
                .Select(t => t.Id)
                .FirstAsync();

            var privateNotification = new Notification(Guid.CreateVersion7())
            {
                RecipientUserId = silverCustomer.User.UserId,
                EventType = "LoyaltyPointsUpdated",
                Title = "Private Points Alert",
                Message = "Silver customer private notification",
                IsRead = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Notifications.Add(privateNotification);
            await db.SaveChangesAsync();
            silverNotificationId = privateNotification.Id;
        }

        // 0. Anonymous 401 UNAUTHORIZED across Phase 8 & Phase 9 protected endpoints
        string[] unauthenticatedProtectedGets =
        [
            "/api/v1/test-drives",
            "/api/v1/test-drives/my",
            $"/api/v1/sales-orders/{hcmSalesOrderId:D}/receipt",
            $"/api/v1/service-invoices/{hcmInvoiceId:D}/receipt",
            "/api/v1/service-reviews",
            "/api/v1/service-reviews/my",
            "/api/v1/warranty-claims",
            "/api/v1/warranty-claims/my",
            "/api/v1/admin/analytics/quality-warranty"
        ];

        foreach (var protectedGet in unauthenticatedProtectedGets)
        {
            using var anonRes = await client.GetAsync(protectedGet);
            Assert.Equal(HttpStatusCode.Unauthorized, anonRes.StatusCode);
            Assert.Equal("application/problem+json", anonRes.Content.Headers.ContentType?.MediaType);
            using var doc = JsonDocument.Parse(await anonRes.Content.ReadAsStringAsync());
            Assert.Equal("UNAUTHORIZED", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var anonSparePartPost = await client.PostAsJsonAsync("/api/v1/spare-parts", new UpsertSparePartRequest
        {
            PartCode = "PRT-ANON-01",
            Name = "Anonymous Blocked Part",
            Category = "Filter",
            UnitLabel = "pc",
            UnitPriceVnd = 100_000m
        }))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, anonSparePartPost.StatusCode);
        }

        // 1. API1 BOLA: Hanoi DealerStaff blocked from reading/mutating Saigon (DLR-HCM-01) resources
        using (var getInvReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/inventories/{hcmInventoryId:D}",
            hanoiStaff.AccessToken))
        using (var getInvRes = await client.SendAsync(getInvReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, getInvRes.StatusCode);
            using var doc = JsonDocument.Parse(await getInvRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var createForeignInvReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/inventories",
            hanoiStaff.AccessToken,
            new CreateDealerInventoryRequest
            {
                DealerId = DatabaseSeeder.HcmDealerId,
                MotorcycleModelId = modelId,
                Vin = "RLHJF5109RZ665544",
                EngineNumber = "ENG-SEC-665544",
                ColorName = "Matte Black",
                ManufactureYear = 2026,
                ListedPrice = 85000000.00m
            }))
        using (var createForeignInvRes = await client.SendAsync(createForeignInvReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, createForeignInvRes.StatusCode);
            using var doc = JsonDocument.Parse(await createForeignInvRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var getBkgReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/bookings/{hcmBookingId:D}",
            hanoiStaff.AccessToken))
        using (var getBkgRes = await client.SendAsync(getBkgReq))
        {
            Assert.True(getBkgRes.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        using (var patchBkgReq = CreateAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/bookings/{hcmBookingId:D}/status",
            hanoiStaff.AccessToken,
            new UpdateBookingStatusRequest
            {
                NewStatus = "Confirmed",
                RowVersion = "AAAAAAAAD4E="
            }))
        using (var patchBkgRes = await client.SendAsync(patchBkgReq))
        {
            Assert.True(patchBkgRes.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        using (var getInvDocReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/service-invoices/{hcmInvoiceId:D}",
            hanoiStaff.AccessToken))
        using (var getInvDocRes = await client.SendAsync(getInvDocReq))
        {
            Assert.True(getInvDocRes.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        using (var foreignNotifReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/notifications/dealer?dealerId={DatabaseSeeder.HcmDealerId:D}",
            hanoiStaff.AccessToken))
        using (var foreignNotifRes = await client.SendAsync(foreignNotifReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, foreignNotifRes.StatusCode);
        }

        // Phase 8 & Phase 9 cross-branch DealerStaff BOLA checks (Hanoi staff -> Saigon / Da Nang resources)
        string[] foreignStaffGets =
        [
            $"/api/v1/test-drives/{hcmTestDriveId:D}",
            $"/api/v1/test-drives?dealerId={DatabaseSeeder.HcmDealerId:D}",
            $"/api/v1/sales-orders/{hcmSalesOrderId:D}/receipt",
            $"/api/v1/service-invoices/{hcmInvoiceId:D}/receipt",
            $"/api/v1/service-reviews?dealerId={DatabaseSeeder.HcmDealerId:D}",
            $"/api/v1/service-reviews/by-invoice/{hcmInvoiceId:D}",
            $"/api/v1/warranty-claims/{hcmWarrantyClaimId:D}",
            $"/api/v1/warranty-claims?dealerId={DatabaseSeeder.HcmDealerId:D}"
        ];

        foreach (var foreignStaffUrl in foreignStaffGets)
        {
            using var req = CreateAuthorizedRequest(HttpMethod.Get, foreignStaffUrl, hanoiStaff.AccessToken);
            using var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var patchForeignTdReq = CreateAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/test-drives/{hcmTestDriveId:D}/status",
            hanoiStaff.AccessToken,
            new UpdateTestDriveStatusRequest { Status = "Confirmed", RowVersion = "AAAAAAAAD4E=" }))
        using (var patchForeignTdRes = await client.SendAsync(patchForeignTdReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, patchForeignTdRes.StatusCode);
            using var doc = JsonDocument.Parse(await patchForeignTdRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var patchForeignRevReq = CreateAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/service-reviews/{dnReviewId:D}/response",
            hanoiStaff.AccessToken,
            new RespondToServiceReviewRequest { StaffResponse = "Cross-dealer reply attempt.", RowVersion = "AAAAAAAAD4E=" }))
        using (var patchForeignRevRes = await client.SendAsync(patchForeignRevReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, patchForeignRevRes.StatusCode);
            using var doc = JsonDocument.Parse(await patchForeignRevRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var patchForeignWarReq = CreateAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/warranty-claims/{hcmWarrantyClaimId:D}/status",
            hanoiStaff.AccessToken,
            new UpdateWarrantyClaimStatusRequest { Status = "UnderReview", RowVersion = "AAAAAAAAD4E=" }))
        using (var patchForeignWarRes = await client.SendAsync(patchForeignWarReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, patchForeignWarRes.StatusCode);
            using var doc = JsonDocument.Parse(await patchForeignWarRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_DEALER_SCOPE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 2. API1 BOLA: Bronze Customer blocked from Silver Customer's vehicles, invoices, and private notifications
        using (var foreignVehReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/vehicles/{silverVehicleId:D}",
            bronzeCustomer.AccessToken))
        using (var foreignVehRes = await client.SendAsync(foreignVehReq))
        {
            Assert.True(foreignVehRes.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        using (var foreignMileageReq = CreateAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/vehicles/{silverVehicleId:D}/mileage",
            bronzeCustomer.AccessToken,
            new UpdateVehicleMileageRequest { CurrentMileageKm = 99999 }))
        using (var foreignMileageRes = await client.SendAsync(foreignMileageReq))
        {
            Assert.True(foreignMileageRes.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        using (var foreignCustInvReq = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/service-invoices/{silverInvoiceId:D}",
            bronzeCustomer.AccessToken))
        using (var foreignCustInvRes = await client.SendAsync(foreignCustInvReq))
        {
            Assert.True(foreignCustInvRes.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        using (var foreignReadNotifReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/notifications/{silverNotificationId:D}/read",
            bronzeCustomer.AccessToken))
        using (var foreignReadNotifRes = await client.SendAsync(foreignReadNotifReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, foreignReadNotifRes.StatusCode);
            using var doc = JsonDocument.Parse(await foreignReadNotifRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // Phase 8 & Phase 9 cross-customer BOLA checks (Bronze customer -> Silver / Platinum resources)
        string[] foreignCustomerGets =
        [
            $"/api/v1/test-drives/{silverTestDriveId:D}",
            $"/api/v1/sales-orders/{silverSalesOrderId:D}/receipt",
            $"/api/v1/service-invoices/{silverInvoiceId:D}/receipt",
            $"/api/v1/service-reviews/by-invoice/{silverInvoiceId:D}",
            $"/api/v1/warranty-claims/{hcmWarrantyClaimId:D}"
        ];

        foreach (var foreignCustUrl in foreignCustomerGets)
        {
            using var req = CreateAuthorizedRequest(HttpMethod.Get, foreignCustUrl, bronzeCustomer.AccessToken);
            using var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var cancelForeignTdReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/test-drives/{silverTestDriveId:D}/cancel",
            bronzeCustomer.AccessToken,
            new CancelTestDriveRequest { Reason = "Cross-customer cancel attempt." }))
        using (var cancelForeignTdRes = await client.SendAsync(cancelForeignTdReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, cancelForeignTdRes.StatusCode);
            using var doc = JsonDocument.Parse(await cancelForeignTdRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", doc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var reviewForeignInvReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/service-reviews",
            bronzeCustomer.AccessToken,
            new CreateServiceReviewRequest { ServiceInvoiceId = silverInvoiceId, Rating = 5, Comment = "Cross-customer review attempt." }))
        using (var reviewForeignInvRes = await client.SendAsync(reviewForeignInvReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, reviewForeignInvRes.StatusCode);
            using var doc = JsonDocument.Parse(await reviewForeignInvRes.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN_RESOURCE_OWNER", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // 3. API5 BFLA: Customer & DealerStaff blocked from /admin/analytics/* & SpareParts.ManageCatalog; Admin/Staff blocked from Customer-exclusive self-service
        foreach (var nonAdminToken in new[] { bronzeCustomer.AccessToken, hanoiStaff.AccessToken })
        {
            foreach (var adminUrl in new[] { "/api/v1/admin/analytics/overview", "/api/v1/admin/analytics/quality-warranty" })
            {
                using var anlReq = CreateAuthorizedRequest(HttpMethod.Get, adminUrl, nonAdminToken);
                using var anlRes = await client.SendAsync(anlReq);
                Assert.Equal(HttpStatusCode.Forbidden, anlRes.StatusCode);
            }

            using var createPartReq = CreateAuthorizedJsonRequest(
                HttpMethod.Post,
                "/api/v1/spare-parts",
                nonAdminToken,
                new UpsertSparePartRequest
                {
                    PartCode = "PRT-BFLA-01",
                    Name = "Unauthorized Part",
                    Category = "Filter",
                    UnitLabel = "pc",
                    UnitPriceVnd = 120_000m
                });
            using var createPartRes = await client.SendAsync(createPartReq);
            Assert.Equal(HttpStatusCode.Forbidden, createPartRes.StatusCode);

            using var updatePartReq = CreateAuthorizedJsonRequest(
                HttpMethod.Put,
                $"/api/v1/spare-parts/{sparePartId:D}",
                nonAdminToken,
                new UpsertSparePartRequest
                {
                    PartCode = "PRT-BFLA-02",
                    Name = "Unauthorized Part Update",
                    Category = "Filter",
                    UnitLabel = "pc",
                    UnitPriceVnd = 120_000m
                });
            using var updatePartRes = await client.SendAsync(updatePartReq);
            Assert.Equal(HttpStatusCode.Forbidden, updatePartRes.StatusCode);
        }

        foreach (var custDealerQueue in new[] { "/api/v1/test-drives", "/api/v1/service-reviews", "/api/v1/warranty-claims" })
        {
            using var custQueueReq = CreateAuthorizedRequest(HttpMethod.Get, custDealerQueue, bronzeCustomer.AccessToken);
            using var custQueueRes = await client.SendAsync(custQueueReq);
            Assert.Equal(HttpStatusCode.Forbidden, custQueueRes.StatusCode);
        }

        foreach (var nonCustomerToken in new[] { admin.AccessToken, hanoiStaff.AccessToken })
        {
            using var redeemReq = CreateAuthorizedJsonRequest(
                HttpMethod.Post,
                "/api/v1/vouchers/redeem",
                nonCustomerToken,
                new RedeemVoucherRequest { VoucherCatalogId = Guid.CreateVersion7(), AccountRowVersion = "AAAAAAAAD4E=" });
            using var redeemRes = await client.SendAsync(redeemReq);
            Assert.Equal(HttpStatusCode.Forbidden, redeemRes.StatusCode);

            using var createTdReq = CreateAuthorizedJsonRequest(
                HttpMethod.Post,
                "/api/v1/test-drives",
                nonCustomerToken,
                new CreateTestDriveRequest
                {
                    DealerId = DatabaseSeeder.HanoiDealerId,
                    MotorcycleModelId = modelId,
                    PreferredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                    StartTime = "09:00",
                    EndTime = "09:45",
                    DriverLicenseNumber = "A1-99001122",
                    ContactPhone = "0912345678"
                });
            using var createTdRes = await client.SendAsync(createTdReq);
            Assert.Equal(HttpStatusCode.Forbidden, createTdRes.StatusCode);

            using var createRevReq = CreateAuthorizedJsonRequest(
                HttpMethod.Post,
                "/api/v1/service-reviews",
                nonCustomerToken,
                new CreateServiceReviewRequest { ServiceInvoiceId = silverInvoiceId, Rating = 5 });
            using var createRevRes = await client.SendAsync(createRevReq);
            Assert.Equal(HttpStatusCode.Forbidden, createRevRes.StatusCode);

            using var createWarReq = CreateAuthorizedJsonRequest(
                HttpMethod.Post,
                "/api/v1/warranty-claims",
                nonCustomerToken,
                new CreateWarrantyClaimRequest
                {
                    CustomerVehicleId = silverVehicleId,
                    DealerId = DatabaseSeeder.HcmDealerId,
                    OdometerAtClaimKm = 4_000,
                    DefectCategory = "Electrical",
                    SymptomDescription = "Non-customer self-service claim attempt."
                });
            using var createWarRes = await client.SendAsync(createWarReq);
            Assert.Equal(HttpStatusCode.Forbidden, createWarRes.StatusCode);
        }
    }

    [Fact]
    public async Task OwaspApi2_BrokenAuthentication_ShouldRejectTamperedSignature_AlgNone_AndRevokeLockedUserUnexpiredJwt()
    {
        using var client = _fixture.CreateClient();
        var customerAuth = await LoginAsync(client, "customer.bronze@motorcycledealer.vn", "Customer@123!");

        // 1. Tampered JWT payload (escalating role from Customer -> Admin while keeping original signature) -> 401
        var parts = customerAuth.AccessToken.Split('.');
        Assert.Equal(3, parts.Length);

        var escalatedPayloadJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [JwtClaimNames.Sub] = customerAuth.User.UserId.ToString("D"),
            [JwtClaimNames.Email] = customerAuth.User.Email,
            [JwtClaimNames.Role] = RoleNames.Admin,
            [JwtClaimNames.Permissions] = Permissions.AdminDefaults,
            [JwtClaimNames.Issuer] = "MotorcycleDealer.WebApi",
            [JwtClaimNames.Audience] = "MotorcycleDealer.Spa",
            [JwtClaimNames.ExpiresAt] = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        });
        var tamperedJwt = $"{parts[0]}.{Base64UrlEncode(Encoding.UTF8.GetBytes(escalatedPayloadJson))}.{parts[2]}";

        using (var tamperedReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/analytics/overview", tamperedJwt))
        using (var tamperedRes = await client.SendAsync(tamperedReq))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, tamperedRes.StatusCode);
            Assert.Equal("application/problem+json", tamperedRes.Content.Headers.ContentType?.MediaType);
        }

        // 2. Unsigned "alg": "none" JWT attack -> 401 Unauthorized
        var algNoneHeader = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var algNoneJwt = $"{algNoneHeader}.{Base64UrlEncode(Encoding.UTF8.GetBytes(escalatedPayloadJson))}.";

        using (var algNoneReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/analytics/overview", algNoneJwt))
        using (var algNoneRes = await client.SendAsync(algNoneReq))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, algNoneRes.StatusCode);
        }

        // 3. Deactivated user holding an unexpired cryptographically valid HS256 JWT -> 403 ACCOUNT_LOCKED
        var adminAuth = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        var suffix = Random.Shared.Next(10000, 99999);
        var tempStaffEmail = $"sec.staff.{suffix}@motorcycledealer.vn";

        DealerStaffDto createdStaff;
        using (var provReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId:D}/staff",
            adminAuth.AccessToken,
            new CreateDealerStaffRequest
            {
                Email = tempStaffEmail,
                Password = "TempStaff@123!",
                FullName = "Security Audit Staff",
                PhoneNumber = $"09771{suffix}",
                StaffEmployeeCode = $"EMP-SEC-{suffix}"
            }))
        using (var provRes = await client.SendAsync(provReq))
        {
            Assert.Equal(HttpStatusCode.Created, provRes.StatusCode);
            var env = await provRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerStaffDto>>(JsonOpts);
            Assert.NotNull(env);
            createdStaff = env.Data;
        }

        var staffLogin = await LoginAsync(client, tempStaffEmail, "TempStaff@123!");

        using (var lockReq = CreateAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId:D}/staff/{createdStaff.UserId:D}/status",
            adminAuth.AccessToken,
            new UpdateStaffStatusRequest
            {
                IsActive = false,
                Reason = "OWASP API2 immediate revocation test"
            }))
        using (var lockRes = await client.SendAsync(lockReq))
        {
            Assert.Equal(HttpStatusCode.OK, lockRes.StatusCode);
        }

        using (var postLockReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/auth/me", staffLogin.AccessToken))
        using (var postLockRes = await client.SendAsync(postLockReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, postLockRes.StatusCode);
            Assert.Equal("application/problem+json", postLockRes.Content.Headers.ContentType?.MediaType);
            using var doc = JsonDocument.Parse(await postLockRes.Content.ReadAsStringAsync());
            Assert.Equal("ACCOUNT_LOCKED", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task OwaspApi4_ResourceConsumption_ShouldThrottleAuthBruteForceWith429ProblemDetails_AndRejectOversizedPageSize()
    {
        // 1. Dedicated WebHostBuilder overriding RateLimiting:Auth:PermitLimit = 3 (shares fixture DB, zero extra DB created)
        await using var rateLimitedFactory = _fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Auth:PermitLimit"] = "3",
                    ["RateLimiting:Auth:WindowSeconds"] = "60",
                    ["RateLimiting:Auth:QueueLimit"] = "0"
                });
            });
        });

        using var throttledClient = rateLimitedFactory.CreateClient();
        var partitionId = $"owasp-login-burst-{Guid.NewGuid():N}";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest
                {
                    Email = "admin@motorcycledealer.vn",
                    Password = "WrongPassword@999!"
                })
            };
            req.Headers.Add(AuthRateLimitOptions.TestPartitionHeaderName, partitionId);

            using var res = await throttledClient.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        // 4th request in the same 60s window -> 429 Too Many Requests ProblemDetails + Retry-After + CORS Expose-Headers
        using (var burstReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = "admin@motorcycledealer.vn",
                Password = "WrongPassword@999!"
            })
        })
        {
            burstReq.Headers.Add(AuthRateLimitOptions.TestPartitionHeaderName, partitionId);
            burstReq.Headers.Add("Origin", "http://localhost:4200");
            using var burstRes = await throttledClient.SendAsync(burstReq);

            Assert.Equal(HttpStatusCode.TooManyRequests, burstRes.StatusCode);
            Assert.Equal("application/problem+json", burstRes.Content.Headers.ContentType?.MediaType);
            Assert.True(burstRes.Headers.RetryAfter?.Delta is { TotalSeconds: >= 1 } ||
                        burstRes.Headers.TryGetValues("Retry-After", out _));
            AssertCorsExposedHeadersPresent(burstRes);

            using var doc = JsonDocument.Parse(await burstRes.Content.ReadAsStringAsync());
            Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Too Many Requests", doc.RootElement.GetProperty("title").GetString());
            Assert.Equal("RATE_LIMIT_EXCEEDED", doc.RootElement.GetProperty("errorCode").GetString());
            Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("traceId").GetString()));
        }

        // Verify /auth/refresh and /auth/register also participate in the same AuthRateLimitOptions policy
        using (var refreshBurstReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = JsonContent.Create(new RefreshTokenRequest { RefreshToken = "invalid-token-under-burst-check-0123456789" })
        })
        {
            refreshBurstReq.Headers.Add(AuthRateLimitOptions.TestPartitionHeaderName, partitionId);
            using var refreshBurstRes = await throttledClient.SendAsync(refreshBurstReq);
            Assert.Equal(HttpStatusCode.TooManyRequests, refreshBurstRes.StatusCode);
        }

        // 2. Oversized pagination pageSize=999 & pageNumber=0 -> 400 VALIDATION_FAILED
        using var standardClient = _fixture.CreateClient();
        var adminAuth = await LoginAsync(standardClient, "admin@motorcycledealer.vn", "Admin@123!");
        var customerAuth = await LoginAsync(standardClient, "customer.silver@motorcycledealer.vn", "Customer@123!");

        string[] oversizedQueries =
        [
            "/api/v1/motorcycles?pageSize=999",
            "/api/v1/motorcycles?pageNumber=0&pageSize=20",
            "/api/v1/bookings?pageSize=999",
            "/api/v1/service-invoices?pageSize=999",
            "/api/v1/test-drives?pageSize=999",
            "/api/v1/spare-parts?pageSize=999",
            "/api/v1/service-reviews?pageSize=999",
            "/api/v1/warranty-claims?pageSize=999"
        ];

        foreach (var url in oversizedQueries)
        {
            using var req = CreateAuthorizedRequest(HttpMethod.Get, url, adminAuth.AccessToken);
            using var res = await standardClient.SendAsync(req);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_FAILED", doc.RootElement.GetProperty("errorCode").GetString());
        }

        string[] oversizedCustomerQueries =
        [
            "/api/v1/test-drives/my?pageSize=999",
            "/api/v1/service-reviews/my?pageSize=999",
            "/api/v1/warranty-claims/my?pageSize=999",
            "/api/v1/loyalty/ledger?pageSize=999"
        ];

        foreach (var url in oversizedCustomerQueries)
        {
            using var req = CreateAuthorizedRequest(HttpMethod.Get, url, customerAuth.AccessToken);
            using var res = await standardClient.SendAsync(req);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_FAILED", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task OwaspApi8_SecurityHeaders_ZeroRawSqlConcatenation_AndLiveCsvFormulaInjectionSanitization()
    {
        using var client = _fixture.CreateClient();

        // 1. Security response headers emitted across /api/v1/health, 200 OK JSON, and 401 ProblemDetails
        using (var healthRes = await client.GetAsync("/api/v1/health?probeDb=true"))
        {
            Assert.Equal(HttpStatusCode.OK, healthRes.StatusCode);
            AssertSecurityHeadersPresent(healthRes);
        }

        using (var unauthRes = await client.GetAsync("/api/v1/bookings/my"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, unauthRes.StatusCode);
            AssertSecurityHeadersPresent(unauthRes);
        }

        // 2. Source scan across backend/src confirming 0 unparameterized FromSqlRaw / ExecuteSqlRaw calls
        var backendSrcRoot = ResolveBackendSrcRoot();
        var csFiles = Directory.GetFiles(backendSrcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(csFiles);
        var unsafeSqlRegex = new Regex(@"\b(FromSqlRaw|ExecuteSqlRaw|ExecuteSqlRawAsync)\s*\(", RegexOptions.CultureInvariant);

        foreach (var file in csFiles)
        {
            var source = await File.ReadAllTextAsync(file);
            Assert.False(
                unsafeSqlRegex.IsMatch(source),
                $"Potential unparameterized raw SQL call detected in {Path.GetFileName(file)}.");
        }

        // 3. Live CSV Formula-Injection proof: create dealer with '=1+1' & '@SUM(1,1)', export CSV, verify single-quote prefix
        var adminAuth = await LoginAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        using (var createMaliciousDealerReq = CreateAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/dealers",
            adminAuth.AccessToken,
            new UpsertDealerRequest
            {
                Code = "DLR-INJ-01",
                Name = "=1+1",
                City = "@SUM(1,1)",
                Address = "100 Security Test Boulevard, Hanoi",
                PhoneNumber = "02439991122",
                ContactEmail = "sec.branch@motorcycledealer.vn",
                OpeningTime = "08:00:00",
                ClosingTime = "18:00:00",
                DefaultSlotCapacity = 3,
                IsActive = true
            }))
        using (var createRes = await client.SendAsync(createMaliciousDealerReq))
        {
            Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        }

        using (var exportReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/analytics/export", adminAuth.AccessToken))
        {
            exportReq.Headers.Add("Origin", "http://localhost:4200");
            using var exportRes = await client.SendAsync(exportReq);
            Assert.Equal(HttpStatusCode.OK, exportRes.StatusCode);
            AssertSecurityHeadersPresent(exportRes);
            AssertCorsExposedHeadersPresent(exportRes);

            var bytes = await exportRes.Content.ReadAsByteArrayAsync();
            Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);

            var csvText = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.Contains("\"'=1+1\"", csvText, StringComparison.Ordinal);
            Assert.Contains("\"'@SUM(1,1)\"", csvText, StringComparison.Ordinal);

            var rows = AdminAnalyticsIntegrationTests.ParseRfc4180Csv(csvText);
            var injectedRow = Assert.Single(rows.Skip(1), r => r[0] == "DLR-INJ-01");
            Assert.Equal("'=1+1", injectedRow[1]);
            Assert.Equal("'@SUM(1,1)", injectedRow[2]);
        }
    }

    private static void AssertCorsExposedHeadersPresent(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Access-Control-Expose-Headers", out var exposedValues));
        var combined = string.Join(",", exposedValues);
        Assert.Contains("Content-Disposition", combined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Retry-After", combined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Location", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertSecurityHeadersPresent(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var xcto));
        Assert.Contains("nosniff", xcto, StringComparer.OrdinalIgnoreCase);

        Assert.True(response.Headers.TryGetValues("X-Frame-Options", out var xfo));
        Assert.Contains("DENY", xfo, StringComparer.OrdinalIgnoreCase);

        Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var rp));
        Assert.Contains("strict-origin-when-cross-origin", rp, StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveBackendSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "backend", "src");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var siblingCandidate = Path.Combine(dir.FullName, "src");
            if (Directory.Exists(Path.Combine(siblingCandidate, "MotorcycleDealer.WebApi")))
            {
                return siblingCandidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate backend/src directory from test base directory.");
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

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
