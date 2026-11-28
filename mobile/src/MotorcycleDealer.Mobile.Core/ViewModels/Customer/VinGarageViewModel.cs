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
using MotorcycleDealer.Mobile.Core.ViewModels.Public;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Customer;

public sealed partial class VinGarageViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly IOfflineOutboxSyncService _outboxSyncService;
    private readonly INavigationService _navigationService;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IValidator<RegisterVehicleInput> _registerVehicleValidator;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private readonly IMotorcycleImageResolver _imageResolver;

    private DateTimeOffset? _lastSyncUtc;

    public VinGarageViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        IConnectivityMonitor connectivityMonitor,
        IOfflineOutboxSyncService outboxSyncService,
        INavigationService navigationService,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        IValidator<RegisterVehicleInput> registerVehicleValidator,
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
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _registerVehicleValidator = registerVehicleValidator ?? throw new ArgumentNullException(nameof(registerVehicleValidator));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _imageResolver = imageResolver ?? MotorcycleImageResolver.Current;

        NewVehicleVin = string.Empty;
        NewLicensePlate = string.Empty;
        NewColorName = string.Empty;
        VinCounterLabel = "0/17";
        VinValidationMessage = string.Empty;
        SelectedVehicleAngleKey = "front";

        _messenger.Register<VinGarageViewModel, BookingMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public ObservableCollection<CustomerVehicleDto> Vehicles { get; } = [];
    public ObservableCollection<MotorcycleModelDto> AvailableModels { get; } = [];
    public ObservableCollection<MaintenanceReminderDto> SelectedVehicleReminders { get; } = [];
    public ObservableCollection<ShowroomGalleryThumb> SelectedVehicleGallery { get; } = [];

    public string SelectedVehicleImageSource =>
        GetActiveVehicleThumb()?.ImageSource ?? _imageResolver.ResolveForVehicle(SelectedVehicle);
    public bool HasMultipleVehicleGalleryAngles => SelectedVehicleGallery.Count > 1;
    public string SelectedVehicleGalleryCounter
    {
        get
        {
            if (SelectedVehicleGallery.Count == 0)
            {
                return string.Empty;
            }
            var active = GetActiveVehicleThumb();
            return $"{(active?.Index ?? 0) + 1} / {SelectedVehicleGallery.Count}";
        }
    }
    public string SelectedVehicleAngleLabel => GetActiveVehicleThumb()?.Label ?? Loc.ShowroomAngleFront;
    public double SelectedVehicleWearRatio
    {
        get
        {
            if (SelectedVehicle is null || SelectedVehicle.NextDueMileageKm <= 0)
            {
                return 0.08d;
            }

            int span = Math.Max(1, SelectedVehicle.NextDueMileageKm - SelectedVehicle.LastServiceMileageKm);
            int covered = Math.Max(0, SelectedVehicle.CurrentMileageKm - SelectedVehicle.LastServiceMileageKm);
            return Math.Clamp((double)covered / span, 0.05d, 1.0d);
        }
    }
    public string OfflineGarageBannerText => Loc.Format("GarageOfflineBanner", SyncAgeLabel);

    [ObservableProperty]
    public partial string SelectedVehicleAngleKey { get; set; }

    [ObservableProperty]
    public partial CustomerVehicleDto? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial VehicleMaintenanceScheduleDto? SelectedSchedule { get; set; }

    [ObservableProperty]
    public partial string NewVehicleVin { get; set; }

    [ObservableProperty]
    public partial MotorcycleModelDto? SelectedRegistrationModel { get; set; }

    [ObservableProperty]
    public partial int NewCurrentMileageKm { get; set; }

    [ObservableProperty]
    public partial string NewLicensePlate { get; set; }

    [ObservableProperty]
    public partial string NewColorName { get; set; }

    [ObservableProperty]
    public partial string VinCounterLabel { get; set; }

    [ObservableProperty]
    public partial bool IsVinValid { get; set; }

    [ObservableProperty]
    public partial string VinValidationMessage { get; set; }

    [ObservableProperty]
    public partial int UpdatedOdometerKm { get; set; }

    partial void OnSelectedVehicleChanged(CustomerVehicleDto? value)
    {
        RebuildSelectedVehicleGallery();
        OnPropertyChanged(nameof(SelectedVehicleWearRatio));
    }

    partial void OnSelectedVehicleAngleKeyChanged(string value) => NotifyVehicleGalleryChanged();

    partial void OnNewVehicleVinChanged(string value)
    {
        var sanitized = Iso3779VinValidator.SanitizeLiveInput(value);
        if (!string.Equals(sanitized, value, StringComparison.Ordinal))
        {
            NewVehicleVin = sanitized;
            return;
        }

        RefreshVinValidation(sanitized);
    }

    protected override void OnCultureChanged(string cultureCode)
    {
        if (!string.IsNullOrEmpty(NewVehicleVin))
        {
            RefreshVinValidation(NewVehicleVin);
        }

        if (_lastSyncUtc.HasValue || IsOfflineBannerVisible)
        {
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
        }

        var selectedVehicle = SelectedVehicle;
        var selectedModel = SelectedRegistrationModel;
        var savedOdometer = UpdatedOdometerKm;
        RefreshCollection(Vehicles);
        RefreshCollection(AvailableModels);
        RefreshCollection(SelectedVehicleReminders);
        SelectedVehicle = selectedVehicle;
        SelectedRegistrationModel = selectedModel;
        UpdatedOdometerKm = savedOdometer;

        foreach (var thumb in SelectedVehicleGallery)
        {
            thumb.Label = Loc.GetString(thumb.LabelResourceKey);
        }

        NotifyVehicleGalleryChanged();
        OnPropertyChanged(nameof(OfflineGarageBannerText));
    }

    [RelayCommand]
    public async Task LoadGarageAsync()
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
            var modelsTask = _apiClient.ListMotorcyclesAsync(pageNumber: 1, pageSize: 50);
            await Task.WhenAll(vehiclesTask, modelsTask).ConfigureAwait(false);

            IReadOnlyList<CustomerVehicleDto> vehicles;
            if (vehiclesTask.Result.IsSuccess && vehiclesTask.Result.Data is not null)
            {
                vehicles = vehiclesTask.Result.Data;
                await _offlineDb.SaveCustomerVehiclesAsync(scope, vehicles).ConfigureAwait(false);
                IsOfflineBannerVisible = false;
                _lastSyncUtc = DateTimeOffset.UtcNow;
                SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
            }
            else
            {
                vehicles = await _offlineDb.GetCachedCustomerVehiclesAsync(scope).ConfigureAwait(false);
                var sync = await _offlineDb.GetSyncStateAsync(scope, "vehicles").ConfigureAwait(false);
                IsOfflineBannerVisible = true;
                _lastSyncUtc = sync?.LastSyncedUtc;
                SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
            }

            OnPropertyChanged(nameof(OfflineGarageBannerText));

            IReadOnlyList<MotorcycleModelDto> models = modelsTask.Result.IsSuccess && modelsTask.Result.Data is not null
                ? modelsTask.Result.Data
                : await _offlineDb.GetCachedMotorcycleModelsAsync().ConfigureAwait(false);

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                Vehicles.Clear();
                foreach (var v in vehicles)
                {
                    Vehicles.Add(v);
                }

                AvailableModels.Clear();
                foreach (var m in models)
                {
                    AvailableModels.Add(m);
                }

                if (SelectedRegistrationModel is null && models.Count > 0)
                {
                    SelectedRegistrationModel = models[0];
                }

                if (SelectedVehicle is null && vehicles.Count > 0)
                {
                    SelectedVehicle = vehicles[0];
                    UpdatedOdometerKm = vehicles[0].CurrentMileageKm;
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);

            if (SelectedVehicle is not null)
            {
                await LoadVehicleScheduleAsync(SelectedVehicle).ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task LoadVehicleScheduleAsync(CustomerVehicleDto? vehicle)
    {
        if (vehicle is null)
        {
            return;
        }

        SelectedVehicle = vehicle;
        UpdatedOdometerKm = vehicle.CurrentMileageKm;

        var result = await _apiClient.GetVehicleMaintenanceScheduleAsync(vehicle.Id).ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
        {
            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                SelectedSchedule = result.Data;
                SelectedVehicleReminders.Clear();
                foreach (var r in result.Data.Reminders)
                {
                    SelectedVehicleReminders.Add(r);
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task ScanVinAsync()
    {
        var raw = await _navigationService
            .RequestBarcodeScanAsync(Loc.GarageScanPromptTitle, requireIso3779Vin: true)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var parsed = DealerQrPayloadParser.Parse(raw);
        NewVehicleVin = parsed.Kind == QrPayloadKind.Vin && !string.IsNullOrWhiteSpace(parsed.Vin)
            ? parsed.Vin
            : Iso3779VinValidator.SanitizeLiveInput(raw);
        _hapticService.Perform(HapticKind.Success);
    }

    [RelayCommand]
    public async Task RegisterExternalVehicleAsync()
    {
        ClearMessages();
        var input = new RegisterVehicleInput(
            Vin: NewVehicleVin,
            MotorcycleModelId: SelectedRegistrationModel?.Id ?? Guid.Empty,
            CurrentMileageKm: NewCurrentMileageKm,
            LicensePlate: NewLicensePlate,
            ColorName: NewColorName);

        var validation = _registerVehicleValidator.Validate(input);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.Errors[0].ErrorMessage;
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        var normalizedVin = Iso3779VinValidator.Validate(NewVehicleVin).NormalizedVin;
        IsBusy = true;
        try
        {
            var result = await _apiClient.RegisterExternalVehicleAsync(new RegisterExternalVehicleRequest
            {
                Vin = normalizedVin,
                MotorcycleModelId = SelectedRegistrationModel!.Id,
                CurrentMileageKm = NewCurrentMileageKm,
                LastServiceMileageKm = NewCurrentMileageKm,
                LicensePlate = string.IsNullOrWhiteSpace(NewLicensePlate) ? null : NewLicensePlate.Trim(),
                ColorName = string.IsNullOrWhiteSpace(NewColorName) ? null : NewColorName.Trim()
            }).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                _hapticService.Perform(HapticKind.Warning);
                return;
            }

            NewVehicleVin = string.Empty;
            NewLicensePlate = string.Empty;
            NewColorName = string.Empty;
            NewCurrentMileageKm = 0;
            SuccessMessage = Loc.Format("GarageVehicleAddedSuccess", result.Data.Vin);
            _hapticService.Perform(HapticKind.Success);
            _messenger.Send(new VehicleMutatedMessage(result.Data.Vin));
            await _feedbackService.ShowToastAsync(Loc.Format("GarageVehicleRegisteredToast", result.Data.Vin)).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadGarageAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task UpdateSelectedVehicleMileageAsync()
    {
        if (SelectedVehicle is null)
        {
            return;
        }

        ClearMessages();
        if (UpdatedOdometerKm < SelectedVehicle.CurrentMileageKm)
        {
            ErrorMessage = Loc.Format("GarageOdometerRegressionError", VietnamFormatting.FormatMileageKm(SelectedVehicle.CurrentMileageKm));
            return;
        }

        if (!_connectivityMonitor.IsConnected)
        {
            await _outboxSyncService.EnqueueVehicleMileageUpdateAsync(
                _sessionState.OwnerScopeKey,
                SelectedVehicle.Id,
                UpdatedOdometerKm).ConfigureAwait(false);
            SuccessMessage = Loc.GarageOdometerQueuedOffline;
            await _feedbackService.ShowSnackbarAsync(SuccessMessage).ConfigureAwait(false);
            return;
        }

        var result = await _apiClient.UpdateVehicleMileageAsync(
            SelectedVehicle.Id,
            new UpdateVehicleMileageRequest
            {
                CurrentMileageKm = UpdatedOdometerKm,
                NewMileageKm = UpdatedOdometerKm
            }).ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            SelectedVehicle = result.Data;
            SuccessMessage = Loc.Format("GarageOdometerUpdated", VietnamFormatting.FormatMileageKm(result.Data.CurrentMileageKm));
            _hapticService.Perform(HapticKind.Success);
            _messenger.Send(new VehicleMutatedMessage(result.Data.Vin));
            await _feedbackService.ShowToastAsync(SuccessMessage).ConfigureAwait(false);
            await LoadGarageAsync().ConfigureAwait(false);
        }
        else if (result.IsNetworkError)
        {
            await _outboxSyncService.EnqueueVehicleMileageUpdateAsync(
                _sessionState.OwnerScopeKey,
                SelectedVehicle.Id,
                UpdatedOdometerKm).ConfigureAwait(false);
            SuccessMessage = Loc.GarageOdometerQueuedDropped;
            await _feedbackService.ShowSnackbarAsync(SuccessMessage).ConfigureAwait(false);
        }
        else
        {
            ErrorMessage = FormatApiError(result);
        }
    }

    [RelayCommand]
    private void SelectVehicleGalleryAngle(ShowroomGalleryThumb? thumb)
    {
        if (thumb is not null && !string.IsNullOrWhiteSpace(thumb.AngleKey))
        {
            SelectedVehicleAngleKey = thumb.AngleKey;
        }
    }

    [RelayCommand]
    private void NextVehicleGalleryAngle() => StepVehicleGalleryAngle(1);

    [RelayCommand]
    private void PreviousVehicleGalleryAngle() => StepVehicleGalleryAngle(-1);

    private void StepVehicleGalleryAngle(int delta)
    {
        if (SelectedVehicleGallery.Count <= 1)
        {
            return;
        }

        var current = GetActiveVehicleThumb()?.Index ?? 0;
        var next = ((current + delta) % SelectedVehicleGallery.Count + SelectedVehicleGallery.Count) % SelectedVehicleGallery.Count;
        SelectedVehicleAngleKey = SelectedVehicleGallery[next].AngleKey;
    }

    private ShowroomGalleryThumb? GetActiveVehicleThumb()
    {
        if (SelectedVehicleGallery.Count == 0)
        {
            return null;
        }

        return SelectedVehicleGallery.FirstOrDefault(
                   g => string.Equals(g.AngleKey, SelectedVehicleAngleKey, StringComparison.OrdinalIgnoreCase))
               ?? SelectedVehicleGallery[0];
    }

    private void RebuildSelectedVehicleGallery()
    {
        SelectedVehicleGallery.Clear();
        if (SelectedVehicle is not null)
        {
            var resolved = MotorcycleImageResolver.ResolveModelGallery(SelectedVehicle.ModelCode, SelectedVehicle.ImageUrl);
            for (var i = 0; i < resolved.Count; i++)
            {
                var item = resolved[i];
                SelectedVehicleGallery.Add(new ShowroomGalleryThumb(
                    item.AngleKey,
                    item.ImageSource,
                    item.LabelResourceKey,
                    Loc.GetString(item.LabelResourceKey),
                    i));
            }
        }

        SelectedVehicleAngleKey = SelectedVehicleGallery.FirstOrDefault()?.AngleKey ?? "front";
        NotifyVehicleGalleryChanged();
    }

    private void NotifyVehicleGalleryChanged()
    {
        OnPropertyChanged(nameof(SelectedVehicleImageSource));
        OnPropertyChanged(nameof(HasMultipleVehicleGalleryAngles));
        OnPropertyChanged(nameof(SelectedVehicleGalleryCounter));
        OnPropertyChanged(nameof(SelectedVehicleAngleLabel));
    }

    [RelayCommand]
    private Task BookServiceForSelectedVehicleAsync()
    {
        var parameters = SelectedVehicle is not null
            ? new Dictionary<string, object> { ["vehicleId"] = SelectedVehicle.Id.ToString("D") }
            : null;
        return _navigationService.GoToAsync(MobileRoutes.BookingWizard, parameters);
    }

    private void RefreshVinValidation(string sanitized)
    {
        var check = Iso3779VinValidator.Validate(sanitized);
        VinCounterLabel = Iso3779VinValidator.FormatLiveCounter(sanitized);
        IsVinValid = check.IsValid;
        VinValidationMessage = check.IsValid ? Loc.GarageValidVinMessage : (check.ErrorMessage ?? string.Empty);
    }

    private async void TriggerCrossTabRefresh()
    {
        if (!_sessionState.IsAuthenticated || !_sessionState.IsCustomer || IsBusy)
        {
            return;
        }

        try
        {
            await LoadGarageAsync().ConfigureAwait(false);
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
