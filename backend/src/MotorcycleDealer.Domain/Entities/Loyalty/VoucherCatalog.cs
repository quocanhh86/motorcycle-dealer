using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Loyalty;

public sealed class VoucherCatalog : AuditableEntity, ISoftDeletable
{
    public VoucherCatalog()
    {
    }

    public VoucherCatalog(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string CodePrefix { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApplicableScope { get; set; } = "ServiceOnly";
    public int PointsCost { get; set; }
    public string DiscountType { get; set; } = "FixedAmount";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int ValidDays { get; set; } = 90;
    public int? QuotaTotal { get; set; }
    public int QuotaRedeemed { get; set; }
    public Guid MinTierId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public LoyaltyTier? MinTier { get; set; }
    public ICollection<CustomerVoucher> CustomerVouchers { get; set; } = new List<CustomerVoucher>();
}
