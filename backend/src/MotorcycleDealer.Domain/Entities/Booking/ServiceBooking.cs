using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Booking;

public sealed class ServiceBooking : AuditableEntity, IDealerScoped, IConcurrencyProtected
{
    public ServiceBooking()
    {
    }

    public ServiceBooking(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string BookingNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid DealerId { get; set; }
    public Guid CustomerVehicleId { get; set; }
    public Guid ServiceTimeSlotId { get; set; }
    public Guid? AppliedCustomerVoucherId { get; set; }
    public string ServiceType { get; set; } = "PeriodicMaintenance";
    public string? ReportedNotes { get; set; }
    public int OdometerAtBookingKm { get; set; }
    public string Status { get; set; } = "Pending";
    public string? CancellationReason { get; set; }
    public DateTimeOffset? CheckedInAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Identity.User? Customer { get; set; }
    public Showroom.Dealer? Dealer { get; set; }
    public Garage.CustomerVehicle? CustomerVehicle { get; set; }
    public ServiceTimeSlot? ServiceTimeSlot { get; set; }
    public Loyalty.CustomerVoucher? AppliedCustomerVoucher { get; set; }
    public ServiceInvoice? ServiceInvoice { get; set; }
    public ICollection<Garage.MaintenanceReminder> ResolvedReminders { get; set; } = new List<Garage.MaintenanceReminder>();
}
