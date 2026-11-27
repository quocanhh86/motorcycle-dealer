using Microsoft.AspNetCore.Authorization;

namespace MotorcycleDealer.WebApi.Security;

public sealed record PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }

    public string Permission { get; }
}
