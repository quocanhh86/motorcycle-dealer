using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Identity;

public sealed class User : AuditableEntity, ISoftDeletable
{
    public User()
    {
    }

    public User(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid? DealerId { get; set; }
    public string? StaffEmployeeCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Showroom.Dealer? Dealer { get; set; }
    public Loyalty.LoyaltyAccount? LoyaltyAccount { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public static string NormalizeEmailValue(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToUpperInvariant();
    }

    public static User CreateCustomer(
        string email,
        string passwordHash,
        string fullName,
        string phoneNumber,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var trimmedEmail = email.Trim();
        return new User(id ?? Guid.Empty)
        {
            Email = trimmedEmail,
            NormalizedEmail = NormalizeEmailValue(trimmedEmail),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            DealerId = null,
            StaffEmployeeCode = null,
            IsActive = true
        };
    }

    public static User CreateDealerStaff(
        string email,
        string passwordHash,
        string fullName,
        string phoneNumber,
        Guid dealerId,
        string staffEmployeeCode,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        if (dealerId == Guid.Empty || string.IsNullOrWhiteSpace(staffEmployeeCode))
        {
            throw new BusinessRuleViolationException(
                "DealerStaff user requires both a non-empty DealerId and StaffEmployeeCode.",
                "INVALID_DEALER_STAFF_BINDING");
        }

        var trimmedEmail = email.Trim();
        return new User(id ?? Guid.Empty)
        {
            Email = trimmedEmail,
            NormalizedEmail = NormalizeEmailValue(trimmedEmail),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            DealerId = dealerId,
            StaffEmployeeCode = staffEmployeeCode.Trim().ToUpperInvariant(),
            IsActive = true
        };
    }

    public static User CreateAdmin(
        string email,
        string passwordHash,
        string fullName,
        string phoneNumber,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var trimmedEmail = email.Trim();
        return new User(id ?? Guid.Empty)
        {
            Email = trimmedEmail,
            NormalizedEmail = NormalizeEmailValue(trimmedEmail),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            DealerId = null,
            StaffEmployeeCode = null,
            IsActive = true
        };
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTimeOffset utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }
}
