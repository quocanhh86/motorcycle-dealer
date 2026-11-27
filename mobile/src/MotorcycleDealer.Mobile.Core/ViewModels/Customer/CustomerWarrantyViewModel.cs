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

namespace MotorcycleDealer.Mobile.Core.ViewModels.Customer;

public sealed partial class CustomerWarrantyViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly IOfflineOutboxSyncService _outboxSyncService;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IValidator<WarrantyClaimInput> _warrantyValidator;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private readonly IMotorcycleImageResolver _imageResolver;
    private readonly Dictionary<Guid, MotorcycleModelDto> _modelsById = [];
    private bool _isSelfBroadcasting;

    public CustomerWarrantyViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        IConnectivityMonitor connectivityMonitor,
        IOfflineOutboxSyncService outboxSyncService,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        IValidator<WarrantyClaimInput> warrantyValidator,
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
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _warrantyValidator = warrantyValidator ?? throw new ArgumentNullException(nameof(warrantyValidator));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _imageResolver = imageResolver ?? MotorcycleImageResolver.Current;

        SelectedDefectCategory = "Engine";
        SymptomDescription = string.Empty;
        WarrantyEligibilitySummary = Loc.WarrantySelectVehiclePrompt;

        _messenger.Register<CustomerWarrantyViewModel, VehicleMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<CustomerWarrantyViewModel, WarrantyMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public IReadOnlyList<string> DefectCategories { get; } =
        ["Engine", "Transmission", "Electrical", "BrakesAndSuspension", "FrameAndPaint", "ExhaustAndFuel"];

    public ObservableCollection<WarrantyClaimDto> Claims { get; } = [];
    public ObservableCollection<CustomerVehicleDto> Vehicles { get; } = [];
    public ObservableCollection<DealerDto> Dealers { get; } = [];

    public string SelectedVehicleImageSource => _imageResolver.ResolveForVehicle(SelectedVehicle);

    [ObservableProperty]
    public partial CustomerVehicleDto? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial DealerDto? SelectedDealer { get; set; }

    [ObservableProperty]
    public partial int OdometerAtClaimKm { get; set; }

    [ObservableProperty]
    public partial string SelectedDefectCategory { get; set; }

    [ObservableProperty]
    public partial string SymptomDescription { get; set; }

    [ObservableProperty]
    public partial bool IsWithinOemCoverage { get; set; }

    [ObservableProperty]
    public partial int ActiveWarrantyMonths { get; set; }

    [ObservableProperty]
    public partial int ActiveWarrantyMileageKm { get; set; }

    [ObservableProperty]
    public partial string WarrantyEligibilitySummary { get; set; }

    partial void OnSelectedVehicleChanged(CustomerVehicleDto? value)
    {
        if (value is not null)
        {
            OdometerAtClaimKm = value.CurrentMileageKm;
        }

        OnPropertyChanged(nameof(SelectedVehicleImageSource));
        RecomputeCoverageIndicator();
    }

    partial void OnOdometerAtClaimKmChanged(int value) => RecomputeCoverageIndicator();

    protected override void OnCultureChanged(string cultureCode) => RecomputeCoverageIndicator();

    [RelayCommand]
    public async Task LoadWarrantyDataAsync()
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
            var claimsTask = _apiClient.ListMyWarrantyClaimsAsync(pageNumber: 1, pageSize: 30);
            var vehiclesTask = _apiClient.ListMyVehiclesAsync();
            var dealersTask = _apiClient.ListDealersAsync();
            var modelsTask = _apiClient.ListMotorcyclesAsync(pageNumber: 1, pageSize: 50);

            await Task.WhenAll(claimsTask, vehiclesTask, dealersTask, modelsTask).ConfigureAwait(false);

            var vehicles = vehiclesTask.Result.IsSuccess && vehiclesTask.Result.Data is not null
                ? vehiclesTask.Result.Data
                : await _offlineDb.GetCachedCustomerVehiclesAsync(scope).ConfigureAwait(false);

            var dealers = dealersTask.Result.IsSuccess && dealersTask.Result.Data is not null
                ? dealersTask.Result.Data
                : await _offlineDb.GetCachedDealersAsync().ConfigureAwait(false);

            IReadOnlyList<MotorcycleModelDto> models;
            if (modelsTask.Result.IsSuccess && modelsTask.Result.Data is not null)
            {
                models = modelsTask.Result.Data;
                await _offlineDb.SaveMotorcycleModelsAsync(models).ConfigureAwait(false);
            }
            else
            {
                models = await _offlineDb.GetCachedMotorcycleModelsAsync().ConfigureAwait(false);
            }

            var modelLookup = new Dictionary<Guid, MotorcycleModelDto>(models.Count);
            foreach (var model in models)
            {
                modelLookup[model.Id] = model;
            }

            foreach (var vehicle in vehicles)
            {
                if (vehicle.MotorcycleModelId != Guid.Empty && !modelLookup.ContainsKey(vehicle.MotorcycleModelId))
                {
                    var singleModelRes = await _apiClient.GetMotorcycleByIdAsync(vehicle.MotorcycleModelId)
                        .ConfigureAwait(false);
                    if (singleModelRes.IsSuccess && singleModelRes.Data is not null)
                    {
                        modelLookup[singleModelRes.Data.Id] = singleModelRes.Data;
                    }
                }
            }

            IsOfflineBannerVisible = claimsTask.Result.IsNetworkError;

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                _modelsById.Clear();
                foreach (var kv in modelLookup)
                {
                    _modelsById[kv.Key] = kv.Value;
                }

                Claims.Clear();
                if (claimsTask.Result.IsSuccess && claimsTask.Result.Data is not null)
                {
                    foreach (var c in claimsTask.Result.Data) Claims.Add(c);
                }

                Vehicles.Clear();
                foreach (var v in vehicles) Vehicles.Add(v);

                Dealers.Clear();
                foreach (var d in dealers) Dealers.Add(d);

                if (SelectedVehicle is null && Vehicles.Count > 0)
                {
                    SelectedVehicle = Vehicles[0];
                }

                if (SelectedDealer is null && Dealers.Count > 0)
                {
                    SelectedDealer = Dealers[0];
                }

                RecomputeCoverageIndicator();
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
    public async Task SubmitWarrantyClaimAsync()
    {
        ClearMessages();
        var input = new WarrantyClaimInput(
            CustomerVehicleId: SelectedVehicle?.Id ?? Guid.Empty,
            DealerId: SelectedDealer?.Id ?? Guid.Empty,
            OdometerAtClaimKm: OdometerAtClaimKm,
            DefectCategory: SelectedDefectCategory,
            SymptomDescription: SymptomDescription);

        var validation = _warrantyValidator.Validate(input);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.Errors[0].ErrorMessage;
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        var request = new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = SelectedVehicle!.Id,
            DealerId = SelectedDealer!.Id,
            OdometerAtClaimKm = OdometerAtClaimKm,
            DefectCategory = SelectedDefectCategory,
            SymptomDescription = SymptomDescription.Trim()
        };

        if (!_connectivityMonitor.IsConnected)
        {
            await _outboxSyncService.EnqueueWarrantyClaimSubmitAsync(_sessionState.OwnerScopeKey, request)
                .ConfigureAwait(false);
            SymptomDescription = string.Empty;
            SuccessMessage = Loc.WarrantyQueuedOffline;
            await _feedbackService.ShowSnackbarAsync(SuccessMessage).ConfigureAwait(false);
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiClient.CreateWarrantyClaimAsync(request).ConfigureAwait(false);
            if (result.IsSuccess && result.Data is not null)
            {
                SymptomDescription = string.Empty;
                SuccessMessage = Loc.Format("WarrantySubmittedSuccess", result.Data.ClaimNumber, result.Data.DealerName);
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

                await _feedbackService.ShowToastAsync(Loc.Format("WarrantySubmittedToast", result.Data.ClaimNumber)).ConfigureAwait(false);
            }
            else if (result.IsNetworkError)
            {
                await _outboxSyncService.EnqueueWarrantyClaimSubmitAsync(_sessionState.OwnerScopeKey, request)
                    .ConfigureAwait(false);
                SuccessMessage = Loc.WarrantyQueuedDropped;
                await _feedbackService.ShowSnackbarAsync(SuccessMessage).ConfigureAwait(false);
            }
            else
            {
                ErrorMessage = FormatApiError(result);
                _hapticService.Perform(HapticKind.Warning);
                return;
            }
        }
        finally
        {
            IsBusy = false;
        }

        await LoadWarrantyDataAsync().ConfigureAwait(false);
    }

    private void RecomputeCoverageIndicator()
    {
        if (SelectedVehicle is null)
        {
            ActiveWarrantyMonths = 0;
            ActiveWarrantyMileageKm = 0;
            IsWithinOemCoverage = false;
            WarrantyEligibilitySummary = Loc.WarrantySelectVehiclePrompt;
            return;
        }

        if (!_modelsById.TryGetValue(SelectedVehicle.MotorcycleModelId, out var model) ||
            model.WarrantyMonths <= 0 ||
            model.WarrantyMileageKm <= 0)
        {
            ActiveWarrantyMonths = 0;
            ActiveWarrantyMileageKm = 0;
            IsWithinOemCoverage = false;
            WarrantyEligibilitySummary = Loc.WarrantyCheckingCatalog;
            return;
        }

        ActiveWarrantyMonths = model.WarrantyMonths;
        ActiveWarrantyMileageKm = model.WarrantyMileageKm;

        var expiresAt = SelectedVehicle.OwnershipStartDateUtc.AddMonths(model.WarrantyMonths);
        var withinTime = DateTimeOffset.UtcNow <= expiresAt;
        var withinKm = OdometerAtClaimKm <= model.WarrantyMileageKm;
        IsWithinOemCoverage = withinTime && withinKm;

        WarrantyEligibilitySummary = IsWithinOemCoverage
            ? Loc.Format("WarrantyEligibleFormat", VietnamFormatting.FormatVietnamDate(expiresAt), VietnamFormatting.FormatMileageKm(model.WarrantyMileageKm), model.WarrantyMonths)
            : Loc.Format("WarrantyOutsideLimitFormat", model.WarrantyMonths, VietnamFormatting.FormatMileageKm(model.WarrantyMileageKm));
    }

    private async void TriggerCrossTabRefresh()
    {
        if (_isSelfBroadcasting || !_sessionState.IsAuthenticated || !_sessionState.IsCustomer || IsBusy)
        {
            return;
        }

        try
        {
            await LoadWarrantyDataAsync().ConfigureAwait(false);
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
