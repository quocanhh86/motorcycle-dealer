namespace MotorcycleDealer.Infrastructure.BackgroundJobs;

public sealed class VinMaintenanceReminderOptions
{
    public const string SectionName = "MaintenanceReminderWorker";

    public bool Enabled { get; set; } = true;
    public int SweepIntervalSeconds { get; set; } = 3600;
    public int InitialDelaySeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 200;
    public int MileageLeadKm { get; set; } = 300;
    public int DaysLeadWindow { get; set; } = 14;
}
