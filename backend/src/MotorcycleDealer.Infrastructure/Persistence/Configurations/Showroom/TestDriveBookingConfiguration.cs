using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Showroom;

public sealed class TestDriveBookingConfiguration : IEntityTypeConfiguration<TestDriveBooking>
{
    public void Configure(EntityTypeBuilder<TestDriveBooking> builder)
    {
        builder.ToTable("TestDriveBookings", t =>
        {
            t.HasCheckConstraint("CK_TestDriveBookings_TimeWindow", "[EndTime] > [StartTime]");
            t.HasCheckConstraint(
                "CK_TestDriveBookings_Status",
                "[Status] IN ('Requested', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
        });

        builder.HasKey(x => x.Id).HasName("PK_TestDriveBookings");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.BookingNumber).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(x => x.PreferredDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time(0)").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time(0)").IsRequired();
        builder.Property(x => x.DriverLicenseNumber).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(x => x.ContactPhone).HasMaxLength(20).IsUnicode(false).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsUnicode(false)
            .HasDefaultValue(TestDriveStatus.Requested)
            .HasSentinel((TestDriveStatus)0)
            .IsRequired();

        builder.Property(x => x.CustomerNotes).HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.StaffNotes).HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.ConfirmedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CancelledAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CancellationReason).HasMaxLength(300).IsUnicode(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.BookingNumber)
            .IsUnique()
            .HasDatabaseName("UQ_TestDriveBookings_BookingNumber");

        builder.HasIndex(x => new { x.DealerId, x.PreferredDate, x.Status })
            .IncludeProperties(x => new
            {
                x.BookingNumber,
                x.CustomerId,
                x.MotorcycleModelId,
                x.StartTime,
                x.EndTime,
                x.AssignedInventoryId
            })
            .HasDatabaseName("IX_TestDriveBookings_Dealer_Date_Status");

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new
            {
                x.BookingNumber,
                x.DealerId,
                x.MotorcycleModelId,
                x.PreferredDate,
                x.StartTime,
                x.EndTime,
                x.Status
            })
            .HasDatabaseName("IX_TestDriveBookings_Customer_CreatedDesc");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TestDriveBookings_Customer");

        builder.HasOne(x => x.Dealer)
            .WithMany()
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TestDriveBookings_Dealers");

        builder.HasOne(x => x.MotorcycleModel)
            .WithMany()
            .HasForeignKey(x => x.MotorcycleModelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TestDriveBookings_Models");

        builder.HasOne(x => x.AssignedInventory)
            .WithMany()
            .HasForeignKey(x => x.AssignedInventoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TestDriveBookings_Inventories");
    }
}
