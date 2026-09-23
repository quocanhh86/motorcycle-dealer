using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Humanizer;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Customer;

public sealed partial class CustomerHomeViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly ISignalRMobileService _signalRService;
    private readonly INavigationService _navigationService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;

    public CustomerHomeViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        ISignalRMobileService signalRService,
        INavigationService navigationService,
        IMainThreadDispatcher mainThreadDispatcher,
        IMessenger messenger,
        IUserFeedbackService feedbackService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));

        GreetingTitle = "Welcome back";
        TierCode = _sessionState.TierCode ?? string.Empty;
        TierMultiplierLabel = _sessionState.TierMultiplier.HasValue
            ? $"{_sessionState.TierMultiplier.Value:0.00}x"
            : string.Empty;
        FormattedAvailablePoints = VietnamFormatting.FormatPointsBalance(_sessionState.AvailablePoints ?? 0);
        NextTierSummary = "Syncing membership tiers...";
        NextTierHint = "Syncing membership tiers...";
        TierProgressRatio = 0d;

        _messenger.Register<CustomerHomeViewModel, BookingMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<CustomerHomeViewModel, WalletMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<CustomerHomeViewModel, VehicleMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<CustomerHomeViewModel, TestDriveMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<CustomerHomeViewModel, WarrantyMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public ObservableCollection<LoyaltyTierDto> LoyaltyTiers { get; } = [];
    public ObservableCollection<CustomerVehicleDto> Vehicles { get; } = [];
    public ObservableCollection<BookingDto> UpcomingBookings { get; } = [];
    public ObservableCollection<TestDriveBookingDto> UpcomingTestDrives { get; } = [];

    [ObservableProperty]
    public partial string GreetingTitle { get; set; }

    [ObservableProperty]
    public partial LoyaltyAccountSummaryDto? Wallet { get; set; }

    [ObservableProperty]
    public partial string TierCode { get; set; }

    [ObservableProperty]
    public partial string TierMultiplierLabel { get; set; }

    [ObservableProperty]
    public partial string FormattedAvailablePoints { get; set; }

    [ObservableProperty]
    public partial string NextTierSummary { get; set; }

    [ObservableProperty]
    public partial double TierProgressRatio { get; set; }

    [ObservableProperty]
    public partial string NextTierHint { get; set; }

    [ObservableProperty]
    public partial int DueSoonVehicleCount { get; set; }

    [ObservableProperty]
    public partial int UnreadNotificationsCount { get; set; }

    public string DueSoonAlertMessage =>
        $"{"motorcycle".ToQuantity(DueSoonVehicleCount)} {(DueSoonVehicleCount == 1 ? "needs" : "need")} periodic service soon.";

    partial void OnDueSoonVehicleCountChanged(int value) => OnPropertyChanged(nameof(DueSoonAlertMessage));

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        var scope = _sessionState.OwnerScopeKey;
        GreetingTitle = string.IsNullOrWhiteSpace(_sessionState.FullName)
            ? "Welcome back"
            : $"Hello, {_sessionState.FullName}";
        UnreadNotificationsCount = _signalRService.UnreadCount;

        try
        {
            var walletTask = _apiClient.GetMyLoyaltyWalletAsync();
            var tiersTask = _apiClient.ListLoyaltyTiersAsync();
            var vehiclesTask = _apiClient.ListMyVehiclesAsync();
            var bookingsTask = _apiClient.ListMyBookingsAsync(pageNumber: 1, pageSize: 10);
            var testDrivesTask = _apiClient.ListMyTestDrivesAsync(pageNumber: 1, pageSize: 10);

            await Task.WhenAll(walletTask, tiersTask, vehiclesTask, bookingsTask, testDrivesTask).ConfigureAwait(false);

            var anyOffline = false;

            // 1. Wallet
            LoyaltyAccountSummaryDto? walletData;
            if (walletTask.Result.IsSuccess && walletTask.Result.Data is not null)
            {
                walletData = walletTask.Result.Data;
                await _offlineDb.SaveLoyaltyWalletAsync(scope, walletData).ConfigureAwait(false);
            }
            else
            {
                anyOffline |= walletTask.Result.IsNetworkError;
                walletData = await _offlineDb.GetCachedLoyaltyWalletAsync(scope).ConfigureAwait(false);
            }

            // 2. Live Loyalty Tiers (with encrypted SQLCipher offline fallback)
            IReadOnlyList<LoyaltyTierDto> tiersData;
            if (tiersTask.Result.IsSuccess && tiersTask.Result.Data is not null)
            {
                tiersData = tiersTask.Result.Data;
                await _offlineDb.SaveLoyaltyTiersAsync(tiersData).ConfigureAwait(false);
            }
            else
            {
                anyOffline |= tiersTask.Result.IsNetworkError;
                tiersData = await _offlineDb.GetLoyaltyTiersAsync().ConfigureAwait(false);
            }

            // 3. Vehicles
            IReadOnlyList<CustomerVehicleDto> vehicleData;
            if (vehiclesTask.Result.IsSuccess && vehiclesTask.Result.Data is not null)
            {
                vehicleData = vehiclesTask.Result.Data;
                await _offlineDb.SaveCustomerVehiclesAsync(scope, vehicleData).ConfigureAwait(false);
            }
            else
            {
                anyOffline |= vehiclesTask.Result.IsNetworkError;
                vehicleData = await _offlineDb.GetCachedCustomerVehiclesAsync(scope).ConfigureAwait(false);
            }

            // 4. Bookings
            IReadOnlyList<BookingDto> bookingData;
            if (bookingsTask.Result.IsSuccess && bookingsTask.Result.Data is not null)
            {
                bookingData = bookingsTask.Result.Data;
                await _offlineDb.SaveActiveBookingsAsync(scope, bookingData).ConfigureAwait(false);
            }
            else
            {
                anyOffline |= bookingsTask.Result.IsNetworkError;
                bookingData = await _offlineDb.GetCachedActiveBookingsAsync(scope).ConfigureAwait(false);
            }

            // 5. Test drives
            IReadOnlyList<TestDriveBookingDto> testDriveData;
            if (testDrivesTask.Result.IsSuccess && testDrivesTask.Result.Data is not null)
            {
                testDriveData = testDrivesTask.Result.Data;
                await _offlineDb.SaveActiveTestDrivesAsync(scope, testDriveData).ConfigureAwait(false);
            }
            else
            {
                anyOffline |= testDrivesTask.Result.IsNetworkError;
                testDriveData = await _offlineDb.GetCachedActiveTestDrivesAsync(scope).ConfigureAwait(false);
            }

            var syncState = await _offlineDb.GetSyncStateAsync(scope, "loyalty_wallet").ConfigureAwait(false);
            IsOfflineBannerVisible = anyOffline;
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(anyOffline ? syncState?.LastSyncedUtc : DateTimeOffset.UtcNow);
            if (anyOffline)
            {
                await _feedbackService.ShowToastAsync($"Offline cache active ({SyncAgeLabel}).").ConfigureAwait(false);
            }

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                LoyaltyTiers.Clear();
                foreach (var tier in tiersData)
                {
                    LoyaltyTiers.Add(tier);
                }

                ApplyWallet(walletData, tiersData);

                Vehicles.Clear();
                foreach (var v in vehicleData)
                {
                    Vehicles.Add(v);
                }

                DueSoonVehicleCount = vehicleData.Count(v => v.IsDueSoonOrOverdue);

                UpcomingBookings.Clear();
                foreach (var b in bookingData.Where(b => b.Status is "Pending" or "Confirmed" or "CheckedIn" or "InProgress"))
                {
                    UpcomingBookings.Add(b);
                }

                UpcomingTestDrives.Clear();
                foreach (var td in testDriveData.Where(t => t.Status is "Requested" or "Confirmed"))
                {
                    UpcomingTestDrives.Add(td);
                }

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
    private Task OpenGarageAsync() => _navigationService.GoToAsync(MobileRoutes.VinGarage);

    [RelayCommand]
    private Task OpenBookingWizardAsync() => _navigationService.GoToAsync(MobileRoutes.BookingWizard);

    [RelayCommand]
    private Task OpenLoyaltyPassbookAsync() => _navigationService.GoToAsync(MobileRoutes.LoyaltyPassbook);

    [RelayCommand]
    private Task OpenWarrantyAsync() => _navigationService.GoToAsync(MobileRoutes.CustomerWarranty);

    [RelayCommand]
    private Task OpenNotificationsAsync() => _navigationService.GoToAsync(MobileRoutes.NotificationsFeed);

    private void ApplyWallet(LoyaltyAccountSummaryDto? wallet, IReadOnlyList<LoyaltyTierDto> tiers)
    {
        Wallet = wallet;
        var lifetimePoints = wallet?.LifetimeQualifyingPoints ?? _sessionState.LifetimeQualifyingPoints ?? 0;
        var (progressRatio, nextTierHint) = TwoStepLoyaltyPreviewCalculator.CalculateTierProgress(lifetimePoints, tiers);
        TierProgressRatio = progressRatio;
        NextTierHint = nextTierHint;

        if (wallet is null)
        {
            TierCode = _sessionState.TierCode ?? string.Empty;
            TierMultiplierLabel = _sessionState.TierMultiplier.HasValue
                ? $"{_sessionState.TierMultiplier.Value:0.00}x"
                : string.Empty;
            FormattedAvailablePoints = VietnamFormatting.FormatPointsBalance(_sessionState.AvailablePoints ?? 0);
            NextTierSummary = tiers.Count > 0 ? nextTierHint : "Sign in online to sync tier status";
            return;
        }

        TierCode = wallet.TierCode;
        TierMultiplierLabel = $"{wallet.PointsMultiplier:0.00}x";
        FormattedAvailablePoints = VietnamFormatting.FormatPointsBalance(wallet.AvailablePoints);
        NextTierSummary = nextTierHint;
    }

    private async void TriggerCrossTabRefresh()
    {
        if (!_sessionState.IsAuthenticated || !_sessionState.IsCustomer || IsBusy)
        {
            return;
        }

        try
        {
            await LoadDashboardAsync().ConfigureAwait(false);
        }
        catch
        {
            // Prevent unobserved exception on background cross-tab sync.
        }
    }

    public void Dispose() => _messenger.UnregisterAll(this);
}
