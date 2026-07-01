namespace MotorcycleDealer.Application.Features.Bookings;

public sealed record VinMaintenanceReminderScanResult(
    int VehiclesEvaluated,
    int RemindersCreated,
    int RemindersSkippedExisting,
    DateTimeOffset EvaluatedAtUtc);

public interface IVinMaintenanceReminderScanner
{
    Task<VinMaintenanceReminderScanResult> ScanAndDispatchDueRemindersAsync(
        CancellationToken cancellationToken = default);
}
