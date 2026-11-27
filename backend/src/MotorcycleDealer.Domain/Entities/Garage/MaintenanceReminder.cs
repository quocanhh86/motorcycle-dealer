using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Garage;

public sealed class MaintenanceReminder : AuditableEntity
{
    public MaintenanceReminder()
    {
    }

    public MaintenanceReminder(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid CustomerVehicleId { get; set; }
    public Guid MaintenanceRuleId { get; set; }
    public int TargetMileageKm { get; set; }
    public DateTimeOffset DueDateUtc { get; set; }
    public string TriggerReason { get; set; } = string.Empty;
    public string DeduplicationKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid? ResolvedBookingId { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }

    public CustomerVehicle? CustomerVehicle { get; set; }
    public MaintenanceRule? MaintenanceRule { get; set; }
    public Booking.ServiceBooking? ResolvedBooking { get; set; }
}
