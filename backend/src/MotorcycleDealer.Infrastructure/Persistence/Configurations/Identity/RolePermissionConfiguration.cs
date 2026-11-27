using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Identity;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Identity;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(x => new { x.RoleId, x.PermissionId }).HasName("PK_RolePermissions");

        builder.Property(x => x.GrantedAtUtc)
            .HasColumnType("datetimeoffset(7)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.HasOne(x => x.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RolePermissions_Roles");

        builder.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RolePermissions_Permissions");
    }
}
