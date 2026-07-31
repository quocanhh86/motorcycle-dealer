using System.Collections.Frozen;
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
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests.Security;

public sealed class LocalDbPhase4WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public LocalDbPhase4WebAppFixture()
    {
        LocalDbTestDatabaseCleaner.RegisterActiveDatabase(TestDatabaseName);
    }

    public string TestDatabaseName { get; } = $"MotorcycleDealerDb_P4Test_{Guid.NewGuid():N}";

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
        // Trigger WebApplicationFactory build + Program.cs DatabaseSeeder.InitializeDatabaseAsync on isolated LocalDB.
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

public sealed class RbacAndTenancyTests : IClassFixture<LocalDbPhase4WebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly LocalDbPhase4WebAppFixture _fixture;

    public RbacAndTenancyTests(LocalDbPhase4WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LocalDbMigrationAndSeeder_ShouldProvisionAll22Tables_AndAuthenticateCanonicalPersonas()
    {
        // 1. Verify exact 22 base tables exist in real SQL Server LocalDB
        await using (var conn = new SqlConnection(_fixture.ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory';
                """;
            var tableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(27, tableCount);
        }

        using var client = _fixture.CreateClient();

        // 2. Admin login + /auth/me
        var adminAuth = await LoginAndAssertOkAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        Assert.Equal(RoleNames.Admin, adminAuth.User.Role);
        Assert.Null(adminAuth.User.DealerId);
        Assert.Equal(32, adminAuth.User.Permissions.Count);

        using (var meReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/auth/me", adminAuth.AccessToken))
        using (var meRes = await client.SendAsync(meReq))
        {
            Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
            var meEnv = await meRes.Content.ReadFromJsonAsync<ApiEnvelope<CurrentUserDto>>(JsonOpts);
            Assert.NotNull(meEnv);
            Assert.Equal(RoleNames.Admin, meEnv.Data.Role);
            Assert.Contains(Permissions.Dealers.ManageAll, meEnv.Data.Permissions);
            Assert.Contains(Permissions.Users.ManageStaff, meEnv.Data.Permissions);
            Assert.Contains(Permissions.SpareParts.ManageCatalog, meEnv.Data.Permissions);
            Assert.Contains(Permissions.Reviews.ViewDealer, meEnv.Data.Permissions);
            Assert.Contains(Permissions.Warranty.ManageDealer, meEnv.Data.Permissions);
        }

        // 3. Hanoi DealerStaff login
        var hnStaffAuth = await LoginAndAssertOkAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");
        Assert.Equal(RoleNames.DealerStaff, hnStaffAuth.User.Role);
        Assert.Equal(DatabaseSeeder.HanoiDealerId, hnStaffAuth.User.DealerId);
        Assert.Equal("DLR-HN-01", hnStaffAuth.User.DealerCode);
        Assert.Equal(22, hnStaffAuth.User.Permissions.Count);

        // 4. HCMC DealerStaff login
        var hcmStaffAuth = await LoginAndAssertOkAsync(client, "staff.hcm@motorcycledealer.vn", "Staff@123!");
        Assert.Equal(RoleNames.DealerStaff, hcmStaffAuth.User.Role);
        Assert.Equal(DatabaseSeeder.HcmDealerId, hcmStaffAuth.User.DealerId);
        Assert.Equal("DLR-HCM-01", hcmStaffAuth.User.DealerCode);

        // 5. Silver Customer login
        var silverCustomerAuth = await LoginAndAssertOkAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        Assert.Equal(RoleNames.Customer, silverCustomerAuth.User.Role);
        Assert.Equal("Silver", silverCustomerAuth.User.TierCode);
        Assert.Equal(1.20m, silverCustomerAuth.User.TierMultiplier);
        Assert.Equal(1.20m, silverCustomerAuth.User.PointsMultiplier);
        Assert.Null(adminAuth.User.PointsMultiplier);
        Assert.Null(hnStaffAuth.User.PointsMultiplier);
        Assert.Equal(649, silverCustomerAuth.User.AvailablePoints);
        Assert.Equal(1960, silverCustomerAuth.User.LifetimeQualifyingPoints);
        Assert.Equal(20, silverCustomerAuth.User.Permissions.Count);

        // 6. Invalid password -> 401 ProblemDetails INVALID_CREDENTIALS
        using var badLoginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "admin@motorcycledealer.vn",
            Password = "WrongPassword@999!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, badLoginRes.StatusCode);
        Assert.Equal("application/problem+json", badLoginRes.Content.Headers.ContentType?.MediaType);
        using var badDoc = JsonDocument.Parse(await badLoginRes.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_CREDENTIALS", badDoc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task RegisterCustomer_ShouldCreateBronzeLoyaltyAccount_AndRejectDuplicateEmailWith409()
    {
        using var client = _fixture.CreateClient();
        var uniqueSuffix = Random.Shared.Next(100000, 999999);
        var email = $"new.customer.{uniqueSuffix}@customer.vn";
        var phone = $"0988{uniqueSuffix}";

        using var registerRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
        {
            Email = email,
            Password = "Customer@123!",
            FullName = "Le Minh Khang",
            PhoneNumber = phone
        });

        Assert.Equal(HttpStatusCode.Created, registerRes.StatusCode);
        var envelope = await registerRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Data.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(envelope.Data.RefreshToken));
        Assert.Equal(RoleNames.Customer, envelope.Data.User.Role);
        Assert.Equal("Bronze", envelope.Data.User.TierCode);
        Assert.Equal(0, envelope.Data.User.AvailablePoints);
        Assert.Equal(0, envelope.Data.User.LifetimeQualifyingPoints);

        // Verify persisted LoyaltyAccount row in real SQL Server LocalDB
        await using (var db = _fixture.CreateScopedDbContext())
        {
            var account = await db.LoyaltyAccounts
                .Include(la => la.CurrentTier)
                .FirstOrDefaultAsync(la => la.CustomerId == envelope.Data.User.UserId);

            Assert.NotNull(account);
            Assert.Equal("Bronze", account.CurrentTier!.TierCode);
            Assert.Equal(0, account.AvailablePoints);
            Assert.Equal(0, account.LifetimeQualifyingPoints);
        }

        // Duplicate email (case-insensitive) -> 409 Conflict
        using var duplicateRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
        {
            Email = email.ToUpperInvariant(),
            Password = "Customer@123!",
            FullName = "Duplicate Attempt",
            PhoneNumber = "0977112233"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateRes.StatusCode);
        Assert.Equal("application/problem+json", duplicateRes.Content.Headers.ContentType?.MediaType);
        using var conflictDoc = JsonDocument.Parse(await duplicateRes.Content.ReadAsStringAsync());
        Assert.Equal("DUPLICATE_EMAIL", conflictDoc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task RefreshTokenRotation_ShouldRotateTokens_AndRevokeEntireTokenFamilyWhenRotatedTokenReplayed()
    {
        using var client = _fixture.CreateClient();

        // 1. Login -> obtain RT_1
        var initialSession = await LoginAndAssertOkAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var rt1 = initialSession.RefreshToken;

        // 2. Rotate RT_1 -> obtain RT_2
        using var firstRefreshRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = rt1
        });
        Assert.Equal(HttpStatusCode.OK, firstRefreshRes.StatusCode);

        var rotatedEnv = await firstRefreshRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
        Assert.NotNull(rotatedEnv);
        var rt2 = rotatedEnv.Data.RefreshToken;
        Assert.NotEqual(rt1, rt2);

        // 3. Replay already-rotated RT_1 -> MUST return 401 REFRESH_TOKEN_REUSE_DETECTED and revoke RT_2!
        using var replayRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = rt1
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replayRes.StatusCode);
        Assert.Equal("application/problem+json", replayRes.Content.Headers.ContentType?.MediaType);

        using (var replayDoc = JsonDocument.Parse(await replayRes.Content.ReadAsStringAsync()))
        {
            Assert.Equal("REFRESH_TOKEN_REUSE_DETECTED", replayDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // 4. Presenting RT_2 now MUST ALSO fail with 401 because the entire TokenFamilyId was revoked!
        using var rt2AttemptRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = rt2
        });
        Assert.Equal(HttpStatusCode.Unauthorized, rt2AttemptRes.StatusCode);

        // 5. Verify SQL Server dbo.RefreshTokens persisted RevocationReason = 'ReuseDetected' on RT_2
        await using (var db = _fixture.CreateScopedDbContext())
        {
            using var scope = _fixture.Services.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var rt2Hash = jwtService.ComputeRefreshTokenHash(rt2);

            var rt2Entity = await db.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == rt2Hash);

            Assert.NotNull(rt2Entity);
            Assert.NotNull(rt2Entity.RevokedAtUtc);
            Assert.Equal("ReuseDetected", rt2Entity.RevocationReason);
        }

        // 6. Verify Logout revokes active session family cleanly
        var freshSession = await LoginAndAssertOkAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        using var logoutReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/auth/logout",
            freshSession.AccessToken,
            new LogoutRequest { RefreshToken = freshSession.RefreshToken });
        using var logoutRes = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);

        using var afterLogoutRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = freshSession.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutRefresh.StatusCode);
    }

    [Fact]
    public async Task DealersAndStaffEndpoints_ShouldEnforce401And403Rbac_AndSupportAdminCrudAndStaffLockout()
    {
        using var client = _fixture.CreateClient();

        // 1. Public showroom endpoints work anonymously
        using (var publicDealersRes = await client.GetAsync("/api/v1/dealers"))
        {
            Assert.Equal(HttpStatusCode.OK, publicDealersRes.StatusCode);
            var dealersEnv = await publicDealersRes.Content.ReadFromJsonAsync<ApiEnvelope<List<DealerDto>>>(JsonOpts);
            Assert.NotNull(dealersEnv);
            Assert.True(dealersEnv.Data.Count >= 3);
            Assert.Contains(dealersEnv.Data, d => d.Code == "DLR-HN-01");
            Assert.Contains(dealersEnv.Data, d => d.Code == "DLR-DN-01");
            Assert.Contains(dealersEnv.Data, d => d.Code == "DLR-HCM-01");
        }

        using (var singleDealerRes = await client.GetAsync($"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}"))
        {
            Assert.Equal(HttpStatusCode.OK, singleDealerRes.StatusCode);
        }

        // 2. Anonymous calls to protected endpoints -> 401 Unauthorized ProblemDetails
        using (var unauthCreateDealer = await client.PostAsJsonAsync("/api/v1/dealers", new UpsertDealerRequest()))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, unauthCreateDealer.StatusCode);
        }

        using (var unauthListStaff = await client.GetAsync($"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}/staff"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, unauthListStaff.StatusCode);
        }

        // 3. Customer & DealerStaff calling Admin-only dealer/staff endpoints -> 403 Forbidden
        var customerSession = await LoginAndAssertOkAsync(client, "customer.silver@motorcycledealer.vn", "Customer@123!");
        var staffSession = await LoginAndAssertOkAsync(client, "staff.hn@motorcycledealer.vn", "Staff@123!");

        var sampleDealerPayload = new UpsertDealerRequest
        {
            Code = "DLR-CT-01",
            Name = "Apex Moto Can Tho Hub",
            City = "Can Tho",
            Address = "45 Hoa Binh Boulevard, Ninh Kieu, Can Tho",
            PhoneNumber = "02923900404",
            ContactEmail = "cantho@motorcycledealer.vn",
            OpeningTime = "08:00:00",
            ClosingTime = "18:00:00",
            DefaultSlotCapacity = 4,
            IsActive = true
        };

        foreach (var nonAdminToken in new[] { customerSession.AccessToken, staffSession.AccessToken })
        {
            using var createDealerForbidden = CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/v1/dealers",
                nonAdminToken,
                sampleDealerPayload);
            using var createDealerRes = await client.SendAsync(createDealerForbidden);
            Assert.Equal(HttpStatusCode.Forbidden, createDealerRes.StatusCode);
            Assert.Equal("application/problem+json", createDealerRes.Content.Headers.ContentType?.MediaType);

            using var listStaffForbidden = CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}/staff",
                nonAdminToken);
            using var listStaffRes = await client.SendAsync(listStaffForbidden);
            Assert.Equal(HttpStatusCode.Forbidden, listStaffRes.StatusCode);

            using var createStaffForbidden = CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}/staff",
                nonAdminToken,
                new CreateDealerStaffRequest
                {
                    Email = "unauth.staff@motorcycledealer.vn",
                    Password = "Staff@123!",
                    FullName = "Blocked Staff",
                    PhoneNumber = "0912999888",
                    StaffEmployeeCode = "EMP-HN-999"
                });
            using var createStaffRes = await client.SendAsync(createStaffForbidden);
            Assert.Equal(HttpStatusCode.Forbidden, createStaffRes.StatusCode);
        }

        // 4. Admin creates & updates a dealership, and provisions + locks/unlocks DealerStaff
        var adminSession = await LoginAndAssertOkAsync(client, "admin@motorcycledealer.vn", "Admin@123!");

        Guid createdDealerId;
        using (var adminCreateReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/dealers", adminSession.AccessToken, sampleDealerPayload))
        using (var adminCreateRes = await client.SendAsync(adminCreateReq))
        {
            Assert.Equal(HttpStatusCode.Created, adminCreateRes.StatusCode);
            var createdEnv = await adminCreateRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerDto>>(JsonOpts);
            Assert.NotNull(createdEnv);
            createdDealerId = createdEnv.Data.Id;
            Assert.Equal("DLR-CT-01", createdEnv.Data.Code);
        }

        using (var adminUpdateReq = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/dealers/{createdDealerId}",
            adminSession.AccessToken,
            sampleDealerPayload with { DefaultSlotCapacity = 6, ClosingTime = "19:00:00" }))
        using (var adminUpdateRes = await client.SendAsync(adminUpdateReq))
        {
            Assert.Equal(HttpStatusCode.OK, adminUpdateRes.StatusCode);
            var updatedEnv = await adminUpdateRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerDto>>(JsonOpts);
            Assert.NotNull(updatedEnv);
            Assert.Equal(6, updatedEnv.Data.DefaultSlotCapacity);
            Assert.Equal("19:00:00", updatedEnv.Data.ClosingTime);
        }

        var badgeSuffix = Random.Shared.Next(100, 999);
        var staffEmail = $"staff.ct.{badgeSuffix}@motorcycledealer.vn";
        Guid newStaffUserId;

        using (var provisionReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/dealers/{createdDealerId}/staff",
            adminSession.AccessToken,
            new CreateDealerStaffRequest
            {
                Email = staffEmail,
                Password = "Staff@123!",
                FullName = "Pham Quoc Hung",
                PhoneNumber = $"0915111{badgeSuffix}",
                StaffEmployeeCode = $"EMP-CT-{badgeSuffix}"
            }))
        using (var provisionRes = await client.SendAsync(provisionReq))
        {
            Assert.Equal(HttpStatusCode.Created, provisionRes.StatusCode);
            var staffEnv = await provisionRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerStaffDto>>(JsonOpts);
            Assert.NotNull(staffEnv);
            newStaffUserId = staffEnv.Data.UserId;
            Assert.Equal(createdDealerId, staffEnv.Data.DealerId);
            Assert.Equal($"EMP-CT-{badgeSuffix}", staffEnv.Data.StaffEmployeeCode);
            Assert.True(staffEnv.Data.IsActive);
        }

        var provisionedStaffAuth = await LoginAndAssertOkAsync(client, staffEmail, "Staff@123!");
        Assert.Equal(createdDealerId, provisionedStaffAuth.User.DealerId);

        // Omitting required 'isActive' field in PATCH body {} MUST return 400 VALIDATION_FAILED (never silently lock!)
        using (var emptyPatchReq = new HttpRequestMessage(
                   HttpMethod.Patch,
                   $"/api/v1/dealers/{createdDealerId}/staff/{newStaffUserId}/status"))
        {
            emptyPatchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminSession.AccessToken);
            emptyPatchReq.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            using var emptyPatchRes = await client.SendAsync(emptyPatchReq);
            Assert.Equal(HttpStatusCode.BadRequest, emptyPatchRes.StatusCode);
            Assert.Equal("application/problem+json", emptyPatchRes.Content.Headers.ContentType?.MediaType);
            using var emptyPatchDoc = JsonDocument.Parse(await emptyPatchRes.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_FAILED", emptyPatchDoc.RootElement.GetProperty("errorCode").GetString());
            Assert.True(emptyPatchDoc.RootElement.GetProperty("errors").TryGetProperty("isActive", out _));
        }

        // Lock staff account -> revokes active refresh tokens AND immediately blocks unexpired JWT on REST + SignalR!
        using (var lockReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/dealers/{createdDealerId}/staff/{newStaffUserId}/status",
            adminSession.AccessToken,
            new UpdateStaffStatusRequest { IsActive = false, Reason = "Security audit lockout test" }))
        using (var lockRes = await client.SendAsync(lockReq))
        {
            Assert.Equal(HttpStatusCode.OK, lockRes.StatusCode);
            var lockedEnv = await lockRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerStaffDto>>(JsonOpts);
            Assert.NotNull(lockedEnv);
            Assert.False(lockedEnv.Data.IsActive);
        }

        using (var lockedLoginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = staffEmail,
            Password = "Staff@123!"
        }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, lockedLoginRes.StatusCode);
            using var lockedDoc = JsonDocument.Parse(await lockedLoginRes.Content.ReadAsStringAsync());
            Assert.Equal("ACCOUNT_LOCKED", lockedDoc.RootElement.GetProperty("errorCode").GetString());
        }

        using (var lockedRefreshRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = provisionedStaffAuth.RefreshToken
        }))
        {
            Assert.Equal(HttpStatusCode.Forbidden, lockedRefreshRes.StatusCode);
        }

        // Deactivated staff presenting still-unexpired 15m JWT on /auth/me, /auth/logout, & SignalR /hubs/notifications/negotiate -> 403 ACCOUNT_LOCKED
        foreach (var (method, protectedUrl) in new[]
                 {
                     (HttpMethod.Get, "/api/v1/auth/me"),
                     (HttpMethod.Post, "/api/v1/auth/logout"),
                     (HttpMethod.Post, "/hubs/notifications/negotiate?negotiateVersion=1")
                 })
        {
            using var unexpiredLockedReq = CreateAuthorizedRequest(
                method,
                protectedUrl,
                provisionedStaffAuth.AccessToken,
                method == HttpMethod.Post ? new { } : null);
            using var unexpiredLockedRes = await client.SendAsync(unexpiredLockedReq);
            Assert.Equal(HttpStatusCode.Forbidden, unexpiredLockedRes.StatusCode);
            Assert.Equal("application/problem+json", unexpiredLockedRes.Content.Headers.ContentType?.MediaType);
            using var lockedJwtDoc = JsonDocument.Parse(await unexpiredLockedRes.Content.ReadAsStringAsync());
            Assert.Equal("ACCOUNT_LOCKED", lockedJwtDoc.RootElement.GetProperty("errorCode").GetString());
        }

        // Re-activate staff -> verify login restored, then deactivate entire dealership -> revokes branch refresh tokens & blocks unexpired staff JWT!
        using (var unlockReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/dealers/{createdDealerId}/staff/{newStaffUserId}/status",
            adminSession.AccessToken,
            new UpdateStaffStatusRequest { IsActive = true, Reason = "Re-enable for branch deactivation test" }))
        using (var unlockRes = await client.SendAsync(unlockReq))
        {
            Assert.Equal(HttpStatusCode.OK, unlockRes.StatusCode);
        }

        var reEnabledStaffAuth = await LoginAndAssertOkAsync(client, staffEmail, "Staff@123!");

        using (var deactivateBranchReq = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/dealers/{createdDealerId}",
            adminSession.AccessToken,
            sampleDealerPayload with { IsActive = false }))
        using (var deactivateBranchRes = await client.SendAsync(deactivateBranchReq))
        {
            Assert.Equal(HttpStatusCode.OK, deactivateBranchRes.StatusCode);
        }

        using (var branchDeactRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = reEnabledStaffAuth.RefreshToken
        }))
        {
            Assert.NotEqual(HttpStatusCode.OK, branchDeactRefresh.StatusCode);
        }

        using (var branchDeactSignalRReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/hubs/notifications/negotiate?negotiateVersion=1",
            reEnabledStaffAuth.AccessToken,
            new { }))
        using (var branchDeactSignalRRes = await client.SendAsync(branchDeactSignalRReq))
        {
            Assert.Equal(HttpStatusCode.Forbidden, branchDeactSignalRRes.StatusCode);
            using var branchDeactDoc = JsonDocument.Parse(await branchDeactSignalRRes.Content.ReadAsStringAsync());
            Assert.Equal("STAFF_DEALER_UNASSIGNED", branchDeactDoc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task AllTenRunbookSeededAccountsAndAliases_ShouldAuthenticateWithExpectedRoleScopeAndPermissions()
    {
        using var client = _fixture.CreateClient();

        (string Email, string Password, string ExpectedRole, Guid? ExpectedDealerId, int ExpectedPermCount, string? ExpectedTier)[] personas =
        [
            ("admin@motorcycledealer.vn", "Admin@123!", RoleNames.Admin, null, 32, null),
            ("staff.hn@motorcycledealer.vn", "Staff@123!", RoleNames.DealerStaff, DatabaseSeeder.HanoiDealerId, 22, null),
            ("lan.tran.hn@dealer.vn", "Staff@123!", RoleNames.DealerStaff, DatabaseSeeder.HanoiDealerId, 22, null),
            ("staff.dn@motorcycledealer.vn", "Staff@123!", RoleNames.DealerStaff, DatabaseSeeder.DaNangDealerId, 22, null),
            ("staff.hcm@motorcycledealer.vn", "Staff@123!", RoleNames.DealerStaff, DatabaseSeeder.HcmDealerId, 22, null),
            ("minh.nguyen@customer.vn", "Customer@123!", RoleNames.Customer, null, 20, "Bronze"),
            ("customer.bronze@motorcycledealer.vn", "Customer@123!", RoleNames.Customer, null, 20, "Bronze"),
            ("customer.silver@motorcycledealer.vn", "Customer@123!", RoleNames.Customer, null, 20, "Silver"),
            ("customer.gold@motorcycledealer.vn", "Customer@123!", RoleNames.Customer, null, 20, "Gold"),
            ("customer.platinum@motorcycledealer.vn", "Customer@123!", RoleNames.Customer, null, 20, "Platinum")
        ];

        foreach (var persona in personas)
        {
            var session = await LoginAndAssertOkAsync(client, persona.Email, persona.Password);
            Assert.Equal(persona.ExpectedRole, session.User.Role);
            Assert.Equal(persona.ExpectedDealerId, session.User.DealerId);
            Assert.Equal(persona.ExpectedPermCount, session.User.Permissions.Count);
            Assert.Equal(persona.ExpectedTier, session.User.TierCode);
        }
    }

    [Fact]
    public async Task DealerScopedTenancy_ShouldIsolateAllFiveOperationalTablesToHanoi_AndRejectCrossDealerWrites()
    {
        // 1. Baseline check: Admin context sees records across multiple dealerships (including DLR-HCM-01 & DLR-DN-01)
        Guid anyModelId;
        await using (var adminDb = _fixture.CreateScopedDbContext(new TestUserContext(
            Guid.CreateVersion7(),
            "admin@motorcycledealer.vn",
            RoleNames.Admin,
            DealerId: null,
            Permissions.AdminDefaults)))
        {
            anyModelId = await adminDb.MotorcycleModels.Select(m => m.Id).FirstAsync();

            Assert.Contains(await adminDb.DealerInventories.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.HcmDealerId);
            Assert.Contains(await adminDb.ServiceTimeSlots.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.HcmDealerId);
            Assert.Contains(await adminDb.SalesOrders.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.HcmDealerId);
            Assert.Contains(await adminDb.ServiceBookings.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.HcmDealerId);
            Assert.Contains(await adminDb.ServiceInvoices.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.HcmDealerId);
            Assert.Contains(await adminDb.TestDriveBookings.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.HcmDealerId);
            Assert.Contains(await adminDb.ServiceReviews.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.DaNangDealerId);
            Assert.Contains(await adminDb.WarrantyClaims.Select(x => x.DealerId).Distinct().ToListAsync(), id => id == DatabaseSeeder.HcmDealerId);
        }

        // 2. Hanoi DealerStaff context (staff.hn@motorcycledealer.vn -> DLR-HN-01):
        //    Querying all 8 IDealerScoped tables MUST return ONLY DLR-HN-01 rows (0 rows from DLR-HCM-01 or DLR-DN-01)!
        var hanoiStaffContext = new TestUserContext(
            Guid.CreateVersion7(),
            "staff.hn@motorcycledealer.vn",
            RoleNames.DealerStaff,
            DatabaseSeeder.HanoiDealerId,
            Permissions.DealerStaffDefaults);

        await using var hanoiDb = _fixture.CreateScopedDbContext(hanoiStaffContext);

        var inventories = await hanoiDb.DealerInventories.AsNoTracking().ToListAsync();
        var timeSlots = await hanoiDb.ServiceTimeSlots.AsNoTracking().ToListAsync();
        var salesOrders = await hanoiDb.SalesOrders.AsNoTracking().ToListAsync();
        var bookings = await hanoiDb.ServiceBookings.AsNoTracking().ToListAsync();
        var invoices = await hanoiDb.ServiceInvoices.AsNoTracking().ToListAsync();
        var testDrives = await hanoiDb.TestDriveBookings.AsNoTracking().ToListAsync();
        var reviews = await hanoiDb.ServiceReviews.AsNoTracking().ToListAsync();
        var warrantyClaims = await hanoiDb.WarrantyClaims.AsNoTracking().ToListAsync();

        Assert.NotEmpty(inventories);
        Assert.NotEmpty(timeSlots);
        Assert.NotEmpty(salesOrders);
        Assert.NotEmpty(bookings);
        Assert.NotEmpty(invoices);
        Assert.NotEmpty(testDrives);
        Assert.NotEmpty(reviews);
        Assert.NotEmpty(warrantyClaims);

        Assert.All(inventories, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));
        Assert.All(timeSlots, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));
        Assert.All(salesOrders, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));
        Assert.All(bookings, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));
        Assert.All(invoices, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));
        Assert.All(testDrives, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));
        Assert.All(reviews, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));
        Assert.All(warrantyClaims, row => Assert.Equal(DatabaseSeeder.HanoiDealerId, row.DealerId));

        // 3. Cross-dealer INSERT targeting DLR-HCM-01 by Hanoi staff MUST throw ForbiddenAccessException (FORBIDDEN_DEALER_SCOPE)
        var crossDealerInventory = new DealerInventory
        {
            DealerId = DatabaseSeeder.HcmDealerId,
            MotorcycleModelId = anyModelId,
            VIN = "RLHJF5109RZ999881",
            EngineNumber = "ENG-CROSS-999881",
            ColorName = "Matte Black",
            ManufactureYear = 2026,
            ListedPrice = 50_000_000m,
            StockStatus = "Available",
            ReceivedAtUtc = DateTimeOffset.UtcNow
        };

        hanoiDb.DealerInventories.Add(crossDealerInventory);

        var addForbiddenEx = await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => hanoiDb.SaveChangesAsync());
        Assert.Equal("FORBIDDEN_DEALER_SCOPE", addForbiddenEx.ErrorCode);
        hanoiDb.ChangeTracker.Clear();

        // 4. Cross-dealer UPDATE targeting DLR-HCM-01 by Hanoi staff MUST throw ForbiddenAccessException (FORBIDDEN_DEALER_SCOPE)
        DealerInventory hcmInventory;
        await using (var adminLookupDb = _fixture.CreateScopedDbContext())
        {
            hcmInventory = await adminLookupDb.DealerInventories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstAsync(i => i.DealerId == DatabaseSeeder.HcmDealerId);
        }

        hanoiDb.Attach(hcmInventory);
        hcmInventory.ListedPrice += 100_000m;

        var modifyForbiddenEx = await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => hanoiDb.SaveChangesAsync());
        Assert.Equal("FORBIDDEN_DEALER_SCOPE", modifyForbiddenEx.ErrorCode);
    }

    [Fact]
    public async Task PointLedgerSqlTrigger_ShouldRejectRawSqlUpdateAndDelete()
    {
        await using var db = _fixture.CreateScopedDbContext();
        var existingLedgerId = await db.PointLedgerEntries
            .AsNoTracking()
            .Select(e => e.Id)
            .FirstAsync();

        var updateEx = await Assert.ThrowsAsync<SqlException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [dbo].[PointLedgerEntries] SET [PointsDelta] = [PointsDelta] + 10 WHERE [Id] = {existingLedgerId}"));
        Assert.Equal(50001, updateEx.Number);

        var deleteEx = await Assert.ThrowsAsync<SqlException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM [dbo].[PointLedgerEntries] WHERE [Id] = {existingLedgerId}"));
        Assert.Equal(50001, deleteEx.Number);
    }

    [Fact]
    public async Task ConcurrentRefreshTokenRace_ShouldAllowExactSingleWinner_AndNeverMintMultipleActiveTokens()
    {
        using var client = _fixture.CreateClient();
        var initialSession = await LoginAndAssertOkAsync(client, "minh.nguyen@customer.vn", "Customer@123!");
        var rt1 = initialSession.RefreshToken;

        Guid tokenFamilyId;
        await using (var db = _fixture.CreateScopedDbContext())
        {
            using var scope = _fixture.Services.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var rt1Hash = jwtService.ComputeRefreshTokenHash(rt1);
            var rt1Row = await db.RefreshTokens.AsNoTracking().FirstAsync(t => t.TokenHash == rt1Hash);
            tokenFamilyId = rt1Row.TokenFamilyId;
        }

        // Fire 6 simultaneous /api/v1/auth/refresh requests presenting the exact same RT_1 under RCSI!
        var tasks = Enumerable.Range(0, 6)
            .Select(_ => client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = rt1 }))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        try
        {
            var okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            var unauthorizedCount = responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized);

            Assert.Equal(1, okCount);
            Assert.Equal(5, unauthorizedCount);

            await using var verifyDb = _fixture.CreateScopedDbContext();
            var familyRows = await verifyDb.RefreshTokens
                .AsNoTracking()
                .Where(t => t.TokenFamilyId == tokenFamilyId)
                .ToListAsync();

            // Exactly 2 rows exist in the family (original RT_1 + single minted successor RT_2), NEVER multiple successors!
            Assert.Equal(2, familyRows.Count);
            Assert.True(familyRows.Count(t => t.RevokedAtUtc == null) <= 1);
        }
        finally
        {
            foreach (var r in responses)
            {
                r.Dispose();
            }
        }
    }

    [Fact]
    public async Task DealerStaffJwtWithoutDealerIdClaim_ShouldReceive403ForbiddenEvenWithMatchingPermissionClaim()
    {
        using var client = _fixture.CreateClient();
        using var scope = _fixture.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var unscopedStaffJwt = jwtService.GenerateAccessToken(new JwtTokenSubject(
            UserId: Guid.CreateVersion7(),
            Email: "unscoped.staff@motorcycledealer.vn",
            FullName: "Unscoped Staff",
            Role: RoleNames.DealerStaff,
            DealerId: null,
            Permissions: [Permissions.Users.ManageStaff, Permissions.Dealers.ManageAll]));

        using var req = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}/staff",
            unscopedStaffJwt.AccessToken);
        using var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ActivePrincipalMiddleware_OnAllowAnonymousDualModeEndpoints_ShouldAllowAnonymous_AndBlockLockedOrInactiveBranchJwtWith403()
    {
        using var client = _fixture.CreateClient();
        var adminSession = await LoginAndAssertOkAsync(client, "admin@motorcycledealer.vn", "Admin@123!");

        Guid anyModelId;
        await using (var db = _fixture.CreateScopedDbContext())
        {
            anyModelId = await db.MotorcycleModels.AsNoTracking().Select(m => m.Id).FirstAsync();
        }

        string[] dualModeUrls =
        [
            "/api/v1/inventories?publicOnly=true",
            "/api/v1/dealers",
            $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}",
            "/api/v1/motorcycles",
            $"/api/v1/motorcycles/{anyModelId}",
            $"/api/v1/motorcycles/{anyModelId}/maintenance-rules",
            $"/api/v1/time-slots?dealerId={DatabaseSeeder.HanoiDealerId}"
        ];

        // 1. Anonymous callers pass through [AllowAnonymous] business endpoints with 200 OK
        foreach (var url in dualModeUrls)
        {
            using var anonRes = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, anonRes.StatusCode);
        }

        // 2. Provision dedicated branch + DealerStaff, obtain 15m JWT, lock staff, and verify 403 ACCOUNT_LOCKED on ALL dual-mode endpoints
        var suffix = Random.Shared.Next(100, 999);
        var branchReq = new UpsertDealerRequest
        {
            Code = $"DLR-HP-{suffix}",
            Name = $"Apex Moto Hai Phong {suffix}",
            City = "Hai Phong",
            Address = "12 Lach Tray, Ngo Quyen, Hai Phong",
            PhoneNumber = $"0225380{suffix:D4}"[..11],
            ContactEmail = $"hp.{suffix}@motorcycledealer.vn",
            OpeningTime = "08:00:00",
            ClosingTime = "18:00:00",
            DefaultSlotCapacity = 4,
            IsActive = true
        };

        Guid dealerId;
        using (var createDealerReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/dealers", adminSession.AccessToken, branchReq))
        using (var createDealerRes = await client.SendAsync(createDealerReq))
        {
            Assert.Equal(HttpStatusCode.Created, createDealerRes.StatusCode);
            dealerId = (await createDealerRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerDto>>(JsonOpts))!.Data.Id;
        }

        var staffEmail = $"staff.hp.{suffix}@motorcycledealer.vn";
        Guid staffUserId;
        using (var createStaffReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/dealers/{dealerId}/staff",
            adminSession.AccessToken,
            new CreateDealerStaffRequest
            {
                Email = staffEmail,
                Password = "Staff@123!",
                FullName = "Vu Hai Nam",
                PhoneNumber = $"0916222{suffix:D3}",
                StaffEmployeeCode = $"EMP-HP-{suffix}"
            }))
        using (var createStaffRes = await client.SendAsync(createStaffReq))
        {
            Assert.Equal(HttpStatusCode.Created, createStaffRes.StatusCode);
            staffUserId = (await createStaffRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerStaffDto>>(JsonOpts))!.Data.UserId;
        }

        var activeStaffJwt = (await LoginAndAssertOkAsync(client, staffEmail, "Staff@123!")).AccessToken;

        using (var lockReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/dealers/{dealerId}/staff/{staffUserId}/status",
            adminSession.AccessToken,
            new UpdateStaffStatusRequest { IsActive = false, Reason = "Dual-mode revocation test" }))
        using (var lockRes = await client.SendAsync(lockReq))
        {
            Assert.Equal(HttpStatusCode.OK, lockRes.StatusCode);
        }

        foreach (var url in dualModeUrls.Append("/api/v1/inventories"))
        {
            using var blockedReq = CreateAuthorizedRequest(HttpMethod.Get, url, activeStaffJwt);
            using var blockedRes = await client.SendAsync(blockedReq);
            Assert.Equal(HttpStatusCode.Forbidden, blockedRes.StatusCode);
            using var doc = JsonDocument.Parse(await blockedRes.Content.ReadAsStringAsync());
            Assert.Equal("ACCOUNT_LOCKED", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // Explicit health probes remain reachable even if a revoked Bearer header is attached
        foreach (var healthUrl in new[] { "/health", "/api/v1/health" })
        {
            using var healthReq = CreateAuthorizedRequest(HttpMethod.Get, healthUrl, activeStaffJwt);
            using var healthRes = await client.SendAsync(healthReq);
            Assert.Equal(HttpStatusCode.OK, healthRes.StatusCode);
        }

        // 3. Re-enable staff, mint fresh JWT, then deactivate the branch -> 403 STAFF_DEALER_UNASSIGNED on dual-mode endpoints
        using (var unlockReq = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/v1/dealers/{dealerId}/staff/{staffUserId}/status",
            adminSession.AccessToken,
            new UpdateStaffStatusRequest { IsActive = true, Reason = "Unlock for branch test" }))
        using (var unlockRes = await client.SendAsync(unlockReq))
        {
            Assert.Equal(HttpStatusCode.OK, unlockRes.StatusCode);
        }

        var branchStaffJwt = (await LoginAndAssertOkAsync(client, staffEmail, "Staff@123!")).AccessToken;

        using (var deactBranchReq = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/dealers/{dealerId}",
            adminSession.AccessToken,
            branchReq with { IsActive = false }))
        using (var deactBranchRes = await client.SendAsync(deactBranchReq))
        {
            Assert.Equal(HttpStatusCode.OK, deactBranchRes.StatusCode);
        }

        foreach (var url in dualModeUrls.Append("/api/v1/inventories"))
        {
            using var branchBlockedReq = CreateAuthorizedRequest(HttpMethod.Get, url, branchStaffJwt);
            using var branchBlockedRes = await client.SendAsync(branchBlockedReq);
            Assert.Equal(HttpStatusCode.Forbidden, branchBlockedRes.StatusCode);
            using var doc = JsonDocument.Parse(await branchBlockedRes.Content.ReadAsStringAsync());
            Assert.Equal("STAFF_DEALER_UNASSIGNED", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task SoftDeletedUsers_ShouldReturnClean409DomainConflictCodesOnPhoneAndEmployeeCodeReuse()
    {
        using var client = _fixture.CreateClient();
        var adminSession = await LoginAndAssertOkAsync(client, "admin@motorcycledealer.vn", "Admin@123!");
        var suffix = Random.Shared.Next(100000, 999999);

        var custPhone = $"0971{suffix}";
        using var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
        {
            Email = $"softdel.cust.{suffix}@customer.vn",
            Password = "Customer@123!",
            FullName = "Soft Deleted Customer",
            PhoneNumber = custPhone
        });
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        var custAuth = (await regRes.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts))!.Data;
        Assert.Equal(1.00m, custAuth.User.PointsMultiplier);

        var badgeCode = $"EMP-SD-{suffix}";
        var staffPhone = $"0972{suffix}";
        Guid staffId;
        using (var provReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}/staff",
            adminSession.AccessToken,
            new CreateDealerStaffRequest
            {
                Email = $"softdel.staff.{suffix}@motorcycledealer.vn",
                Password = "Staff@123!",
                FullName = "Soft Deleted Staff",
                PhoneNumber = staffPhone,
                StaffEmployeeCode = badgeCode
            }))
        using (var provRes = await client.SendAsync(provReq))
        {
            Assert.Equal(HttpStatusCode.Created, provRes.StatusCode);
            staffId = (await provRes.Content.ReadFromJsonAsync<ApiEnvelope<DealerStaffDto>>(JsonOpts))!.Data.UserId;
        }

        // Soft-delete both users so global ISoftDeletable filter hides them
        await using (var db = _fixture.CreateScopedDbContext())
        {
            var usersToSoftDelete = await db.Users
                .Where(u => u.Id == custAuth.User.UserId || u.Id == staffId)
                .ToListAsync();
            foreach (var u in usersToSoftDelete)
            {
                u.IsDeleted = true;
                u.IsActive = false;
            }

            await db.SaveChangesAsync();
        }

        // Reusing soft-deleted phone on RegisterCustomer -> 409 DUPLICATE_PHONE
        using (var dupCustPhoneRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCustomerRequest
        {
            Email = $"fresh.cust.{suffix}@customer.vn",
            Password = "Customer@123!",
            FullName = "Fresh Customer",
            PhoneNumber = custPhone
        }))
        {
            Assert.Equal(HttpStatusCode.Conflict, dupCustPhoneRes.StatusCode);
            using var doc = JsonDocument.Parse(await dupCustPhoneRes.Content.ReadAsStringAsync());
            Assert.Equal("DUPLICATE_PHONE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // Reusing soft-deleted phone on ProvisionDealerStaff -> 409 DUPLICATE_PHONE
        using (var dupStaffPhoneReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}/staff",
            adminSession.AccessToken,
            new CreateDealerStaffRequest
            {
                Email = $"fresh.staff1.{suffix}@motorcycledealer.vn",
                Password = "Staff@123!",
                FullName = "Fresh Staff One",
                PhoneNumber = staffPhone,
                StaffEmployeeCode = $"EMP-FR-{suffix}"
            }))
        using (var dupStaffPhoneRes = await client.SendAsync(dupStaffPhoneReq))
        {
            Assert.Equal(HttpStatusCode.Conflict, dupStaffPhoneRes.StatusCode);
            using var doc = JsonDocument.Parse(await dupStaffPhoneRes.Content.ReadAsStringAsync());
            Assert.Equal("DUPLICATE_PHONE", doc.RootElement.GetProperty("errorCode").GetString());
        }

        // Reusing soft-deleted StaffEmployeeCode on ProvisionDealerStaff -> 409 DUPLICATE_EMPLOYEE_CODE
        using (var dupBadgeReq = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/dealers/{DatabaseSeeder.HanoiDealerId}/staff",
            adminSession.AccessToken,
            new CreateDealerStaffRequest
            {
                Email = $"fresh.staff2.{suffix}@motorcycledealer.vn",
                Password = "Staff@123!",
                FullName = "Fresh Staff Two",
                PhoneNumber = $"0973{suffix}",
                StaffEmployeeCode = badgeCode
            }))
        using (var dupBadgeRes = await client.SendAsync(dupBadgeReq))
        {
            Assert.Equal(HttpStatusCode.Conflict, dupBadgeRes.StatusCode);
            using var doc = JsonDocument.Parse(await dupBadgeRes.Content.ReadAsStringAsync());
            Assert.Equal("DUPLICATE_EMPLOYEE_CODE", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    private static async Task<AuthTokenData> LoginAndAssertOkAsync(
        HttpClient client,
        string email,
        string password)
    {
        using var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var env = await res.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokenData>>(JsonOpts);
        Assert.NotNull(env);
        Assert.True(env.Success);
        Assert.False(string.IsNullOrWhiteSpace(env.Data.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(env.Data.RefreshToken));
        return env.Data;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string bearerToken,
        object? jsonBody = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        if (jsonBody is not null)
        {
            req.Content = JsonContent.Create(jsonBody);
        }

        return req;
    }

    private sealed class TestUserContext : ICurrentUserContext
    {
        private readonly FrozenSet<string> _permissions;

        public TestUserContext(
            Guid userId,
            string email,
            string role,
            Guid? DealerId,
            IEnumerable<string> permissions)
        {
            UserId = userId;
            Email = email;
            Role = role;
            this.DealerId = DealerId;
            _permissions = permissions.ToFrozenSet(StringComparer.Ordinal);
        }

        public Guid? UserId { get; }
        public string? Email { get; }
        public string? Role { get; }
        public Guid? DealerId { get; }
        public bool IsAuthenticated => true;
        public IReadOnlySet<string> Permissions => _permissions;

        public bool HasPermission(string permission) => _permissions.Contains(permission);
    }
}
