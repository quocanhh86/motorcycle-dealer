using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Showroom;

public sealed class Dealer : AuditableEntity, ISoftDeletable
{
    public Dealer()
    {
    }

    public Dealer(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly OpeningTime { get; set; } = new(8, 0);
    public TimeOnly ClosingTime { get; set; } = new(18, 0);
    public int DefaultSlotCapacity { get; set; } = 4;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public ICollection<Identity.User> StaffUsers { get; set; } = new List<Identity.User>();
    public ICollection<DealerInventory> Inventories { get; set; } = new List<DealerInventory>();
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<Booking.ServiceTimeSlot> ServiceTimeSlots { get; set; } = new List<Booking.ServiceTimeSlot>();
    public ICollection<Booking.ServiceBooking> ServiceBookings { get; set; } = new List<Booking.ServiceBooking>();
    public ICollection<Booking.ServiceInvoice> ServiceInvoices { get; set; } = new List<Booking.ServiceInvoice>();
}
