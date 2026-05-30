using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Booking;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Booking;

public sealed class ServiceBookingConfiguration : IEntityTypeConfiguration<ServiceBooking>
{
    public void Configure(EntityTypeBuilder<ServiceBooking> builder)
    {
        builder.ToTable("ServiceBookings", t =>
        {
            t.HasCheckConstraint("CK_ServiceBookings_Odometer", "[OdometerAtBookingKm] >= 0");
            t.HasCheckConstraint(
                "CK_ServiceBookings_ServiceType",
                "[ServiceType] IN ('PeriodicMaintenance', 'WarrantyRepair', 'GeneralRepair', 'Inspection', 'OilAndTire')");
            t.HasCheckConstraint(
                "CK_ServiceBookings_Status",
                "[Status] IN ('Pending', 'Confirmed', 'CheckedIn', 'InProgress', 'Completed', 'Cancelled', 'NoShow')");
        });

        builder.HasKey(x => x.Id).HasName("PK_ServiceBookings");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.BookingNumber).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.ServiceType).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.ReportedNotes).HasMaxLength(1000).IsUnicode(true);
        builder.Property(x => x.OdometerAtBookingKm).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.CancellationReason).HasMaxLength(300).IsUnicode(true);
        builder.Property(x => x.CheckedInAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CancelledAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.BookingNumber).IsUnique().HasDatabaseName("UQ_ServiceBookings_BookingNumber");

        builder.HasIndex(x => new { x.CustomerVehicleId, x.ServiceTimeSlotId })
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled' AND [Status] <> 'NoShow'")
            .HasDatabaseName("UQ_ServiceBookings_ActiveVehicleSlot");

        builder.HasIndex(x => x.AppliedCustomerVoucherId)
            .IsUnique()
            .HasFilter("[AppliedCustomerVoucherId] IS NOT NULL AND [Status] <> 'Cancelled' AND [Status] <> 'NoShow'")
            .HasDatabaseName("UQ_ServiceBookings_ActiveVoucher");

        builder.HasIndex(x => new { x.DealerId, x.Status, x.CreatedAtUtc })
            .IsDescending(false, false, true)
            .IncludeProperties(x => new
            {
                x.BookingNumber,
                x.CustomerId,
                x.CustomerVehicleId,
                x.ServiceTimeSlotId,
                x.ServiceType
            })
            .HasDatabaseName("IX_ServiceBookings_Dealer_Status_Created");

        builder.HasIndex(x => new { x.ServiceTimeSlotId, x.Status })
            .IncludeProperties(x => new { x.CustomerId, x.CustomerVehicleId, x.BookingNumber })
            .HasFilter("[Status] <> 'Cancelled' AND [Status] <> 'NoShow'")
            .HasDatabaseName("IX_ServiceBookings_Slot_Active");

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new
            {
                x.BookingNumber,
                x.DealerId,
                x.CustomerVehicleId,
                x.ServiceTimeSlotId,
                x.Status
            })
            .HasDatabaseName("IX_ServiceBookings_Customer_CreatedDesc");

        builder.HasIndex(x => new { x.DealerId, x.CreatedAtUtc, x.Status })
            .IncludeProperties(x => new { x.ServiceType })
            .HasDatabaseName("IX_ServiceBookings_Analytics_Dealer");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceBookings_Customer");

        builder.HasOne(x => x.Dealer)
            .WithMany(d => d.ServiceBookings)
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceBookings_Dealers");

        builder.HasOne(x => x.CustomerVehicle)
            .WithMany(v => v.ServiceBookings)
            .HasForeignKey(x => x.CustomerVehicleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceBookings_Vehicles");

        builder.HasOne(x => x.ServiceTimeSlot)
            .WithMany(s => s.ServiceBookings)
            .HasForeignKey(x => x.ServiceTimeSlotId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceBookings_Slots");

        builder.HasOne(x => x.AppliedCustomerVoucher)
            .WithMany()
            .HasForeignKey(x => x.AppliedCustomerVoucherId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceBookings_Vouchers");
    }
}
