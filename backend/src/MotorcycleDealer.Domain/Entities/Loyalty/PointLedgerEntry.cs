using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Loyalty;

public sealed class PointLedgerEntry : BaseEntity, IAppendOnlyEntity
{
    public PointLedgerEntry()
    {
    }

    public PointLedgerEntry(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid LoyaltyAccountId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int PointsDelta { get; set; }
    public int BalanceAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public Identity.User? CreatedByUser { get; set; }
}
