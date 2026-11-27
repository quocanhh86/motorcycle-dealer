using Microsoft.AspNetCore.Authorization;

namespace MotorcycleDealer.WebApi.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(policy: permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
