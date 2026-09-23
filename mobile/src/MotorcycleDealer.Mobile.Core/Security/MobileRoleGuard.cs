using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.Core.Security;

public enum MobileAccessMode
{
    Public = 0,
    Customer = 1,
    DealerStaff = 2,
    AdminWebOnly = 3,
    StaffDealerUnassigned = 4
}

public sealed record RoleRouteDecision(
    MobileAccessMode Mode,
    string DefaultRoute,
    bool IsAllowedInMobileApp,
    string? BlockingReasonCode = null,
    string? BlockingMessage = null);

/// <summary>
/// Pure 48-permission RBAC and role-boundary guard for the mobile app.
/// Mirrors server-side Customer (20), DealerStaff (22, DealerId-bound), and Admin (32, web-console only) rules.
/// </summary>
public static class MobileRoleGuard
{
    public static RoleRouteDecision EvaluateSession(MobileSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.IsAuthenticated)
        {
            return new RoleRouteDecision(
                Mode: MobileAccessMode.Public,
                DefaultRoute: MobileRoutes.ShowroomCatalog,
                IsAllowedInMobileApp: true);
        }

        if (string.Equals(session.Role, RoleNames.Admin, StringComparison.Ordinal))
        {
            return new RoleRouteDecision(
                Mode: MobileAccessMode.AdminWebOnly,
                DefaultRoute: MobileRoutes.AdminWebConsoleNotice,
                IsAllowedInMobileApp: false,
                BlockingReasonCode: "ADMIN_WEB_CONSOLE_ONLY",
                BlockingMessage: AppResources.Err_ADMIN_WEB_CONSOLE_ONLY);
        }

        if (string.Equals(session.Role, RoleNames.DealerStaff, StringComparison.Ordinal))
        {
            if (!session.DealerId.HasValue || session.DealerId.Value == Guid.Empty)
            {
                return new RoleRouteDecision(
                    Mode: MobileAccessMode.StaffDealerUnassigned,
                    DefaultRoute: MobileRoutes.LoginRegister,
                    IsAllowedInMobileApp: false,
                    BlockingReasonCode: "STAFF_DEALER_UNASSIGNED",
                    BlockingMessage: AppResources.Err_STAFF_DEALER_UNASSIGNED);
            }

            return new RoleRouteDecision(
                Mode: MobileAccessMode.DealerStaff,
                DefaultRoute: MobileRoutes.StaffWorkbench,
                IsAllowedInMobileApp: true);
        }

        if (string.Equals(session.Role, RoleNames.Customer, StringComparison.Ordinal))
        {
            return new RoleRouteDecision(
                Mode: MobileAccessMode.Customer,
                DefaultRoute: MobileRoutes.CustomerHome,
                IsAllowedInMobileApp: true);
        }

        return new RoleRouteDecision(
            Mode: MobileAccessMode.Public,
            DefaultRoute: MobileRoutes.ShowroomCatalog,
            IsAllowedInMobileApp: false,
            BlockingReasonCode: "UNSUPPORTED_ROLE",
            BlockingMessage: AppResources.Format("Err_UNSUPPORTED_ROLE", session.Role));
    }

    public static bool CanAccessRoute(MobileSessionState session, string route)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(route))
        {
            return false;
        }

        var normalized = route.Trim();

        if (normalized.StartsWith("//public/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(normalized, MobileRoutes.AdminWebConsoleNotice, StringComparison.OrdinalIgnoreCase))
        {
            return session.IsAdminWebOnly;
        }

        if (normalized.StartsWith("//customer/", StringComparison.OrdinalIgnoreCase))
        {
            return session.IsCustomer;
        }

        if (normalized.StartsWith("//staff/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, MobileRoutes.MobileInvoiceBuilder, StringComparison.OrdinalIgnoreCase))
        {
            return session.IsDealerStaff && session.DealerId.HasValue && session.DealerId.Value != Guid.Empty;
        }

        if (string.Equals(normalized, MobileRoutes.NotificationsFeed, StringComparison.OrdinalIgnoreCase))
        {
            return session.IsCustomer || (session.IsDealerStaff && session.DealerId.HasValue && session.DealerId.Value != Guid.Empty);
        }

        if (string.Equals(normalized, MobileRoutes.BarcodeScanner, StringComparison.OrdinalIgnoreCase))
        {
            return session.IsCustomer || (session.IsDealerStaff && session.DealerId.HasValue && session.DealerId.Value != Guid.Empty);
        }

        return false;
    }

    public static bool HasPermission(MobileSessionState session, string permission)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.IsAuthenticated && session.HasPermission(permission);
    }

    public static bool HasAllPermissions(MobileSessionState session, params string[] requiredPermissions)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsAuthenticated || requiredPermissions is null || requiredPermissions.Length == 0)
        {
            return false;
        }

        foreach (var permission in requiredPermissions)
        {
            if (!session.HasPermission(permission))
            {
                return false;
            }
        }

        return true;
    }
}
