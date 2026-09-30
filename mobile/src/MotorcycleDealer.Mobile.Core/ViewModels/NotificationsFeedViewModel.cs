using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels;

public sealed partial class NotificationsFeedViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly MobileSessionState _sessionState;
    private readonly ISignalRMobileService _signalRService;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly IOfflineOutboxSyncService _outboxSyncService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IUserActionLogger? _userActionLogger;

    private readonly Lock _eventSubscriptionLock = new();
    private int? _lastServerTotalBufferedCount;
    private bool _eventsAttached;
    private IUserActionLogger? _subscribedLogger;

    public NotificationsFeedViewModel(
        IDealerMobileApiClient apiClient,
        MobileSessionState sessionState,
        ISignalRMobileService signalRService,
        IConnectivityMonitor connectivityMonitor,
        IOfflineOutboxSyncService outboxSyncService,
        IMainThreadDispatcher mainThreadDispatcher,
        IUserActionLogger? userActionLogger = null,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _connectivityMonitor = connectivityMonitor ?? throw new ArgumentNullException(nameof(connectivityMonitor));
        _outboxSyncService = outboxSyncService ?? throw new ArgumentNullException(nameof(outboxSyncService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _userActionLogger = userActionLogger;

        CanFilterDealerFeed = _sessionState.HasPermission(Permissions.Notifications.ViewDealer);
        ActiveSection = "Alerts";
        ActivityLogSummary = Loc.NotifDefaultLogSummary;
    }

    public ObservableCollection<NotificationDto> Notifications { get; } = [];
    public ObservableCollection<UserActionLogDto> ActivityLogs { get; } = [];

    [ObservableProperty]
    public partial bool UnreadOnly { get; set; }

    [ObservableProperty]
    public partial bool DealerFeedOnly { get; set; }

    [ObservableProperty]
    public partial bool CanFilterDealerFeed { get; set; }

    [ObservableProperty]
    public partial int UnreadCount { get; set; }

    [ObservableProperty]
    public partial string ActiveSection { get; set; }

    [ObservableProperty]
    public partial bool ShowServerLogs { get; set; }

    [ObservableProperty]
    public partial string ActivityLogSummary { get; set; }

    public bool IsAlertsSection => string.Equals(ActiveSection, "Alerts", StringComparison.OrdinalIgnoreCase);
    public bool IsLogsSection => string.Equals(ActiveSection, "Logs", StringComparison.OrdinalIgnoreCase);
    public string LogSourceToggleLabel => ShowServerLogs ? Loc.NotifSourceServerMobile : Loc.NotifSourceLocalDevice;
    public string UnreadHeaderLabel => Loc.Format("NotifUnreadAlertsFormat", UnreadCount);

    partial void OnActiveSectionChanged(string value)
    {
        OnPropertyChanged(nameof(IsAlertsSection));
        OnPropertyChanged(nameof(IsLogsSection));
    }

    partial void OnShowServerLogsChanged(bool value) => OnPropertyChanged(nameof(LogSourceToggleLabel));
    partial void OnUnreadCountChanged(int value) => OnPropertyChanged(nameof(UnreadHeaderLabel));

    protected override void OnCultureChanged(string cultureCode)
    {
        OnPropertyChanged(nameof(LogSourceToggleLabel));
        OnPropertyChanged(nameof(UnreadHeaderLabel));
        RefreshActivitySummary();
        RefreshCollection(Notifications);
        RefreshCollection(ActivityLogs);
    }

    /// <summary>
    /// Idempotently attaches singleton SignalR and action-logger event subscriptions while the page is visible.
    /// Pair with <see cref="DetachEventSubscriptions"/> on disappearing/unloaded to prevent RISK-04 transient VM leaks.
    /// </summary>
    public void AttachEventSubscriptions()
    {
        lock (_eventSubscriptionLock)
        {
            if (_eventsAttached)
            {
                return;
            }

            _signalRService.MissedNotificationsRecovered += OnMissedNotificationsRecovered;
            var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
            if (activeLogger is not null)
            {
                activeLogger.ActionLogged += OnActionLogged;
                _subscribedLogger = activeLogger;
            }

            _eventsAttached = true;
        }
    }

    /// <summary>
    /// Idempotently detaches singleton SignalR and action-logger event subscriptions so transient ViewModels can be GC'd.
    /// </summary>
    public void DetachEventSubscriptions()
    {
        lock (_eventSubscriptionLock)
        {
            if (!_eventsAttached)
            {
                return;
            }

            _signalRService.MissedNotificationsRecovered -= OnMissedNotificationsRecovered;
            if (_subscribedLogger is not null)
            {
                _subscribedLogger.ActionLogged -= OnActionLogged;
                _subscribedLogger = null;
            }

            _eventsAttached = false;
        }
    }

    [RelayCommand]
    private async Task SelectSectionAsync(string section)
    {
        var target = string.Equals(section, "Logs", StringComparison.OrdinalIgnoreCase) ? "Logs" : "Alerts";
        if (string.Equals(ActiveSection, target, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ActiveSection = target;
        TrackUserAction("Notifications.SwitchSection", "UserInteraction", target);
        if (IsLogsSection)
        {
            await LoadActivityLogsAsync().ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task ToggleLogSourceAsync()
    {
        ShowServerLogs = !ShowServerLogs;
        TrackUserAction("ActivityLogs.ToggleSource", "UserInteraction", ShowServerLogs ? "ServerCorrelated" : "LocalDevice");
        await LoadActivityLogsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task LoadActivityLogsAsync()
    {
        ClearMessages();
        var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;

        if (!ShowServerLogs || !_sessionState.IsAuthenticated)
        {
            var local = activeLogger?.GetRecentLogs(80) ?? [];
            _lastServerTotalBufferedCount = null;
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                ActivityLogs.Clear();
                foreach (var item in local)
                {
                    ActivityLogs.Add(item);
                }

                ActivityLogSummary = Loc.Format(
                    "NotifLocalLogsSummaryFormat",
                    local.Count,
                    activeLogger?.ClientPlatform ?? "Mobile",
                    activeLogger?.SessionId ?? "-");
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            return;
        }

        IsBusy = true;
        try
        {
            if (activeLogger is not null)
            {
                await activeLogger.FlushPendingAsync().ConfigureAwait(false);
            }

            var result = await _apiClient.GetMyActivityLogsAsync(take: 80).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                return;
            }

            _lastServerTotalBufferedCount = result.Data.TotalBufferedCount;
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                ActivityLogs.Clear();
                foreach (var item in result.Data.Items)
                {
                    ActivityLogs.Add(item);
                }

                ActivityLogSummary = Loc.Format("NotifServerLogsSummaryFormat", result.Data.TotalBufferedCount);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task SyncAndRefreshLogsAsync()
    {
        var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
        if (activeLogger is not null)
        {
            await activeLogger.FlushPendingAsync().ConfigureAwait(false);
        }

        await LoadActivityLogsAsync().ConfigureAwait(false);
        SuccessMessage = Loc.NotifLogsSyncedSuccess;
    }

    [RelayCommand]
    public async Task LoadNotificationsAsync()
    {
        if (IsLogsSection)
        {
            await LoadActivityLogsAsync().ConfigureAwait(false);
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        CanFilterDealerFeed = _sessionState.HasPermission(Permissions.Notifications.ViewDealer);

        try
        {
            var result = await _apiClient.ListNotificationsAsync(
                pageNumber: 1,
                pageSize: 40,
                unreadOnly: UnreadOnly,
                dealerFeedOnly: DealerFeedOnly && CanFilterDealerFeed).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                IsOfflineBannerVisible = result.IsNetworkError;
                return;
            }

            IsOfflineBannerVisible = false;
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                Notifications.Clear();
                foreach (var item in result.Data)
                {
                    Notifications.Add(item);
                }

                UnreadCount = result.UnreadCount ?? result.Data.Count(n => !n.IsRead);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task MarkReadAsync(NotificationDto? item)
    {
        if (item is null || item.IsRead)
        {
            return;
        }

        ClearMessages();
        TrackUserAction("Notifications.MarkRead", "Notification", item.EventType);
        if (!_connectivityMonitor.IsConnected)
        {
            await _outboxSyncService.EnqueueNotificationReadAsync(_sessionState.OwnerScopeKey, item.Id).ConfigureAwait(false);
            ApplyLocalReadState(item.Id);
            SuccessMessage = Loc.NotifMarkedReadLocal;
            return;
        }

        var result = await _apiClient.MarkNotificationReadAsync(item.Id).ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
        {
            ApplyLocalReadState(item.Id);
            UnreadCount = result.Data.UnreadCount;
        }
        else if (result.IsNetworkError)
        {
            await _outboxSyncService.EnqueueNotificationReadAsync(_sessionState.OwnerScopeKey, item.Id).ConfigureAwait(false);
            ApplyLocalReadState(item.Id);
            SuccessMessage = Loc.NotifMarkedReadQueued;
        }
        else
        {
            ErrorMessage = FormatApiError(result);
        }
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        ClearMessages();
        TrackUserAction("Notifications.MarkAllRead", "Notification", DealerFeedOnly ? "DealerFeed" : "PersonalFeed");
        var dealerScope = DealerFeedOnly ? _sessionState.DealerId : null;

        if (!_connectivityMonitor.IsConnected)
        {
            await _outboxSyncService.EnqueueNotificationReadAllAsync(_sessionState.OwnerScopeKey, dealerScope).ConfigureAwait(false);
            MarkAllLocally();
            SuccessMessage = Loc.NotifAllMarkedReadLocal;
            return;
        }

        var result = await _apiClient.MarkAllNotificationsReadAsync(dealerScope).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            MarkAllLocally();
            UnreadCount = result.Data?.UnreadCount ?? 0;
            SuccessMessage = Loc.NotifAllMarkedReadSuccess;
        }
        else if (result.IsNetworkError)
        {
            await _outboxSyncService.EnqueueNotificationReadAllAsync(_sessionState.OwnerScopeKey, dealerScope).ConfigureAwait(false);
            MarkAllLocally();
            SuccessMessage = Loc.NotifAllMarkedReadQueued;
        }
        else
        {
            ErrorMessage = FormatApiError(result);
        }
    }

    private void RefreshActivitySummary()
    {
        if (_lastServerTotalBufferedCount.HasValue && ShowServerLogs)
        {
            ActivityLogSummary = Loc.Format("NotifServerLogsSummaryFormat", _lastServerTotalBufferedCount.Value);
        }
        else if (ActivityLogs.Count > 0)
        {
            var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
            ActivityLogSummary = Loc.Format(
                "NotifLocalLogsSummaryFormat",
                ActivityLogs.Count,
                activeLogger?.ClientPlatform ?? "Mobile",
                activeLogger?.SessionId ?? "-");
        }
        else
        {
            ActivityLogSummary = Loc.NotifDefaultLogSummary;
        }
    }

    private void ApplyLocalReadState(Guid notificationId)
    {
        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            for (var i = 0; i < Notifications.Count; i++)
            {
                if (Notifications[i].Id == notificationId)
                {
                    Notifications[i] = Notifications[i] with { IsRead = true, ReadAtUtc = DateTimeOffset.UtcNow };
                    UnreadCount = Math.Max(0, UnreadCount - 1);
                    break;
                }
            }
        });
    }

    private void MarkAllLocally()
    {
        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < Notifications.Count; i++)
            {
                if (!Notifications[i].IsRead)
                {
                    Notifications[i] = Notifications[i] with { IsRead = true, ReadAtUtc = now };
                }
            }

            UnreadCount = 0;
        });
    }

    private void OnMissedNotificationsRecovered(object? sender, IReadOnlyList<NotificationDto> recovered)
    {
        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            var existingIds = Notifications.Select(n => n.Id).ToHashSet();
            foreach (var item in recovered.OrderBy(n => n.OccurredAtUtc))
            {
                if (existingIds.Add(item.Id))
                {
                    Notifications.Insert(0, item);
                }
            }
        });
    }

    private void OnActionLogged(object? sender, UserActionLogDto entry)
    {
        if (!IsLogsSection || ShowServerLogs)
        {
            return;
        }

        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            ActivityLogs.Insert(0, entry);
            while (ActivityLogs.Count > 80)
            {
                ActivityLogs.RemoveAt(ActivityLogs.Count - 1);
            }
        });
    }

    public void Dispose()
    {
        DetachEventSubscriptions();
        UnregisterBaseMessages();
    }
}
