namespace MotorcycleDealer.Domain.Constants;

public static class RoleNames
{
    public const string Customer = "Customer";
    public const string DealerStaff = "DealerStaff";
    public const string Admin = "Admin";

    public static IReadOnlyList<string> All { get; } =
    [
        Customer,
        DealerStaff,
        Admin
    ];
}
