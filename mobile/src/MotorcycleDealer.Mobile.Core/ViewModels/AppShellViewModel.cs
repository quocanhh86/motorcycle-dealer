using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels;

/// <summary>
/// Drives role-adaptive AppShell tabs (Public 3 tabs, Customer 5 tabs, DealerStaff 5 tabs, Admin Web Notice),
/// live unread notification badge, SignalR status indicator, and connectivity-triggered outbox flushing.
/// </summary>
public sealed partial class AppShellViewModel : MobileViewModelBase, IDisposable
{
    private readonly MobileSessionState _sessionState;
    private readonly ISecureTokenStore _tokenStore;
    private readonly IApiEndpointProvider _endpointProvider;
    private readonly IDealerMobileApiClient _apiClient;
    private readonly ILogoutCoordinator _logoutCoordinator;
    private readonly ISignalRMobileService _signalRService;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly IOfflineOutboxSyncService _outboxSyncService;
    private readonly INavigationService _navigationService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMobileOfflineDb? _offlineDb;

    public AppShellViewModel(
        MobileSessionState sessionState,
        ISecureTokenStore tokenStore,
        IApiEndpointProvider endpointProvider,
        IDealerMobileApiClient apiClient,
        ILogoutCoordinator logoutCoordinator,
        ISignalRMobileService signalRService,
        IConnectivityMonitor connectivityMonitor,
        IOfflineOutboxSyncService outboxSyncService,
        INavigationService navigationService,
        IMainThreadDispatcher mainThreadDispatcher,
        IMobileOfflineDb? offlineDb = null)
    {
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logoutCoordinator = logoutCoordinator ?? throw new ArgumentNullException(nameof(logoutCoordinator));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _connectivityMonitor = connectivityMonitor ?? throw new ArgumentNullException(nameof(connectivityMonitor));
        _outboxSyncService = outboxSyncService ?? throw new ArgumentNullException(nameof(outboxSyncService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _offlineDb = offlineDb;

        CurrentUserDisplayName = "Guest Rider";
        CurrentRoleBadge = "Public";
        CurrentDealerBadge = string.Empty;
        RealtimeStatusLabel = "Offline";
        IsPublicVisible = true;

        _sessionState.SessionChanged += OnSessionChanged;
        _signalRService.UnreadCountChanged += OnUnreadCountChanged;
        _signalRService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _connectivityMonitor.ConnectivityChanged += OnConnectivityChanged;
        _logoutCoordinator.SessionReset += OnSessionReset;

        RefreshRoleVisibility();
    }

    [ObservableProperty]
    public partial bool IsPublicVisible { get; set; }

    [ObservableProperty]
    public partial bool IsCustomerVisible { get; set; }

    [ObservableProperty]
    public partial bool IsStaffVisible { get; set; }

    [ObservableProperty]
    public partial bool IsAdminNoticeVisible { get; set; }

    [ObservableProperty]
    public partial string CurrentUserDisplayName { get; set; }

    [ObservableProperty]
    public partial string CurrentRoleBadge { get; set; }

    [ObservableProperty]
    public partial string CurrentDealerBadge { get; set; }

    [ObservableProperty]
    public partial int UnreadNotificationCount { get; set; }

    [ObservableProperty]
    public partial bool IsRealtimeConnected { get; set; }

    [ObservableProperty]
    public partial string RealtimeStatusLabel { get; set; }

    [ObservableProperty]
    public partial int PendingOutboxCount { get; set; }

    public bool HasUnreadNotifications => UnreadNotificationCount > 0;
    public bool HasPendingOutbox => PendingOutboxCount > 0;

    partial void OnUnreadNotificationCountChanged(int value) => OnPropertyChanged(nameof(HasUnreadNotifications));
    partial void OnPendingOutboxCountChanged(int value) => OnPropertyChanged(nameof(HasPendingOutbox));

    [RelayCommand]
    public async Task BootstrapSessionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            await _endpointProvider.InitializeAsync().ConfigureAwait(false);
            var existingToken = await _tokenStore.GetAccessTokenAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(existingToken))
            {
                RefreshRoleVisibility();
                return;
            }

            var meResult = await _apiClient.GetMeAsync().ConfigureAwait(false);
            if (meResult.IsSuccess && meResult.Data is not null)
            {
                _sessionState.ApplyAuthenticatedUser(meResult.Data);
                var decision = MobileRoleGuard.EvaluateSession(_sessionState);

                if (decision.Mode == MobileAccessMode.StaffDealerUnassigned)
                {
                    await _logoutCoordinator.ForceSignOutAsync(
                        decision.BlockingReasonCode ?? "STAFF_DEALER_UNASSIGNED",
                        decision.BlockingMessage).ConfigureAwait(false);
                    return;
                }

                if (_offlineDb is not null)
                {
                    await _offlineDb.SaveLastAuthenticatedUserAsync(meResult.Data).ConfigureAwait(false);
                }

                if (decision.Mode is MobileAccessMode.Customer or MobileAccessMode.DealerStaff)
                {
                    await _signalRService.StartAsync().ConfigureAwait(false);
                    await FlushOutboxIfConnectedAsync().ConfigureAwait(false);
                }

                await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(decision.DefaultRoute))
                    .ConfigureAwait(false);
                return;
            }

            if (meResult.IsNetworkError && !meResult.IsUnauthorized401 && !meResult.IsForbidden403 && _offlineDb is not null)
            {
                var cachedUser = await _offlineDb.GetLastAuthenticatedUserAsync().ConfigureAwait(false);
                if (cachedUser is not null)
                {
                    _sessionState.ApplyAuthenticatedUser(cachedUser);
                    var offlineDecision = MobileRoleGuard.EvaluateSession(_sessionState);

                    if (offlineDecision.Mode == MobileAccessMode.StaffDealerUnassigned)
                    {
                        await _logoutCoordinator.ForceSignOutAsync(
                            offlineDecision.BlockingReasonCode ?? "STAFF_DEALER_UNASSIGNED",
                            offlineDecision.BlockingMessage).ConfigureAwait(false);
                        return;
                    }

                    _mainThreadDispatcher.BeginInvokeOnMainThread(() => IsOfflineBannerVisible = true);
                    await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(offlineDecision.DefaultRoute))
                        .ConfigureAwait(false);
                    return;
                }
            }

            RefreshRoleVisibility();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenNotificationsAsync() => _navigationService.GoToAsync(MobileRoutes.NotificationsFeed);

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _logoutCoordinator.LogoutAsync(revokeAllDevices: false).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SignOutAllDevicesAsync()
    {
        await _logoutCoordinator.LogoutAsync(revokeAllDevices: true).ConfigureAwait(false);
    }

    private void RefreshRoleVisibility()
    {
        var decision = MobileRoleGuard.EvaluateSession(_sessionState);

        void Apply()
        {
            IsPublicVisible = decision.Mode == MobileAccessMode.Public || decision.Mode == MobileAccessMode.StaffDealerUnassigned;
            IsCustomerVisible = decision.Mode == MobileAccessMode.Customer;
            IsStaffVisible = decision.Mode == MobileAccessMode.DealerStaff;
            IsAdminNoticeVisible = decision.Mode == MobileAccessMode.AdminWebOnly;

            CurrentUserDisplayName = _sessionState.IsAuthenticated ? _sessionState.FullName : "Guest Rider";
            CurrentRoleBadge = decision.Mode switch
            {
                MobileAccessMode.Customer => $"{_sessionState.TierCode ?? "Bronze"} Rider",
                MobileAccessMode.DealerStaff => "Dealer Staff",
                MobileAccessMode.AdminWebOnly => "Admin (Web Console)",
                _ => "Public Showroom"
            };
            CurrentDealerBadge = _sessionState.DealerCode ?? string.Empty;
            IsOfflineBannerVisible = !_connectivityMonitor.IsConnected;
        }

        if (_mainThreadDispatcher.IsMainThread)
        {
            Apply();
        }
        else
        {
            _mainThreadDispatcher.BeginInvokeOnMainThread(Apply);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e) => RefreshRoleVisibility();

    private void OnUnreadCountChanged(object? sender, int count) =>
        _mainThreadDispatcher.BeginInvokeOnMainThread(() => UnreadNotificationCount = count);

    private void OnConnectionStatusChanged(object? sender, SignalRConnectionStatus status) =>
        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            IsRealtimeConnected = status == SignalRConnectionStatus.Connected;
            RealtimeStatusLabel = status switch
            {
                SignalRConnectionStatus.Connected => "Live",
                SignalRConnectionStatus.Connecting => "Connecting…",
                SignalRConnectionStatus.Reconnecting => "Reconnecting…",
                _ => "Offline"
            };
        });

    private async void OnConnectivityChanged(object? sender, bool isConnected)
    {
        _mainThreadDispatcher.BeginInvokeOnMainThread(() => IsOfflineBannerVisible = !isConnected);
        if (isConnected)
        {
            await FlushOutboxIfConnectedAsync().ConfigureAwait(false);
            if (_sessionState.IsAuthenticated && !_sessionState.IsAdminWebOnly)
            {
                await _signalRService.OnAppResumedAsync().ConfigureAwait(false);
            }
        }
    }

    private async void OnSessionReset(object? sender, SessionResetEventArgs e)
    {
        RefreshRoleVisibility();
        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            UnreadNotificationCount = 0;
            PendingOutboxCount = 0;
            if (e.WasForced && !string.IsNullOrWhiteSpace(e.Message))
            {
                ErrorMessage = e.Message;
            }
        });

        await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            _navigationService.GoToAsync(MobileRoutes.LoginRegister)).ConfigureAwait(false);
    }

    private async Task FlushOutboxIfConnectedAsync()
    {
        if (!_sessionState.IsAuthenticated || _sessionState.IsAdminWebOnly)
        {
            return;
        }

        var summary = await _outboxSyncService.FlushAsync(_sessionState.OwnerScopeKey).ConfigureAwait(false);
        _mainThreadDispatcher.BeginInvokeOnMainThread(() => PendingOutboxCount = summary.RemainingCount);
    }

    public void Dispose()
    {
        _sessionState.SessionChanged -= OnSessionChanged;
        _signalRService.UnreadCountChanged -= OnUnreadCountChanged;
        _signalRService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _connectivityMonitor.ConnectivityChanged -= OnConnectivityChanged;
        _logoutCoordinator.SessionReset -= OnSessionReset;
    }
}
