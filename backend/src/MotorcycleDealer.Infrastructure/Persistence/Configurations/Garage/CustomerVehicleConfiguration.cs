using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Garage;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Garage;

public sealed class CustomerVehicleConfiguration : IEntityTypeConfiguration<CustomerVehicle>
{
    public void Configure(EntityTypeBuilder<CustomerVehicle> builder)
    {
        builder.ToTable("CustomerVehicles", t =>
        {
            t.HasCheckConstraint(
                "CK_CustomerVehicles_VIN",
                "LEN([VIN]) = 17 AND [VIN] NOT LIKE '%[^A-HJ-NPR-Z0-9]%'");
            t.HasCheckConstraint(
                "CK_CustomerVehicles_Source",
                "[RegistrationSource] IN ('ShowroomSale', 'ExternalManual')");
            t.HasCheckConstraint(
                "CK_CustomerVehicles_Mileage",
                "[CurrentMileageKm] >= 0 AND [LastServiceMileageKm] >= 0 AND [CurrentMileageKm] >= [LastServiceMileageKm] AND [NextDueMileageKm] > [LastServiceMileageKm]");
        });

        builder.HasKey(x => x.Id).HasName("PK_CustomerVehicles");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.VIN).HasMaxLength(17).IsUnicode(false).IsRequired();
        builder.Property(x => x.LicensePlate).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.ColorName).HasMaxLength(60).IsUnicode(true);
        builder.Property(x => x.RegistrationSource).HasMaxLength(20).IsUnicode(false).HasDefaultValue("ShowroomSale").IsRequired();
        builder.Property(x => x.OwnershipStartDateUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.CurrentMileageKm).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LastServiceDateUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.LastServiceMileageKm).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.NextDueMileageKm).IsRequired();
        builder.Property(x => x.NextDueDateUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.VIN).IsUnique().HasDatabaseName("UQ_CustomerVehicles_VIN");

        builder.HasIndex(x => x.PurchasedSalesOrderId)
            .IsUnique()
            .HasFilter("[PurchasedSalesOrderId] IS NOT NULL")
            .HasDatabaseName("UQ_CustomerVehicles_SalesOrder");

        builder.HasIndex(x => x.LicensePlate)
            .IsUnique()
            .HasFilter("[LicensePlate] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UQ_CustomerVehicles_LicensePlate");

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new
            {
                x.VIN,
                x.MotorcycleModelId,
                x.LicensePlate,
                x.CurrentMileageKm,
                x.NextDueMileageKm,
                x.NextDueDateUtc
            })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_CustomerVehicles_Customer_Active");

        builder.HasIndex(x => new { x.NextDueDateUtc, x.NextDueMileageKm })
            .IncludeProperties(x => new
            {
                x.CustomerId,
                x.MotorcycleModelId,
                x.VIN,
                x.CurrentMileageKm,
                x.LastServiceMileageKm
            })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_CustomerVehicles_ReminderScan");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerVehicles_Users");

        builder.HasOne(x => x.MotorcycleModel)
            .WithMany(m => m.CustomerVehicles)
            .HasForeignKey(x => x.MotorcycleModelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerVehicles_Models");

        builder.HasOne(x => x.PurchasedSalesOrder)
            .WithOne(so => so.OnboardedCustomerVehicle)
            .HasForeignKey<CustomerVehicle>(x => x.PurchasedSalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CustomerVehicles_SalesOrders");
    }
}
