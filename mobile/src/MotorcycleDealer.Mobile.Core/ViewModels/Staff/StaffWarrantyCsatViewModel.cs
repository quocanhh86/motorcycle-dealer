using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Staff;

public sealed partial class StaffWarrantyCsatViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private bool _isSelfBroadcasting;

    public StaffWarrantyCsatViewModel(
        IDealerMobileApiClient apiClient,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        IMessenger messenger,
        IUserFeedbackService feedbackService,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));

        SelectedTab = "Warranty";
        ResolutionNotesInput = string.Empty;
        RejectionReasonInput = string.Empty;
        StaffReviewReplyInput = string.Empty;

        _messenger.Register<StaffWarrantyCsatViewModel, WarrantyMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public ObservableCollection<WarrantyClaimDto> WarrantyClaims { get; } = [];
    public ObservableCollection<ServiceReviewDto> ServiceReviews { get; } = [];
    public ObservableCollection<SparePartDto> SpareParts { get; } = [];

    [ObservableProperty]
    public partial string SelectedTab { get; set; }

    [ObservableProperty]
    public partial WarrantyClaimDto? SelectedClaim { get; set; }

    [ObservableProperty]
    public partial SparePartDto? SelectedCoveredPart { get; set; }

    [ObservableProperty]
    public partial decimal CoveredAmountVnd { get; set; }

    [ObservableProperty]
    public partial string ResolutionNotesInput { get; set; }

    [ObservableProperty]
    public partial string RejectionReasonInput { get; set; }

    [ObservableProperty]
    public partial ServiceReviewDto? SelectedReview { get; set; }

    [ObservableProperty]
    public partial string StaffReviewReplyInput { get; set; }

    [ObservableProperty]
    public partial decimal AverageCsatRating { get; set; }

    [ObservableProperty]
    public partial int UnansweredReviewCount { get; set; }

    [ObservableProperty]
    public partial bool UnansweredReviewsOnly { get; set; }

    public string AverageCsatDisplay => Loc.Format("Common_RatingScore", AverageCsatRating);
    public string ReplyingToHeading => SelectedReview is null
        ? string.Empty
        : Loc.Format("StaffQaReplyingToFormat", SelectedReview.ReviewNumber);

    partial void OnAverageCsatRatingChanged(decimal value) => OnPropertyChanged(nameof(AverageCsatDisplay));
    partial void OnSelectedReviewChanged(ServiceReviewDto? value) => OnPropertyChanged(nameof(ReplyingToHeading));

    protected override void OnCultureChanged(string cultureCode)
    {
        var selectedClaim = SelectedClaim;
        var selectedReview = SelectedReview;
        var selectedPart = SelectedCoveredPart;

        RefreshCollection(WarrantyClaims);
        RefreshCollection(ServiceReviews);
        RefreshCollection(SpareParts);

        SelectedClaim = selectedClaim;
        SelectedReview = selectedReview;
        SelectedCoveredPart = selectedPart;

        OnPropertyChanged(nameof(AverageCsatDisplay));
        OnPropertyChanged(nameof(ReplyingToHeading));
    }

    [RelayCommand]
    public async Task LoadDeskAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var claimsTask = _apiClient.ListDealerWarrantyClaimsAsync(pageSize: 40);
            var reviewsTask = _apiClient.ListDealerServiceReviewsAsync(
                unansweredOnly: UnansweredReviewsOnly ? true : null,
                pageSize: 40);
            var partsTask = _apiClient.ListSparePartsAsync(pageSize: 60);

            await Task.WhenAll(claimsTask, reviewsTask, partsTask).ConfigureAwait(false);

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                WarrantyClaims.Clear();
                if (claimsTask.Result.IsSuccess && claimsTask.Result.Data is not null)
                {
                    foreach (var c in claimsTask.Result.Data) WarrantyClaims.Add(c);
                }

                ServiceReviews.Clear();
                if (reviewsTask.Result.IsSuccess && reviewsTask.Result.Data is not null)
                {
                    AverageCsatRating = reviewsTask.Result.Data.AverageRating;
                    UnansweredReviewCount = reviewsTask.Result.Data.UnansweredCount;
                    foreach (var r in reviewsTask.Result.Data.Items) ServiceReviews.Add(r);
                }

                SpareParts.Clear();
                if (partsTask.Result.IsSuccess && partsTask.Result.Data is not null)
                {
                    foreach (var p in partsTask.Result.Data) SpareParts.Add(p);
                }

                if (SelectedClaim is null && WarrantyClaims.Count > 0)
                {
                    SelectedClaim = WarrantyClaims[0];
                }

                if (SelectedReview is null && ServiceReviews.Count > 0)
                {
                    SelectedReview = ServiceReviews.FirstOrDefault(r => !r.IsAnswered) ?? ServiceReviews[0];
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
    private void SelectTab(string tab) => SelectedTab = string.IsNullOrWhiteSpace(tab) ? "Warranty" : tab;

    [RelayCommand]
    public Task MoveClaimUnderReviewAsync(WarrantyClaimDto? claim) =>
        TransitionClaimAsync(claim, "UnderReview");

    [RelayCommand]
    public Task ApproveClaimAsync(WarrantyClaimDto? claim) =>
        TransitionClaimAsync(claim, "Approved");

    [RelayCommand]
    public Task CompleteClaimAsync(WarrantyClaimDto? claim) =>
        TransitionClaimAsync(claim, "Completed");

    [RelayCommand]
    public Task RejectClaimAsync(WarrantyClaimDto? claim) =>
        TransitionClaimAsync(claim, "Rejected");

    [RelayCommand]
    public async Task RespondToReviewAsync()
    {
        if (SelectedReview is null)
        {
            ErrorMessage = Loc.StaffQaSelectReviewError;
            return;
        }

        ClearMessages();
        if (string.IsNullOrWhiteSpace(StaffReviewReplyInput) || StaffReviewReplyInput.Trim().Length < 5)
        {
            ErrorMessage = Loc.StaffQaReplyMinLengthError;
            return;
        }

        var result = await _apiClient.RespondToServiceReviewAsync(
            SelectedReview.ReviewId,
            new RespondToServiceReviewRequest
            {
                StaffResponse = StaffReviewReplyInput.Trim(),
                RowVersion = SelectedReview.RowVersion
            }).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        StaffReviewReplyInput = string.Empty;
        SuccessMessage = Loc.Format("StaffQaReplyPostedSuccess", result.Data.ReviewNumber);
        _hapticService.Perform(HapticKind.Success);
        await _feedbackService.ShowToastAsync(Loc.Format("StaffQaReplyPostedToast", result.Data.ReviewNumber)).ConfigureAwait(false);
        await LoadDeskAsync().ConfigureAwait(false);
    }

    private async Task TransitionClaimAsync(WarrantyClaimDto? claim, string nextStatus)
    {
        var target = claim ?? SelectedClaim;
        if (target is null)
        {
            return;
        }

        ClearMessages();
        var result = await _apiClient.UpdateWarrantyClaimStatusAsync(
            target.ClaimId,
            new UpdateWarrantyClaimStatusRequest
            {
                NewStatus = nextStatus,
                CoveredSparePartId = SelectedCoveredPart?.Id ?? target.CoveredSparePartId,
                CoveredAmountVnd = CoveredAmountVnd > 0m ? CoveredAmountVnd : (SelectedCoveredPart?.UnitPriceVnd ?? target.CoveredAmountVnd),
                ResolutionNotes = string.IsNullOrWhiteSpace(ResolutionNotesInput) ? null : ResolutionNotesInput.Trim(),
                RejectionReason = nextStatus == "Rejected"
                    ? (string.IsNullOrWhiteSpace(RejectionReasonInput) ? Loc.StaffQaDefaultRejectionReason : RejectionReasonInput.Trim())
                    : null,
                RowVersion = target.RowVersion
            }).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        ResolutionNotesInput = string.Empty;
        RejectionReasonInput = string.Empty;
        var statusLabel = Loc.GetEnumLabel("WarrantyStatus", result.Data.Status);
        SuccessMessage = Loc.Format("StaffQaClaimUpdatedSuccess", result.Data.ClaimNumber, statusLabel);
        _hapticService.Perform(HapticKind.Success);
        _isSelfBroadcasting = true;
        try
        {
            _messenger.Send(new WarrantyMutatedMessage(result.Data.ClaimId));
        }
        finally
        {
            _isSelfBroadcasting = false;
        }

        await _feedbackService.ShowToastAsync(Loc.Format("StaffQaClaimUpdatedToast", result.Data.ClaimNumber, statusLabel)).ConfigureAwait(false);
        await LoadDeskAsync().ConfigureAwait(false);
    }

    private async void TriggerCrossTabRefresh()
    {
        if (_isSelfBroadcasting || IsBusy)
        {
            return;
        }

        try
        {
            await LoadDeskAsync().ConfigureAwait(false);
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
