using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Garage;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Garage;

public sealed class MaintenanceReminderConfiguration : IEntityTypeConfiguration<MaintenanceReminder>
{
    public void Configure(EntityTypeBuilder<MaintenanceReminder> builder)
    {
        builder.ToTable("MaintenanceReminders", t =>
        {
            t.HasCheckConstraint("CK_MaintenanceReminders_TargetKm", "[TargetMileageKm] > 0");
            t.HasCheckConstraint(
                "CK_MaintenanceReminders_Reason",
                "[TriggerReason] IN ('MileageThreshold', 'DateThreshold', 'Overdue')");
            t.HasCheckConstraint(
                "CK_MaintenanceReminders_Status",
                "[Status] IN ('Pending', 'Sent', 'Booked', 'Completed', 'Dismissed', 'Failed')");
        });

        builder.HasKey(x => x.Id).HasName("PK_MaintenanceReminders");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.TargetMileageKm).IsRequired();
        builder.Property(x => x.DueDateUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(x => x.TriggerReason).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.DeduplicationKey).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.SentAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.DeduplicationKey).IsUnique().HasDatabaseName("UQ_MaintenanceReminders_DedupKey");

        builder.HasIndex(x => new { x.Status, x.DueDateUtc })
            .IncludeProperties(x => new { x.CustomerVehicleId, x.MaintenanceRuleId, x.TargetMileageKm, x.DeduplicationKey })
            .HasFilter("[Status] IN ('Pending', 'Sent')")
            .HasDatabaseName("IX_MaintenanceReminders_PendingDispatch");

        builder.HasIndex(x => new { x.CustomerVehicleId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new { x.MaintenanceRuleId, x.TargetMileageKm, x.Status, x.SentAtUtc })
            .HasDatabaseName("IX_MaintenanceReminders_Vehicle_History");

        builder.HasOne(x => x.CustomerVehicle)
            .WithMany(v => v.MaintenanceReminders)
            .HasForeignKey(x => x.CustomerVehicleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MaintenanceReminders_Vehicles");

        builder.HasOne(x => x.MaintenanceRule)
            .WithMany(r => r.MaintenanceReminders)
            .HasForeignKey(x => x.MaintenanceRuleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MaintenanceReminders_Rules");

        builder.HasOne(x => x.ResolvedBooking)
            .WithMany(b => b.ResolvedReminders)
            .HasForeignKey(x => x.ResolvedBookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MaintenanceReminders_Bookings");
    }
}
