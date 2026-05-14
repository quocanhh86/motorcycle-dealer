using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Loyalty;

public sealed class LoyaltyTier : AuditableEntity
{
    public LoyaltyTier()
    {
    }

    public LoyaltyTier(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string TierCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public byte RankOrder { get; set; }
    public int MinQualifyingPoints { get; set; }
    public decimal PointsMultiplier { get; set; } = 1.00m;
    public string BenefitsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;

    public ICollection<LoyaltyAccount> LoyaltyAccounts { get; set; } = new List<LoyaltyAccount>();
    public ICollection<VoucherCatalog> VoucherCatalogs { get; set; } = new List<VoucherCatalog>();
}
