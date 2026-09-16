using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Staff;

/// <summary>
/// DealerStaff Workshop Workbench: dealer-scoped segmented status lanes, 1-swipe bay check-in,
/// VIN/Booking QR intake scanner, live SignalR updates, and 1-tap launch into MobileInvoiceBuilder.
/// </summary>
public sealed partial class StaffWorkbenchViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly ISignalRMobileService _signalRService;
    private readonly INavigationService _navigationService;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;

    private List<BookingDto> _allBookings = [];
    private bool _isSelfBroadcasting;

    public StaffWorkbenchViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        ISignalRMobileService signalRService,
        INavigationService navigationService,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        IMessenger messenger,
        IUserFeedbackService feedbackService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));

        SelectedLane = "All";
        SearchVinOrNumber = string.Empty;
        DealerHeaderLabel = _sessionState.DealerName ?? _sessionState.DealerCode ?? "Dealer Workbench";

        _signalRService.BookingCreatedReceived += OnBookingCreatedRealtime;
        _signalRService.BookingStatusChangedReceived += OnBookingStatusChangedRealtime;
        _messenger.Register<StaffWorkbenchViewModel, BookingMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public IReadOnlyList<string> StatusLanes { get; } =
        ["All", "Pending", "Confirmed", "CheckedIn", "InProgress", "Completed"];

    public ObservableCollection<BookingDto> FilteredBookings { get; } = [];

    [ObservableProperty]
    public partial string DealerHeaderLabel { get; set; }

    [ObservableProperty]
    public partial string SelectedLane { get; set; }

    [ObservableProperty]
    public partial string SearchVinOrNumber { get; set; }

    [ObservableProperty]
    public partial DateOnly? FilterDate { get; set; }

    [ObservableProperty]
    public partial BookingDto? SelectedBooking { get; set; }

    [ObservableProperty]
    public partial CustomerVehicleDto? ScannedIntakeVehicle { get; set; }

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int CheckedInOrInProgressCount { get; set; }

    partial void OnSelectedLaneChanged(string value) => ApplyLaneFilter();
    partial void OnSearchVinOrNumberChanged(string value) => ApplyLaneFilter();

    [RelayCommand]
    public async Task LoadWorkbenchAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        var scope = _sessionState.OwnerScopeKey;
        DealerHeaderLabel = _sessionState.DealerName ?? _sessionState.DealerCode ?? "Dealer Workbench";

        try
        {
            var result = await _apiClient.ListDealerBookingsAsync(
                slotDate: FilterDate,
                pageNumber: 1,
                pageSize: 50).ConfigureAwait(false);

            IReadOnlyList<BookingDto> items;
            if (result.IsSuccess && result.Data is not null)
            {
                items = result.Data;
                await _offlineDb.SaveActiveBookingsAsync(scope, items).ConfigureAwait(false);
                IsOfflineBannerVisible = false;
                SyncAgeLabel = VietnamFormatting.FormatSyncAge(DateTimeOffset.UtcNow);
            }
            else
            {
                items = await _offlineDb.GetCachedActiveBookingsAsync(scope).ConfigureAwait(false);
                var sync = await _offlineDb.GetSyncStateAsync(scope, "bookings").ConfigureAwait(false);
                IsOfflineBannerVisible = true;
                SyncAgeLabel = VietnamFormatting.FormatSyncAge(sync?.LastSyncedUtc);
                if (items.Count == 0)
                {
                    ErrorMessage = result.ErrorMessage;
                }
            }

            _allBookings = items.ToList();
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                RecomputeCounts();
                ApplyLaneFilterInternal();
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
    private void SelectLane(string lane) => SelectedLane = string.IsNullOrWhiteSpace(lane) ? "All" : lane;

    [RelayCommand]
    public Task ConfirmBookingAsync(BookingDto? booking) =>
        TransitionBookingStatusAsync(booking, "Confirmed");

    [RelayCommand]
    public Task SwipeCheckInBookingAsync(BookingDto? booking) =>
        TransitionBookingStatusAsync(booking, "CheckedIn");

    [RelayCommand]
    public Task StartServiceBayAsync(BookingDto? booking) =>
        TransitionBookingStatusAsync(booking, "InProgress");

    [RelayCommand]
    public Task MarkNoShowAsync(BookingDto? booking) =>
        TransitionBookingStatusAsync(booking, "NoShow");

    [RelayCommand]
    public async Task ScanForIntakeAsync()
    {
        var raw = await _navigationService.RequestBarcodeScanAsync("Scan VIN Sticker or Booking QR").ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        await LookupByScannedCodeAsync(raw).ConfigureAwait(false);
    }

    public async Task LookupByScannedCodeAsync(string rawPayload)
    {
        ClearMessages();
        var parsed = DealerQrPayloadParser.Parse(rawPayload);

        if (parsed.Kind == QrPayloadKind.Booking)
        {
            var match = _allBookings.FirstOrDefault(b =>
                (parsed.EntityId.HasValue && b.BookingId == parsed.EntityId.Value) ||
                (!string.IsNullOrWhiteSpace(parsed.DocumentNumber) &&
                 string.Equals(b.BookingNumber, parsed.DocumentNumber, StringComparison.OrdinalIgnoreCase)));

            if (match is not null)
            {
                SelectedBooking = match;
                SearchVinOrNumber = match.BookingNumber;
                SuccessMessage = $"Booking {match.BookingNumber} located ({match.Vin}).";
                _hapticService.Perform(HapticKind.Success);
                return;
            }

            if (parsed.EntityId.HasValue)
            {
                var fetched = await _apiClient.GetBookingByIdAsync(parsed.EntityId.Value).ConfigureAwait(false);
                if (fetched.IsSuccess && fetched.Data is not null)
                {
                    SelectedBooking = fetched.Data;
                    SearchVinOrNumber = fetched.Data.BookingNumber;
                    SuccessMessage = $"Loaded booking {fetched.Data.BookingNumber}.";
                    return;
                }
            }

            ErrorMessage = "Booking QR not found in your dealership queue.";
            return;
        }

        var vinCandidate = parsed.Kind == QrPayloadKind.Vin && !string.IsNullOrWhiteSpace(parsed.Vin)
            ? parsed.Vin
            : Iso3779VinValidator.SanitizeLiveInput(rawPayload);

        if (Iso3779VinValidator.IsValid(vinCandidate))
        {
            SearchVinOrNumber = vinCandidate;
            var lookup = await _apiClient.LookupVehiclesAsync(vin: vinCandidate).ConfigureAwait(false);
            if (lookup.IsSuccess && lookup.Data is { Count: > 0 })
            {
                ScannedIntakeVehicle = lookup.Data[0];
                SuccessMessage = $"VIN intake matched: {ScannedIntakeVehicle.MotorcycleModelName} ({ScannedIntakeVehicle.CustomerFullName}).";
                _hapticService.Perform(HapticKind.Success);
            }
            else
            {
                SuccessMessage = $"Filtered workbench by VIN {vinCandidate}.";
            }
        }
        else
        {
            ErrorMessage = "Unrecognized barcode/QR payload. Scan a 17-char VIN or Booking QR.";
        }
    }

    [RelayCommand]
    private Task OpenInvoiceBuilderAsync(BookingDto? booking)
    {
        var target = booking ?? SelectedBooking;
        if (target is null)
        {
            return Task.CompletedTask;
        }

        var parameters = new Dictionary<string, object>
        {
            ["bookingId"] = target.BookingId.ToString("D")
        };

        return _navigationService.GoToAsync(MobileRoutes.MobileInvoiceBuilder, parameters);
    }

    private async Task TransitionBookingStatusAsync(BookingDto? booking, string nextStatus)
    {
        if (booking is null)
        {
            return;
        }

        ClearMessages();
        var result = await _apiClient.UpdateBookingStatusAsync(
            booking.BookingId,
            new UpdateBookingStatusRequest
            {
                NewStatus = nextStatus,
                RowVersion = booking.RowVersion
            }).ConfigureAwait(false);

        if (result.IsConflict409)
        {
            ErrorMessage = "This booking was updated by another staff member. Refreshing bay queue…";
            _hapticService.Perform(HapticKind.Warning);
            await _feedbackService.ShowSnackbarAsync("Booking updated elsewhere — refreshing bay queue.").ConfigureAwait(false);
            await LoadWorkbenchAsync().ConfigureAwait(false);
            return;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = result.ErrorMessage;
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        var updated = result.Data;
        var idx = _allBookings.FindIndex(b => b.BookingId == updated.BookingId);
        if (idx >= 0)
        {
            _allBookings[idx] = updated;
        }
        else
        {
            _allBookings.Insert(0, updated);
        }

        SelectedBooking = updated;
        var statusLabel = VietnamFormatting.HumanizeStatus(updated.Status);
        SuccessMessage = $"Booking {updated.BookingNumber} moved to {statusLabel}.";
        _hapticService.Perform(HapticKind.Success);

        await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
        {
            RecomputeCounts();
            ApplyLaneFilterInternal();
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        _isSelfBroadcasting = true;
        try
        {
            _messenger.Send(new BookingMutatedMessage(updated.BookingId));
        }
        finally
        {
            _isSelfBroadcasting = false;
        }

        await _feedbackService.ShowToastAsync($"Booking {updated.BookingNumber} → {statusLabel}").ConfigureAwait(false);
    }

    private void ApplyLaneFilter() =>
        _mainThreadDispatcher.BeginInvokeOnMainThread(ApplyLaneFilterInternal);

    private void ApplyLaneFilterInternal()
    {
        IEnumerable<BookingDto> query = _allBookings;
        if (!string.IsNullOrWhiteSpace(SelectedLane) && !string.Equals(SelectedLane, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(b => string.Equals(b.Status, SelectedLane, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchVinOrNumber))
        {
            var term = SearchVinOrNumber.Trim();
            query = query.Where(b =>
                b.Vin.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.BookingNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.CustomerFullName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        FilteredBookings.Clear();
        foreach (var item in query)
        {
            FilteredBookings.Add(item);
        }
    }

    private void RecomputeCounts()
    {
        PendingCount = _allBookings.Count(b => b.Status is "Pending" or "Confirmed");
        CheckedInOrInProgressCount = _allBookings.Count(b => b.Status is "CheckedIn" or "InProgress");
    }

    private async void OnBookingCreatedRealtime(object? sender, BookingCreatedRealtimeEvent e)
    {
        if (!_sessionState.DealerId.HasValue || _sessionState.DealerId.Value == e.DealerId)
        {
            await LoadWorkbenchAsync().ConfigureAwait(false);
        }
    }

    private async void OnBookingStatusChangedRealtime(object? sender, BookingStatusChangedRealtimeEvent e)
    {
        await LoadWorkbenchAsync().ConfigureAwait(false);
    }

    private async void TriggerCrossTabRefresh()
    {
        if (_isSelfBroadcasting || !_sessionState.IsAuthenticated || !_sessionState.IsDealerStaff || IsBusy)
        {
            return;
        }

        try
        {
            await LoadWorkbenchAsync().ConfigureAwait(false);
        }
        catch
        {
            // Prevent unobserved exception on background cross-tab sync.
        }
    }

    public void Dispose()
    {
        _signalRService.BookingCreatedReceived -= OnBookingCreatedRealtime;
        _signalRService.BookingStatusChangedReceived -= OnBookingStatusChangedRealtime;
        _messenger.UnregisterAll(this);
    }
}
