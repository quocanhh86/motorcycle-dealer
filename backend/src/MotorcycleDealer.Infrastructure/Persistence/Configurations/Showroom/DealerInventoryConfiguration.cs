using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Showroom;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Showroom;

public sealed class DealerInventoryConfiguration : IEntityTypeConfiguration<DealerInventory>
{
    public void Configure(EntityTypeBuilder<DealerInventory> builder)
    {
        builder.ToTable("DealerInventories", t =>
        {
            t.HasCheckConstraint(
                "CK_DealerInventories_VIN",
                "LEN([VIN]) = 17 AND [VIN] NOT LIKE '%[^A-HJ-NPR-Z0-9]%'");
            t.HasCheckConstraint("CK_DealerInventories_Year", "[ManufactureYear] BETWEEN 2000 AND 2100");
            t.HasCheckConstraint("CK_DealerInventories_Price", "[ListedPrice] >= 0");
            t.HasCheckConstraint(
                "CK_DealerInventories_Status",
                "[StockStatus] IN ('Available', 'Reserved', 'Sold', 'Transferred')");
        });

        builder.HasKey(x => x.Id).HasName("PK_DealerInventories");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.VIN).HasMaxLength(17).IsUnicode(false).IsRequired();
        builder.Property(x => x.EngineNumber).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(x => x.ColorName).HasMaxLength(60).IsUnicode(true).IsRequired();
        builder.Property(x => x.ManufactureYear).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.ListedPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.StockStatus).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Available").IsRequired();
        builder.Property(x => x.ReceivedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.SoldAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.VIN).IsUnique().HasDatabaseName("UQ_DealerInventories_VIN");
        builder.HasIndex(x => x.EngineNumber).IsUnique().HasDatabaseName("UQ_DealerInventories_EngineNumber");

        builder.HasIndex(x => new { x.DealerId, x.StockStatus, x.MotorcycleModelId })
            .IncludeProperties(x => new { x.VIN, x.ColorName, x.ListedPrice, x.ManufactureYear })
            .HasDatabaseName("IX_DealerInventories_Dealer_Status_Model");

        builder.HasOne(x => x.Dealer)
            .WithMany(d => d.Inventories)
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DealerInventories_Dealers");

        builder.HasOne(x => x.MotorcycleModel)
            .WithMany(m => m.Inventories)
            .HasForeignKey(x => x.MotorcycleModelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DealerInventories_Models");
    }
}
