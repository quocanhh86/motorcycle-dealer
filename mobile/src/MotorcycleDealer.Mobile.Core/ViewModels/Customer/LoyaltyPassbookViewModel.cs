using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Customer;

/// <summary>
/// Customer Loyalty Passbook: Metallic 4-tier card, offline-accessible QR voucher wallet,
/// online voucher redemption store, immutable append-only point ledger, and showroom sales receipts.
/// </summary>
public sealed partial class LoyaltyPassbookViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly IShareService _shareService;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private DateTimeOffset? _lastSyncUtc;
    private bool _isSelfBroadcasting;

    public LoyaltyPassbookViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        IConnectivityMonitor connectivityMonitor,
        IShareService shareService,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        IMessenger messenger,
        IUserFeedbackService feedbackService,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _connectivityMonitor = connectivityMonitor ?? throw new ArgumentNullException(nameof(connectivityMonitor));
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));

        SelectedSection = "Vouchers";
        SelectedVoucherQrPayload = string.Empty;
        ActiveReceiptQrPayload = string.Empty;
        TierCode = _sessionState.TierCode ?? string.Empty;
        FormattedAvailablePoints = VietnamFormatting.FormatPointsBalance(_sessionState.AvailablePoints ?? 0);
        FormattedLifetimeSpent = VietnamFormatting.FormatVnd(_sessionState.LifetimeSpentVnd ?? 0m);
        TierProgressRatio = 0d;
        NextTierHint = Loc.LoyaltySyncingTiers;

        _messenger.Register<LoyaltyPassbookViewModel, WalletMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<LoyaltyPassbookViewModel, BookingMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public ObservableCollection<LoyaltyTierDto> Tiers { get; } = [];
    public ObservableCollection<LoyaltyTierDto> LoyaltyTiers => Tiers;
    public ObservableCollection<CustomerVoucherDto> MyVouchers { get; } = [];
    public ObservableCollection<VoucherCatalogDto> VoucherStore { get; } = [];
    public ObservableCollection<PointLedgerEntryDto> LedgerEntries { get; } = [];
    public ObservableCollection<SalesOrderDto> MySalesOrders { get; } = [];

    public string OfflinePassbookBannerText => Loc.Format("LoyaltyOfflineBanner", SyncAgeLabel);

    [ObservableProperty]
    public partial string SelectedSection { get; set; }

    [ObservableProperty]
    public partial LoyaltyAccountSummaryDto? Wallet { get; set; }

    [ObservableProperty]
    public partial string TierCode { get; set; }

    [ObservableProperty]
    public partial string FormattedAvailablePoints { get; set; }

    [ObservableProperty]
    public partial string FormattedLifetimeSpent { get; set; }

    [ObservableProperty]
    public partial double TierProgressRatio { get; set; }

    [ObservableProperty]
    public partial string NextTierHint { get; set; }

    [ObservableProperty]
    public partial CustomerVoucherDto? SelectedVoucher { get; set; }

    [ObservableProperty]
    public partial string SelectedVoucherQrPayload { get; set; }

    [ObservableProperty]
    public partial PrintableSalesOrderReceiptDto? ActiveSalesReceipt { get; set; }

    [ObservableProperty]
    public partial string ActiveReceiptQrPayload { get; set; }

    partial void OnSelectedVoucherChanged(CustomerVoucherDto? value)
    {
        SelectedVoucherQrPayload = value is null
            ? string.Empty
            : DealerQrPayloadParser.BuildVoucherQrPayload(value.VoucherCode, value.Id, value.RowVersion);
    }

    protected override void OnCultureChanged(string cultureCode)
    {
        var lifetimePoints = Wallet?.LifetimeQualifyingPoints ?? _sessionState.LifetimeQualifyingPoints ?? 0;
        var (progressRatio, nextTierHint) = TwoStepLoyaltyPreviewCalculator.CalculateTierProgress(lifetimePoints, Tiers);
        TierProgressRatio = progressRatio;
        NextTierHint = nextTierHint;

        if (_lastSyncUtc.HasValue || IsOfflineBannerVisible)
        {
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
        }

        OnPropertyChanged(nameof(OfflinePassbookBannerText));
    }

    [RelayCommand]
    public async Task LoadPassbookAsync()
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
            var walletTask = _apiClient.GetMyLoyaltyWalletAsync();
            var vouchersTask = _apiClient.ListMyVouchersAsync();
            var storeTask = _apiClient.ListVoucherCatalogAsync();
            var ledgerTask = _apiClient.ListMyPointLedgerAsync(pageNumber: 1, pageSize: 30);
            var tiersTask = _apiClient.ListLoyaltyTiersAsync();
            var salesTask = _apiClient.ListMySalesOrdersAsync(pageNumber: 1, pageSize: 20);

            await Task.WhenAll(walletTask, vouchersTask, storeTask, ledgerTask, tiersTask, salesTask)
                .ConfigureAwait(false);

            var usedOfflineFallback = false;

            LoyaltyAccountSummaryDto? walletData;
            if (walletTask.Result.IsSuccess && walletTask.Result.Data is not null)
            {
                walletData = walletTask.Result.Data;
                await _offlineDb.SaveLoyaltyWalletAsync(scope, walletData).ConfigureAwait(false);
            }
            else
            {
                usedOfflineFallback = true;
                walletData = await _offlineDb.GetCachedLoyaltyWalletAsync(scope).ConfigureAwait(false);
            }

            IReadOnlyList<CustomerVoucherDto> voucherData;
            if (vouchersTask.Result.IsSuccess && vouchersTask.Result.Data is not null)
            {
                voucherData = vouchersTask.Result.Data;
                await _offlineDb.SaveCustomerVouchersAsync(scope, voucherData).ConfigureAwait(false);
            }
            else
            {
                usedOfflineFallback = true;
                voucherData = await _offlineDb.GetCachedCustomerVouchersAsync(scope).ConfigureAwait(false);
            }

            IReadOnlyList<LoyaltyTierDto> tiersData;
            if (tiersTask.Result.IsSuccess && tiersTask.Result.Data is not null)
            {
                tiersData = tiersTask.Result.Data;
                await _offlineDb.SaveLoyaltyTiersAsync(tiersData).ConfigureAwait(false);
            }
            else
            {
                tiersData = await _offlineDb.GetLoyaltyTiersAsync().ConfigureAwait(false);
            }

            var syncState = await _offlineDb.GetSyncStateAsync(scope, "loyalty_wallet").ConfigureAwait(false);
            IsOfflineBannerVisible = usedOfflineFallback;
            _lastSyncUtc = usedOfflineFallback ? syncState?.LastSyncedUtc : DateTimeOffset.UtcNow;
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
            OnPropertyChanged(nameof(OfflinePassbookBannerText));

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                Wallet = walletData;
                if (walletData is not null)
                {
                    TierCode = walletData.TierCode;
                    FormattedAvailablePoints = VietnamFormatting.FormatPointsBalance(walletData.AvailablePoints);
                    FormattedLifetimeSpent = VietnamFormatting.FormatVnd(walletData.LifetimeSpentVnd);
                }

                Tiers.Clear();
                foreach (var t in tiersData) Tiers.Add(t);

                var lifetimePoints = walletData?.LifetimeQualifyingPoints ?? _sessionState.LifetimeQualifyingPoints ?? 0;
                var (progressRatio, nextTierHint) = TwoStepLoyaltyPreviewCalculator.CalculateTierProgress(lifetimePoints, tiersData);
                TierProgressRatio = progressRatio;
                NextTierHint = nextTierHint;

                MyVouchers.Clear();
                foreach (var v in voucherData) MyVouchers.Add(v);
                if (SelectedVoucher is null)
                {
                    SelectedVoucher = MyVouchers.FirstOrDefault(v => v.EffectiveStatus == "Available") ?? MyVouchers.FirstOrDefault();
                }

                VoucherStore.Clear();
                if (storeTask.Result.IsSuccess && storeTask.Result.Data is not null)
                {
                    foreach (var s in storeTask.Result.Data) VoucherStore.Add(s);
                }

                LedgerEntries.Clear();
                if (ledgerTask.Result.IsSuccess && ledgerTask.Result.Data is not null)
                {
                    foreach (var l in ledgerTask.Result.Data) LedgerEntries.Add(l);
                }

                MySalesOrders.Clear();
                if (salesTask.Result.IsSuccess && salesTask.Result.Data is not null)
                {
                    foreach (var o in salesTask.Result.Data) MySalesOrders.Add(o);
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
    private void SelectSection(string section)
    {
        if (!string.IsNullOrWhiteSpace(section))
        {
            SelectedSection = section;
            TrackUserAction("Loyalty.SwitchSegment", "Loyalty", section);
        }
    }

    [RelayCommand]
    private void SelectVoucherForQr(CustomerVoucherDto? voucher)
    {
        SelectedVoucher = voucher;
        if (voucher is not null)
        {
            TrackUserAction("Loyalty.SelectVoucherQr", "Loyalty", voucher.VoucherCode);
        }
    }

    [RelayCommand]
    public async Task RedeemVoucherAsync(VoucherCatalogDto? catalogItem)
    {
        if (catalogItem is null)
        {
            return;
        }

        ClearMessages();
        TrackUserAction("Loyalty.RedeemVoucher", "Loyalty", catalogItem.Title);
        if (!_connectivityMonitor.IsConnected)
        {
            ErrorMessage = Loc.LoyaltyRedeemOnlineRequired;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiClient.RedeemVoucherAsync(new RedeemVoucherRequest
            {
                VoucherCatalogId = catalogItem.Id,
                AccountRowVersion = Wallet?.RowVersion,
                IdempotencyKey = $"MOB-REDEEM-{Guid.CreateVersion7():N}"
            }).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                _hapticService.Perform(HapticKind.Warning);
                return;
            }

            SelectedVoucher = result.Data;
            SuccessMessage = Loc.Format("LoyaltyVoucherRedeemedSuccess", result.Data.VoucherCode);
            _hapticService.Perform(HapticKind.Success);
            _isSelfBroadcasting = true;
            try
            {
                _messenger.Send(new WalletMutatedMessage($"VoucherRedeemed:{result.Data.VoucherCode}"));
            }
            finally
            {
                _isSelfBroadcasting = false;
            }

            await _feedbackService.ShowSnackbarAsync(Loc.Format("LoyaltyVoucherRedeemedSnackbar", result.Data.VoucherCode)).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadPassbookAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task LoadAndShareSalesReceiptAsync(SalesOrderDto? order)
    {
        if (order is null)
        {
            return;
        }

        ClearMessages();
        var scope = _sessionState.OwnerScopeKey;
        PrintableSalesOrderReceiptDto? receipt;

        var apiResult = await _apiClient.GetSalesOrderReceiptAsync(order.SalesOrderId).ConfigureAwait(false);
        if (apiResult.IsSuccess && apiResult.Data is not null)
        {
            receipt = apiResult.Data;
            await _offlineDb.SaveSalesReceiptAsync(scope, receipt).ConfigureAwait(false);
        }
        else
        {
            receipt = await _offlineDb.GetCachedSalesReceiptAsync(scope, order.SalesOrderId).ConfigureAwait(false);
            if (receipt is null)
            {
                ErrorMessage = FormatApiError(apiResult);
                return;
            }
        }

        ActiveSalesReceipt = receipt;
        ActiveReceiptQrPayload = DealerQrPayloadParser.BuildReceiptQrPayload(
            receipt.ReceiptNumber,
            receipt.VerificationCode,
            receipt.ReceiptType);

        var text = Loc.Format(
            "LoyaltySalesReceiptShareBody",
            receipt.ReceiptNumber,
            receipt.Dealer.Name,
            receipt.Vehicle.ModelName,
            receipt.Vehicle.Vin,
            VietnamFormatting.FormatVnd(receipt.FinalAmount),
            receipt.Loyalty.PointsEarned,
            Loc.GetEnumLabel("LoyaltyTier", receipt.Loyalty.TierCodeAfter),
            receipt.VerificationCode);

        await _shareService.ShareTextAsync(
            Loc.Format("LoyaltySalesReceiptShareTitle", receipt.ReceiptNumber),
            text).ConfigureAwait(false);
    }

    private async void TriggerCrossTabRefresh()
    {
        if (_isSelfBroadcasting || !_sessionState.IsAuthenticated || !_sessionState.IsCustomer || IsBusy)
        {
            return;
        }

        try
        {
            await LoadPassbookAsync().ConfigureAwait(false);
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
