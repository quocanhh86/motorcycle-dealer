using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Booking;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Booking;

public sealed class ServiceReviewConfiguration : IEntityTypeConfiguration<ServiceReview>
{
    public void Configure(EntityTypeBuilder<ServiceReview> builder)
    {
        builder.ToTable("ServiceReviews", t =>
        {
            t.HasCheckConstraint("CK_ServiceReviews_Rating", "[Rating] >= 1 AND [Rating] <= 5");
            t.HasCheckConstraint("CK_ServiceReviews_BonusPoints", "[BonusPointsAwarded] >= 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_ServiceReviews");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.ReviewNumber).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(x => x.ServiceInvoiceId).IsRequired();
        builder.Property(x => x.ServiceBookingId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.DealerId).IsRequired();
        builder.Property(x => x.CustomerVehicleId).IsRequired();
        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.QualityTags).HasMaxLength(200).IsUnicode(false);
        builder.Property(x => x.Comment).HasMaxLength(1000).IsUnicode(true);
        builder.Property(x => x.BonusPointsAwarded).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.StaffResponse).HasMaxLength(1000).IsUnicode(true);
        builder.Property(x => x.RespondedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.ReviewNumber)
            .IsUnique()
            .HasDatabaseName("UQ_ServiceReviews_ReviewNumber");

        builder.HasIndex(x => x.ServiceInvoiceId)
            .IsUnique()
            .HasDatabaseName("UQ_ServiceReviews_ServiceInvoiceId");

        builder.HasIndex(x => new { x.DealerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(r => new { r.Rating, r.BonusPointsAwarded })
            .HasDatabaseName("IX_ServiceReviews_Dealer_CreatedDesc");

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_ServiceReviews_Customer_CreatedDesc");

        builder.HasOne(x => x.ServiceInvoice)
            .WithOne()
            .HasForeignKey<ServiceReview>(x => x.ServiceInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceReviews_ServiceInvoices");

        builder.HasOne(x => x.ServiceBooking)
            .WithMany()
            .HasForeignKey(x => x.ServiceBookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceReviews_ServiceBookings");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceReviews_Customers");

        builder.HasOne(x => x.Dealer)
            .WithMany()
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceReviews_Dealers");

        builder.HasOne(x => x.CustomerVehicle)
            .WithMany()
            .HasForeignKey(x => x.CustomerVehicleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceReviews_CustomerVehicles");

        builder.HasOne(x => x.RespondedByUser)
            .WithMany()
            .HasForeignKey(x => x.RespondedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceReviews_RespondedByUsers");
    }
}
