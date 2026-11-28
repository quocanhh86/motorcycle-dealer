using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.Realtime;

public enum SignalRConnectionStatus
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3
}

public sealed class SignalRReconnectPolicy : IRetryPolicy
{
    public static IReadOnlyList<TimeSpan> DefaultDelays { get; } =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    ];

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        ArgumentNullException.ThrowIfNull(retryContext);
        var idx = Math.Min((int)retryContext.PreviousRetryCount, DefaultDelays.Count - 1);
        return DefaultDelays[Math.Max(0, idx)];
    }
}

public interface ISignalRMobileService : IAsyncDisposable
{
    SignalRConnectionStatus ConnectionStatus { get; }
    DateTimeOffset? LastEventWatermarkUtc { get; }
    int UnreadCount { get; }

    event EventHandler<SignalRConnectionStatus>? ConnectionStatusChanged;
    event EventHandler<int>? UnreadCountChanged;
    event EventHandler<IReadOnlyList<NotificationDto>>? MissedNotificationsRecovered;

    event EventHandler<BookingCreatedRealtimeEvent>? BookingCreatedReceived;
    event EventHandler<BookingStatusChangedRealtimeEvent>? BookingStatusChangedReceived;
    event EventHandler<MaintenanceReminderRealtimeEvent>? MaintenanceReminderReceived;
    event EventHandler<LoyaltyPointsUpdatedRealtimeEvent>? LoyaltyPointsUpdatedReceived;
    event EventHandler<TierUpgradedRealtimeEvent>? TierUpgradedReceived;
    event EventHandler<TestDriveCreatedRealtimeEvent>? TestDriveCreatedReceived;
    event EventHandler<TestDriveStatusChangedRealtimeEvent>? TestDriveStatusChangedReceived;
    event EventHandler<ServiceReviewSubmittedRealtimeEvent>? ServiceReviewSubmittedReceived;
    event EventHandler<WarrantyClaimCreatedRealtimeEvent>? WarrantyClaimCreatedReceived;
    event EventHandler<WarrantyClaimStatusChangedRealtimeEvent>? WarrantyClaimStatusChangedReceived;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task OnAppBackgroundedAsync(CancellationToken cancellationToken = default);
    Task OnAppResumedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> SyncMissedNotificationsSinceWatermarkAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages the /hubs/notifications SignalR HubConnection with dynamic JWT AccessTokenProvider,
/// [0s, 2s, 5s, 10s, 30s] reconnect backoff, all 10 canonical event subscriptions, native Android
/// local notification alerts, foreground/background lifecycle suspension, and sinceUtc watermark recovery.
/// </summary>
public sealed class SignalRMobileService : ISignalRMobileService
{
    private const string NotificationWatermarkResource = "notifications_watermark";

    private readonly IApiEndpointProvider _endpointProvider;
    private readonly ISecureTokenStore _tokenStore;
    private readonly MobileSessionState _sessionState;
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly ILocalNotificationService _localNotificationService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IUserActionLogger? _userActionLogger;
    private readonly LocalizationService? _localizationService;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private HubConnection? _hubConnection;
    private SignalRConnectionStatus _connectionStatus = SignalRConnectionStatus.Disconnected;
    private DateTimeOffset? _lastEventWatermarkUtc;
    private int _unreadCount;
    private bool _wasPausedForBackground;

    public SignalRMobileService(
        IApiEndpointProvider endpointProvider,
        ISecureTokenStore tokenStore,
        MobileSessionState sessionState,
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        ILocalNotificationService localNotificationService,
        IMainThreadDispatcher mainThreadDispatcher,
        ILogoutCoordinator? logoutCoordinator = null,
        IUserActionLogger? userActionLogger = null,
        LocalizationService? localizationService = null)
    {
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _localNotificationService = localNotificationService ?? throw new ArgumentNullException(nameof(localNotificationService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _userActionLogger = userActionLogger;
        _localizationService = localizationService;

        logoutCoordinator?.RegisterRealtimeStopHook(StopAsync);
    }

    private LocalizationService Loc => _localizationService ?? LocalizationService.Current;

    public SignalRConnectionStatus ConnectionStatus => _connectionStatus;
    public DateTimeOffset? LastEventWatermarkUtc => _lastEventWatermarkUtc;
    public int UnreadCount => _unreadCount;

    public event EventHandler<SignalRConnectionStatus>? ConnectionStatusChanged;
    public event EventHandler<int>? UnreadCountChanged;
    public event EventHandler<IReadOnlyList<NotificationDto>>? MissedNotificationsRecovered;

    public event EventHandler<BookingCreatedRealtimeEvent>? BookingCreatedReceived;
    public event EventHandler<BookingStatusChangedRealtimeEvent>? BookingStatusChangedReceived;
    public event EventHandler<MaintenanceReminderRealtimeEvent>? MaintenanceReminderReceived;
    public event EventHandler<LoyaltyPointsUpdatedRealtimeEvent>? LoyaltyPointsUpdatedReceived;
    public event EventHandler<TierUpgradedRealtimeEvent>? TierUpgradedReceived;
    public event EventHandler<TestDriveCreatedRealtimeEvent>? TestDriveCreatedReceived;
    public event EventHandler<TestDriveStatusChangedRealtimeEvent>? TestDriveStatusChangedReceived;
    public event EventHandler<ServiceReviewSubmittedRealtimeEvent>? ServiceReviewSubmittedReceived;
    public event EventHandler<WarrantyClaimCreatedRealtimeEvent>? WarrantyClaimCreatedReceived;
    public event EventHandler<WarrantyClaimStatusChangedRealtimeEvent>? WarrantyClaimStatusChangedReceived;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionState.IsAuthenticated || _sessionState.IsAdminWebOnly)
        {
            return;
        }

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _wasPausedForBackground = false;

            if (_lastEventWatermarkUtc is null)
            {
                var saved = await _offlineDb.GetSyncStateAsync(
                    _sessionState.OwnerScopeKey,
                    NotificationWatermarkResource,
                    cancellationToken).ConfigureAwait(false);
                _lastEventWatermarkUtc = saved?.LastSyncedUtc;
            }

            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                SetStatus(SignalRConnectionStatus.Connected);
                return;
            }

            await DisposeConnectionInternalAsync().ConfigureAwait(false);

            SetStatus(SignalRConnectionStatus.Connecting);

            var hubUri = _endpointProvider.BuildNotificationHubUri();
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUri, options =>
                {
                    options.AccessTokenProvider = async () =>
                        await _tokenStore.GetAccessTokenAsync(CancellationToken.None).ConfigureAwait(false);
                    var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
                    if (activeLogger is not null)
                    {
                        options.Headers[MobileUserActionHeaders.ClientPlatform] = activeLogger.ClientPlatform;
                        options.Headers[MobileUserActionHeaders.ClientSessionId] = activeLogger.SessionId;
                    }
                    options.Headers["Accept-Language"] = Loc.AcceptLanguageHeaderValue;
                })
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, MobileJsonSerializerContext.Default);
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .WithAutomaticReconnect(new SignalRReconnectPolicy())
                .Build();

            WireHubHandlers(connection);

            connection.Reconnecting += _ =>
            {
                SetStatus(SignalRConnectionStatus.Reconnecting);
                return Task.CompletedTask;
            };

            connection.Reconnected += async _ =>
            {
                SetStatus(SignalRConnectionStatus.Connected);
                await SyncMissedNotificationsSinceWatermarkAsync(CancellationToken.None).ConfigureAwait(false);
            };

            connection.Closed += _ =>
            {
                SetStatus(SignalRConnectionStatus.Disconnected);
                return Task.CompletedTask;
            };

            _hubConnection = connection;

            try
            {
                await connection.StartAsync(cancellationToken).ConfigureAwait(false);
                SetStatus(SignalRConnectionStatus.Connected);
                await SyncMissedNotificationsSinceWatermarkAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                SetStatus(SignalRConnectionStatus.Disconnected);
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _wasPausedForBackground = false;
            _lastEventWatermarkUtc = null;
            await DisposeConnectionInternalAsync().ConfigureAwait(false);
            UpdateUnreadCount(0);
            SetStatus(SignalRConnectionStatus.Disconnected);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task OnAppBackgroundedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sessionState.IsAuthenticated && !_sessionState.IsAdminWebOnly)
            {
                _wasPausedForBackground = true;
                var watermark = _lastEventWatermarkUtc ?? DateTimeOffset.UtcNow;
                await RecordWatermarkAsync(watermark, cancellationToken).ConfigureAwait(false);
            }

            await DisposeConnectionInternalAsync().ConfigureAwait(false);
            SetStatus(SignalRConnectionStatus.Disconnected);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task OnAppResumedAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionState.IsAuthenticated || _sessionState.IsAdminWebOnly)
        {
            return;
        }

        if (_wasPausedForBackground || _connectionStatus != SignalRConnectionStatus.Connected)
        {
            await StartAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await SyncMissedNotificationsSinceWatermarkAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<NotificationDto>> SyncMissedNotificationsSinceWatermarkAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_sessionState.IsAuthenticated || _sessionState.IsAdminWebOnly)
        {
            return [];
        }

        var watermark = _lastEventWatermarkUtc;
        if (watermark is null)
        {
            var saved = await _offlineDb.GetSyncStateAsync(
                _sessionState.OwnerScopeKey,
                NotificationWatermarkResource,
                cancellationToken).ConfigureAwait(false);
            watermark = saved?.LastSyncedUtc;
            _lastEventWatermarkUtc = watermark;
        }

        var result = await _apiClient.ListNotificationsAsync(
            pageNumber: 1,
            pageSize: 50,
            unreadOnly: false,
            sinceUtc: watermark,
            dealerId: null,
            dealerFeedOnly: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            return [];
        }

        if (result.UnreadCount.HasValue)
        {
            UpdateUnreadCount(result.UnreadCount.Value);
        }

        var latestTimestamp = result.Data.Count > 0
            ? result.Data.Max(n => n.OccurredAtUtc)
            : DateTimeOffset.UtcNow;
        await RecordWatermarkAsync(latestTimestamp, cancellationToken).ConfigureAwait(false);

        if (result.Data.Count > 0)
        {
            DispatchToMainThread(() => MissedNotificationsRecovered?.Invoke(this, result.Data));
        }

        return result.Data;
    }

    public async Task HandleIncomingBookingCreatedAsync(BookingCreatedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        var serviceLabel = Loc.GetEnumLabel("ServiceType", payload.ServiceType);
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.BookingAlerts,
            Title: Loc.Format("SignalR_BookingCreated_Title", payload.BookingNumber),
            Body: Loc.Format("SignalR_BookingCreated_Body", payload.CustomerFullName, payload.MotorcycleModelName, serviceLabel, VietnamFormatting.FormatSlotDate(payload.SlotDate)),
            EventType: SignalRHubEventNames.BookingCreated,
            TargetRoute: MobileRoutes.StaffWorkbench,
            PayloadId: payload.BookingId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => BookingCreatedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingBookingStatusChangedAsync(BookingStatusChangedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        var prevLabel = Loc.GetEnumLabel("BookingStatus", payload.PreviousStatus);
        var nextLabel = Loc.GetEnumLabel("BookingStatus", payload.NewStatus);
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.BookingAlerts,
            Title: Loc.Format("SignalR_BookingStatusChanged_Title", payload.BookingNumber, nextLabel),
            Body: Loc.Format("SignalR_BookingStatusChanged_Body", payload.DealerName, prevLabel, nextLabel),
            EventType: SignalRHubEventNames.BookingStatusChanged,
            TargetRoute: _sessionState.IsDealerStaff ? MobileRoutes.StaffWorkbench : MobileRoutes.BookingWizard,
            PayloadId: payload.BookingId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => BookingStatusChangedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingMaintenanceReminderAsync(MaintenanceReminderRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.BookingAlerts,
            Title: Loc.Format("SignalR_MaintenanceReminder_Title", payload.MilestoneName),
            Body: Loc.Format("SignalR_MaintenanceReminder_Body", payload.MotorcycleModelName, payload.Vin, VietnamFormatting.FormatMileageKm(payload.CurrentMileageKm), VietnamFormatting.FormatMileageKm(payload.TargetMileageKm)),
            EventType: SignalRHubEventNames.MaintenanceReminderTriggered,
            TargetRoute: MobileRoutes.VinGarage,
            PayloadId: payload.CustomerVehicleId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => MaintenanceReminderReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingLoyaltyPointsUpdatedAsync(LoyaltyPointsUpdatedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        _sessionState.UpdateLoyaltySnapshot(payload.AvailablePointsAfter, payload.LifetimeQualifyingPointsAfter);

        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.LoyaltyRewards,
            Title: Loc.Format("SignalR_LoyaltyPointsUpdated_Title", VietnamFormatting.FormatPointsLocalized(payload.PointsDelta, signed: true, Loc.CurrentCulture)),
            Body: Loc.Format("SignalR_LoyaltyPointsUpdated_Body", payload.ReferenceNumber, VietnamFormatting.FormatPointsLocalized(payload.AvailablePointsAfter, signed: false, Loc.CurrentCulture)),
            EventType: SignalRHubEventNames.LoyaltyPointsUpdated,
            TargetRoute: MobileRoutes.LoyaltyPassbook,
            PayloadId: payload.LoyaltyAccountId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => LoyaltyPointsUpdatedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingTierUpgradedAsync(TierUpgradedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        _sessionState.UpdateLoyaltySnapshot(
            _sessionState.AvailablePoints ?? 0,
            payload.LifetimeQualifyingPoints,
            payload.NewTierCode,
            payload.NewPointsMultiplier);

        var prevTier = Loc.GetEnumLabel("LoyaltyTier", payload.PreviousTierCode);
        var nextTier = Loc.GetEnumLabel("LoyaltyTier", payload.NewTierCode);
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.LoyaltyRewards,
            Title: Loc.Format("SignalR_TierUpgraded_Title", nextTier),
            Body: Loc.Format("SignalR_TierUpgraded_Body", payload.NewPointsMultiplier, prevTier, nextTier),
            EventType: SignalRHubEventNames.TierUpgraded,
            TargetRoute: MobileRoutes.LoyaltyPassbook,
            PayloadId: payload.LoyaltyAccountId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => TierUpgradedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingTestDriveCreatedAsync(TestDriveCreatedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.BookingAlerts,
            Title: Loc.Format("SignalR_TestDriveCreated_Title", payload.BookingNumber),
            Body: Loc.Format("SignalR_TestDriveCreated_Body", payload.CustomerFullName, payload.MotorcycleModelName, VietnamFormatting.FormatSlotDate(payload.PreferredDate)),
            EventType: SignalRHubEventNames.TestDriveCreated,
            TargetRoute: MobileRoutes.StaffTestDrive,
            PayloadId: payload.TestDriveId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => TestDriveCreatedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingTestDriveStatusChangedAsync(TestDriveStatusChangedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        var nextLabel = Loc.GetEnumLabel("TestDriveStatus", payload.NewStatus);
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.BookingAlerts,
            Title: Loc.Format("SignalR_TestDriveStatusChanged_Title", payload.BookingNumber, nextLabel),
            Body: Loc.Format("SignalR_TestDriveStatusChanged_Body", payload.DealerName, payload.MotorcycleModelName, nextLabel),
            EventType: SignalRHubEventNames.TestDriveStatusChanged,
            TargetRoute: _sessionState.IsDealerStaff ? MobileRoutes.StaffTestDrive : MobileRoutes.CustomerHome,
            PayloadId: payload.TestDriveId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => TestDriveStatusChangedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingServiceReviewSubmittedAsync(ServiceReviewSubmittedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.LoyaltyRewards,
            Title: Loc.Format("SignalR_ServiceReviewSubmitted_Title", payload.Rating, payload.ReviewNumber),
            Body: Loc.Format("SignalR_ServiceReviewSubmitted_Body", payload.CustomerFullName, payload.InvoiceNumber, payload.MotorcycleModelName),
            EventType: SignalRHubEventNames.ServiceReviewSubmitted,
            TargetRoute: MobileRoutes.StaffWarrantyCsat,
            PayloadId: payload.ReviewId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => ServiceReviewSubmittedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingWarrantyClaimCreatedAsync(WarrantyClaimCreatedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        var defectLabel = Loc.GetEnumLabel("WarrantyDefect", payload.DefectCategory);
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.WarrantyUpdates,
            Title: Loc.Format("SignalR_WarrantyClaimCreated_Title", payload.ClaimNumber),
            Body: Loc.Format("SignalR_WarrantyClaimCreated_Body", payload.CustomerFullName, defectLabel, payload.Vin, VietnamFormatting.FormatMileageKm(payload.OdometerAtClaimKm)),
            EventType: SignalRHubEventNames.WarrantyClaimCreated,
            TargetRoute: MobileRoutes.StaffWarrantyCsat,
            PayloadId: payload.ClaimId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => WarrantyClaimCreatedReceived?.Invoke(this, payload));
    }

    public async Task HandleIncomingWarrantyClaimStatusChangedAsync(WarrantyClaimStatusChangedRealtimeEvent payload)
    {
        await RecordWatermarkAsync(payload.OccurredAtUtc, CancellationToken.None).ConfigureAwait(false);
        IncrementUnreadCount();
        var nextLabel = Loc.GetEnumLabel("WarrantyStatus", payload.NewStatus);
        await _localNotificationService.ShowAsync(new LocalNotificationRequest(
            ChannelId: NotificationChannels.WarrantyUpdates,
            Title: Loc.Format("SignalR_WarrantyClaimStatusChanged_Title", payload.ClaimNumber, nextLabel),
            Body: Loc.Format("SignalR_WarrantyClaimStatusChanged_Body", payload.DealerName, payload.Vin, nextLabel, VietnamFormatting.FormatVnd(payload.CoveredAmountVnd)),
            EventType: SignalRHubEventNames.WarrantyClaimStatusChanged,
            TargetRoute: _sessionState.IsDealerStaff ? MobileRoutes.StaffWarrantyCsat : MobileRoutes.CustomerWarranty,
            PayloadId: payload.ClaimId.ToString("D"))).ConfigureAwait(false);
        DispatchToMainThread(() => WarrantyClaimStatusChangedReceived?.Invoke(this, payload));
    }

    public void UpdateUnreadCount(int unreadCount)
    {
        var safeCount = Math.Max(0, unreadCount);
        if (_unreadCount == safeCount)
        {
            return;
        }

        _unreadCount = safeCount;
        DispatchToMainThread(() => UnreadCountChanged?.Invoke(this, safeCount));
    }

    private void IncrementUnreadCount() => UpdateUnreadCount(_unreadCount + 1);

    private void WireHubHandlers(HubConnection connection)
    {
        connection.On<BookingCreatedRealtimeEvent>(SignalRHubEventNames.BookingCreated, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.BookingCreated, p.BookingNumber);
            await HandleIncomingBookingCreatedAsync(p).ConfigureAwait(false);
        });
        connection.On<BookingStatusChangedRealtimeEvent>(SignalRHubEventNames.BookingStatusChanged, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.BookingStatusChanged, $"{p.BookingNumber}:{p.NewStatus}");
            await HandleIncomingBookingStatusChangedAsync(p).ConfigureAwait(false);
        });
        connection.On<MaintenanceReminderRealtimeEvent>(SignalRHubEventNames.MaintenanceReminderTriggered, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.MaintenanceReminderTriggered, p.Vin);
            await HandleIncomingMaintenanceReminderAsync(p).ConfigureAwait(false);
        });
        connection.On<LoyaltyPointsUpdatedRealtimeEvent>(SignalRHubEventNames.LoyaltyPointsUpdated, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.LoyaltyPointsUpdated, p.ReferenceNumber);
            await HandleIncomingLoyaltyPointsUpdatedAsync(p).ConfigureAwait(false);
        });
        connection.On<TierUpgradedRealtimeEvent>(SignalRHubEventNames.TierUpgraded, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.TierUpgraded, p.NewTierCode);
            await HandleIncomingTierUpgradedAsync(p).ConfigureAwait(false);
        });
        connection.On<TestDriveCreatedRealtimeEvent>(SignalRHubEventNames.TestDriveCreated, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.TestDriveCreated, p.BookingNumber);
            await HandleIncomingTestDriveCreatedAsync(p).ConfigureAwait(false);
        });
        connection.On<TestDriveStatusChangedRealtimeEvent>(SignalRHubEventNames.TestDriveStatusChanged, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.TestDriveStatusChanged, $"{p.BookingNumber}:{p.NewStatus}");
            await HandleIncomingTestDriveStatusChangedAsync(p).ConfigureAwait(false);
        });
        connection.On<ServiceReviewSubmittedRealtimeEvent>(SignalRHubEventNames.ServiceReviewSubmitted, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.ServiceReviewSubmitted, p.ReviewNumber);
            await HandleIncomingServiceReviewSubmittedAsync(p).ConfigureAwait(false);
        });
        connection.On<WarrantyClaimCreatedRealtimeEvent>(SignalRHubEventNames.WarrantyClaimCreated, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.WarrantyClaimCreated, p.ClaimNumber);
            await HandleIncomingWarrantyClaimCreatedAsync(p).ConfigureAwait(false);
        });
        connection.On<WarrantyClaimStatusChangedRealtimeEvent>(SignalRHubEventNames.WarrantyClaimStatusChanged, async p =>
        {
            LogRealtimeEvent(SignalRHubEventNames.WarrantyClaimStatusChanged, $"{p.ClaimNumber}:{p.NewStatus}");
            await HandleIncomingWarrantyClaimStatusChangedAsync(p).ConfigureAwait(false);
        });
    }

    private void LogRealtimeEvent(string eventName, string? target)
    {
        var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
        activeLogger?.Track(
            action: $"Realtime.{eventName}",
            category: "Realtime",
            target: target,
            outcome: "Success");
    }

    private async Task RecordWatermarkAsync(DateTimeOffset timestampUtc, CancellationToken cancellationToken)
    {
        if (_lastEventWatermarkUtc is null || timestampUtc > _lastEventWatermarkUtc.Value)
        {
            _lastEventWatermarkUtc = timestampUtc;
        }

        if (_sessionState.IsAuthenticated)
        {
            await _offlineDb.UpsertSyncStateAsync(
                _sessionState.OwnerScopeKey,
                NotificationWatermarkResource,
                _lastEventWatermarkUtc.Value,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private void SetStatus(SignalRConnectionStatus status)
    {
        if (_connectionStatus == status)
        {
            return;
        }

        _connectionStatus = status;
        var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
        activeLogger?.Track(
            action: "Realtime.ConnectionStatus",
            category: "Realtime",
            target: status.ToString(),
            outcome: status == SignalRConnectionStatus.Disconnected ? "Warning" : "Success",
            syncToServer: false);
        DispatchToMainThread(() => ConnectionStatusChanged?.Invoke(this, status));
    }

    private void DispatchToMainThread(Action action)
    {
        if (_mainThreadDispatcher.IsMainThread)
        {
            action();
        }
        else
        {
            _mainThreadDispatcher.BeginInvokeOnMainThread(action);
        }
    }

    private async Task DisposeConnectionInternalAsync()
    {
        if (_hubConnection is null)
        {
            return;
        }

        var conn = _hubConnection;
        _hubConnection = null;

        try
        {
            if (conn.State != HubConnectionState.Disconnected)
            {
                await conn.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch
        {
            // Ignore transport exceptions while tearing down.
        }

        await conn.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _lifecycleLock.Dispose();
    }
}
