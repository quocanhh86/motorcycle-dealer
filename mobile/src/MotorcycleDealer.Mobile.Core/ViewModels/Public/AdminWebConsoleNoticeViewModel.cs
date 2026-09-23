using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Public;

public sealed partial class AdminWebConsoleNoticeViewModel : MobileViewModelBase
{
    private readonly MobileSessionState _sessionState;
    private readonly ILogoutCoordinator _logoutCoordinator;
    private readonly INavigationService _navigationService;

    public AdminWebConsoleNoticeViewModel(
        MobileSessionState sessionState,
        ILogoutCoordinator logoutCoordinator,
        INavigationService navigationService)
    {
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _logoutCoordinator = logoutCoordinator ?? throw new ArgumentNullException(nameof(logoutCoordinator));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        AdminEmail = _sessionState.Email;
        WebConsoleUrl = MobileEndpointDefaults.AdminWebConsoleUrl;
        NoticeDescription =
            "Platform Admin governance (Chart.js multi-dealer analytics, dealer provisioning, loyalty tier rules, and OEM catalogs) is optimized for the desktop Web Admin Console.";
    }

    [ObservableProperty]
    public partial string AdminEmail { get; set; }

    [ObservableProperty]
    public partial string WebConsoleUrl { get; set; }

    [ObservableProperty]
    public partial string NoticeDescription { get; set; }

    [RelayCommand]
    private Task OpenWebConsoleAsync() =>
        _navigationService.OpenExternalUrlAsync(WebConsoleUrl);

    [RelayCommand]
    private async Task SignOutAndSwitchAccountAsync()
    {
        IsBusy = true;
        try
        {
            await _logoutCoordinator.LogoutAsync(revokeAllDevices: false).ConfigureAwait(false);
            await _navigationService.GoToAsync(MobileRoutes.LoginRegister).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
