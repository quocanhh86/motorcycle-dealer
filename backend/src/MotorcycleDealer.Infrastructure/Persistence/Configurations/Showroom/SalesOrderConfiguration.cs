using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Showroom;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Showroom;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders", t =>
        {
            t.HasCheckConstraint(
                "CK_SalesOrders_Amounts",
                "[TotalAmount] >= 0 AND [DiscountAmount] >= 0 AND [DiscountAmount] <= [TotalAmount] AND [FinalAmount] = [TotalAmount] - [DiscountAmount]");
            t.HasCheckConstraint("CK_SalesOrders_Points", "[PointsEarned] >= 0");
            t.HasCheckConstraint(
                "CK_SalesOrders_PaymentMethod",
                "[PaymentMethod] IN ('Cash', 'BankTransfer', 'Card', 'Installment')");
            t.HasCheckConstraint(
                "CK_SalesOrders_Status",
                "[Status] IN ('Pending', 'Completed', 'Cancelled')");
        });

        builder.HasKey(x => x.Id).HasName("PK_SalesOrders");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.OrderNumber).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0.00m).IsRequired();
        builder.Property(x => x.FinalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PointsEarned).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.PaymentMethod).HasMaxLength(30).IsUnicode(false).HasDefaultValue("BankTransfer").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Completed").IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.OrderNumber).IsUnique().HasDatabaseName("UQ_SalesOrders_OrderNumber");

        builder.HasIndex(x => x.DealerInventoryId)
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled'")
            .HasDatabaseName("UQ_SalesOrders_ActiveInventory");

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new { x.OrderNumber, x.DealerId, x.FinalAmount, x.PointsEarned, x.Status })
            .HasDatabaseName("IX_SalesOrders_Customer_CreatedDesc");

        builder.HasIndex(x => new { x.DealerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new { x.OrderNumber, x.CustomerId, x.DealerInventoryId, x.FinalAmount, x.Status })
            .HasDatabaseName("IX_SalesOrders_Dealer_CreatedDesc");

        builder.HasIndex(x => new { x.DealerId, x.CompletedAtUtc })
            .IncludeProperties(x => new { x.TotalAmount, x.DiscountAmount, x.FinalAmount, x.PointsEarned })
            .HasFilter("[Status] = 'Completed'")
            .HasDatabaseName("IX_SalesOrders_Analytics_Covering");

        builder.HasOne(x => x.Dealer)
            .WithMany(d => d.SalesOrders)
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalesOrders_Dealers");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalesOrders_Customer");

        builder.HasOne(x => x.DealerInventory)
            .WithMany(i => i.SalesOrders)
            .HasForeignKey(x => x.DealerInventoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalesOrders_Inventory");

        builder.HasOne(x => x.AppliedCustomerVoucher)
            .WithMany()
            .HasForeignKey(x => x.AppliedCustomerVoucherId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalesOrders_Voucher");

        builder.HasOne(x => x.SoldByStaff)
            .WithMany()
            .HasForeignKey(x => x.SoldByStaffId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalesOrders_Staff");
    }
}
