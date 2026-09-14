using System.Net;
using CommunityToolkit.Mvvm.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Media;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.Core.Validation;
using MotorcycleDealer.Mobile.Core.ViewModels;
using MotorcycleDealer.Mobile.Core.ViewModels.Customer;
using MotorcycleDealer.Mobile.Core.ViewModels.Public;
using MotorcycleDealer.Mobile.Core.ViewModels.Staff;
using Polly;
using Polly.Timeout;

namespace MotorcycleDealer.Mobile.Core.DependencyInjection;

/// <summary>
/// Single-call DI registration for all pure net10.0 Mobile.Core services, typed HTTP pipelines
/// with Polly v8 idempotent resilience, FluentValidation form validators, WeakReferenceMessenger,
/// encrypted SQLCipher storage, SignalR realtime client, and all 17 MVVM ViewModels.
/// The host MAUI app (Mobile.App) registers platform implementations of:
/// ISecureTokenStore, IMainThreadDispatcher, ILocalNotificationService,
/// IConnectivityMonitor, INavigationService, IHapticService, IShareService, IUserFeedbackService.
/// </summary>
public static class ServiceCollectionExtensions
{
    public const string ResiliencePipelineName = "DealerMobileResilience";

    public static IServiceCollection AddMotorcycleDealerMobileCore(
        this IServiceCollection services,
        string? defaultApiBaseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var initialBaseUrl = string.IsNullOrWhiteSpace(defaultApiBaseUrl)
            ? MobileEndpointDefaults.EmulatorBaseUrl
            : defaultApiBaseUrl;

        // Messaging, validation, localization, preferences, media resolver, logging & default no-op feedback
        services.AddValidatorsFromAssemblyContaining<LoginInputValidator>(ServiceLifetime.Singleton);
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.TryAddSingleton<IAppPreferencesStore, InMemoryAppPreferencesStore>();
        services.TryAddSingleton<IMotionPreferencesService>(DefaultMotionPreferencesService.Instance);
        services.AddSingleton<LocalizationService>();
        services.TryAddSingleton<IUserFeedbackService>(NoOpUserFeedbackService.Instance);
        services.TryAddSingleton<IAppPlatformInfo>(_ => new DefaultAppPlatformInfo());
        services.TryAddSingleton<IUserActionPlatformSink>(NoOpUserActionPlatformSink.Instance);

        // Security & Session singletons
        services.AddSingleton<SingleFlightRefreshLock>();
        services.AddSingleton<MobileSessionState>();
        services.TryAddSingleton<IApiEndpointProvider>(sp =>
            new DefaultApiEndpointProvider(sp.GetRequiredService<ISecureTokenStore>(), initialBaseUrl));
        services.AddSingleton<MotorcycleImageResolver>();
        services.AddSingleton<IMotorcycleImageResolver>(sp => sp.GetRequiredService<MotorcycleImageResolver>());

        // Persistence (SQLCipher 9-table store + allow-listed FIFO outbox)
        services.TryAddSingleton<IDatabasePathProvider>(_ => new DefaultDatabasePathProvider());
        services.AddSingleton<IMobileOfflineDb, MobileOfflineDb>();
        services.AddSingleton<IOfflineOutboxSyncService, OfflineOutboxSyncService>();

        // Dedicated non-recursive activity log uploader client & user action logger
        services.AddHttpClient(UserActionLogger.ActivityLogHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        services.AddSingleton<IUserActionLogger, UserActionLogger>();
        services.AddTransient<UserActionHttpLoggingHandler>();

        // Dedicated handler-free refresh/logout HTTP client (attaches correlation headers, zero 401 recursion!)
        services.AddHttpClient(AuthRefreshClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddHttpMessageHandler<UserActionHttpLoggingHandler>();
        services.AddSingleton<IAuthRefreshClient, AuthRefreshClient>();

        // Logout & forced security reset coordinator
        services.AddSingleton<LogoutCoordinator>();
        services.AddSingleton<ILogoutCoordinator>(sp => sp.GetRequiredService<LogoutCoordinator>());
        services.AddSingleton<ISessionResetCoordinator>(sp => sp.GetRequiredService<LogoutCoordinator>());

        // Main REST API HTTP client:
        // Outermost: UserActionHttpLoggingHandler (X-Correlation-Id, X-Client-Platform, timing logs)
        // Middle: SingleFlightTokenRefreshHandler (buffers body, handles 401 single-flight refresh & 403 revocation)
        // Inner: Polly v8 DealerMobileResilience (idempotent-only retry, circuit breaker, 15s attempt timeout)
        services.AddTransient<SingleFlightTokenRefreshHandler>();
        services.AddHttpClient(DealerMobileApiClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddHttpMessageHandler<UserActionHttpLoggingHandler>()
        .AddHttpMessageHandler<SingleFlightTokenRefreshHandler>()
        .AddResilienceHandler(ResiliencePipelineName, ConfigureDealerResilience);

        services.AddSingleton<IDealerMobileApiClient, DealerMobileApiClient>();

        // SignalR Realtime client (/hubs/notifications, 10 canonical events)
        services.AddSingleton<ISignalRMobileService, SignalRMobileService>();

        // Shell & Shared ViewModels
        services.AddSingleton<AppShellViewModel>();
        services.AddTransient<NotificationsFeedViewModel>();

        // Public ViewModels (4)
        services.AddTransient<ShowroomCatalogViewModel>();
        services.AddTransient<DealerLocatorViewModel>();
        services.AddTransient<LoginRegisterViewModel>();
        services.AddTransient<AdminWebConsoleNoticeViewModel>();

        // Customer ViewModels (5)
        services.AddTransient<CustomerHomeViewModel>();
        services.AddTransient<VinGarageViewModel>();
        services.AddTransient<BookingWizardViewModel>();
        services.AddTransient<LoyaltyPassbookViewModel>();
        services.AddTransient<CustomerWarrantyViewModel>();

        // DealerStaff ViewModels (6)
        services.AddTransient<StaffWorkbenchViewModel>();
        services.AddTransient<MobileInvoiceBuilderViewModel>();
        services.AddTransient<ShowroomVinSalesViewModel>();
        services.AddTransient<StaffTestDriveViewModel>();
        services.AddTransient<StaffWarrantyCsatViewModel>();
        services.AddTransient<SlotCapacityViewModel>();

        return services;
    }

    public static void ConfigureDealerResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(350),
            ShouldHandle = static args =>
            {
                var request = args.Context.GetRequestMessage() ?? args.Outcome.Result?.RequestMessage;
                if (!IsIdempotentRequest(request))
                {
                    return ValueTask.FromResult(false);
                }

                return ValueTask.FromResult(IsTransientHttpFailure(args.Outcome));
            }
        });

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 6,
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = static args => ValueTask.FromResult(IsTransientHttpFailure(args.Outcome))
        });

        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(15)
        });
    }

    /// <summary>
    /// Allows retries strictly for safe/idempotent HTTP methods (GET, HEAD, OPTIONS)
    /// or mutating requests carrying a non-empty explicit Idempotency-Key header.
    /// Non-idempotent POST bookings, invoices, and showroom sales are never retried.
    /// </summary>
    public static bool IsIdempotentRequest(HttpRequestMessage? request)
    {
        if (request is null)
        {
            return false;
        }

        if (request.Method == HttpMethod.Get ||
            request.Method == HttpMethod.Head ||
            request.Method == HttpMethod.Options)
        {
            return true;
        }

        return request.Headers.TryGetValues("Idempotency-Key", out var values) &&
               values.Any(v => !string.IsNullOrWhiteSpace(v));
    }

    private static bool IsTransientHttpFailure(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is HttpRequestException or TimeoutRejectedException)
        {
            return true;
        }

        if (outcome.Result is HttpResponseMessage response)
        {
            return response.StatusCode == HttpStatusCode.RequestTimeout ||
                   (int)response.StatusCode >= 500;
        }

        return false;
    }
}
