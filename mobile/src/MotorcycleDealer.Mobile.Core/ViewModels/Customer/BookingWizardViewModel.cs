using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Media;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Customer;

/// <summary>
/// Customer 3-step Service Booking Wizard with 409 RowVersion slot conflict auto-reload recovery,
/// Test-Drive booking, +25 pts 5-star CSAT ServiceReview submission, and offline-cached Receipt sharing.
/// </summary>
public sealed partial class BookingWizardViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly IOfflineOutboxSyncService _outboxSyncService;
    private readonly IShareService _shareService;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private readonly IMotorcycleImageResolver _imageResolver;
    private bool _isSelfBroadcasting;

    public BookingWizardViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        IConnectivityMonitor connectivityMonitor,
        IOfflineOutboxSyncService outboxSyncService,
        IShareService shareService,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        IMessenger messenger,
        IUserFeedbackService feedbackService,
        IMotorcycleImageResolver? imageResolver = null,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _connectivityMonitor = connectivityMonitor ?? throw new ArgumentNullException(nameof(connectivityMonitor));
        _outboxSyncService = outboxSyncService ?? throw new ArgumentNullException(nameof(outboxSyncService));
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _imageResolver = imageResolver ?? MotorcycleImageResolver.Current;

        CurrentStep = 1;
        SelectedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1));
        SelectedServiceType = "PeriodicMaintenance";
        ReportedNotes = string.Empty;
        ConflictRecoveryMessage = string.Empty;
        ReviewRating = 5;
        ReviewQualityTags = "FastService,FriendlyStaff";
        ReviewComment = string.Empty;
        ReceiptQrPayload = string.Empty;
        TestDriveLicenseNumber = string.Empty;
        TestDriveContactPhone = _sessionState.PhoneNumber;
        TestDriveStartTime = "09:00:00";
        TestDriveEndTime = "10:00:00";

        _messenger.Register<BookingWizardViewModel, VehicleMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<BookingWizardViewModel, WalletMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public IReadOnlyList<string> ServiceTypes { get; } =
        ["PeriodicMaintenance", "WarrantyRepair", "GeneralRepair", "Inspection", "OilAndTire"];

    public ObservableCollection<CustomerVehicleDto> Vehicles { get; } = [];
    public ObservableCollection<DealerDto> Dealers { get; } = [];
    public ObservableCollection<ServiceTimeSlotDto> AvailableSlots { get; } = [];
    public ObservableCollection<CustomerVoucherDto> EligibleVouchers { get; } = [];
    public ObservableCollection<BookingDto> MyBookings { get; } = [];
    public ObservableCollection<ServiceInvoiceDto> MyServiceInvoices { get; } = [];
    public ObservableCollection<TestDriveBookingDto> MyTestDrives { get; } = [];

    public string HeroWorkshopImageSource => _imageResolver.ResolveHeroWorkshop();
    public string SelectedVehicleImageSource => _imageResolver.ResolveForVehicle(SelectedVehicle);
    public string SelectedRatingLabel => Loc.Format("BookingSelectedRatingFormat", ReviewRating);
    public string RatingInvoiceHeading => SelectedInvoiceForReview is null
        ? string.Empty
        : Loc.Format("BookingRatingInvoiceFormat", SelectedInvoiceForReview.InvoiceNumber);

    [ObservableProperty]
    public partial int CurrentStep { get; set; }

    [ObservableProperty]
    public partial CustomerVehicleDto? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial DealerDto? SelectedDealer { get; set; }

    [ObservableProperty]
    public partial DateOnly SelectedDate { get; set; }

    [ObservableProperty]
    public partial string SelectedServiceType { get; set; }

    [ObservableProperty]
    public partial int OdometerAtBookingKm { get; set; }

    [ObservableProperty]
    public partial ServiceTimeSlotDto? SelectedSlot { get; set; }

    [ObservableProperty]
    public partial CustomerVoucherDto? SelectedVoucher { get; set; }

    [ObservableProperty]
    public partial string ReportedNotes { get; set; }

    [ObservableProperty]
    public partial bool HasSlotConflict { get; set; }

    [ObservableProperty]
    public partial string ConflictRecoveryMessage { get; set; }

    [ObservableProperty]
    public partial BookingDto? LastCreatedBooking { get; set; }

    // Post-service +25 pts CSAT review state
    [ObservableProperty]
    public partial ServiceInvoiceDto? SelectedInvoiceForReview { get; set; }

    [ObservableProperty]
    public partial int ReviewRating { get; set; }

    [ObservableProperty]
    public partial string ReviewQualityTags { get; set; }

    [ObservableProperty]
    public partial string ReviewComment { get; set; }

    [ObservableProperty]
    public partial PrintableServiceReceiptDto? ActiveServiceReceipt { get; set; }

    [ObservableProperty]
    public partial string ReceiptQrPayload { get; set; }

    // Test-drive quick form state
    [ObservableProperty]
    public partial string TestDriveLicenseNumber { get; set; }

    [ObservableProperty]
    public partial string TestDriveContactPhone { get; set; }

    [ObservableProperty]
    public partial string TestDriveStartTime { get; set; }

    [ObservableProperty]
    public partial string TestDriveEndTime { get; set; }

    partial void OnSelectedVehicleChanged(CustomerVehicleDto? value) =>
        OnPropertyChanged(nameof(SelectedVehicleImageSource));

    partial void OnReviewRatingChanged(int value) =>
        OnPropertyChanged(nameof(SelectedRatingLabel));

    partial void OnSelectedInvoiceForReviewChanged(ServiceInvoiceDto? value) =>
        OnPropertyChanged(nameof(RatingInvoiceHeading));

    protected override void OnCultureChanged(string cultureCode)
    {
        var selectedVehicle = SelectedVehicle;
        var selectedDealer = SelectedDealer;
        var selectedSlot = SelectedSlot;
        var selectedVoucher = SelectedVoucher;
        var selectedInvoice = SelectedInvoiceForReview;

        RefreshCollection(Vehicles);
        RefreshCollection(AvailableSlots);
        RefreshCollection(EligibleVouchers);
        RefreshCollection(MyBookings);
        RefreshCollection(MyServiceInvoices);
        RefreshCollection(MyTestDrives);

        SelectedVehicle = selectedVehicle;
        SelectedDealer = selectedDealer;
        SelectedSlot = selectedSlot;
        SelectedVoucher = selectedVoucher;
        SelectedInvoiceForReview = selectedInvoice;

        OnPropertyChanged(nameof(SelectedRatingLabel));
        OnPropertyChanged(nameof(RatingInvoiceHeading));
    }

    [RelayCommand]
    public async Task LoadWizardDataAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        var scope = _sessionState.OwnerScopeKey;

        try
        {
            var vehiclesTask = _apiClient.ListMyVehiclesAsync();
            var dealersTask = _apiClient.ListDealersAsync();
            var vouchersTask = _apiClient.ListMyVouchersAsync(status: "Available");
            var bookingsTask = _apiClient.ListMyBookingsAsync(pageNumber: 1, pageSize: 20);
            var invoicesTask = _apiClient.ListMyServiceInvoicesAsync(pageNumber: 1, pageSize: 20);
            var testDrivesTask = _apiClient.ListMyTestDrivesAsync(pageNumber: 1, pageSize: 20);

            await Task.WhenAll(vehiclesTask, dealersTask, vouchersTask, bookingsTask, invoicesTask, testDrivesTask)
                .ConfigureAwait(false);

            var vehicles = vehiclesTask.Result.IsSuccess && vehiclesTask.Result.Data is not null
                ? vehiclesTask.Result.Data
                : await _offlineDb.GetCachedCustomerVehiclesAsync(scope).ConfigureAwait(false);

            var dealers = dealersTask.Result.IsSuccess && dealersTask.Result.Data is not null
                ? dealersTask.Result.Data
                : await _offlineDb.GetCachedDealersAsync().ConfigureAwait(false);

            var vouchers = vouchersTask.Result.IsSuccess && vouchersTask.Result.Data is not null
                ? vouchersTask.Result.Data.Where(v => v.ApplicableScope is "ServiceOnly" or "Both").ToArray()
                : (await _offlineDb.GetCachedCustomerVouchersAsync(scope).ConfigureAwait(false))
                    .Where(v => v.EffectiveStatus == "Available" && v.ApplicableScope is "ServiceOnly" or "Both").ToArray();

            var bookings = bookingsTask.Result.IsSuccess && bookingsTask.Result.Data is not null
                ? bookingsTask.Result.Data
                : await _offlineDb.GetCachedActiveBookingsAsync(scope).ConfigureAwait(false);

            if (bookingsTask.Result.IsSuccess && bookingsTask.Result.Data is not null)
            {
                await _offlineDb.SaveActiveBookingsAsync(scope, bookingsTask.Result.Data).ConfigureAwait(false);
            }

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                Vehicles.Clear();
                foreach (var v in vehicles) Vehicles.Add(v);

                Dealers.Clear();
                foreach (var d in dealers) Dealers.Add(d);

                EligibleVouchers.Clear();
                foreach (var vc in vouchers) EligibleVouchers.Add(vc);

                MyBookings.Clear();
                foreach (var b in bookings) MyBookings.Add(b);

                MyServiceInvoices.Clear();
                if (invoicesTask.Result.IsSuccess && invoicesTask.Result.Data is not null)
                {
                    foreach (var inv in invoicesTask.Result.Data) MyServiceInvoices.Add(inv);
                }

                MyTestDrives.Clear();
                if (testDrivesTask.Result.IsSuccess && testDrivesTask.Result.Data is not null)
                {
                    foreach (var td in testDrivesTask.Result.Data) MyTestDrives.Add(td);
                }

                if (SelectedVehicle is null && Vehicles.Count > 0)
                {
                    SelectedVehicle = Vehicles[0];
                    OdometerAtBookingKm = Vehicles[0].CurrentMileageKm;
                }

                if (SelectedDealer is null && Dealers.Count > 0)
                {
                    SelectedDealer = Dealers[0];
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);

            if (SelectedDealer is not null)
            {
                await LoadAvailableSlotsAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task LoadAvailableSlotsAsync()
    {
        if (SelectedDealer is null)
        {
            return;
        }

        var result = await _apiClient.ListTimeSlotsAsync(
            dealerId: SelectedDealer.Id,
            slotDate: SelectedDate,
            availableOnly: true).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            return;
        }

        await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
        {
            AvailableSlots.Clear();
            foreach (var slot in result.Data.Where(s => s.IsAvailable && !s.IsBlocked && s.RemainingCapacity > 0))
            {
                AvailableSlots.Add(slot);
            }

            SelectedSlot = AvailableSlots.FirstOrDefault();
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void GoToStep(int step)
    {
        CurrentStep = Math.Clamp(step, 1, 3);
        ClearMessages();
    }

    [RelayCommand]
    public async Task ConfirmBookingAsync()
    {
        ClearMessages();
        HasSlotConflict = false;
        ConflictRecoveryMessage = string.Empty;

        if (!_connectivityMonitor.IsConnected)
        {
            ErrorMessage = Loc.BookingOnlineRequiredError;
            return;
        }

        if (SelectedDealer is null || SelectedVehicle is null || SelectedSlot is null)
        {
            ErrorMessage = Loc.BookingSelectRequiredError;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiClient.CreateBookingAsync(new CreateServiceBookingRequest
            {
                DealerId = SelectedDealer.Id,
                ServiceTimeSlotId = SelectedSlot.Id,
                SlotRowVersion = SelectedSlot.RowVersion,
                CustomerVehicleId = SelectedVehicle.Id,
                AppliedCustomerVoucherId = SelectedVoucher?.Id,
                VoucherCode = SelectedVoucher?.VoucherCode,
                VoucherRowVersion = SelectedVoucher?.RowVersion,
                ServiceType = SelectedServiceType,
                OdometerAtBookingKm = Math.Max(SelectedVehicle.CurrentMileageKm, OdometerAtBookingKm),
                ReportedNotes = string.IsNullOrWhiteSpace(ReportedNotes) ? null : ReportedNotes.Trim()
            }).ConfigureAwait(false);

            if (result.IsConflict409)
            {
                // Optimistic concurrency (RowVersion) or full-capacity race recovery!
                HasSlotConflict = true;
                ConflictRecoveryMessage = !string.IsNullOrWhiteSpace(result.Problem?.ErrorCode)
                    ? Loc.GetErrorLabel(result.Problem.ErrorCode, Loc.BookingSlotConflictRecovery)
                    : Loc.BookingSlotConflictRecovery;
                ErrorMessage = ConflictRecoveryMessage;
                CurrentStep = 2;
                _hapticService.Perform(HapticKind.Warning);
                await _feedbackService.ShowSnackbarAsync(Loc.BookingSlotConflictSnackbar).ConfigureAwait(false);
                await LoadAvailableSlotsAsync().ConfigureAwait(false);
                return;
            }

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                return;
            }

            var reservedVoucher = SelectedVoucher is not null || result.Data.AppliedCustomerVoucherId.HasValue;
            LastCreatedBooking = result.Data;
            SuccessMessage = Loc.Format(
                "BookingConfirmedSuccess",
                result.Data.BookingNumber,
                VietnamFormatting.FormatSlotDate(result.Data.SlotDate),
                VietnamFormatting.FormatTimeRange(result.Data.StartTime, result.Data.EndTime));
            ReportedNotes = string.Empty;
            SelectedVoucher = null;
            CurrentStep = 1;
            _hapticService.Perform(HapticKind.Success);

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                MyBookings.Insert(0, result.Data);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            BroadcastSelf(() =>
            {
                _messenger.Send(new BookingMutatedMessage(result.Data.BookingId));
                if (reservedVoucher)
                {
                    _messenger.Send(new WalletMutatedMessage("BookingVoucherReserved"));
                }
            });

            await _feedbackService.ShowToastAsync(Loc.Format("BookingConfirmedToast", result.Data.BookingNumber)).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CancelBookingAsync(BookingDto? booking)
    {
        if (booking is null)
        {
            return;
        }

        ClearMessages();
        var result = await _apiClient.CancelBookingAsync(
            booking.BookingId,
            new CancelBookingRequest
            {
                CancellationReason = Loc.BookingCancelledReasonDefault,
                RowVersion = booking.RowVersion
            }).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            return;
        }

        SuccessMessage = Loc.Format("BookingCancelledSuccess", result.Data.BookingNumber);
        BroadcastSelf(() => _messenger.Send(new BookingMutatedMessage(result.Data.BookingId)));
        await _feedbackService.ShowToastAsync(SuccessMessage).ConfigureAwait(false);
        await LoadWizardDataAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task SubmitCsatReviewAsync()
    {
        if (SelectedInvoiceForReview is null)
        {
            ErrorMessage = Loc.BookingSelectInvoiceToRateError;
            return;
        }

        ClearMessages();
        var request = new CreateServiceReviewRequest
        {
            ServiceInvoiceId = SelectedInvoiceForReview.InvoiceId,
            Rating = Math.Clamp(ReviewRating, 1, 5),
            QualityTags = string.IsNullOrWhiteSpace(ReviewQualityTags) ? null : ReviewQualityTags.Trim(),
            Comment = string.IsNullOrWhiteSpace(ReviewComment) ? null : ReviewComment.Trim()
        };

        if (!_connectivityMonitor.IsConnected)
        {
            await _outboxSyncService.EnqueueServiceReviewSubmitAsync(_sessionState.OwnerScopeKey, request)
                .ConfigureAwait(false);
            SuccessMessage = Loc.BookingCsatQueuedOffline;
            await _feedbackService.ShowSnackbarAsync(SuccessMessage).ConfigureAwait(false);
            return;
        }

        var result = await _apiClient.CreateServiceReviewAsync(request).ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
        {
            SuccessMessage = Loc.Format("BookingCsatSubmittedSuccess", result.Data.ReviewNumber, result.Data.BonusPointsAwarded);
            ReviewComment = string.Empty;
            SelectedInvoiceForReview = null;
            _hapticService.Perform(HapticKind.Success);
            BroadcastSelf(() => _messenger.Send(new WalletMutatedMessage($"CsatReview:{result.Data.ReviewNumber}")));
            await _feedbackService.ShowSnackbarAsync(Loc.Format("BookingCsatSubmittedSnackbar", result.Data.BonusPointsAwarded)).ConfigureAwait(false);
        }
        else if (result.IsNetworkError)
        {
            await _outboxSyncService.EnqueueServiceReviewSubmitAsync(_sessionState.OwnerScopeKey, request)
                .ConfigureAwait(false);
            SuccessMessage = Loc.BookingCsatQueuedDropped;
            await _feedbackService.ShowSnackbarAsync(SuccessMessage).ConfigureAwait(false);
        }
        else
        {
            ErrorMessage = FormatApiError(result);
        }
    }

    [RelayCommand]
    public async Task LoadAndShareServiceReceiptAsync(ServiceInvoiceDto? invoice)
    {
        if (invoice is null)
        {
            return;
        }

        ClearMessages();
        var scope = _sessionState.OwnerScopeKey;
        PrintableServiceReceiptDto? receipt;

        var apiResult = await _apiClient.GetServiceInvoiceReceiptAsync(invoice.InvoiceId).ConfigureAwait(false);
        if (apiResult.IsSuccess && apiResult.Data is not null)
        {
            receipt = apiResult.Data;
            await _offlineDb.SaveServiceReceiptAsync(scope, receipt).ConfigureAwait(false);
        }
        else
        {
            receipt = await _offlineDb.GetCachedServiceReceiptAsync(scope, invoice.InvoiceId).ConfigureAwait(false);
            if (receipt is null)
            {
                ErrorMessage = FormatApiError(apiResult);
                return;
            }
        }

        ActiveServiceReceipt = receipt;
        ReceiptQrPayload = DealerQrPayloadParser.BuildReceiptQrPayload(
            receipt.ReceiptNumber,
            receipt.VerificationCode,
            receipt.ReceiptType);

        var summaryText = Loc.Format(
            "BookingReceiptShareBody",
            receipt.ReceiptNumber,
            receipt.Dealer.Name,
            receipt.Vehicle.Vin,
            receipt.Vehicle.ModelName,
            VietnamFormatting.FormatVnd(receipt.NetPaidAmount),
            receipt.Loyalty.PointsEarned,
            Loc.GetEnumLabel("LoyaltyTier", receipt.Loyalty.TierCodeAfter),
            receipt.VerificationCode);

        await _shareService.ShareTextAsync(
            Loc.Format("BookingReceiptShareTitle", receipt.ReceiptNumber),
            summaryText).ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task CreateTestDriveAsync(Guid motorcycleModelId)
    {
        if (SelectedDealer is null || motorcycleModelId == Guid.Empty)
        {
            ErrorMessage = Loc.BookingSelectTestDriveError;
            return;
        }

        ClearMessages();
        var result = await _apiClient.CreateTestDriveAsync(new CreateTestDriveRequest
        {
            DealerId = SelectedDealer.Id,
            MotorcycleModelId = motorcycleModelId,
            PreferredDate = SelectedDate,
            StartTime = TestDriveStartTime,
            EndTime = TestDriveEndTime,
            DriverLicenseNumber = TestDriveLicenseNumber.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(TestDriveContactPhone) ? _sessionState.PhoneNumber : TestDriveContactPhone.Trim(),
            CustomerNotes = string.IsNullOrWhiteSpace(ReportedNotes) ? null : ReportedNotes.Trim()
        }).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            return;
        }

        SuccessMessage = Loc.Format("BookingTestDriveRequestedSuccess", result.Data.BookingNumber, result.Data.DealerName);
        _hapticService.Perform(HapticKind.Success);
        BroadcastSelf(() => _messenger.Send(new TestDriveMutatedMessage(result.Data.TestDriveId)));
        await _feedbackService.ShowToastAsync(Loc.Format("BookingTestDriveRequestedToast", result.Data.BookingNumber)).ConfigureAwait(false);
        await LoadWizardDataAsync().ConfigureAwait(false);
    }

    private void BroadcastSelf(Action broadcast)
    {
        _isSelfBroadcasting = true;
        try
        {
            broadcast();
        }
        finally
        {
            _isSelfBroadcasting = false;
        }
    }

    private async void TriggerCrossTabRefresh()
    {
        if (_isSelfBroadcasting || !_sessionState.IsAuthenticated || !_sessionState.IsCustomer || IsBusy)
        {
            return;
        }

        try
        {
            await LoadWizardDataAsync().ConfigureAwait(false);
        }
        catch
        {
            // Prevent unobserved exception on background cross-tab sync.
        }
    }

    public void Dispose()
    {
        _messenger.UnregisterAll(this);
        UnregisterBaseMessages();
    }
}
