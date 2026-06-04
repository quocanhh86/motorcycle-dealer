using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Loyalty;

public sealed class LoyaltyAccount : AuditableEntity, IConcurrencyProtected
{
    public LoyaltyAccount()
    {
    }

    public LoyaltyAccount(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid CustomerId { get; set; }
    public Guid CurrentTierId { get; set; }
    public int AvailablePoints { get; set; }
    public int LifetimeQualifyingPoints { get; set; }
    public decimal LifetimeSpentVnd { get; set; }
    public DateTimeOffset? TierUpgradedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Identity.User? Customer { get; set; }
    public LoyaltyTier? CurrentTier { get; set; }
    public ICollection<PointLedgerEntry> PointLedgerEntries { get; set; } = new List<PointLedgerEntry>();
    public ICollection<CustomerVoucher> CustomerVouchers { get; set; } = new List<CustomerVoucher>();
}
