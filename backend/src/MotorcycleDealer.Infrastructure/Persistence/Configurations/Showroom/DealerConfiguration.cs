using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Showroom;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Showroom;

public sealed class DealerConfiguration : IEntityTypeConfiguration<Dealer>
{
    public void Configure(EntityTypeBuilder<Dealer> builder)
    {
        builder.ToTable("Dealers", t =>
        {
            t.HasCheckConstraint("CK_Dealers_Hours", "[ClosingTime] > [OpeningTime]");
            t.HasCheckConstraint("CK_Dealers_Capacity", "[DefaultSlotCapacity] BETWEEN 1 AND 50");
            t.HasCheckConstraint(
                "CK_Dealers_Coordinates",
                "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
        });

        builder.HasKey(x => x.Id).HasName("PK_Dealers");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsUnicode(true).IsRequired();
        builder.Property(x => x.City).HasMaxLength(80).IsUnicode(true).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(300).IsUnicode(true).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(256).IsUnicode(false).IsRequired();

        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);

        builder.Property(x => x.OpeningTime).HasColumnType("time(0)").HasDefaultValueSql("'08:00:00'").IsRequired();
        builder.Property(x => x.ClosingTime).HasColumnType("time(0)").HasDefaultValueSql("'18:00:00'").IsRequired();
        builder.Property(x => x.DefaultSlotCapacity).HasDefaultValue(4).IsRequired();

        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_Dealers_Code");
    }
}
