using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Identity;

public sealed class Permission : BaseEntity
{
    public Permission()
    {
    }

    public Permission(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
