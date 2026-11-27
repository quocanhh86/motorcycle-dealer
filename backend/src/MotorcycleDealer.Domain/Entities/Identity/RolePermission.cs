namespace MotorcycleDealer.Domain.Entities.Identity;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTimeOffset GrantedAtUtc { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
