using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Identity;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
        {
            t.HasCheckConstraint(
                "CK_Users_DealerStaff_DealerId",
                "([StaffEmployeeCode] IS NULL AND [DealerId] IS NULL) OR ([StaffEmployeeCode] IS NOT NULL AND [DealerId] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_Users");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Email).HasMaxLength(256).IsUnicode(false).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsUnicode(false).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsUnicode(false).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(150).IsUnicode(true).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.StaffEmployeeCode).HasMaxLength(30).IsUnicode(false);

        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.LastLoginAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        builder.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("UQ_Users_NormalizedEmail");

        builder.HasIndex(x => x.PhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_Users_PhoneNumber");

        builder.HasIndex(x => x.StaffEmployeeCode)
            .IsUnique()
            .HasFilter("[StaffEmployeeCode] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UQ_Users_StaffEmployeeCode");

        builder.HasIndex(x => new { x.DealerId, x.IsActive })
            .IncludeProperties(x => new { x.FullName, x.Email, x.StaffEmployeeCode })
            .HasFilter("[DealerId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_Users_DealerId_Active");

        builder.HasOne(x => x.Dealer)
            .WithMany(d => d.StaffUsers)
            .HasForeignKey(x => x.DealerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Users_Dealers_DealerId");
    }
}
