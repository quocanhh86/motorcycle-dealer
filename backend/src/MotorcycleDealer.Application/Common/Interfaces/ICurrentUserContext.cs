using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;

namespace MotorcycleDealer.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? DealerId { get; }
    bool IsAuthenticated { get; }
    IReadOnlySet<string> Permissions { get; }

    bool HasPermission(string permission);

    bool IsCustomer => string.Equals(Role, RoleNames.Customer, StringComparison.Ordinal);
    bool IsDealerStaff => string.Equals(Role, RoleNames.DealerStaff, StringComparison.Ordinal);
    bool IsAdmin => string.Equals(Role, RoleNames.Admin, StringComparison.Ordinal);

    Guid RequireUserId() =>
        IsAuthenticated && UserId is { } id && id != Guid.Empty
            ? id
            : throw new UnauthorizedAccessException("Authenticated user identifier is required.");

    Guid RequireAssignedDealerId() =>
        DealerId is { } dealerId && dealerId != Guid.Empty
            ? dealerId
            : throw new ForbiddenAccessException(
                "DealerStaff principal does not have an assigned DealerId scope.",
                "FORBIDDEN_DEALER_SCOPE");
}
