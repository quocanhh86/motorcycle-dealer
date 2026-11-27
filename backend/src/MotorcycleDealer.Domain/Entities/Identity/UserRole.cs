namespace MotorcycleDealer.Domain.Entities.Identity;

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public Guid? AssignedByUserId { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
    public User? AssignedByUser { get; set; }
}
