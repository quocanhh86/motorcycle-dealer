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

namespace MotorcycleDealer.Mobile.Core.ViewModels.Staff;

public sealed partial class InvoiceLineDraft : ObservableObject
{
    public InvoiceLineDraft(
        Guid? sparePartId,
        string? partCode,
        string description,
        string itemType,
        int quantity,
        decimal unitPriceVnd)
    {
        SparePartId = sparePartId;
        PartCode = partCode;
        Description = description;
        ItemType = itemType;
        Quantity = Math.Max(1, quantity);
        UnitPriceVnd = Math.Max(0m, unitPriceVnd);
    }

    public Guid? SparePartId { get; }
    public string? PartCode { get; }
    public string Description { get; }
    public string ItemType { get; }

    [ObservableProperty]
    public partial int Quantity { get; set; }

    [ObservableProperty]
    public partial decimal UnitPriceVnd { get; set; }

    public decimal LineTotalVnd => Math.Round(Quantity * UnitPriceVnd, 2, MidpointRounding.AwayFromZero);
    public string FormattedLineTotal => VietnamFormatting.FormatVnd(LineTotalVnd);

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotalVnd));
        OnPropertyChanged(nameof(FormattedLineTotal));
    }

    partial void OnUnitPriceVndChanged(decimal value)
    {
        OnPropertyChanged(nameof(LineTotalVnd));
        OnPropertyChanged(nameof(FormattedLineTotal));
    }
}

/// <summary>
/// DealerStaff Mobile Invoice Builder: OEM SpareParts picker, QR voucher scanner,
/// and live two-step floor loyalty points &amp; tier upgrade preview before atomic settlement.
/// </summary>
public sealed partial class MobileInvoiceBuilderViewModel : MobileViewModelBase
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly INavigationService _navigationService;
    private readonly IShareService _shareService;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;

    public MobileInvoiceBuilderViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        INavigationService navigationService,
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
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));

        SelectedPaymentMethod = "BankTransfer";
        WorkSummaryNotes = string.Empty;
        CustomLaborDescription = Loc.InvoiceDefaultLaborDesc;
        VoucherCodeInput = string.Empty;
        CustomerTierCode = string.Empty;
        CustomerTierMultiplier = 1.00m;
        PreviewResultingTierCode = string.Empty;
        TierProgressRatio = 0d;
        NextTierHint = string.Empty;
        SettledReceiptQrPayload = string.Empty;
    }

    public IReadOnlyList<string> PaymentMethods { get; } = ["BankTransfer", "Cash", "Card", "EWallet"];

    public ObservableCollection<LoyaltyTierDto> LoyaltyTiers { get; } = [];
    public ObservableCollection<SparePartDto> CompatibleSpareParts { get; } = [];
    public ObservableCollection<InvoiceLineDraft> DraftLines { get; } = [];
    public ObservableCollection<CustomerVoucherDto> EligibleCustomerVouchers { get; } = [];

    [ObservableProperty]
    public partial BookingDto? ActiveBooking { get; set; }

    [ObservableProperty]
    public partial CustomerLoyaltyInspectionDto? CustomerLoyalty { get; set; }

    [ObservableProperty]
    public partial decimal BaseLaborAmountVnd { get; set; }

    [ObservableProperty]
    public partial int OdometerAtCompletionKm { get; set; }

    [ObservableProperty]
    public partial string SelectedPaymentMethod { get; set; }

    [ObservableProperty]
    public partial string WorkSummaryNotes { get; set; }

    [ObservableProperty]
    public partial string CustomLaborDescription { get; set; }

    [ObservableProperty]
    public partial decimal CustomLaborPriceVnd { get; set; }

    [ObservableProperty]
    public partial string VoucherCodeInput { get; set; }

    [ObservableProperty]
    public partial CustomerVoucherDto? AppliedVoucher { get; set; }

    [ObservableProperty]
    public partial Guid? ScannedVoucherId { get; set; }

    [ObservableProperty]
    public partial string? ScannedVoucherRowVersion { get; set; }

    // Customer tier inputs & live two-step floor preview outputs
    [ObservableProperty]
    public partial string CustomerTierCode { get; set; }

    [ObservableProperty]
    public partial decimal CustomerTierMultiplier { get; set; }

    [ObservableProperty]
    public partial int CustomerCurrentAvailablePoints { get; set; }

    [ObservableProperty]
    public partial int CustomerCurrentQualifyingPoints { get; set; }

    [ObservableProperty]
    public partial decimal PartsSubtotalVnd { get; set; }

    [ObservableProperty]
    public partial decimal LaborSubtotalVnd { get; set; }

    [ObservableProperty]
    public partial decimal GrossAmountVnd { get; set; }

    [ObservableProperty]
    public partial decimal VoucherDiscountVnd { get; set; }

    [ObservableProperty]
    public partial decimal NetPaidAmountVnd { get; set; }

    [ObservableProperty]
    public partial int PreviewBasePoints { get; set; }

    [ObservableProperty]
    public partial int PreviewPointsEarned { get; set; }

    [ObservableProperty]
    public partial string PreviewResultingTierCode { get; set; }

    [ObservableProperty]
    public partial bool PreviewTierUpgraded { get; set; }

    [ObservableProperty]
    public partial double TierProgressRatio { get; set; }

    [ObservableProperty]
    public partial string NextTierHint { get; set; }

    [ObservableProperty]
    public partial ServiceInvoiceDto? SettledInvoice { get; set; }

    [ObservableProperty]
    public partial string SettledReceiptQrPayload { get; set; }

    public string FormattedGrossAmount => VietnamFormatting.FormatVnd(GrossAmountVnd);
    public string FormattedDiscountAmount => VietnamFormatting.FormatVnd(VoucherDiscountVnd);
    public string FormattedNetPaidAmount => VietnamFormatting.FormatVnd(NetPaidAmountVnd);
    public string PreviewPointsLabel => Loc.Format("InvoicePreviewPointsFormat", PreviewPointsEarned);
    public string NextTierSummaryLabel => Loc.Format("InvoiceNextTierFormat", Loc.GetEnumLabel("LoyaltyTier", PreviewResultingTierCode));
    public string TierUpgradeBannerLabel => Loc.Format("InvoiceTierUpgradeFormat", Loc.GetEnumLabel("LoyaltyTier", PreviewResultingTierCode));
    public string EarnMultiplierBadgeLabel => Loc.Format("InvoiceEarnMultiplierBadge", CustomerTierMultiplier);

    partial void OnBaseLaborAmountVndChanged(decimal value) => RecalculateTotalsAndLoyaltyPreview();
    partial void OnAppliedVoucherChanged(CustomerVoucherDto? value) => RecalculateTotalsAndLoyaltyPreview();
    partial void OnCustomerTierCodeChanged(string value) => RecalculateTotalsAndLoyaltyPreview();
    partial void OnCustomerTierMultiplierChanged(decimal value)
    {
        OnPropertyChanged(nameof(EarnMultiplierBadgeLabel));
        RecalculateTotalsAndLoyaltyPreview();
    }

    protected override void OnCultureChanged(string cultureCode)
    {
        RecalculateTotalsAndLoyaltyPreview();
        OnPropertyChanged(nameof(EarnMultiplierBadgeLabel));
    }

    [RelayCommand]
    public async Task LoadForBookingAsync(Guid bookingId)
    {
        if (bookingId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        SettledInvoice = null;
        try
        {
            var bookingRes = await _apiClient.GetBookingByIdAsync(bookingId).ConfigureAwait(false);
            if (!bookingRes.IsSuccess || bookingRes.Data is null)
            {
                ErrorMessage = FormatApiError(bookingRes);
                return;
            }

            var booking = bookingRes.Data;
            ActiveBooking = booking;
            OdometerAtCompletionKm = booking.OdometerAtBookingKm;

            var partsTask = _apiClient.ListSparePartsAsync(
                compatibleModelId: booking.MotorcycleModelId != Guid.Empty ? booking.MotorcycleModelId : null,
                includeUniversal: true,
                pageSize: 60);
            var loyaltyTask = _apiClient.InspectCustomerLoyaltyAsync(booking.CustomerId);
            var tiersTask = _apiClient.ListLoyaltyTiersAsync();

            await Task.WhenAll(partsTask, loyaltyTask, tiersTask).ConfigureAwait(false);

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

                CompatibleSpareParts.Clear();
                if (partsTask.Result.IsSuccess && partsTask.Result.Data is not null)
                {
                    foreach (var p in partsTask.Result.Data) CompatibleSpareParts.Add(p);
                }

                EligibleCustomerVouchers.Clear();
                if (loyaltyTask.Result.IsSuccess && loyaltyTask.Result.Data is not null)
                {
                    var insp = loyaltyTask.Result.Data;
                    CustomerLoyalty = insp;
                    CustomerTierCode = insp.TierCode;
                    CustomerTierMultiplier = insp.PointsMultiplier;
                    CustomerCurrentAvailablePoints = insp.AvailablePoints;
                    CustomerCurrentQualifyingPoints = insp.LifetimeQualifyingPoints;

                    foreach (var v in insp.AvailableServiceVouchers)
                    {
                        EligibleCustomerVouchers.Add(v);
                    }

                    if (booking.AppliedCustomerVoucherId.HasValue)
                    {
                        AppliedVoucher = insp.EligibleVouchers
                            .Concat(insp.AvailableServiceVouchers)
                            .FirstOrDefault(v => v.Id == booking.AppliedCustomerVoucherId.Value);
                    }
                }

                RecalculateTotalsAndLoyaltyPreview();
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void AddSparePart(SparePartDto? part)
    {
        if (part is null)
        {
            return;
        }

        var existing = DraftLines.FirstOrDefault(l => l.SparePartId == part.Id);
        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            DraftLines.Add(new InvoiceLineDraft(
                sparePartId: part.Id,
                partCode: part.PartCode,
                description: part.Name,
                itemType: "Part",
                quantity: 1,
                unitPriceVnd: part.UnitPriceVnd));
        }

        _hapticService.Perform(HapticKind.Click);
        RecalculateTotalsAndLoyaltyPreview();
    }

    [RelayCommand]
    public void AddCustomLaborLine()
    {
        if (string.IsNullOrWhiteSpace(CustomLaborDescription) || CustomLaborPriceVnd <= 0m)
        {
            ErrorMessage = Loc.InvoiceLaborValidationError;
            return;
        }

        ClearMessages();
        DraftLines.Add(new InvoiceLineDraft(
            sparePartId: null,
            partCode: null,
            description: CustomLaborDescription.Trim(),
            itemType: "Labor",
            quantity: 1,
            unitPriceVnd: CustomLaborPriceVnd));

        CustomLaborPriceVnd = 0m;
        RecalculateTotalsAndLoyaltyPreview();
    }

    [RelayCommand]
    public void IncrementLineQuantity(InvoiceLineDraft? line)
    {
        if (line is null) return;
        line.Quantity++;
        RecalculateTotalsAndLoyaltyPreview();
    }

    [RelayCommand]
    public void DecrementLineQuantity(InvoiceLineDraft? line)
    {
        if (line is null) return;
        if (line.Quantity <= 1)
        {
            DraftLines.Remove(line);
        }
        else
        {
            line.Quantity--;
        }

        RecalculateTotalsAndLoyaltyPreview();
    }

    [RelayCommand]
    public void RemoveLine(InvoiceLineDraft? line)
    {
        if (line is null) return;
        DraftLines.Remove(line);
        RecalculateTotalsAndLoyaltyPreview();
    }

    [RelayCommand]
    public async Task ScanVoucherQrAsync()
    {
        var raw = await _navigationService.RequestBarcodeScanAsync(Loc.InvoiceScanVoucherPrompt).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        if (ApplyScannedVoucherQr(raw))
        {
            await _feedbackService.ShowToastAsync(Loc.Format("InvoiceVoucherAppliedToast", VoucherCodeInput)).ConfigureAwait(false);
        }
    }

    public bool ApplyScannedVoucherQr(string rawQrPayload)
    {
        ClearMessages();
        var parsed = DealerQrPayloadParser.Parse(rawQrPayload);
        if (parsed.Kind != QrPayloadKind.Voucher)
        {
            ErrorMessage = Loc.InvoiceInvalidVoucherQrError;
            return false;
        }

        VoucherCodeInput = parsed.VoucherCode ?? string.Empty;
        ScannedVoucherId = parsed.EntityId;
        ScannedVoucherRowVersion = parsed.RowVersion;

        var matched = EligibleCustomerVouchers.FirstOrDefault(v =>
            (parsed.EntityId.HasValue && v.Id == parsed.EntityId.Value) ||
            (!string.IsNullOrWhiteSpace(parsed.VoucherCode) &&
             string.Equals(v.VoucherCode, parsed.VoucherCode, StringComparison.OrdinalIgnoreCase)));

        if (matched is not null)
        {
            AppliedVoucher = matched;
        }

        SuccessMessage = Loc.Format("InvoiceVoucherAttachedSuccess", VoucherCodeInput);
        _hapticService.Perform(HapticKind.Success);
        RecalculateTotalsAndLoyaltyPreview();
        return true;
    }

    [RelayCommand]
    public void ClearVoucher()
    {
        AppliedVoucher = null;
        VoucherCodeInput = string.Empty;
        ScannedVoucherId = null;
        ScannedVoucherRowVersion = null;
        RecalculateTotalsAndLoyaltyPreview();
    }

    public void RecalculateTotalsAndLoyaltyPreview()
    {
        var partsSum = DraftLines
            .Where(l => string.Equals(l.ItemType, "Part", StringComparison.OrdinalIgnoreCase))
            .Sum(l => l.LineTotalVnd);

        var lineLaborSum = DraftLines
            .Where(l => string.Equals(l.ItemType, "Labor", StringComparison.OrdinalIgnoreCase))
            .Sum(l => l.LineTotalVnd);

        PartsSubtotalVnd = partsSum;
        LaborSubtotalVnd = Math.Max(0m, BaseLaborAmountVnd) + lineLaborSum;
        GrossAmountVnd = PartsSubtotalVnd + LaborSubtotalVnd;

        decimal discount = 0m;
        if (AppliedVoucher is not null && GrossAmountVnd >= AppliedVoucher.MinOrderAmount)
        {
            discount = TwoStepLoyaltyPreviewCalculator.CalculateVoucherDiscount(
                AppliedVoucher.DiscountType,
                AppliedVoucher.DiscountValue,
                AppliedVoucher.MaxDiscountAmount,
                GrossAmountVnd);
        }

        VoucherDiscountVnd = discount;
        NetPaidAmountVnd = Math.Max(0m, GrossAmountVnd - VoucherDiscountVnd);

        var preview = TwoStepLoyaltyPreviewCalculator.EvaluateAccrualPreview(
            netPaidVnd: NetPaidAmountVnd,
            currentAvailablePoints: CustomerCurrentAvailablePoints,
            currentLifetimeQualifyingPoints: CustomerCurrentQualifyingPoints,
            currentTierCode: CustomerTierCode,
            overrideMultiplier: CustomerTierMultiplier >= 1.00m ? CustomerTierMultiplier : null,
            tiers: LoyaltyTiers.Count > 0 ? LoyaltyTiers : null);

        PreviewBasePoints = preview.BasePoints;
        PreviewPointsEarned = preview.PointsEarned;
        PreviewResultingTierCode = preview.ResultingTierCode;
        PreviewTierUpgraded = preview.TierUpgraded;

        var (progressRatio, nextTierHint) = TwoStepLoyaltyPreviewCalculator.CalculateTierProgress(
            preview.LifetimeQualifyingPointsAfter,
            LoyaltyTiers);
        TierProgressRatio = progressRatio;
        NextTierHint = nextTierHint;

        OnPropertyChanged(nameof(FormattedGrossAmount));
        OnPropertyChanged(nameof(FormattedDiscountAmount));
        OnPropertyChanged(nameof(FormattedNetPaidAmount));
        OnPropertyChanged(nameof(PreviewPointsLabel));
        OnPropertyChanged(nameof(NextTierSummaryLabel));
        OnPropertyChanged(nameof(TierUpgradeBannerLabel));
    }

    [RelayCommand]
    public async Task SettleInvoiceAsync()
    {
        ClearMessages();
        if (ActiveBooking is null)
        {
            ErrorMessage = Loc.InvoiceNoBookingError;
            return;
        }

        RecalculateTotalsAndLoyaltyPreview();
        if (GrossAmountVnd <= 0m)
        {
            ErrorMessage = Loc.InvoiceEmptyAmountError;
            return;
        }

        IsBusy = true;
        try
        {
            var lineItems = DraftLines.Count > 0
                ? DraftLines.Select(l => new CreateServiceInvoiceLineItemRequest
                {
                    SparePartId = l.SparePartId,
                    ItemType = l.ItemType,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPriceVnd = l.UnitPriceVnd
                }).ToArray()
                : null;

            var request = new CreateServiceInvoiceRequest
            {
                ServiceBookingId = ActiveBooking.BookingId,
                BookingRowVersion = ActiveBooking.RowVersion,
                LaborAmount = LaborSubtotalVnd,
                PartsAmount = PartsSubtotalVnd,
                OdometerAtCompletionKm = Math.Max(ActiveBooking.OdometerAtBookingKm, OdometerAtCompletionKm),
                AppliedCustomerVoucherId = AppliedVoucher?.Id ?? ScannedVoucherId,
                VoucherCode = AppliedVoucher?.VoucherCode ?? (string.IsNullOrWhiteSpace(VoucherCodeInput) ? null : VoucherCodeInput.Trim()),
                CustomerVoucherRowVersion = AppliedVoucher?.RowVersion ?? ScannedVoucherRowVersion,
                UseBookingReservedVoucher = AppliedVoucher is null && ScannedVoucherId is null && string.IsNullOrWhiteSpace(VoucherCodeInput)
                    ? null
                    : false,
                PaymentMethod = SelectedPaymentMethod,
                WorkSummaryNotes = string.IsNullOrWhiteSpace(WorkSummaryNotes) ? null : WorkSummaryNotes.Trim(),
                LineItems = lineItems
            };

            var result = await _apiClient.CreateServiceInvoiceAsync(request).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                _hapticService.Perform(HapticKind.Warning);
                return;
            }

            SettledInvoice = result.Data;
            var tierLabel = Loc.GetEnumLabel("LoyaltyTier", result.Data.CurrentTierAfter);
            SuccessMessage = Loc.Format("InvoiceSettledSuccess", result.Data.InvoiceNumber, result.Data.PointsEarned, tierLabel);
            _hapticService.Perform(HapticKind.Success);
            _messenger.Send(new BookingMutatedMessage(result.Data.ServiceBookingId));
            _messenger.Send(new WalletMutatedMessage($"InvoiceSettled:{result.Data.InvoiceNumber}"));
            await _feedbackService.ShowSnackbarAsync(Loc.Format("InvoiceSettledSnackbar", result.Data.InvoiceNumber, result.Data.PointsEarned)).ConfigureAwait(false);

            var receiptRes = await _apiClient.GetServiceInvoiceReceiptAsync(result.Data.InvoiceId).ConfigureAwait(false);
            if (receiptRes.IsSuccess && receiptRes.Data is not null)
            {
                await _offlineDb.SaveServiceReceiptAsync(_sessionState.OwnerScopeKey, receiptRes.Data)
                    .ConfigureAwait(false);
                SettledReceiptQrPayload = DealerQrPayloadParser.BuildReceiptQrPayload(
                    receiptRes.Data.ReceiptNumber,
                    receiptRes.Data.VerificationCode,
                    receiptRes.Data.ReceiptType);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ShareSettledReceiptAsync()
    {
        if (SettledInvoice is null)
        {
            return;
        }

        var receipt = await _offlineDb.GetCachedServiceReceiptAsync(_sessionState.OwnerScopeKey, SettledInvoice.InvoiceId)
            .ConfigureAwait(false);
        var text = receipt is not null
            ? Loc.Format(
                "InvoiceShareReceiptBody",
                receipt.ReceiptNumber,
                receipt.Vehicle.Vin,
                VietnamFormatting.FormatVnd(receipt.NetPaidAmount),
                receipt.Loyalty.PointsEarned,
                receipt.VerificationCode)
            : Loc.Format(
                "InvoiceShareFallbackBody",
                SettledInvoice.InvoiceNumber,
                SettledInvoice.Vin,
                VietnamFormatting.FormatVnd(SettledInvoice.NetPaidAmount),
                SettledInvoice.PointsEarned);

        await _shareService.ShareTextAsync(
            Loc.Format("BookingReceiptShareTitle", SettledInvoice.InvoiceNumber),
            text).ConfigureAwait(false);
    }
}
