using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Analytics;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.Notifications;
using MotorcycleDealer.Application.Features.Reviews;
using MotorcycleDealer.Application.Features.SalesOrders;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Application.Features.Vouchers;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Infrastructure.BackgroundJobs;
using MotorcycleDealer.Infrastructure.Identity;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Persistence.Interceptors;
using MotorcycleDealer.Infrastructure.Persistence.Seed;
using MotorcycleDealer.Infrastructure.Services;
using MotorcycleDealer.Infrastructure.Services.Analytics;
using MotorcycleDealer.Infrastructure.Services.Booking;
using MotorcycleDealer.Infrastructure.Services.Invoices;
using MotorcycleDealer.Infrastructure.Services.Loyalty;
using MotorcycleDealer.Infrastructure.Services.Notifications;
using MotorcycleDealer.Infrastructure.Services.Reviews;
using MotorcycleDealer.Infrastructure.Services.Showroom;
using MotorcycleDealer.Infrastructure.Services.Warranty;

namespace MotorcycleDealer.Infrastructure;

public static class DependencyInjection
{
    public const int SqlServerCompatibilityLevel = 150;

    public const string DefaultLocalDbConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=MotorcycleDealerDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<VinMaintenanceReminderOptions>(
            configuration.GetSection(VinMaintenanceReminderOptions.SectionName));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AppendOnlyLedgerInterceptor>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DefaultLocalDbConnectionString;
        }

        services.AddDbContext<MotorcycleDealerDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(SqlServerCompatibilityLevel);
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

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<MotorcycleDealerDbContext>());
        services.AddIdentityAndDealerServices();
        services.AddShowroomAndGarageServices();
        services.AddWorkshopBookingServices();
        services.AddLoyaltyAndVoucherServices();
        services.AddInvoiceAndNotificationServices();
        services.AddTestDriveAndSparePartServices();
        services.AddReviewAndWarrantyServices();
        services.AddAnalyticsServices();
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<VinMaintenanceReminderWorker>();

        return services;
    }

    public static IServiceCollection AddIdentityAndDealerServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IAuthService, AuthService>();
        services.TryAddScoped<IDealerAdministrationService, DealerAdministrationService>();

        return services;
    }

    public static IServiceCollection AddShowroomAndGarageServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IShowroomCatalogService, ShowroomCatalogService>();
        services.TryAddScoped<IDealerInventoryService, DealerInventoryService>();
        services.TryAddScoped<ISalesOrderService, SalesOrderService>();
        services.TryAddScoped<ICustomerVehicleService, CustomerVehicleService>();

        return services;
    }

    public static IServiceCollection AddWorkshopBookingServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ITimeSlotService, TimeSlotService>();
        services.TryAddScoped<IServiceBookingService, ServiceBookingService>();
        services.TryAddScoped<IVinMaintenanceReminderScanner, VinMaintenanceReminderScanner>();

        return services;
    }

    public static IServiceCollection AddLoyaltyAndVoucherServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ILoyaltyService, LoyaltyService>();
        services.TryAddScoped<IVoucherService, VoucherService>();

        return services;
    }

    public static IServiceCollection AddInvoiceAndNotificationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IServiceInvoiceService, ServiceInvoiceService>();
        services.TryAddScoped<INotificationInboxService, NotificationInboxService>();

        return services;
    }

    public static IServiceCollection AddTestDriveAndSparePartServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ITestDriveService, TestDriveService>();
        services.TryAddScoped<ISparePartService, SparePartService>();

        return services;
    }

    public static IServiceCollection AddReviewAndWarrantyServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IServiceReviewService, ServiceReviewService>();
        services.TryAddScoped<IWarrantyClaimService, WarrantyClaimService>();

        return services;
    }

    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IAdminAnalyticsService, AdminAnalyticsService>();

        return services;
    }
}
