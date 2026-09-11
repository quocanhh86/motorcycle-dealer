using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Garage;

public sealed class WarrantyClaimConfiguration : IEntityTypeConfiguration<WarrantyClaim>
{
    public void Configure(EntityTypeBuilder<WarrantyClaim> builder)
    {
        builder.ToTable("WarrantyClaims", t =>
        {
            t.HasCheckConstraint(
                "CK_WarrantyClaims_VinSnapshot",
                "LEN([VinSnapshot]) = 17 AND [VinSnapshot] NOT LIKE '%[^A-HJ-NPR-Z0-9]%'");
            t.HasCheckConstraint("CK_WarrantyClaims_Odometer", "[OdometerAtClaimKm] >= 0");
            t.HasCheckConstraint(
                "CK_WarrantyClaims_DefectCategory",
                "[DefectCategory] IN ('Engine', 'Transmission', 'Electrical', 'BrakesAndSuspension', 'FrameAndPaint', 'ExhaustAndFuel')");
            t.HasCheckConstraint(
                "CK_WarrantyClaims_Status",
                "[Status] IN ('Submitted', 'UnderReview', 'Approved', 'Completed', 'Rejected')");
            t.HasCheckConstraint("CK_WarrantyClaims_CoveredAmount", "[CoveredAmountVnd] >= 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_WarrantyClaims");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.ClaimNumber).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.CustomerVehicleId).IsRequired();
        builder.Property(x => x.DealerId).IsRequired();
        builder.Property(x => x.VinSnapshot).HasMaxLength(17).IsUnicode(false).IsRequired();
        builder.Property(x => x.OdometerAtClaimKm).IsRequired();

        builder.Property(x => x.DefectCategory)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.SymptomDescription).HasMaxLength(1000).IsUnicode(true).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsUnicode(false)
            .HasDefaultValue(WarrantyClaimStatus.Submitted)
            .HasSentinel((WarrantyClaimStatus)0)
            .IsRequired();

        builder.Property(x => x.CoveredAmountVnd).HasPrecision(18, 2).HasDefaultValue(0.00m).IsRequired();
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000).IsUnicode(true);
        builder.Property(x => x.RejectionReason).HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.ReviewedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.ClaimNumber)
            .IsUnique()
            .HasDatabaseName("UQ_WarrantyClaims_ClaimNumber");

        builder.HasIndex(x => new { x.DealerId, x.Status, x.CreatedAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_WarrantyClaims_Dealer_Status_Created");

        builder.HasIndex(x => new { x.CustomerVehicleId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_WarrantyClaims_Vehicle_CreatedDesc");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_WarrantyClaims_Customers");

        builder.HasOne(x => x.CustomerVehicle)
            .WithMany()
            .HasForeignKey(x => x.CustomerVehicleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_WarrantyClaims_CustomerVehicles");

        builder.HasOne(x => x.Dealer)
            .WithMany()
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_WarrantyClaims_Dealers");

        builder.HasOne(x => x.CoveredSparePart)
            .WithMany()
            .HasForeignKey(x => x.CoveredSparePartId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_WarrantyClaims_SpareParts");

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_WarrantyClaims_ReviewedByUsers");
    }
}
