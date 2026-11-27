using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Loyalty;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Loyalty;

public sealed class CustomerVoucherConfiguration : IEntityTypeConfiguration<CustomerVoucher>
{
    public void Configure(EntityTypeBuilder<CustomerVoucher> builder)
    {
        builder.ToTable("CustomerVouchers", t =>
        {
            t.HasCheckConstraint("CK_CustomerVouchers_PointsSpent", "[PointsSpent] > 0");
            t.HasCheckConstraint(
                "CK_CustomerVouchers_Status",
                "[Status] IN ('Available', 'ReservedOnBooking', 'Used', 'Expired')");
            t.HasCheckConstraint("CK_CustomerVouchers_Dates", "[ExpiresAtUtc] > [RedeemedAtUtc]");
        });

        builder.HasKey(x => x.Id).HasName("PK_CustomerVouchers");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.VoucherCode).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(x => x.PointsSpent).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(25).IsUnicode(false).HasDefaultValue("Available").IsRequired();
        builder.Property(x => x.RedeemedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(x => x.UsedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.VoucherCode).IsUnique().HasDatabaseName("UQ_CustomerVouchers_VoucherCode");

        builder.HasIndex(x => new { x.LoyaltyAccountId, x.Status, x.ExpiresAtUtc })
            .IncludeProperties(x => new { x.VoucherCode, x.VoucherCatalogId, x.PointsSpent })
            .HasFilter("[Status] IN ('Available', 'ReservedOnBooking')")
            .HasDatabaseName("IX_CustomerVouchers_Account_Usable");

        builder.HasOne(x => x.LoyaltyAccount)
            .WithMany(a => a.CustomerVouchers)
            .HasForeignKey(x => x.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerVouchers_Accounts");

        builder.HasOne(x => x.VoucherCatalog)
            .WithMany(c => c.CustomerVouchers)
            .HasForeignKey(x => x.VoucherCatalogId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerVouchers_Catalogs");
    }
}
