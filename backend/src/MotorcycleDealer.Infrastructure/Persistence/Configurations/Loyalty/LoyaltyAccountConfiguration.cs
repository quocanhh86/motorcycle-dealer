using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Loyalty;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Loyalty;

public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("LoyaltyAccounts", t =>
        {
            t.HasCheckConstraint(
                "CK_LoyaltyAccounts_Balances",
                "[AvailablePoints] >= 0 AND [LifetimeQualifyingPoints] >= 0 AND [LifetimeSpentVnd] >= 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_LoyaltyAccounts");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.AvailablePoints).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LifetimeQualifyingPoints).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LifetimeSpentVnd).HasPrecision(18, 2).HasDefaultValue(0.00m).IsRequired();
        builder.Property(x => x.TierUpgradedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.CustomerId).IsUnique().HasDatabaseName("UQ_LoyaltyAccounts_CustomerId");

        builder.HasIndex(x => x.CurrentTierId)
            .IncludeProperties(x => new { x.AvailablePoints, x.LifetimeQualifyingPoints, x.LifetimeSpentVnd })
            .HasDatabaseName("IX_LoyaltyAccounts_Tier_Distribution");

        builder.HasOne(x => x.Customer)
            .WithOne(u => u.LoyaltyAccount)
            .HasForeignKey<LoyaltyAccount>(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LoyaltyAccounts_Users");

        builder.HasOne(x => x.CurrentTier)
            .WithMany(t => t.LoyaltyAccounts)
            .HasForeignKey(x => x.CurrentTierId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LoyaltyAccounts_Tiers");
    }
}
