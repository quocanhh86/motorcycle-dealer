using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Loyalty;

public sealed class CustomerVoucher : AuditableEntity, IConcurrencyProtected
{
    public CustomerVoucher()
    {
    }

    public CustomerVoucher(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string VoucherCode { get; set; } = string.Empty;
    public Guid LoyaltyAccountId { get; set; }
    public Guid VoucherCatalogId { get; set; }
    public int PointsSpent { get; set; }
    public string Status { get; set; } = "Available";
    public DateTimeOffset RedeemedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public VoucherCatalog? VoucherCatalog { get; set; }
}
