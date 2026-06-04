using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Domain.Constants;

/// <summary>
/// Canonical 48-permission RBAC catalog matching docs/DESIGN.md and docs/openapi.v1.yaml.
/// Enforces strict Separation of Duties between Customer self-service and Staff/Admin operations.
/// </summary>
public static class Permissions
{
    public const string PolicyPrefix = "Permissions.";

    public static class Showroom
    {
        public const string View = "Permissions.Showroom.View";
        public const string ManageModels = "Permissions.Showroom.ManageModels";
    }

    public static class Inventories
    {
        public const string ViewDealer = "Permissions.Inventories.ViewDealer";
        public const string ManageDealer = "Permissions.Inventories.ManageDealer";
    }

    public static class SalesOrders
    {
        public const string Create = "Permissions.SalesOrders.Create";
        public const string ViewOwn = "Permissions.SalesOrders.ViewOwn";
        public const string ViewDealer = "Permissions.SalesOrders.ViewDealer";
    }

    public static class Vehicles
    {
        public const string ViewOwn = "Permissions.Vehicles.ViewOwn";
        public const string RegisterOwn = "Permissions.Vehicles.RegisterOwn";
        public const string ViewDealer = "Permissions.Vehicles.ViewDealer";
        public const string ManageRules = "Permissions.Vehicles.ManageRules";
    }

    public static class TimeSlots
    {
        public const string ViewAvailable = "Permissions.TimeSlots.ViewAvailable";
        public const string ManageDealer = "Permissions.TimeSlots.ManageDealer";
    }

    public static class Bookings
    {
        public const string Create = "Permissions.Bookings.Create";
        public const string CancelOwn = "Permissions.Bookings.CancelOwn";
        public const string ViewOwn = "Permissions.Bookings.ViewOwn";
        public const string ViewDealer = "Permissions.Bookings.ViewDealer";
        public const string UpdateStatus = "Permissions.Bookings.UpdateStatus";
    }

    public static class TestDrives
    {
        public const string Create = "Permissions.TestDrives.Create";
        public const string CancelOwn = "Permissions.TestDrives.CancelOwn";
        public const string ViewOwn = "Permissions.TestDrives.ViewOwn";
        public const string ViewDealer = "Permissions.TestDrives.ViewDealer";
        public const string UpdateStatus = "Permissions.TestDrives.UpdateStatus";
    }

    public static class SpareParts
    {
        public const string ManageCatalog = "Permissions.SpareParts.ManageCatalog";
    }

    public static class Invoices
    {
        public const string CreateDealer = "Permissions.Invoices.CreateDealer";
        public const string ViewOwn = "Permissions.Invoices.ViewOwn";
        public const string ViewDealer = "Permissions.Invoices.ViewDealer";
    }

    public static class Loyalty
    {
        public const string ViewOwn = "Permissions.Loyalty.ViewOwn";
        public const string RedeemVoucher = "Permissions.Loyalty.RedeemVoucher";
        public const string ViewDealerCustomer = "Permissions.Loyalty.ViewDealerCustomer";
        public const string ManageTiers = "Permissions.Loyalty.ManageTiers";
        public const string ManageVoucherCatalog = "Permissions.Loyalty.ManageVoucherCatalog";
        public const string AdjustPoints = "Permissions.Loyalty.AdjustPoints";
    }

    public static class Notifications
    {
        public const string ViewOwn = "Permissions.Notifications.ViewOwn";
        public const string ViewDealer = "Permissions.Notifications.ViewDealer";
    }

    public static class Dealers
    {
        public const string ViewPublic = "Permissions.Dealers.ViewPublic";
        public const string ManageAll = "Permissions.Dealers.ManageAll";
    }

    public static class Users
    {
        public const string ManageStaff = "Permissions.Users.ManageStaff";
    }

    public static class Analytics
    {
        public const string ViewAllDealers = "Permissions.Analytics.ViewAllDealers";
        public const string ExportReports = "Permissions.Analytics.ExportReports";
    }

    public static class Reviews
    {
        public const string CreateOwn = "Permissions.Reviews.CreateOwn";
        public const string ViewOwn = "Permissions.Reviews.ViewOwn";
        public const string ViewDealer = "Permissions.Reviews.ViewDealer";
        public const string RespondDealer = "Permissions.Reviews.RespondDealer";
    }

    public static class Warranty
    {
        public const string CreateOwn = "Permissions.Warranty.CreateOwn";
        public const string ViewOwn = "Permissions.Warranty.ViewOwn";
        public const string ViewDealer = "Permissions.Warranty.ViewDealer";
        public const string ManageDealer = "Permissions.Warranty.ManageDealer";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Showroom.View,
        Showroom.ManageModels,
        Inventories.ViewDealer,
        Inventories.ManageDealer,
        SalesOrders.Create,
        SalesOrders.ViewOwn,
        SalesOrders.ViewDealer,
        Vehicles.ViewOwn,
        Vehicles.RegisterOwn,
        Vehicles.ViewDealer,
        Vehicles.ManageRules,
        TimeSlots.ViewAvailable,
        TimeSlots.ManageDealer,
        Bookings.Create,
        Bookings.CancelOwn,
        Bookings.ViewOwn,
        Bookings.ViewDealer,
        Bookings.UpdateStatus,
        TestDrives.Create,
        TestDrives.CancelOwn,
        TestDrives.ViewOwn,
        TestDrives.ViewDealer,
        TestDrives.UpdateStatus,
        SpareParts.ManageCatalog,
        Invoices.CreateDealer,
        Invoices.ViewOwn,
        Invoices.ViewDealer,
        Loyalty.ViewOwn,
        Loyalty.RedeemVoucher,
        Loyalty.ViewDealerCustomer,
        Loyalty.ManageTiers,
        Loyalty.ManageVoucherCatalog,
        Loyalty.AdjustPoints,
        Notifications.ViewOwn,
        Notifications.ViewDealer,
        Dealers.ViewPublic,
        Dealers.ManageAll,
        Users.ManageStaff,
        Analytics.ViewAllDealers,
        Analytics.ExportReports,
        Reviews.CreateOwn,
        Reviews.ViewOwn,
        Reviews.ViewDealer,
        Reviews.RespondDealer,
        Warranty.CreateOwn,
        Warranty.ViewOwn,
        Warranty.ViewDealer,
        Warranty.ManageDealer
    ];

    public static IReadOnlyList<string> CustomerDefaults { get; } =
    [
        Showroom.View,
        SalesOrders.ViewOwn,
        Vehicles.ViewOwn,
        Vehicles.RegisterOwn,
        TimeSlots.ViewAvailable,
        Bookings.Create,
        Bookings.CancelOwn,
        Bookings.ViewOwn,
        TestDrives.Create,
        TestDrives.CancelOwn,
        TestDrives.ViewOwn,
        Invoices.ViewOwn,
        Loyalty.ViewOwn,
        Loyalty.RedeemVoucher,
        Notifications.ViewOwn,
        Dealers.ViewPublic,
        Reviews.CreateOwn,
        Reviews.ViewOwn,
        Warranty.CreateOwn,
        Warranty.ViewOwn
    ];

    public static IReadOnlyList<string> DealerStaffDefaults { get; } =
    [
        Showroom.View,
        Inventories.ViewDealer,
        Inventories.ManageDealer,
        SalesOrders.Create,
        SalesOrders.ViewDealer,
        Vehicles.ViewDealer,
        TimeSlots.ViewAvailable,
        TimeSlots.ManageDealer,
        Bookings.ViewDealer,
        Bookings.UpdateStatus,
        TestDrives.ViewDealer,
        TestDrives.UpdateStatus,
        Invoices.CreateDealer,
        Invoices.ViewDealer,
        Loyalty.ViewDealerCustomer,
        Notifications.ViewOwn,
        Notifications.ViewDealer,
        Dealers.ViewPublic,
        Reviews.ViewDealer,
        Reviews.RespondDealer,
        Warranty.ViewDealer,
        Warranty.ManageDealer
    ];

    public static IReadOnlyList<string> AdminDefaults { get; } =
    [
        Showroom.View,
        Showroom.ManageModels,
        Inventories.ViewDealer,
        Inventories.ManageDealer,
        SalesOrders.Create,
        SalesOrders.ViewDealer,
        Vehicles.ViewDealer,
        Vehicles.ManageRules,
        TimeSlots.ViewAvailable,
        TimeSlots.ManageDealer,
        Bookings.ViewDealer,
        Bookings.UpdateStatus,
        TestDrives.ViewDealer,
        TestDrives.UpdateStatus,
        SpareParts.ManageCatalog,
        Invoices.CreateDealer,
        Invoices.ViewDealer,
        Loyalty.ViewDealerCustomer,
        Loyalty.ManageTiers,
        Loyalty.ManageVoucherCatalog,
        Loyalty.AdjustPoints,
        Notifications.ViewOwn,
        Notifications.ViewDealer,
        Dealers.ViewPublic,
        Dealers.ManageAll,
        Users.ManageStaff,
        Analytics.ViewAllDealers,
        Analytics.ExportReports,
        Reviews.ViewDealer,
        Reviews.RespondDealer,
        Warranty.ViewDealer,
        Warranty.ManageDealer
    ];

    public static IReadOnlyList<string> GetDefaultsForRole(UserRoleCode role) =>
        role switch
        {
            UserRoleCode.Customer => CustomerDefaults,
            UserRoleCode.DealerStaff => DealerStaffDefaults,
            UserRoleCode.Admin => AdminDefaults,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported role code.")
        };

    public static IReadOnlyList<string> GetDefaultsForRole(string roleName) =>
        roleName switch
        {
            RoleNames.Customer => CustomerDefaults,
            RoleNames.DealerStaff => DealerStaffDefaults,
            RoleNames.Admin => AdminDefaults,
            _ => throw new ArgumentException($"Unsupported role name '{roleName}'.", nameof(roleName))
        };
}
