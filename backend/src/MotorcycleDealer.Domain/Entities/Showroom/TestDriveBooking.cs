using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Domain.Entities.Showroom;

public sealed class TestDriveBooking : AuditableEntity, IDealerScoped, IConcurrencyProtected
{
    public TestDriveBooking()
    {
    }

    public TestDriveBooking(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string BookingNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid DealerId { get; set; }
    public Guid MotorcycleModelId { get; set; }
    public Guid? AssignedInventoryId { get; set; }
    public DateOnly PreferredDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string DriverLicenseNumber { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public TestDriveStatus Status { get; set; } = TestDriveStatus.Requested;
    public string? CustomerNotes { get; set; }
    public string? StaffNotes { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Identity.User? Customer { get; set; }
    public Dealer? Dealer { get; set; }
    public MotorcycleModel? MotorcycleModel { get; set; }
    public DealerInventory? AssignedInventory { get; set; }
}
