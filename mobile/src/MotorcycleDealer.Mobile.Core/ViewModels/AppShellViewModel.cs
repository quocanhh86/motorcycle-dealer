using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
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
    private static readonly HashSet<string> AllowedExternalDeepLinkRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        MobileRoutes.ShowroomCatalog,
        MobileRoutes.DealerLocator,
        MobileRoutes.LoginRegister,
        MobileRoutes.CustomerHome,
        MobileRoutes.VinGarage,
        MobileRoutes.BookingWizard,
        MobileRoutes.LoyaltyPassbook,
        MobileRoutes.CustomerWarranty,
        MobileRoutes.StaffWorkbench,
        MobileRoutes.ShowroomVinSales,
        MobileRoutes.StaffTestDrive,
        MobileRoutes.StaffWarrantyCsat,
        MobileRoutes.SlotCapacity,
        MobileRoutes.NotificationsFeed
    };

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

    private SignalRConnectionStatus _lastConnectionStatus = SignalRConnectionStatus.Disconnected;
    private volatile bool _hasBootstrapped;
    private string? _pendingDeepLinkRoute;

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
        IMobileOfflineDb? offlineDb = null,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
        : base(localizationService, preferencesStore, messenger)
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

        CurrentUserDisplayName = Loc.ShellGuestRider;
        CurrentRoleBadge = Loc.ShellRolePublic;
        CurrentDealerBadge = string.Empty;
        RealtimeStatusLabel = Loc.ShellRealtimeOffline;
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

    protected override void OnCultureChanged(string cultureCode)
    {
        RefreshRoleVisibility();
        ApplyConnectionStatus(_lastConnectionStatus);
    }

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
                _hasBootstrapped = true;
                RefreshRoleVisibility();
                await TryConsumePendingDeepLinkAsync(defaultRouteAlreadyLoaded: true).ConfigureAwait(false);
                return;
            }

            var meResult = await _apiClient.GetMeAsync().ConfigureAwait(false);
            if (meResult.IsSuccess && meResult.Data is not null)
            {
                _sessionState.ApplyAuthenticatedUser(meResult.Data);
                var decision = MobileRoleGuard.EvaluateSession(_sessionState);

                if (decision.Mode == MobileAccessMode.StaffDealerUnassigned)
                {
                    Interlocked.Exchange(ref _pendingDeepLinkRoute, null);
                    _hasBootstrapped = true;
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

                _hasBootstrapped = true;
                await NavigateAfterBootstrapAsync(decision.DefaultRoute).ConfigureAwait(false);
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
                        Interlocked.Exchange(ref _pendingDeepLinkRoute, null);
                        _hasBootstrapped = true;
                        await _logoutCoordinator.ForceSignOutAsync(
                            offlineDecision.BlockingReasonCode ?? "STAFF_DEALER_UNASSIGNED",
                            offlineDecision.BlockingMessage).ConfigureAwait(false);
                        return;
                    }

                    _mainThreadDispatcher.BeginInvokeOnMainThread(() => IsOfflineBannerVisible = true);
                    _hasBootstrapped = true;
                    await NavigateAfterBootstrapAsync(offlineDecision.DefaultRoute).ConfigureAwait(false);
                    return;
                }
            }

            _hasBootstrapped = true;
            RefreshRoleVisibility();
            await TryConsumePendingDeepLinkAsync(defaultRouteAlreadyLoaded: true).ConfigureAwait(false);
        }
        finally
        {
            _hasBootstrapped = true;
            IsBusy = false;
        }
    }

    /// <summary>
    /// Validates and routes OS notification / external deep links strictly against the canonical
    /// <see cref="MobileRoutes"/> allowlist and <see cref="MobileRoleGuard.CanAccessRoute"/>.
    /// Buffers cold-start deep links until <see cref="BootstrapSessionAsync"/> completes so
    /// authenticated role state is settled before navigation.
    /// </summary>
    public async Task<bool> HandleExternalDeepLinkAsync(string? rawRoute)
    {
        if (!TryNormalizeExternalRoute(rawRoute, out var normalizedRoute, out var baseRoute))
        {
            return false;
        }

        if (!_hasBootstrapped)
        {
            Interlocked.Exchange(ref _pendingDeepLinkRoute, normalizedRoute);
            return true;
        }

        if (!MobileRoleGuard.CanAccessRoute(_sessionState, baseRoute))
        {
            return false;
        }

        await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(normalizedRoute))
            .ConfigureAwait(false);
        return true;
    }

    private async Task NavigateAfterBootstrapAsync(string defaultRoute)
    {
        var pending = Interlocked.Exchange(ref _pendingDeepLinkRoute, null);
        if (pending is not null &&
            TryNormalizeExternalRoute(pending, out var normalizedRoute, out var baseRoute) &&
            MobileRoleGuard.CanAccessRoute(_sessionState, baseRoute))
        {
            if (baseRoute.StartsWith("//", StringComparison.Ordinal))
            {
                await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(normalizedRoute))
                    .ConfigureAwait(false);
                return;
            }

            // Pushed/modal routes require the role's Shell tab hierarchy to be active first
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(defaultRoute))
                .ConfigureAwait(false);
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(normalizedRoute))
                .ConfigureAwait(false);
            return;
        }

        await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(defaultRoute))
            .ConfigureAwait(false);
    }

    private async Task TryConsumePendingDeepLinkAsync(bool defaultRouteAlreadyLoaded)
    {
        var pending = Interlocked.Exchange(ref _pendingDeepLinkRoute, null);
        if (pending is null ||
            !TryNormalizeExternalRoute(pending, out var normalizedRoute, out var baseRoute) ||
            !MobileRoleGuard.CanAccessRoute(_sessionState, baseRoute))
        {
            return;
        }

        if (!defaultRouteAlreadyLoaded && !baseRoute.StartsWith("//", StringComparison.Ordinal))
        {
            var decision = MobileRoleGuard.EvaluateSession(_sessionState);
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(decision.DefaultRoute))
                .ConfigureAwait(false);
        }

        await _mainThreadDispatcher.InvokeOnMainThreadAsync(() => _navigationService.GoToAsync(normalizedRoute))
            .ConfigureAwait(false);
    }

    private static bool TryNormalizeExternalRoute(string? rawRoute, out string normalizedRoute, out string baseRoute)
    {
        normalizedRoute = string.Empty;
        baseRoute = string.Empty;
        if (string.IsNullOrWhiteSpace(rawRoute))
        {
            return false;
        }

        var trimmed = rawRoute.Trim();
        if (trimmed.Contains("..", StringComparison.Ordinal) ||
            trimmed.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        var queryIdx = trimmed.IndexOf('?');
        var candidateBase = queryIdx >= 0 ? trimmed[..queryIdx] : trimmed;
        if (!AllowedExternalDeepLinkRoutes.Contains(candidateBase))
        {
            return false;
        }

        normalizedRoute = trimmed;
        baseRoute = candidateBase;
        return true;
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

            CurrentUserDisplayName = _sessionState.IsAuthenticated ? _sessionState.FullName : Loc.ShellGuestRider;
            CurrentRoleBadge = decision.Mode switch
            {
                MobileAccessMode.Customer => Loc.Format("ShellRoleCustomerTier", Loc.GetEnumLabel("LoyaltyTier", _sessionState.TierCode ?? "Bronze")),
                MobileAccessMode.DealerStaff => Loc.ShellRoleDealerStaff,
                MobileAccessMode.AdminWebOnly => Loc.ShellRoleAdminWeb,
                _ => Loc.ShellRolePublic
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

    private void ApplyConnectionStatus(SignalRConnectionStatus status)
    {
        _lastConnectionStatus = status;
        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            IsRealtimeConnected = status == SignalRConnectionStatus.Connected;
            RealtimeStatusLabel = status switch
            {
                SignalRConnectionStatus.Connected => Loc.ShellRealtimeLive,
                SignalRConnectionStatus.Connecting => Loc.ShellRealtimeConnecting,
                SignalRConnectionStatus.Reconnecting => Loc.ShellRealtimeReconnecting,
                _ => Loc.ShellRealtimeOffline
            };
        });
    }

    private void OnSessionChanged(object? sender, EventArgs e) => RefreshRoleVisibility();

    private void OnUnreadCountChanged(object? sender, int count) =>
        _mainThreadDispatcher.BeginInvokeOnMainThread(() => UnreadNotificationCount = count);

    private void OnConnectionStatusChanged(object? sender, SignalRConnectionStatus status) =>
        ApplyConnectionStatus(status);

    private async void OnConnectivityChanged(object? sender, bool isConnected)
    {
        try
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
        catch
        {
            // Never allow unhandled async void exception during connectivity flaps
        }
    }

    private async void OnSessionReset(object? sender, SessionResetEventArgs e)
    {
        try
        {
            Interlocked.Exchange(ref _pendingDeepLinkRoute, null);
            RefreshRoleVisibility();
            _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
            {
                UnreadNotificationCount = 0;
                PendingOutboxCount = 0;
                if (e.WasForced)
                {
                    ErrorMessage = !string.IsNullOrWhiteSpace(e.ReasonCode)
                        ? Loc.GetErrorLabel(e.ReasonCode, e.Message)
                        : (e.Message ?? string.Empty);
                }
            });

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
                _navigationService.GoToAsync(MobileRoutes.LoginRegister)).ConfigureAwait(false);
        }
        catch
        {
            // Never allow unhandled async void exception during forced sign-out routing
        }
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
        UnregisterBaseMessages();
    }
}
