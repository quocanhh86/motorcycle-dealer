using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.App.Views.Shared;
using MotorcycleDealer.Mobile.App.Views.Staff;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.Core.ViewModels;

namespace MotorcycleDealer.Mobile.App;

public partial class AppShell : Shell
{
    private readonly MobileSessionState _sessionState;
    private readonly AppShellViewModel _viewModel;
    private readonly IUserActionLogger _userActionLogger;

    public AppShell(
        AppShellViewModel viewModel,
        MobileSessionState sessionState,
        IUserActionLogger userActionLogger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _userActionLogger = userActionLogger ?? throw new ArgumentNullException(nameof(userActionLogger));

        InitializeComponent();
        BindingContext = _viewModel;
        ApplyLocalizedTabTitles();

        // Keep all 14 bottom tab titles synchronized immediately when VI <-> EN switches live
        WeakReferenceMessenger.Default.Register<AppShell, CultureChangedMessage>(
            this,
            static (recipient, _) => recipient.RefreshLocalizedTabTitlesOnMainThread());

        Routing.RegisterRoute(MobileRoutes.MobileInvoiceBuilder, typeof(MobileInvoiceBuilderPage));
        Routing.RegisterRoute(MobileRoutes.NotificationsFeed, typeof(NotificationsFeedPage));
    }

    public void EnsureTabVisibilityForRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        string normalized = route.Trim();
        if (normalized.StartsWith("//public/", StringComparison.OrdinalIgnoreCase))
        {
            PublicTabBar.IsVisible = true;
            CustomerTabBar.IsVisible = false;
            StaffTabBar.IsVisible = false;
            AdminTabBar.IsVisible = false;
        }
        else if (normalized.StartsWith("//customer/", StringComparison.OrdinalIgnoreCase))
        {
            PublicTabBar.IsVisible = false;
            CustomerTabBar.IsVisible = true;
            StaffTabBar.IsVisible = false;
            AdminTabBar.IsVisible = false;
        }
        else if (normalized.StartsWith("//staff/", StringComparison.OrdinalIgnoreCase))
        {
            PublicTabBar.IsVisible = false;
            CustomerTabBar.IsVisible = false;
            StaffTabBar.IsVisible = true;
            AdminTabBar.IsVisible = false;
        }
        else if (normalized.StartsWith("//admin/", StringComparison.OrdinalIgnoreCase))
        {
            PublicTabBar.IsVisible = false;
            CustomerTabBar.IsVisible = false;
            StaffTabBar.IsVisible = false;
            AdminTabBar.IsVisible = true;
        }
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        string target = args.Target?.Location.OriginalString ?? string.Empty;
        if (string.IsNullOrWhiteSpace(target) || target == ".." || target.StartsWith("..?", StringComparison.Ordinal))
        {
            return;
        }

        string routePath = target.Split('?', '#')[0];
        if (routePath.StartsWith("//", StringComparison.Ordinal) ||
            routePath.Equals(MobileRoutes.MobileInvoiceBuilder, StringComparison.OrdinalIgnoreCase) ||
            routePath.Equals(MobileRoutes.NotificationsFeed, StringComparison.OrdinalIgnoreCase))
        {
            // Normalize shell route paths that include intermediate tab routes (e.g., //customer/home-tab/home -> //customer/home)
            string canonical = NormalizeShellTarget(routePath);
            if (!MobileRoleGuard.CanAccessRoute(_sessionState, canonical))
            {
                _userActionLogger.Track(
                    action: "Navigation.GuardBlocked",
                    category: "Navigation",
                    target: canonical,
                    outcome: "Denied");
                args.Cancel();
            }
        }
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        string currentRoute = NormalizeShellTarget(args.Current?.Location.OriginalString?.Split('?', '#')[0] ?? string.Empty);
        string previousRoute = NormalizeShellTarget(args.Previous?.Location.OriginalString?.Split('?', '#')[0] ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(currentRoute))
        {
            _userActionLogger.SetCurrentScreen(currentRoute);
            _userActionLogger.Track(
                action: "Navigation.RouteChanged",
                category: "Navigation",
                target: currentRoute,
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["fromRoute"] = string.IsNullOrWhiteSpace(previousRoute) ? "initial" : previousRoute,
                    ["toRoute"] = currentRoute
                });
        }
    }

    private void RefreshLocalizedTabTitlesOnMainThread()
    {
        if (MainThread.IsMainThread)
        {
            ApplyLocalizedTabTitles();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(ApplyLocalizedTabTitles);
        }
    }

    private void ApplyLocalizedTabTitles()
    {
        LocalizationService loc = _viewModel.Loc;
        Title = loc.AppTitle;

        SetShellTabTitle(TabShowroom, loc.TabShowroom);
        SetShellTabTitle(TabDealers, loc.TabDealers);
        SetShellTabTitle(TabSignIn, loc.TabSignIn);

        SetShellTabTitle(TabHome, loc.TabHome);
        SetShellTabTitle(TabGarage, loc.TabGarage);
        SetShellTabTitle(TabBook, loc.TabBook);
        SetShellTabTitle(TabLoyalty, loc.TabLoyalty);
        SetShellTabTitle(TabWarranty, loc.TabWarranty);

        SetShellTabTitle(TabBayOps, loc.TabBayOps);
        SetShellTabTitle(TabVinSales, loc.TabVinSales);
        SetShellTabTitle(TabRides, loc.TabRides);
        SetShellTabTitle(TabQa, loc.TabQa);
        SetShellTabTitle(TabSlots, loc.TabSlots);

        SetShellTabTitle(TabAdminHq, loc.TabAdminHq);
    }

    private static void SetShellTabTitle(ShellContent content, string title)
    {
        content.Title = title;
        if (content.Parent is ShellSection section)
        {
            section.Title = title;
        }
    }

    private static string NormalizeShellTarget(string path)
    {
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && (parts[0] is "public" or "customer" or "staff" or "admin"))
        {
            return $"//{parts[0]}/{parts[^1]}";
        }

        return path;
    }
}
