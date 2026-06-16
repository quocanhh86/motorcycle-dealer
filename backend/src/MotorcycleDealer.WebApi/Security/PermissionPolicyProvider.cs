using System.Collections.Frozen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MotorcycleDealer.Domain.Constants;

namespace MotorcycleDealer.WebApi.Security;

/// <summary>
/// Resolves [Authorize(Policy = Permissions.Module.Action)] in O(1) with zero per-request allocations
/// using a pre-built FrozenDictionary across all 34 canonical permissions.
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private static readonly FrozenDictionary<string, Task<AuthorizationPolicy?>> CachedPermissionPolicies =
        Permissions.All.ToFrozenDictionary(
            permission => permission,
            static permission => Task.FromResult<AuthorizationPolicy?>(
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build()),
            StringComparer.Ordinal);

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!string.IsNullOrWhiteSpace(policyName) &&
            CachedPermissionPolicies.TryGetValue(policyName, out var cachedPolicyTask))
        {
            return cachedPolicyTask;
        }

        return base.GetPolicyAsync(policyName);
    }
}
