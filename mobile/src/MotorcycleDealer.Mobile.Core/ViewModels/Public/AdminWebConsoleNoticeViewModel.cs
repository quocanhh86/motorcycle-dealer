using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
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
        INavigationService navigationService,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _logoutCoordinator = logoutCoordinator ?? throw new ArgumentNullException(nameof(logoutCoordinator));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        AdminEmail = _sessionState.Email;
        WebConsoleUrl = MobileEndpointDefaults.AdminWebConsoleUrl;
        NoticeDescription = Loc.AdminNoticeDescription;
    }

    [ObservableProperty]
    public partial string AdminEmail { get; set; }

    [ObservableProperty]
    public partial string WebConsoleUrl { get; set; }

    [ObservableProperty]
    public partial string NoticeDescription { get; set; }

    public string SignedInAsLabel => Loc.Format("AdminNoticeSignedInAs", AdminEmail);

    protected override void OnCultureChanged(string cultureCode)
    {
        NoticeDescription = Loc.AdminNoticeDescription;
        OnPropertyChanged(nameof(SignedInAsLabel));
    }

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
