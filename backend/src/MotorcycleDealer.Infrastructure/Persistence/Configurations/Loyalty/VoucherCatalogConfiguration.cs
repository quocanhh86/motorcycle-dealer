using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Loyalty;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Loyalty;

public sealed class VoucherCatalogConfiguration : IEntityTypeConfiguration<VoucherCatalog>
{
    public void Configure(EntityTypeBuilder<VoucherCatalog> builder)
    {
        builder.ToTable("VoucherCatalogs", t =>
        {
            t.HasCheckConstraint(
                "CK_VoucherCatalogs_Scope",
                "[ApplicableScope] IN ('ServiceOnly', 'ShowroomOnly', 'All')");
            t.HasCheckConstraint("CK_VoucherCatalogs_PointsCost", "[PointsCost] > 0");
            t.HasCheckConstraint("CK_VoucherCatalogs_MinOrder", "[MinOrderAmount] >= 0");
            t.HasCheckConstraint("CK_VoucherCatalogs_ValidDays", "[ValidDays] BETWEEN 1 AND 3650");
            t.HasCheckConstraint(
                "CK_VoucherCatalogs_DiscountRules",
                "([DiscountType] = 'FixedAmount' AND [DiscountValue] > 0) OR ([DiscountType] = 'Percentage' AND [DiscountValue] > 0 AND [DiscountValue] <= 100 AND [MaxDiscountAmount] IS NOT NULL AND [MaxDiscountAmount] > 0)");
            t.HasCheckConstraint(
                "CK_VoucherCatalogs_Quota",
                "[QuotaRedeemed] >= 0 AND ([QuotaTotal] IS NULL OR ([QuotaTotal] >= 1 AND [QuotaRedeemed] <= [QuotaTotal]))");
        });

        builder.HasKey(x => x.Id).HasName("PK_VoucherCatalogs");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.CodePrefix).HasMaxLength(15).IsUnicode(false).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsUnicode(true).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsUnicode(true).IsRequired();
        builder.Property(x => x.ApplicableScope).HasMaxLength(20).IsUnicode(false).HasDefaultValue("ServiceOnly").IsRequired();
        builder.Property(x => x.PointsCost).IsRequired();
        builder.Property(x => x.DiscountType).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.DiscountValue).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.MaxDiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.MinOrderAmount).HasPrecision(18, 2).HasDefaultValue(0.00m).IsRequired();
        builder.Property(x => x.ValidDays).HasDefaultValue(90).IsRequired();
        builder.Property(x => x.QuotaRedeemed).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.CodePrefix).IsUnique().HasDatabaseName("UQ_VoucherCatalogs_CodePrefix");

        builder.HasOne(x => x.MinTier)
            .WithMany(t => t.VoucherCatalogs)
            .HasForeignKey(x => x.MinTierId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VoucherCatalogs_Tiers");
    }
}
