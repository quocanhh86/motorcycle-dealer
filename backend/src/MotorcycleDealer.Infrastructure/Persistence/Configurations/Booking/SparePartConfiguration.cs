using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Booking;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Booking;

public sealed class SparePartConfiguration : IEntityTypeConfiguration<SparePart>
{
    public void Configure(EntityTypeBuilder<SparePart> builder)
    {
        builder.ToTable("SpareParts", t =>
        {
            t.HasCheckConstraint(
                "CK_SpareParts_Category",
                "[Category] IN ('EngineOil', 'Filter', 'Brakes', 'Transmission', 'Tires', 'Electrical', 'Accessory')");
            t.HasCheckConstraint("CK_SpareParts_UnitPrice", "[UnitPriceVnd] > 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_SpareParts");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.PartCode).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsUnicode(true).IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.UnitLabel).HasMaxLength(24).IsUnicode(true).IsRequired();
        builder.Property(x => x.UnitPriceVnd).HasPrecision(18, 2).IsRequired();

        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.PartCode)
            .IsUnique()
            .HasDatabaseName("UQ_SpareParts_PartCode");

        builder.HasIndex(x => new { x.Category, x.IsActive })
            .IncludeProperties(x => new
            {
                x.PartCode,
                x.Name,
                x.CompatibleModelId,
                x.UnitLabel,
                x.UnitPriceVnd
            })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_SpareParts_Category_Active");

        builder.HasOne(x => x.CompatibleModel)
            .WithMany()
            .HasForeignKey(x => x.CompatibleModelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SpareParts_Models");
    }
}
