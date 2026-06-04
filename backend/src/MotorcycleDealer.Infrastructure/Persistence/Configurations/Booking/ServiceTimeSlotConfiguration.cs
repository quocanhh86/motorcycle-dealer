using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Booking;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Booking;

public sealed class ServiceTimeSlotConfiguration : IEntityTypeConfiguration<ServiceTimeSlot>
{
    public void Configure(EntityTypeBuilder<ServiceTimeSlot> builder)
    {
        builder.ToTable("ServiceTimeSlots", t =>
        {
            t.HasCheckConstraint("CK_ServiceTimeSlots_TimeRange", "[EndTime] > [StartTime]");
            t.HasCheckConstraint(
                "CK_ServiceTimeSlots_Capacity",
                "[MaxCapacity] >= 1 AND [BookedCount] >= 0 AND [BookedCount] <= [MaxCapacity]");
        });

        builder.HasKey(x => x.Id).HasName("PK_ServiceTimeSlots");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.SlotDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time(0)").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time(0)").IsRequired();
        builder.Property(x => x.MaxCapacity).HasDefaultValue(4).IsRequired();
        builder.Property(x => x.BookedCount).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.IsBlocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.DealerId, x.SlotDate, x.StartTime, x.EndTime })
            .IsUnique()
            .HasDatabaseName("UQ_ServiceTimeSlots_Dealer_Window");

        builder.HasIndex(x => new { x.DealerId, x.SlotDate, x.StartTime })
            .IncludeProperties(x => new { x.EndTime, x.MaxCapacity, x.BookedCount, x.RowVersion })
            .HasFilter("[IsBlocked] = 0")
            .HasDatabaseName("IX_ServiceTimeSlots_Dealer_Date_Open");

        builder.HasOne(x => x.Dealer)
            .WithMany(d => d.ServiceTimeSlots)
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceTimeSlots_Dealers");
    }
}
