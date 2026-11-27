using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Loyalty;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Loyalty;

public sealed class LoyaltyTierConfiguration : IEntityTypeConfiguration<LoyaltyTier>
{
    public void Configure(EntityTypeBuilder<LoyaltyTier> builder)
    {
        builder.ToTable("LoyaltyTiers", t =>
        {
            t.HasCheckConstraint(
                "CK_LoyaltyTiers_Code",
                "[TierCode] IN ('Bronze', 'Silver', 'Gold', 'Platinum')");
            t.HasCheckConstraint("CK_LoyaltyTiers_MinPoints", "[MinQualifyingPoints] >= 0");
            t.HasCheckConstraint("CK_LoyaltyTiers_Multiplier", "[PointsMultiplier] BETWEEN 1.00 AND 10.00");
            t.HasCheckConstraint("CK_LoyaltyTiers_BenefitsJson", "ISJSON([BenefitsJson]) = 1");
        });

        builder.HasKey(x => x.Id).HasName("PK_LoyaltyTiers");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.TierCode).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(60).IsUnicode(true).IsRequired();
        builder.Property(x => x.RankOrder).HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.MinQualifyingPoints).IsRequired();
        builder.Property(x => x.PointsMultiplier).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.BenefitsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.TierCode).IsUnique().HasDatabaseName("UQ_LoyaltyTiers_TierCode");
        builder.HasIndex(x => x.RankOrder).IsUnique().HasDatabaseName("UQ_LoyaltyTiers_RankOrder");
        builder.HasIndex(x => x.MinQualifyingPoints).IsUnique().HasDatabaseName("UQ_LoyaltyTiers_MinPoints");
    }
}
