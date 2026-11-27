using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentValidation;
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
using MotorcycleDealer.Mobile.Core.Validation;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Staff;

public sealed partial class ShowroomVinSalesViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly INavigationService _navigationService;
    private readonly IShareService _shareService;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IValidator<SalesOrderInput> _salesOrderValidator;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private readonly IMotorcycleImageResolver _imageResolver;
    private bool _isSelfBroadcasting;

    public ShowroomVinSalesViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        INavigationService navigationService,
        IShareService shareService,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        IValidator<SalesOrderInput> salesOrderValidator,
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
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _salesOrderValidator = salesOrderValidator ?? throw new ArgumentNullException(nameof(salesOrderValidator));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _imageResolver = imageResolver ?? MotorcycleImageResolver.Current;

        CustomerEmail = string.Empty;
        LicensePlate = string.Empty;
        VoucherCode = string.Empty;
        SelectedPaymentMethod = "BankTransfer";
        SaleNotes = string.Empty;
        ReceiveVin = string.Empty;
        ReceiveEngineNumber = string.Empty;
        ReceiveColorName = string.Empty;
        ReceiveManufactureYear = DateTime.UtcNow.Year;
        TierProgressRatio = 0d;
        NextTierHint = string.Empty;

        _messenger.Register<ShowroomVinSalesViewModel, TestDriveMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public IReadOnlyList<string> PaymentMethods { get; } = ["BankTransfer", "Cash", "Card", "EWallet"];

    public ObservableCollection<LoyaltyTierDto> LoyaltyTiers { get; } = [];
    public ObservableCollection<DealerInventoryDto> AvailableStock { get; } = [];
    public ObservableCollection<SalesOrderDto> RecentSalesOrders { get; } = [];
    public ObservableCollection<MotorcycleModelDto> Models { get; } = [];

    public string SelectedInventoryImageSource => _imageResolver.ResolveForInventory(SelectedInventory);

    [ObservableProperty]
    public partial DealerInventoryDto? SelectedInventory { get; set; }

    [ObservableProperty]
    public partial string CustomerEmail { get; set; }

    [ObservableProperty]
    public partial string LicensePlate { get; set; }

    [ObservableProperty]
    public partial string VoucherCode { get; set; }

    [ObservableProperty]
    public partial string SelectedPaymentMethod { get; set; }

    [ObservableProperty]
    public partial string SaleNotes { get; set; }

    [ObservableProperty]
    public partial SalesOrderDto? LastCompletedSale { get; set; }

    [ObservableProperty]
    public partial double TierProgressRatio { get; set; }

    [ObservableProperty]
    public partial string NextTierHint { get; set; }

    // Receive new VIN stock fields
    [ObservableProperty]
    public partial MotorcycleModelDto? SelectedReceiveModel { get; set; }

    [ObservableProperty]
    public partial string ReceiveVin { get; set; }

    [ObservableProperty]
    public partial string ReceiveEngineNumber { get; set; }

    [ObservableProperty]
    public partial string ReceiveColorName { get; set; }

    [ObservableProperty]
    public partial int ReceiveManufactureYear { get; set; }

    [ObservableProperty]
    public partial decimal ReceiveListedPrice { get; set; }

    partial void OnSelectedInventoryChanged(DealerInventoryDto? value) =>
        OnPropertyChanged(nameof(SelectedInventoryImageSource));

    partial void OnLastCompletedSaleChanged(SalesOrderDto? value) => UpdateTierProgress();

    partial void OnReceiveVinChanged(string value)
    {
        var sanitized = Iso3779VinValidator.SanitizeLiveInput(value);
        if (!string.Equals(sanitized, value, StringComparison.Ordinal))
        {
            ReceiveVin = sanitized;
        }
    }

    protected override void OnCultureChanged(string cultureCode) => UpdateTierProgress();

    [RelayCommand]
    public async Task LoadSalesDeskAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var stockTask = _apiClient.ListInventoriesAsync(stockStatus: "Available", pageSize: 50);
            var ordersTask = _apiClient.ListDealerSalesOrdersAsync(pageSize: 30);
            var modelsTask = _apiClient.ListMotorcyclesAsync(pageSize: 50);
            var tiersTask = _apiClient.ListLoyaltyTiersAsync();

            await Task.WhenAll(stockTask, ordersTask, modelsTask, tiersTask).ConfigureAwait(false);

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

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                LoyaltyTiers.Clear();
                foreach (var tier in tiersData) LoyaltyTiers.Add(tier);

                AvailableStock.Clear();
                if (stockTask.Result.IsSuccess && stockTask.Result.Data is not null)
                {
                    foreach (var item in stockTask.Result.Data) AvailableStock.Add(item);
                }

                RecentSalesOrders.Clear();
                if (ordersTask.Result.IsSuccess && ordersTask.Result.Data is not null)
                {
                    foreach (var o in ordersTask.Result.Data) RecentSalesOrders.Add(o);
                }

                Models.Clear();
                if (modelsTask.Result.IsSuccess && modelsTask.Result.Data is not null)
                {
                    foreach (var m in modelsTask.Result.Data) Models.Add(m);
                }

                if (SelectedInventory is null && AvailableStock.Count > 0)
                {
                    SelectedInventory = AvailableStock[0];
                }

                if (SelectedReceiveModel is null && Models.Count > 0)
                {
                    SelectedReceiveModel = Models[0];
                    ReceiveListedPrice = Models[0].BaseRetailPrice;
                }

                UpdateTierProgress();
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private void UpdateTierProgress()
    {
        var points = LastCompletedSale?.LifetimeQualifyingPointsAfter ?? 0;
        var (progressRatio, nextTierHint) = TwoStepLoyaltyPreviewCalculator.CalculateTierProgress(points, LoyaltyTiers);
        TierProgressRatio = progressRatio;
        NextTierHint = nextTierHint;
    }

    [RelayCommand]
    private async Task ScanVinForReceiveAsync()
    {
        var raw = await _navigationService.RequestBarcodeScanAsync(Loc.SalesScanIntakePrompt).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            ReceiveVin = Iso3779VinValidator.SanitizeLiveInput(raw);
            _hapticService.Perform(HapticKind.Success);
        }
    }

    [RelayCommand]
    public async Task ReceiveVinIntoStockAsync()
    {
        ClearMessages();
        var vinCheck = Iso3779VinValidator.Validate(ReceiveVin);
        if (!vinCheck.IsValid)
        {
            ErrorMessage = vinCheck.ErrorMessage ?? Loc.Val_VinInvalidFallback;
            return;
        }

        if (SelectedReceiveModel is null || string.IsNullOrWhiteSpace(ReceiveEngineNumber))
        {
            ErrorMessage = Loc.SalesModelEngineRequiredError;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiClient.CreateInventoryAsync(new CreateDealerInventoryRequest
            {
                MotorcycleModelId = SelectedReceiveModel.Id,
                Vin = vinCheck.NormalizedVin,
                EngineNumber = ReceiveEngineNumber.Trim().ToUpperInvariant(),
                ColorName = string.IsNullOrWhiteSpace(ReceiveColorName) ? Loc.Common_FactoryStandard : ReceiveColorName.Trim(),
                ManufactureYear = ReceiveManufactureYear,
                ListedPrice = ReceiveListedPrice > 0m ? ReceiveListedPrice : SelectedReceiveModel.BaseRetailPrice
            }).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                return;
            }

            ReceiveVin = string.Empty;
            ReceiveEngineNumber = string.Empty;
            SuccessMessage = Loc.Format("SalesVinReceivedSuccess", result.Data.Vin);
            _hapticService.Perform(HapticKind.Success);
            BroadcastSelf(() => _messenger.Send(new VehicleMutatedMessage(result.Data.Vin)));
            await _feedbackService.ShowToastAsync(Loc.Format("SalesVinReceivedToast", result.Data.Vin)).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadSalesDeskAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task CompleteShowroomSaleAsync()
    {
        ClearMessages();
        var input = new SalesOrderInput(
            DealerInventoryId: SelectedInventory?.Id ?? Guid.Empty,
            CustomerEmail: CustomerEmail,
            PaymentMethod: SelectedPaymentMethod,
            LicensePlate: LicensePlate,
            VoucherCode: VoucherCode,
            Notes: SaleNotes);

        var validation = _salesOrderValidator.Validate(input);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.Errors[0].ErrorMessage;
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiClient.CreateSalesOrderAsync(new CreateSalesOrderRequest
            {
                CustomerEmail = CustomerEmail.Trim(),
                DealerInventoryId = SelectedInventory!.Id,
                InventoryRowVersion = SelectedInventory.RowVersion,
                VoucherCode = string.IsNullOrWhiteSpace(VoucherCode) ? null : VoucherCode.Trim(),
                LicensePlate = string.IsNullOrWhiteSpace(LicensePlate) ? null : LicensePlate.Trim(),
                PaymentMethod = SelectedPaymentMethod,
                Notes = string.IsNullOrWhiteSpace(SaleNotes) ? null : SaleNotes.Trim()
            }).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                _hapticService.Perform(HapticKind.Warning);
                return;
            }

            LastCompletedSale = result.Data;
            SuccessMessage = Loc.Format("SalesOrderCompletedSuccess", result.Data.OrderNumber, result.Data.Vin, result.Data.PointsEarned);
            CustomerEmail = string.Empty;
            LicensePlate = string.Empty;
            VoucherCode = string.Empty;
            SaleNotes = string.Empty;
            _hapticService.Perform(HapticKind.Success);
            BroadcastSelf(() =>
            {
                _messenger.Send(new VehicleMutatedMessage(result.Data.Vin));
                _messenger.Send(new WalletMutatedMessage($"SalesOrder:{result.Data.OrderNumber}"));
            });
            await _feedbackService.ShowSnackbarAsync(Loc.Format("SalesOrderCompletedSnackbar", result.Data.OrderNumber, result.Data.PointsEarned)).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadSalesDeskAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task ShareSalesReceiptAsync(SalesOrderDto? order)
    {
        var target = order ?? LastCompletedSale;
        if (target is null)
        {
            return;
        }

        var receiptRes = await _apiClient.GetSalesOrderReceiptAsync(target.SalesOrderId).ConfigureAwait(false);
        if (receiptRes.IsSuccess && receiptRes.Data is not null)
        {
            var receipt = receiptRes.Data;
            await _offlineDb.SaveSalesReceiptAsync(_sessionState.OwnerScopeKey, receipt).ConfigureAwait(false);
            var text = Loc.Format(
                "SalesReceiptShareBody",
                receipt.ReceiptNumber,
                receipt.Dealer.Name,
                receipt.Customer.FullName,
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
        else
        {
            ErrorMessage = FormatApiError(receiptRes);
        }
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
        if (_isSelfBroadcasting || !_sessionState.IsAuthenticated || !_sessionState.IsDealerStaff || IsBusy)
        {
            return;
        }

        try
        {
            await LoadSalesDeskAsync().ConfigureAwait(false);
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
