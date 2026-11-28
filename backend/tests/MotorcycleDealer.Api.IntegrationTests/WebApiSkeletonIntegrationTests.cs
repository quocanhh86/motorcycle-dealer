using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Infrastructure.Identity;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Services;
using MotorcycleDealer.WebApi.Hubs;
using MotorcycleDealer.WebApi.Middleware;
using Xunit;

namespace MotorcycleDealer.Api.IntegrationTests;

public sealed class DealerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:AutoMigrateAndSeed"] = "false",
                ["MaintenanceReminderWorker:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
                ["Cors:AllowedOrigins:1"] = "http://127.0.0.1:4200"
            });
        });
    }
}

public sealed class WebApiSkeletonIntegrationTests : IClassFixture<DealerWebApplicationFactory>
{
    private readonly DealerWebApplicationFactory _factory;

    public WebApiSkeletonIntegrationTests(DealerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRootHealth_ShouldReturn200Ok()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task GetOpenApiV1Document_ShouldReturn200WithValidOpenApi3Json()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var version = doc.RootElement.GetProperty("openapi").GetString();
        Assert.NotNull(version);
        Assert.StartsWith("3.", version, StringComparison.Ordinal);

        var paths = doc.RootElement.GetProperty("paths");
        Assert.True(paths.EnumerateObject().Count() > 30);
    }

    [Fact]
    public async Task GetApiV1Health_ShouldReturn200OkWithSystemHealthDto()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SystemHealthDto>();
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload.Status);
        Assert.Equal("MotorcycleDealer.WebApi", payload.Service);
        Assert.Equal("1.0.0", payload.Version);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task GetApiV1Health_WhenProbeDbIsTrueAndDatabaseUnreachable_ShouldReturn503ProblemDetails()
    {
        await using var degradedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IApplicationDbContext>();
                services.AddScoped<IApplicationDbContext>(_ => new StubApplicationDbContext(canConnect: false));
            });
        });

        using var client = degradedFactory.CreateClient();
        using var response = await client.GetAsync("/api/v1/health?probeDb=true");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(503, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("DATABASE_UNAVAILABLE", doc.RootElement.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task CorsPreflight_FromConfiguredSpaOrigin_ShouldAllowCredentials()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/health");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        using var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("http://localhost:4200", origins);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentials));
        Assert.Contains("true", credentials);
    }

    [Fact]
    public async Task CorsPreflight_FromUntrustedOrigin_ShouldNotGrantAccessControlAllowOrigin()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/health");
        request.Headers.Add("Origin", "https://malicious.example.org");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task UnknownEndpoint_ShouldReturn404Rfc7807ProblemDetailsWithTraceId()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("RESOURCE_NOT_FOUND", root.GetProperty("errorCode").GetString());
        Assert.Equal("/api/v1/does-not-exist", root.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task SignalRNegotiate_WithoutJwt_ShouldReturn401ProblemDetails_AndWithJwt_ShouldReturn200ConnectionId()
    {
        using var client = _factory.CreateClient();

        using var unauthResponse = await client.PostAsync("/hubs/notifications/negotiate?negotiateVersion=1", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthResponse.StatusCode);
        Assert.Equal("application/problem+json", unauthResponse.Content.Headers.ContentType?.MediaType);

        using (var errDoc = JsonDocument.Parse(await unauthResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(401, errDoc.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("UNAUTHORIZED", errDoc.RootElement.GetProperty("errorCode").GetString());
            Assert.False(string.IsNullOrWhiteSpace(errDoc.RootElement.GetProperty("traceId").GetString()));
        }

        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var issued = jwtService.GenerateAccessToken(new JwtTokenSubject(
            UserId: Guid.CreateVersion7(),
            Email: "staff.hn@motorcycledealer.vn",
            FullName: "Tran Thi Lan",
            Role: RoleNames.DealerStaff,
            DealerId: Guid.CreateVersion7(),
            Permissions: Permissions.DealerStaffDefaults));

        using var authRequest = new HttpRequestMessage(HttpMethod.Post, "/hubs/notifications/negotiate?negotiateVersion=1");
        authRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);

        using var authResponse = await client.SendAsync(authRequest);
        Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);

        using var okDoc = JsonDocument.Parse(await authResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(okDoc.RootElement.GetProperty("connectionId").GetString()));
    }

    [Fact]
    public async Task DynamicPermissionPolicyProvider_ShouldResolveAll48CanonicalPermissions_AndReturnNullForUnknownPolicy()
    {
        using var scope = _factory.Services.CreateScope();
        var policyProvider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        Assert.Equal(48, Permissions.All.Count);
        foreach (var permission in Permissions.All)
        {
            var policy = await policyProvider.GetPolicyAsync(permission);
            Assert.NotNull(policy);
        }

        var typoPolicy = await policyProvider.GetPolicyAsync("Permissions.Typo.Invalid");
        Assert.Null(typoPolicy);
    }

    [Theory]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    [InlineData("Testing", true)]
    public void JwtSigningKeyFallback_ShouldBeRestrictedStrictlyToDevelopmentAndTesting(
        string environmentName,
        bool shouldAllowFallback)
    {
        var env = new StubHostEnvironment { EnvironmentName = environmentName };
        var options = new JwtOptions { SigningKey = "SET_VIA_USER_SECRETS_OR_ENV_JWT_SIGNING_KEY" };
        var service = new JwtTokenService(Options.Create(options), new SystemDateTimeProvider(), env);
        var subject = new JwtTokenSubject(
            Guid.CreateVersion7(),
            "user@motorcycledealer.vn",
            "Test User",
            RoleNames.Customer,
            null,
            Permissions.CustomerDefaults);

        Assert.Equal(shouldAllowFallback, JwtOptions.AllowsDevelopmentFallback(env));

        if (shouldAllowFallback)
        {
            var token = service.GenerateAccessToken(subject);
            Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
                options.ResolveEffectiveSigningKey(JwtOptions.AllowsDevelopmentFallback(env)));
            Assert.Throws<InvalidOperationException>(() => service.GenerateAccessToken(subject));
        }
    }

    [Fact]
    public async Task NotificationHub_OnConnectedAsync_ShouldAbortUnscopedDealerStaff_AndEnrollValidDealerStaff()
    {
        var userId = Guid.CreateVersion7();
        var validDealerId = Guid.CreateVersion7();

        // 1. DealerStaff missing dealer_id claim MUST be aborted with zero group enrollments
        var unscopedHub = new NotificationHub(NullLogger<NotificationHub>.Instance);
        var unscopedContext = new StubHubCallerContext(
            "conn-unscoped-staff",
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString("D")),
                new Claim("role", RoleNames.DealerStaff)
            ], "Bearer")));
        var unscopedGroups = new StubGroupManager();
        unscopedHub.Context = unscopedContext;
        unscopedHub.Groups = unscopedGroups;

        await unscopedHub.OnConnectedAsync();

        Assert.True(unscopedContext.Aborted);
        Assert.Empty(unscopedGroups.AddedGroups);

        // 2. DealerStaff with valid dealer_id joins user-{userId} and dealer-{dealerId}
        var scopedHub = new NotificationHub(NullLogger<NotificationHub>.Instance);
        var scopedContext = new StubHubCallerContext(
            "conn-valid-staff",
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString("D")),
                new Claim("role", RoleNames.DealerStaff),
                new Claim("dealer_id", validDealerId.ToString("D"))
            ], "Bearer")));
        var scopedGroups = new StubGroupManager();
        scopedHub.Context = scopedContext;
        scopedHub.Groups = scopedGroups;

        await scopedHub.OnConnectedAsync();

        Assert.False(scopedContext.Aborted);
        Assert.Contains(NotificationHub.BuildUserGroup(userId), scopedGroups.AddedGroups);
        Assert.Contains(NotificationHub.BuildDealerGroup(validDealerId), scopedGroups.AddedGroups);
    }

    [Fact]
    public void AppendOnlyLedgerInterceptor_ShouldBlockPointLedgerSalesOrderServiceInvoiceAndLineItemMutationsAndDeletes()
    {
        var interceptor = new AppendOnlyLedgerInterceptor();
        using var db = CreateInterceptorSandboxDbContext(interceptor);

        var ledger = new PointLedgerEntry { PointsDelta = 100 };
        var invoice = new ServiceInvoice { NetPaidAmount = 500_000m };
        var lineItem = new ServiceInvoiceLineItem { LineTotalVnd = 250_000m };
        var order = new SalesOrder { Status = "Completed" };

        db.LedgerEntries.Add(ledger);
        db.Invoices.Add(invoice);
        db.InvoiceLineItems.Add(lineItem);
        db.Orders.Add(order);

        // Added state is permitted for all four
        InvokeInterceptorSavingChanges(interceptor, db);
        db.ChangeTracker.AcceptAllChanges();

        // 1. PointLedgerEntry: Modified -> blocked
        ledger.PointsDelta = 999;
        var ledgerModEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_LEDGER_VIOLATION", ledgerModEx.ErrorCode);
        db.Entry(ledger).State = EntityState.Unchanged;

        // 2. PointLedgerEntry: Deleted -> blocked
        db.Entry(ledger).State = EntityState.Deleted;
        var ledgerDelEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_LEDGER_VIOLATION", ledgerDelEx.ErrorCode);
        db.Entry(ledger).State = EntityState.Unchanged;

        // 3. ServiceInvoice: Modified -> blocked (zero mutable columns in Table 16)
        invoice.NetPaidAmount = 100_000m;
        var invModEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_FINANCIAL_RECORD_VIOLATION", invModEx.ErrorCode);
        db.Entry(invoice).State = EntityState.Unchanged;

        // 4. ServiceInvoice: Deleted -> blocked
        db.Entry(invoice).State = EntityState.Deleted;
        var invDelEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_FINANCIAL_RECORD_VIOLATION", invDelEx.ErrorCode);
        db.Entry(invoice).State = EntityState.Unchanged;

        // 5. ServiceInvoiceLineItem: Modified & Deleted -> blocked
        lineItem.LineTotalVnd = 999_000m;
        var lineModEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_FINANCIAL_RECORD_VIOLATION", lineModEx.ErrorCode);
        db.Entry(lineItem).State = EntityState.Unchanged;

        db.Entry(lineItem).State = EntityState.Deleted;
        var lineDelEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_FINANCIAL_RECORD_VIOLATION", lineDelEx.ErrorCode);
        db.Entry(lineItem).State = EntityState.Unchanged;

        // 6. SalesOrder: Modified & Deleted -> BOTH blocked per ADR-003 4-table immutability
        order.Status = "Cancelled";
        var ordModEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_FINANCIAL_RECORD_VIOLATION", ordModEx.ErrorCode);
        db.Entry(order).State = EntityState.Unchanged;

        db.Entry(order).State = EntityState.Deleted;
        var ordDelEx = Assert.Throws<DomainException>(() => InvokeInterceptorSavingChanges(interceptor, db));
        Assert.Equal("IMMUTABLE_FINANCIAL_RECORD_VIOLATION", ordDelEx.ErrorCode);
    }

    [Fact]
    public async Task GlobalExceptionHandler_ShouldMapDomainValidationConflictAndSanitizeProduction500()
    {
        var prodEnvironment = new StubHostEnvironment { EnvironmentName = Environments.Production };
        var handler = new GlobalExceptionHandler(prodEnvironment, NullLogger<GlobalExceptionHandler>.Instance);

        var validationContext = CreateHttpContext("/api/v1/vehicles");
        var validationEx = new ValidationException([new ValidationFailure("Vin", "Invalid ISO 3779 VIN.")]);
        Assert.True(await handler.TryHandleAsync(validationContext, validationEx, CancellationToken.None));
        Assert.Equal(StatusCodes.Status400BadRequest, validationContext.Response.StatusCode);
        using (var doc = ReadBodyJson(validationContext))
        {
            Assert.Equal("VALIDATION_FAILED", doc.RootElement.GetProperty("errorCode").GetString());
            Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("vin", out _));
            Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("traceId").GetString()));
        }

        var conflictContext = CreateHttpContext("/api/v1/bookings");
        var conflictEx = new ConflictException("Slot full.", "SLOT_CAPACITY_EXHAUSTED");
        Assert.True(await handler.TryHandleAsync(conflictContext, conflictEx, CancellationToken.None));
        Assert.Equal(StatusCodes.Status409Conflict, conflictContext.Response.StatusCode);
        using (var doc = ReadBodyJson(conflictContext))
        {
            Assert.Equal("SLOT_CAPACITY_EXHAUSTED", doc.RootElement.GetProperty("errorCode").GetString());
        }

        var unhandledContext = CreateHttpContext("/api/v1/sales-orders");
        var secretSqlError = new InvalidOperationException("Sensitive SQL detail: SELECT * FROM dbo.Users");
        Assert.True(await handler.TryHandleAsync(unhandledContext, secretSqlError, CancellationToken.None));
        Assert.Equal(StatusCodes.Status500InternalServerError, unhandledContext.Response.StatusCode);
        using (var doc = ReadBodyJson(unhandledContext))
        {
            var detail = doc.RootElement.GetProperty("detail").GetString() ?? string.Empty;
            Assert.DoesNotContain("Sensitive SQL detail", detail, StringComparison.Ordinal);
            Assert.Equal("INTERNAL_SERVER_ERROR", doc.RootElement.GetProperty("errorCode").GetString());
        }
    }

    private static InterceptorSandboxDbContext CreateInterceptorSandboxDbContext(SaveChangesInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<InterceptorSandboxDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=InterceptorUnitCheckDb;Trusted_Connection=True")
            .AddInterceptors(interceptor)
            .Options;

        return new InterceptorSandboxDbContext(options);
    }

    private static void InvokeInterceptorSavingChanges(AppendOnlyLedgerInterceptor interceptor, DbContext db)
    {
        var eventData = new DbContextEventData(
            eventDefinition: null!,
            messageGenerator: static (_, _) => string.Empty,
            context: db);

        interceptor.SavingChanges(eventData, default);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id-001"
        };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static JsonDocument ReadBodyJson(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body);
    }

    private sealed class PointLedgerEntry : BaseEntity, IAppendOnlyEntity
    {
        public int PointsDelta { get; set; }
    }

    private sealed class ServiceInvoice : BaseEntity, IImmutableFinancialEntity
    {
        public decimal NetPaidAmount { get; set; }
    }

    private sealed class ServiceInvoiceLineItem : BaseEntity, IImmutableFinancialEntity
    {
        public decimal LineTotalVnd { get; set; }
    }

    private sealed class SalesOrder : BaseEntity, IImmutableFinancialEntity
    {
        public string Status { get; set; } = "Pending";
    }

    private sealed class InterceptorSandboxDbContext : DbContext
    {
        public InterceptorSandboxDbContext(DbContextOptions<InterceptorSandboxDbContext> options)
            : base(options)
        {
        }

        public DbSet<PointLedgerEntry> LedgerEntries => Set<PointLedgerEntry>();
        public DbSet<ServiceInvoice> Invoices => Set<ServiceInvoice>();
        public DbSet<ServiceInvoiceLineItem> InvoiceLineItems => Set<ServiceInvoiceLineItem>();
        public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    }

    private sealed class StubApplicationDbContext : IApplicationDbContext
    {
        private readonly bool _canConnect;

        public StubApplicationDbContext(bool canConnect) => _canConnect = canConnect;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(_canConnect);
    }

    private sealed class StubHubCallerContext : HubCallerContext
    {
        public StubHubCallerContext(string connectionId, ClaimsPrincipal user)
        {
            ConnectionId = connectionId;
            User = user;
        }

        public override string ConnectionId { get; }
        public override string? UserIdentifier => User?.FindFirstValue("sub");
        public override ClaimsPrincipal? User { get; }
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public bool Aborted { get; private set; }
        public override void Abort() => Aborted = true;
    }

    private sealed class StubGroupManager : IGroupManager
    {
        public List<string> AddedGroups { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            AddedGroups.Add(groupName);
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            AddedGroups.Remove(groupName);
            return Task.CompletedTask;
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "MotorcycleDealer.WebApi";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
