using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Notifications;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", t =>
        {
            t.HasCheckConstraint(
                "CK_Notifications_Audience",
                "[RecipientUserId] IS NOT NULL OR [TargetDealerId] IS NOT NULL OR [TargetRole] IS NOT NULL");
            t.HasCheckConstraint(
                "CK_Notifications_TargetRole",
                "[TargetRole] IS NULL OR [TargetRole] IN ('Customer', 'DealerStaff', 'Admin')");
            t.HasCheckConstraint(
                "CK_Notifications_EventType",
                "[EventType] IN ('BookingCreated', 'BookingStatusChanged', 'MaintenanceReminderTriggered', 'LoyaltyPointsUpdated', 'TierUpgraded', 'SalesOrderCompleted', 'VoucherRedeemed', 'TestDriveCreated', 'TestDriveStatusChanged', 'ServiceReviewSubmitted', 'WarrantyClaimCreated', 'WarrantyClaimStatusChanged')");
            t.HasCheckConstraint(
                "CK_Notifications_PayloadJson",
                "[PayloadJson] IS NULL OR ISJSON([PayloadJson]) = 1");
        });

        builder.HasKey(x => x.Id).HasName("PK_Notifications");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.TargetRole).HasMaxLength(30).IsUnicode(false);
        builder.Property(x => x.EventType).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsUnicode(true).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsUnicode(true).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IsRead).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ReadAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => new { x.RecipientUserId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new { x.EventType, x.Title, x.Message, x.PayloadJson })
            .HasFilter("[IsRead] = 0 AND [RecipientUserId] IS NOT NULL")
            .HasDatabaseName("IX_Notifications_Recipient_Unread");

        builder.HasIndex(x => new { x.TargetDealerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new { x.EventType, x.Title, x.Message, x.PayloadJson })
            .HasFilter("[IsRead] = 0 AND [TargetDealerId] IS NOT NULL")
            .HasDatabaseName("IX_Notifications_Dealer_Unread");

        builder.HasOne(x => x.RecipientUser)
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Notifications_Users");

        builder.HasOne(x => x.TargetDealer)
            .WithMany()
            .HasForeignKey(x => x.TargetDealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Notifications_Dealers");
    }
}
