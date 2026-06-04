using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Booking;

public sealed class ServiceTimeSlot : AuditableEntity, IDealerScoped, IConcurrencyProtected
{
    public ServiceTimeSlot()
    {
    }

    public ServiceTimeSlot(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid DealerId { get; set; }
    public DateOnly SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxCapacity { get; set; } = 4;
    public int BookedCount { get; set; }
    public bool IsBlocked { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Showroom.Dealer? Dealer { get; set; }
    public ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();

    public int RemainingCapacity => Math.Max(0, MaxCapacity - BookedCount);

    public bool HasAvailableCapacity => !IsBlocked && BookedCount < MaxCapacity;
}
