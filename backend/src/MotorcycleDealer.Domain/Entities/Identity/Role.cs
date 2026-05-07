using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Identity;

public sealed class Role : AuditableEntity
{
    public Role()
    {
    }

    public Role(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
