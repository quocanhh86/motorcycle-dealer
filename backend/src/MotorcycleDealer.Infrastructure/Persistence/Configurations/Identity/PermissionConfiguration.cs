using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Identity;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Identity;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", t =>
        {
            t.HasCheckConstraint("CK_Permissions_CodePrefix", "[Code] LIKE 'Permissions.%._%'");
        });

        builder.HasKey(x => x.Id).HasName("PK_Permissions");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Code).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsUnicode(true).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsUnicode(true).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_Permissions_Code");
    }
}
