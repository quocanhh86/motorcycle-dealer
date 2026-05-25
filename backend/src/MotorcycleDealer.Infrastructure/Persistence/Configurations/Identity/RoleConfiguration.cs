using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Identity;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Identity;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", t =>
        {
            t.HasCheckConstraint("CK_Roles_Name", "[Name] IN ('Customer', 'DealerStaff', 'Admin')");
        });

        builder.HasKey(x => x.Id).HasName("PK_Roles");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Name).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsUnicode(true).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsUnicode(true).IsRequired();
        builder.Property(x => x.IsSystemRole).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_Roles_Name");
    }
}
