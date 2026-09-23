using MotorcycleDealer.Mobile.Core.Contracts;

namespace MotorcycleDealer.Mobile.Core.Security;

/// <summary>
/// Thread-safe singleton session state representing the currently authenticated (or anonymous) mobile user.
/// </summary>
public sealed class MobileSessionState
{
    private readonly object _sync = new();
    private HashSet<string> _permissions = new(StringComparer.Ordinal);

    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public Guid? DealerId { get; private set; }
    public string? DealerCode { get; private set; }
    public string? DealerName { get; private set; }
    public string? StaffEmployeeCode { get; private set; }
    public string? TierCode { get; private set; }
    public decimal? TierMultiplier { get; private set; }
    public int? AvailablePoints { get; private set; }
    public int? LifetimeQualifyingPoints { get; private set; }
    public decimal? LifetimeSpentVnd { get; private set; }

    public IReadOnlyCollection<string> Permissions
    {
        get
        {
            lock (_sync)
            {
                return _permissions.ToArray();
            }
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            lock (_sync)
            {
                return UserId != Guid.Empty && !string.IsNullOrWhiteSpace(Role);
            }
        }
    }

    public bool IsCustomer
    {
        get
        {
            lock (_sync)
            {
                return IsAuthenticated && string.Equals(Role, RoleNames.Customer, StringComparison.Ordinal);
            }
        }
    }

    public bool IsDealerStaff
    {
        get
        {
            lock (_sync)
            {
                return IsAuthenticated && string.Equals(Role, RoleNames.DealerStaff, StringComparison.Ordinal);
            }
        }
    }

    public bool IsAdminWebOnly
    {
        get
        {
            lock (_sync)
            {
                return IsAuthenticated && string.Equals(Role, RoleNames.Admin, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// Strict SQLite partition key preventing cross-user or cross-dealer cache bleed on shared devices.
    /// </summary>
    public string OwnerScopeKey
    {
        get
        {
            lock (_sync)
            {
                if (UserId == Guid.Empty || string.IsNullOrWhiteSpace(Role))
                {
                    return "public:anonymous:global";
                }

                var dealerSegment = DealerId.HasValue && DealerId.Value != Guid.Empty
                    ? DealerId.Value.ToString("D")
                    : "global";
                return $"{UserId:D}:{Role}:{dealerSegment}";
            }
        }
    }

    public event EventHandler? SessionChanged;

    public void ApplyAuthenticatedUser(CurrentUserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_sync)
        {
            UserId = user.UserId;
            Email = user.Email;
            FullName = user.FullName;
            PhoneNumber = user.PhoneNumber;
            Role = user.Role;
            DealerId = user.DealerId;
            DealerCode = user.DealerCode;
            DealerName = user.DealerName;
            StaffEmployeeCode = user.StaffEmployeeCode;
            TierCode = user.TierCode;
            TierMultiplier = user.TierMultiplier ?? user.PointsMultiplier;
            AvailablePoints = user.AvailablePoints;
            LifetimeQualifyingPoints = user.LifetimeQualifyingPoints;
            LifetimeSpentVnd = user.LifetimeSpentVnd;
            _permissions = user.Permissions is { Count: > 0 }
                ? new HashSet<string>(user.Permissions, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateLoyaltySnapshot(
        int availablePoints,
        int lifetimeQualifyingPoints,
        string? tierCode = null,
        decimal? tierMultiplier = null,
        decimal? lifetimeSpentVnd = null)
    {
        lock (_sync)
        {
            AvailablePoints = availablePoints;
            LifetimeQualifyingPoints = lifetimeQualifyingPoints;
            if (!string.IsNullOrWhiteSpace(tierCode))
            {
                TierCode = tierCode;
            }

            if (tierMultiplier.HasValue)
            {
                TierMultiplier = tierMultiplier.Value;
            }

            if (lifetimeSpentVnd.HasValue)
            {
                LifetimeSpentVnd = lifetimeSpentVnd.Value;
            }
        }

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        lock (_sync)
        {
            return _permissions.Contains(permission);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            UserId = Guid.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            PhoneNumber = string.Empty;
            Role = string.Empty;
            DealerId = null;
            DealerCode = null;
            DealerName = null;
            StaffEmployeeCode = null;
            TierCode = null;
            TierMultiplier = null;
            AvailablePoints = null;
            LifetimeQualifyingPoints = null;
            LifetimeSpentVnd = null;
            _permissions.Clear();
        }

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
