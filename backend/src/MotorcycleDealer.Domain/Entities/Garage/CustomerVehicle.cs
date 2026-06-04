using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.ValueObjects;

namespace MotorcycleDealer.Domain.Entities.Garage;

public sealed class CustomerVehicle : AuditableEntity, ISoftDeletable
{
    public CustomerVehicle()
    {
    }

    public CustomerVehicle(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string VIN { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid MotorcycleModelId { get; set; }
    public Guid? PurchasedSalesOrderId { get; set; }
    public string? LicensePlate { get; set; }
    public string? ColorName { get; set; }
    public string RegistrationSource { get; set; } = "ShowroomSale";
    public DateTimeOffset OwnershipStartDateUtc { get; set; }
    public int CurrentMileageKm { get; set; }
    public DateTimeOffset? LastServiceDateUtc { get; set; }
    public int LastServiceMileageKm { get; set; }
    public int NextDueMileageKm { get; set; }
    public DateTimeOffset NextDueDateUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Identity.User? Customer { get; set; }
    public Showroom.MotorcycleModel? MotorcycleModel { get; set; }
    public Showroom.SalesOrder? PurchasedSalesOrder { get; set; }
    public ICollection<MaintenanceReminder> MaintenanceReminders { get; set; } = new List<MaintenanceReminder>();
    public ICollection<Booking.ServiceBooking> ServiceBookings { get; set; } = new List<Booking.ServiceBooking>();

    public Vin GetValidatedVin() => Vin.Create(VIN);
}
