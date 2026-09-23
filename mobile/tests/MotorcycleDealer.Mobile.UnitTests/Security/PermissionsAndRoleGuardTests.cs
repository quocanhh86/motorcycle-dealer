using System.Text.RegularExpressions;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.UnitTests.Fakes;
using Xunit;

namespace MotorcycleDealer.Mobile.UnitTests.Security;

public sealed class PermissionsAndRoleGuardTests
{
    [Fact]
    public void Permissions_All48Strings_MatchBackendDomainPermissionsFileVerbatim()
    {
        var backendPermissionsPath = LocateBackendPermissionsFile();
        Assert.True(File.Exists(backendPermissionsPath), $"Backend Permissions.cs not found at {backendPermissionsPath}");

        var source = File.ReadAllText(backendPermissionsPath);
        var matches = Regex.Matches(source, "\"(Permissions\\.[A-Za-z0-9]+\\.[A-Za-z0-9]+)\"");
        var backendPermissions = matches
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        var mobilePermissions = Permissions.All
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(48, backendPermissions.Length);
        Assert.Equal(48, Permissions.All.Count);
        Assert.Equal(backendPermissions, mobilePermissions);
    }

    [Fact]
    public void RolePermissionCounts_MatchCanonical_Customer20_Staff22_Admin32()
    {
        Assert.Equal(20, Permissions.CustomerDefaults.Count);
        Assert.Equal(22, Permissions.DealerStaffDefaults.Count);
        Assert.Equal(32, Permissions.AdminDefaults.Count);

        Assert.Equal(20, Permissions.GetDefaultsForRole(RoleNames.Customer).Count);
        Assert.Equal(22, Permissions.GetDefaultsForRole(RoleNames.DealerStaff).Count);
        Assert.Equal(32, Permissions.GetDefaultsForRole(RoleNames.Admin).Count);

        Assert.All(Permissions.CustomerDefaults, p => Assert.Contains(p, Permissions.All));
        Assert.All(Permissions.DealerStaffDefaults, p => Assert.Contains(p, Permissions.All));
        Assert.All(Permissions.AdminDefaults, p => Assert.Contains(p, Permissions.All));
    }

    [Fact]
    public void RoleGuard_UnauthenticatedSession_RoutesToPublicShowroom()
    {
        var session = new MobileSessionState();
        var decision = MobileRoleGuard.EvaluateSession(session);

        Assert.Equal(MobileAccessMode.Public, decision.Mode);
        Assert.Equal(MobileRoutes.ShowroomCatalog, decision.DefaultRoute);
        Assert.True(decision.IsAllowedInMobileApp);
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.ShowroomCatalog));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.DealerLocator));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.CustomerHome));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.StaffWorkbench));
    }

    [Fact]
    public void RoleGuard_CustomerSession_AllowsCustomerRoutesAndBlocksStaffAndAdmin()
    {
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser());

        var decision = MobileRoleGuard.EvaluateSession(session);
        Assert.Equal(MobileAccessMode.Customer, decision.Mode);
        Assert.Equal(MobileRoutes.CustomerHome, decision.DefaultRoute);
        Assert.True(decision.IsAllowedInMobileApp);

        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.CustomerHome));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.VinGarage));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.BookingWizard));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.LoyaltyPassbook));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.CustomerWarranty));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.NotificationsFeed));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.StaffWorkbench));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.MobileInvoiceBuilder));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.AdminWebConsoleNotice));
    }

    [Fact]
    public void RoleGuard_DealerStaffWithValidDealerId_AllowsStaffRoutesAndBlocksCustomerRoutes()
    {
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.StaffUser());

        var decision = MobileRoleGuard.EvaluateSession(session);
        Assert.Equal(MobileAccessMode.DealerStaff, decision.Mode);
        Assert.Equal(MobileRoutes.StaffWorkbench, decision.DefaultRoute);
        Assert.True(decision.IsAllowedInMobileApp);

        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.StaffWorkbench));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.MobileInvoiceBuilder));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.ShowroomVinSales));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.StaffTestDrive));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.StaffWarrantyCsat));
        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.SlotCapacity));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.CustomerHome));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.LoyaltyPassbook));
    }

    [Fact]
    public void RoleGuard_DealerStaffMissingDealerId_ReturnsStaffDealerUnassignedBlock()
    {
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.StaffUser() with { DealerId = null });

        var decision = MobileRoleGuard.EvaluateSession(session);
        Assert.Equal(MobileAccessMode.StaffDealerUnassigned, decision.Mode);
        Assert.False(decision.IsAllowedInMobileApp);
        Assert.Equal("STAFF_DEALER_UNASSIGNED", decision.BlockingReasonCode);
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.StaffWorkbench));
    }

    [Fact]
    public void RoleGuard_AdminSession_RedirectsToAdminWebConsoleNotice()
    {
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.AdminUser());

        var decision = MobileRoleGuard.EvaluateSession(session);
        Assert.Equal(MobileAccessMode.AdminWebOnly, decision.Mode);
        Assert.Equal(MobileRoutes.AdminWebConsoleNotice, decision.DefaultRoute);
        Assert.False(decision.IsAllowedInMobileApp);
        Assert.Equal("ADMIN_WEB_CONSOLE_ONLY", decision.BlockingReasonCode);

        Assert.True(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.AdminWebConsoleNotice));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.CustomerHome));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.StaffWorkbench));
        Assert.False(MobileRoleGuard.CanAccessRoute(session, MobileRoutes.NotificationsFeed));
    }

    [Theory]
    [InlineData(Permissions.Bookings.Create, true)]
    [InlineData(Permissions.Loyalty.RedeemVoucher, true)]
    [InlineData(Permissions.Reviews.CreateOwn, true)]
    [InlineData(Permissions.Warranty.CreateOwn, true)]
    [InlineData(Permissions.Invoices.CreateDealer, false)]
    [InlineData(Permissions.Analytics.ViewAllDealers, false)]
    public void RoleGuard_HasPermission_EvaluatesCustomerCapabilitiesAccurately(string permission, bool expected)
    {
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.CustomerUser());

        Assert.Equal(expected, MobileRoleGuard.HasPermission(session, permission));
    }

    [Theory]
    [InlineData(Permissions.Invoices.CreateDealer, true)]
    [InlineData(Permissions.Bookings.UpdateStatus, true)]
    [InlineData(Permissions.Warranty.ManageDealer, true)]
    [InlineData(Permissions.Reviews.RespondDealer, true)]
    [InlineData(Permissions.Loyalty.RedeemVoucher, false)]
    [InlineData(Permissions.Dealers.ManageAll, false)]
    public void RoleGuard_HasPermission_EvaluatesDealerStaffCapabilitiesAccurately(string permission, bool expected)
    {
        var session = new MobileSessionState();
        session.ApplyAuthenticatedUser(TestDataFactory.StaffUser());

        Assert.Equal(expected, MobileRoleGuard.HasPermission(session, permission));
    }

    private static string LocateBackendPermissionsFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "backend",
                "src",
                "MotorcycleDealer.Domain",
                "Constants",
                "Permissions.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return string.Empty;
    }
}
