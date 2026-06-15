using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using MotorcycleDealer.Application;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Identity;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using MotorcycleDealer.WebApi.Hubs;
using MotorcycleDealer.WebApi.Logging;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IUserActionAuditStore, UserActionAuditStore>();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        GlobalExceptionHandler.EnrichProblemDetails(context.ProblemDetails, context.HttpContext);
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
var allowedOrigins = corsOptions.ResolveEffectiveOrigins();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsOptions.PolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders([.. AuthRateLimitOptions.ExposedCorsHeaders]);
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var allowDevJwtFallback = JwtOptions.AllowsDevelopmentFallback(builder.Environment);
var effectiveSigningKey = jwtOptions.ResolveEffectiveSigningKey(allowDevJwtFallback);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(effectiveSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtClaimNames.Sub,
            RoleClaimType = JwtClaimNames.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.Configure<AuthRateLimitOptions>(
    builder.Configuration.GetSection(AuthRateLimitOptions.SectionName));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthRateLimitOptions.PolicyName, httpContext =>
    {
        var runtimeConfig = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var runtimeEnv = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var rateSection = runtimeConfig.GetSection(AuthRateLimitOptions.SectionName);
        var rateOpts = rateSection.Get<AuthRateLimitOptions>() ?? new AuthRateLimitOptions();
        var hasExplicitPermitLimit = !string.IsNullOrWhiteSpace(
            rateSection[nameof(AuthRateLimitOptions.PermitLimit)]);

        var partitionKey = AuthRateLimitOptions.ResolvePartitionKey(httpContext, runtimeEnv);
        var isInMemoryPipe = httpContext.Connection.RemoteIpAddress is null;
        var permitLimit = rateOpts.ResolveEffectivePermitLimit(hasExplicitPermitLimit, isInMemoryPipe);
        var windowSeconds = rateOpts.ResolveEffectiveWindowSeconds();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = Math.Max(0, rateOpts.QueueLimit),
                AutoReplenishment = true
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        var runtimeConfig = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var rateOpts = runtimeConfig.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>()
                       ?? new AuthRateLimitOptions();
        var fallbackWindowSeconds = rateOpts.ResolveEffectiveWindowSeconds();

        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
            : fallbackWindowSeconds;

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        var problemDetails = GlobalExceptionHandler.CreateCustomProblemDetails(
            httpContext,
            StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            detail: $"Authentication rate limit exceeded. Please retry after {retryAfterSeconds} seconds.",
            errorCode: "RATE_LIMIT_EXCEEDED");

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

var seedOnlyRequested = args.Any(arg =>
    string.Equals(arg, "--seed-only", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "--seed", StringComparison.OrdinalIgnoreCase));

var autoMigrateSetting = app.Configuration.GetValue<bool?>("Database:AutoMigrateAndSeed");
if (seedOnlyRequested || (autoMigrateSetting ?? app.Environment.IsDevelopment()))
{
    await DatabaseSeeder.InitializeDatabaseAsync(app.Services);
    if (args.Any(arg => string.Equals(arg, "--seed-only", StringComparison.OrdinalIgnoreCase)))
    {
        return;
    }
}

app.Use(async (context, next) =>
{
    AuthRateLimitOptions.ApplySecurityResponseHeaders(context.Response.Headers);
    context.Response.OnStarting(() =>
    {
        AuthRateLimitOptions.ApplySecurityResponseHeaders(context.Response.Headers);
        return Task.CompletedTask;
    });

    await next();
});

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    if (httpContext.Response.HasStarted)
    {
        return;
    }

    var problemDetails = GlobalExceptionHandler.CreateStatusCodeProblemDetails(
        httpContext,
        httpContext.Response.StatusCode);

    httpContext.Response.ContentType = "application/problem+json";
    await httpContext.Response.WriteAsJsonAsync(
        problemDetails,
        options: null,
        contentType: "application/problem+json",
        cancellationToken: httpContext.RequestAborted);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors(CorsOptions.PolicyName);

var webRootPath = !string.IsNullOrWhiteSpace(app.Environment.WebRootPath)
    ? app.Environment.WebRootPath
    : Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var staticAssetsPath = Path.Combine(webRootPath, "assets");
if (Directory.Exists(staticAssetsPath))
{
    var staticContentTypeProvider = new FileExtensionContentTypeProvider();
    staticContentTypeProvider.Mappings[".webp"] = "image/webp";
    staticContentTypeProvider.Mappings[".avif"] = "image/avif";

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(staticAssetsPath),
        RequestPath = "/assets",
        ContentTypeProvider = staticContentTypeProvider,
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=86400";
            context.Context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        }
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<ActivePrincipalMiddleware>();
app.UseAuthorization();
app.UseMiddleware<UserActionLoggingMiddleware>();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

public partial class Program;
