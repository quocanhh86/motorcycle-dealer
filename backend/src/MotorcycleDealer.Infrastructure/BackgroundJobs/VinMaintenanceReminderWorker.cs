using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotorcycleDealer.Application.Features.Bookings;

namespace MotorcycleDealer.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted background polling worker evaluating CustomerVehicles against OEM MaintenanceRules (ADR-005, FR-04).
/// Delegates deterministic VIN milestone evaluation and DeduplicationKey persistence to scoped IVinMaintenanceReminderScanner.
/// </summary>
public sealed class VinMaintenanceReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<VinMaintenanceReminderOptions> _options;
    private readonly ILogger<VinMaintenanceReminderWorker> _logger;

    public VinMaintenanceReminderWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<VinMaintenanceReminderOptions> options,
        ILogger<VinMaintenanceReminderWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerOptions = _options.Value;
        if (!workerOptions.Enabled)
        {
            _logger.LogInformation("VinMaintenanceReminderWorker is disabled via configuration.");
            return;
        }

        var sweepInterval = TimeSpan.FromSeconds(Math.Max(5, workerOptions.SweepIntervalSeconds));
        var initialDelay = TimeSpan.FromSeconds(Math.Clamp(workerOptions.InitialDelaySeconds, 0, 300));

        _logger.LogInformation(
            "VinMaintenanceReminderWorker started with InitialDelay={InitialDelay} and SweepInterval={SweepInterval}.",
            initialDelay,
            sweepInterval);

        using var timer = new PeriodicTimer(sweepInterval);

        try
        {
            if (initialDelay > TimeSpan.Zero)
            {
                await Task.Delay(initialDelay, stoppingToken);
            }

            await ExecuteSweepTickSafelyAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteSweepTickSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during graceful host shutdown.
        }
        finally
        {
            _logger.LogInformation("VinMaintenanceReminderWorker stopped cleanly.");
        }
    }

    private async Task ExecuteSweepTickSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var scanner = scope.ServiceProvider.GetRequiredService<IVinMaintenanceReminderScanner>();
            await scanner.ScanAndDispatchDueRemindersAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during VinMaintenanceReminderWorker sweep tick.");
        }
    }
}
