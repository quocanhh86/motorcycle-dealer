using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Identity;

public sealed class RefreshToken : BaseEntity
{
    public RefreshToken()
    {
    }

    public RefreshToken(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid TokenFamilyId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevocationReason { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public User? User { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsExpired(DateTimeOffset utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsActive(DateTimeOffset utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid tokenFamilyId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        string? createdByIp = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("INVALID_USER_ID", "RefreshToken requires a non-empty UserId.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (tokenFamilyId == Guid.Empty)
        {
            throw new DomainException("INVALID_TOKEN_FAMILY", "RefreshToken requires a non-empty TokenFamilyId.");
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new DomainException("INVALID_REFRESH_EXPIRY", "ExpiresAtUtc must be strictly greater than CreatedAtUtc.");
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            TokenFamilyId = tokenFamilyId,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = createdAtUtc,
            CreatedByIp = createdByIp
        };
    }

    public void Rotate(string successorTokenHash, DateTimeOffset utcNow, Guid? successorTokenId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(successorTokenHash);
        RevokedAtUtc = utcNow;
        ReplacedByTokenId = successorTokenId;
        ReplacedByTokenHash = successorTokenHash;
        RevocationReason = "Rotated";
    }

    public void Revoke(string reason, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        RevokedAtUtc = utcNow;
        RevocationReason = reason;
    }
}
