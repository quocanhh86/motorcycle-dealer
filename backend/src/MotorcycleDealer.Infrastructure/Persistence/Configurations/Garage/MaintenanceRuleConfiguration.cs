using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Garage;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Garage;

public sealed class MaintenanceRuleConfiguration : IEntityTypeConfiguration<MaintenanceRule>
{
    public void Configure(EntityTypeBuilder<MaintenanceRule> builder)
    {
        builder.ToTable("MaintenanceRules", t =>
        {
            t.HasCheckConstraint(
                "CK_MaintenanceRules_Intervals",
                "[IntervalKm] > 0 AND [IntervalMonths] > 0 AND [LeadTimeDays] >= 0 AND [LeadMileageKm] >= 0 AND [EstimatedDurationMinutes] > 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_MaintenanceRules");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.MilestoneCode).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.MilestoneName).HasMaxLength(120).IsUnicode(true).IsRequired();
        builder.Property(x => x.IntervalKm).IsRequired();
        builder.Property(x => x.IntervalMonths).IsRequired();
        builder.Property(x => x.IsRecurring).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.LeadTimeDays).HasDefaultValue(7).IsRequired();
        builder.Property(x => x.LeadMileageKm).HasDefaultValue(250).IsRequired();
        builder.Property(x => x.EstimatedDurationMinutes).HasDefaultValue(60).IsRequired();
        builder.Property(x => x.ChecklistSummary).HasMaxLength(1000).IsUnicode(true).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => new { x.MotorcycleModelId, x.MilestoneCode })
            .IsUnique()
            .HasDatabaseName("UQ_MaintenanceRules_Model_Code");

        builder.HasOne(x => x.MotorcycleModel)
            .WithMany(m => m.MaintenanceRules)
            .HasForeignKey(x => x.MotorcycleModelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MaintenanceRules_Models");
    }
}
