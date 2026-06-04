using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Booking;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Booking;

public sealed class ServiceInvoiceConfiguration : IEntityTypeConfiguration<ServiceInvoice>
{
    public void Configure(EntityTypeBuilder<ServiceInvoice> builder)
    {
        builder.ToTable("ServiceInvoices", t =>
        {
            t.HasCheckConstraint(
                "CK_ServiceInvoices_Amounts",
                "[LaborAmount] >= 0 AND [PartsAmount] >= 0 AND [GrossAmount] = [LaborAmount] + [PartsAmount] AND [VoucherDiscountAmount] >= 0 AND [VoucherDiscountAmount] <= [GrossAmount] AND [NetPaidAmount] = [GrossAmount] - [VoucherDiscountAmount]");
            t.HasCheckConstraint("CK_ServiceInvoices_Odometer", "[OdometerAtCompletionKm] >= 0");
            t.HasCheckConstraint("CK_ServiceInvoices_Points", "[PointsEarned] >= 0");
            t.HasCheckConstraint(
                "CK_ServiceInvoices_PaymentMethod",
                "[PaymentMethod] IN ('Cash', 'BankTransfer', 'Card', 'EWallet')");
            t.HasCheckConstraint(
                "CK_ServiceInvoices_LineItemsJson",
                "[LineItemsJson] IS NULL OR ISJSON([LineItemsJson]) = 1");
        });

        builder.HasKey(x => x.Id).HasName("PK_ServiceInvoices");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.InvoiceNumber).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.LaborAmount).HasPrecision(18, 2).HasDefaultValue(0.00m).IsRequired();
        builder.Property(x => x.PartsAmount).HasPrecision(18, 2).HasDefaultValue(0.00m).IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VoucherDiscountAmount).HasPrecision(18, 2).HasDefaultValue(0.00m).IsRequired();
        builder.Property(x => x.NetPaidAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.OdometerAtCompletionKm).IsRequired();
        builder.Property(x => x.PointsEarned).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.WorkSummaryNotes).HasMaxLength(1500).IsUnicode(true);
        builder.Property(x => x.LineItemsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PaymentMethod).HasMaxLength(30).IsUnicode(false).HasDefaultValue("BankTransfer").IsRequired();
        builder.Property(x => x.PaidAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.InvoiceNumber).IsUnique().HasDatabaseName("UQ_ServiceInvoices_InvoiceNumber");
        builder.HasIndex(x => x.ServiceBookingId).IsUnique().HasDatabaseName("UQ_ServiceInvoices_BookingId");

        builder.HasIndex(x => new { x.CustomerId, x.PaidAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new
            {
                x.InvoiceNumber,
                x.DealerId,
                x.ServiceBookingId,
                x.NetPaidAmount,
                x.PointsEarned
            })
            .HasDatabaseName("IX_ServiceInvoices_Customer_PaidDesc");

        builder.HasIndex(x => new { x.DealerId, x.PaidAtUtc })
            .IncludeProperties(x => new
            {
                x.LaborAmount,
                x.PartsAmount,
                x.GrossAmount,
                x.VoucherDiscountAmount,
                x.NetPaidAmount,
                x.PointsEarned
            })
            .HasDatabaseName("IX_ServiceInvoices_Analytics_Covering");

        builder.HasOne(x => x.ServiceBooking)
            .WithOne(b => b.ServiceInvoice)
            .HasForeignKey<ServiceInvoice>(x => x.ServiceBookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceInvoices_Bookings");

        builder.HasOne(x => x.Dealer)
            .WithMany(d => d.ServiceInvoices)
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceInvoices_Dealers");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceInvoices_Customer");

        builder.HasOne(x => x.AppliedCustomerVoucher)
            .WithMany()
            .HasForeignKey(x => x.AppliedCustomerVoucherId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceInvoices_Vouchers");

        builder.HasOne(x => x.IssuedByStaff)
            .WithMany()
            .HasForeignKey(x => x.IssuedByStaffId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceInvoices_Staff");
    }
}
