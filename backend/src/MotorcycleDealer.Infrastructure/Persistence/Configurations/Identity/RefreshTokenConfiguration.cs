using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Identity;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Identity;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", t =>
        {
            t.HasCheckConstraint("CK_RefreshTokens_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
        });

        builder.HasKey(x => x.Id).HasName("PK_RefreshTokens");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.TokenHash).HasMaxLength(128).IsUnicode(false).IsRequired();
        builder.Property(x => x.TokenFamilyId).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(x => x.RevokedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(128).IsUnicode(false);
        builder.Property(x => x.RevocationReason).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.CreatedByIp).HasMaxLength(45).IsUnicode(false);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_RefreshTokens_TokenHash");

        builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc })
            .IsDescending(false, true)
            .IncludeProperties(x => new { x.TokenHash, x.TokenFamilyId })
            .HasFilter("[RevokedAtUtc] IS NULL")
            .HasDatabaseName("IX_RefreshTokens_UserId_Active");

        builder.HasOne(x => x.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RefreshTokens_Users");

        builder.HasOne(x => x.ReplacedByToken)
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RefreshTokens_ReplacedBy");
    }
}
