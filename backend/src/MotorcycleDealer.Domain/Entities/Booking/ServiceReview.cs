using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Booking;

public sealed class ServiceReview : AuditableEntity, IDealerScoped, IConcurrencyProtected
{
    public ServiceReview()
    {
    }

    public ServiceReview(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string ReviewNumber { get; set; } = string.Empty;
    public Guid ServiceInvoiceId { get; set; }
    public Guid ServiceBookingId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid DealerId { get; set; }
    public Guid CustomerVehicleId { get; set; }
    public int Rating { get; set; }
    public string? QualityTags { get; set; }
    public string? Comment { get; set; }
    public int BonusPointsAwarded { get; set; }
    public string? StaffResponse { get; set; }
    public Guid? RespondedByUserId { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ServiceInvoice? ServiceInvoice { get; set; }
    public ServiceBooking? ServiceBooking { get; set; }
    public Identity.User? Customer { get; set; }
    public Showroom.Dealer? Dealer { get; set; }
    public Garage.CustomerVehicle? CustomerVehicle { get; set; }
    public Identity.User? RespondedByUser { get; set; }
}
