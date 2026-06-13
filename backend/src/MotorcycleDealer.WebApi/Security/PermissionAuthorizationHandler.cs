using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MotorcycleDealer.Domain.Constants;

namespace MotorcycleDealer.WebApi.Security;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // MOD-01 §6.2 #3: DealerStaff principals MUST carry a valid non-empty Guid in claim "dealer_id".
        var roleClaim = context.User.FindFirstValue(JwtClaimNames.Role) ??
                        context.User.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(roleClaim, RoleNames.DealerStaff, StringComparison.Ordinal) ||
            context.User.IsInRole(RoleNames.DealerStaff))
        {
            var rawDealerId = context.User.FindFirstValue(JwtClaimNames.DealerId);
            if (!Guid.TryParse(rawDealerId, out var parsedDealerId) || parsedDealerId == Guid.Empty)
            {
                return Task.CompletedTask;
            }
        }

        var httpContext = context.Resource as HttpContext;
        var grantedPermissions = HttpCurrentUserContext.GetOrExtractPermissions(context.User, httpContext);
        if (grantedPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
