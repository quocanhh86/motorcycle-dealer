using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Domain.Entities.Garage;

public sealed class WarrantyClaim : AuditableEntity, IDealerScoped, IConcurrencyProtected
{
    public WarrantyClaim()
    {
    }

    public WarrantyClaim(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string ClaimNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid CustomerVehicleId { get; set; }
    public Guid DealerId { get; set; }
    public string VinSnapshot { get; set; } = string.Empty;
    public int OdometerAtClaimKm { get; set; }
    public WarrantyDefectCategory DefectCategory { get; set; } = WarrantyDefectCategory.Engine;
    public string SymptomDescription { get; set; } = string.Empty;
    public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Submitted;
    public Guid? CoveredSparePartId { get; set; }
    public decimal CoveredAmountVnd { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Identity.User? Customer { get; set; }
    public CustomerVehicle? CustomerVehicle { get; set; }
    public Showroom.Dealer? Dealer { get; set; }
    public Booking.SparePart? CoveredSparePart { get; set; }
    public Identity.User? ReviewedByUser { get; set; }
}
