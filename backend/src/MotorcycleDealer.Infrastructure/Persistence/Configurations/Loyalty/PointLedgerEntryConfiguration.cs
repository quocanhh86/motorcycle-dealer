using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Loyalty;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Loyalty;

public sealed class PointLedgerEntryConfiguration : IEntityTypeConfiguration<PointLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PointLedgerEntry> builder)
    {
        builder.ToTable("PointLedgerEntries", t =>
        {
            t.HasTrigger("TR_PointLedgerEntries_BlockMutation");
            t.HasCheckConstraint("CK_PointLedgerEntries_NonZero", "[PointsDelta] <> 0");
            t.HasCheckConstraint("CK_PointLedgerEntries_Balance", "[BalanceAfter] >= 0");
            t.HasCheckConstraint(
                "CK_PointLedgerEntries_TxType",
                "[TransactionType] IN ('SalesOrderEarn', 'ServiceInvoiceEarn', 'VoucherRedeem', 'VoucherExpireRefund', 'AdminAdjustment', 'WelcomeBonus', 'ServiceReviewBonus')");
            t.HasCheckConstraint(
                "CK_PointLedgerEntries_RefType",
                "[ReferenceType] IN ('SalesOrder', 'ServiceInvoice', 'CustomerVoucher', 'AdminManual', 'AccountOnboarding', 'ServiceReview')");
        });

        builder.HasKey(x => x.Id).HasName("PK_PointLedgerEntries");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.TransactionType).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.PointsDelta).IsRequired();
        builder.Property(x => x.BalanceAfter).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.ReferenceId).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UQ_PointLedgerEntries_IdempotencyKey");

        builder.HasIndex(x => new { x.LoyaltyAccountId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new
            {
                x.TransactionType,
                x.PointsDelta,
                x.BalanceAfter,
                x.ReferenceType,
                x.ReferenceId,
                x.Notes
            })
            .HasDatabaseName("IX_PointLedgerEntries_Account_CreatedDesc");

        builder.HasOne(x => x.LoyaltyAccount)
            .WithMany(a => a.PointLedgerEntries)
            .HasForeignKey(x => x.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PointLedgerEntries_Accounts");

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PointLedgerEntries_Users");
    }
}
