namespace MotorcycleDealer.Mobile.Core.Domain;

/// <summary>
/// Canonical MAUI Shell routes shared across ViewModels and AppShell.xaml (16 screens + modal scanner).
/// </summary>
public static class MobileRoutes
{
    // Public (3 tabs + Admin web-only gate)
    public const string ShowroomCatalog = "//public/showroom";
    public const string DealerLocator = "//public/dealers";
    public const string LoginRegister = "//public/auth";
    public const string AdminWebConsoleNotice = "//admin/web-notice";

    // Customer (5 tabs)
    public const string CustomerHome = "//customer/home";
    public const string VinGarage = "//customer/garage";
    public const string BookingWizard = "//customer/bookings";
    public const string LoyaltyPassbook = "//customer/loyalty";
    public const string CustomerWarranty = "//customer/warranty";

    // DealerStaff (5 tabs + pushed invoice builder)
    public const string StaffWorkbench = "//staff/workbench";
    public const string ShowroomVinSales = "//staff/sales";
    public const string StaffTestDrive = "//staff/test-drives";
    public const string StaffWarrantyCsat = "//staff/warranty-csat";
    public const string SlotCapacity = "//staff/slots";
    public const string MobileInvoiceBuilder = "staff-invoice-builder";

    // Shared push/modal routes
    public const string NotificationsFeed = "notifications";
    public const string BarcodeScanner = "barcode-scanner";
}

/// <summary>
/// Default transport and desktop Admin Web Console endpoint constants for mobile connectivity.
/// </summary>
public static class MobileEndpointDefaults
{
    public const string EmulatorBaseUrl = "http://10.0.2.2:5080";
    public const string AdbReverseLocalhostBaseUrl = "http://localhost:5080";
    public const string IosSimulatorBaseUrl = "http://localhost:5080";
    public const string AdminWebConsoleUrl = "http://localhost:4200/admin/analytics";
}
