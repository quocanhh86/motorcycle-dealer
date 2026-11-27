using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Showroom;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Showroom;

public sealed class MotorcycleModelConfiguration : IEntityTypeConfiguration<MotorcycleModel>
{
    public void Configure(EntityTypeBuilder<MotorcycleModel> builder)
    {
        builder.ToTable("MotorcycleModels", t =>
        {
            t.HasCheckConstraint(
                "CK_MotorcycleModels_Category",
                "[Category] IN ('Scooter', 'Underbone', 'Sport', 'Naked', 'Adventure', 'Cruiser')");
            t.HasCheckConstraint("CK_MotorcycleModels_EngineCc", "[EngineDisplacementCc] BETWEEN 49 AND 2500");
            t.HasCheckConstraint("CK_MotorcycleModels_Price", "[BaseRetailPrice] > 0");
            t.HasCheckConstraint("CK_MotorcycleModels_Warranty", "[WarrantyMonths] > 0 AND [WarrantyMileageKm] > 0");
            t.HasCheckConstraint("CK_MotorcycleModels_SpecsJson", "[SpecificationsJson] IS NULL OR ISJSON([SpecificationsJson]) = 1");
        });

        builder.HasKey(x => x.Id).HasName("PK_MotorcycleModels");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.ModelCode).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(80).IsUnicode(true).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(150).IsUnicode(true).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.EngineDisplacementCc).IsRequired();
        builder.Property(x => x.BaseRetailPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.WarrantyMonths).HasDefaultValue(36).IsRequired();
        builder.Property(x => x.WarrantyMileageKm).HasDefaultValue(30_000).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsUnicode(false);
        builder.Property(x => x.SpecificationsJson).HasColumnType("nvarchar(max)");

        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.ModelCode).IsUnique().HasDatabaseName("UQ_MotorcycleModels_ModelCode");

        builder.HasIndex(x => new { x.Category, x.BaseRetailPrice })
            .IncludeProperties(x => new { x.Brand, x.ModelName, x.EngineDisplacementCc, x.ImageUrl })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_MotorcycleModels_Category_Active");
    }
}
