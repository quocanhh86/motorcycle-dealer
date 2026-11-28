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
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Public;

public sealed partial class ShowroomGalleryThumb : ObservableObject
{
    public ShowroomGalleryThumb(
        string angleKey,
        string imageSource,
        string labelResourceKey,
        string label,
        int index)
    {
        AngleKey = angleKey;
        ImageSource = imageSource;
        LabelResourceKey = labelResourceKey;
        Label = label;
        Index = index;
    }

    public string AngleKey { get; }
    public string ImageSource { get; }
    public string LabelResourceKey { get; }
    public int Index { get; }

    [ObservableProperty]
    public partial string Label { get; set; }
}

public sealed partial class ShowroomCatalogViewModel : MobileViewModelBase
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly INavigationService _navigationService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMotorcycleImageResolver _imageResolver;

    private DateTimeOffset? _lastSyncUtc;

    public ShowroomCatalogViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        INavigationService navigationService,
        IMainThreadDispatcher mainThreadDispatcher,
        IMotorcycleImageResolver? imageResolver = null,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _imageResolver = imageResolver ?? MotorcycleImageResolver.Current;

        SearchText = string.Empty;
        SelectedCategory = "All";
        SelectedGalleryAngleKey = "front";
    }

    public ObservableCollection<MotorcycleModelDto> Models { get; } = [];
    public ObservableCollection<ShowroomGalleryThumb> SelectedModelGallery { get; } = [];

    public IReadOnlyList<string> CategoryFilters { get; } =
        ["All", "Scooter", "Sport", "Naked", "Cruiser", "Adventure", "Underbone"];

    public string HeroShowroomImageSource => _imageResolver.ResolveHeroShowroom();
    public string SelectedModelImageSource =>
        GetActiveGalleryThumb()?.ImageSource ?? _imageResolver.ResolveForModel(SelectedModel);
    public string SelectedModelStockStatus =>
        SelectedModel is { AvailableStockCount: > 0 } ? "Available" : "OutOfStock";

    public bool HasMultipleGalleryAngles => SelectedModelGallery.Count > 1;

    public string SelectedGalleryCounter
    {
        get
        {
            if (SelectedModelGallery.Count == 0)
            {
                return string.Empty;
            }
            var active = GetActiveGalleryThumb();
            var idx = active?.Index ?? 0;
            return $"{idx + 1} / {SelectedModelGallery.Count}";
        }
    }

    public string SelectedGalleryAngleLabel =>
        GetActiveGalleryThumb()?.Label ?? Loc.ShowroomAngleFront;

    public string SelectedModelEstimatedPointsLabel
    {
        get
        {
            if (SelectedModel is null)
            {
                return string.Empty;
            }

            var mult = _sessionState.TierMultiplier is > 0m ? _sessionState.TierMultiplier.Value : 1.00m;
            var pts = TwoStepLoyaltyPreviewCalculator.CalculatePointsEarned(SelectedModel.BaseRetailPrice, mult);
            return $"{VietnamFormatting.FormatPointsLocalized(pts, signed: true, culture: Loc.CurrentCulture)} ({mult:0.00}x)";
        }
    }

    [ObservableProperty]
    public partial string SelectedGalleryAngleKey { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial string SelectedCategory { get; set; }

    [ObservableProperty]
    public partial MotorcycleModelDto? SelectedModel { get; set; }

    [ObservableProperty]
    public partial int TotalModelsCount { get; set; }

    [ObservableProperty]
    public partial int ActiveDealerCount { get; set; }

    public string ShowroomSummarySubtitle =>
        ActiveDealerCount > 0
            ? Loc.Format("ShowroomSubtitleAcrossBranches", Loc.FormatQuantity("model", TotalModelsCount), Loc.FormatQuantity("branch", ActiveDealerCount))
            : Loc.Format("ShowroomSubtitleAvailable", Loc.FormatQuantity("model", TotalModelsCount));

    public string OfflineCatalogBannerText =>
        Loc.Format("ShowroomOfflineBanner", SyncAgeLabel);

    partial void OnTotalModelsCountChanged(int value) => OnPropertyChanged(nameof(ShowroomSummarySubtitle));
    partial void OnActiveDealerCountChanged(int value) => OnPropertyChanged(nameof(ShowroomSummarySubtitle));
    partial void OnSelectedModelChanged(MotorcycleModelDto? value)
    {
        RebuildSelectedModelGallery(resetToFront: true);
    }

    partial void OnSelectedGalleryAngleKeyChanged(string value)
    {
        NotifyGalleryPresentationChanged();
    }

    protected override void OnCultureChanged(string cultureCode)
    {
        OnPropertyChanged(nameof(ShowroomSummarySubtitle));
        if (_lastSyncUtc.HasValue || IsOfflineBannerVisible)
        {
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
        }

        OnPropertyChanged(nameof(OfflineCatalogBannerText));

        // Update gallery thumbnail labels in-place (zero Image/BindableLayout teardown or JPEG re-decode on UI thread)
        foreach (var thumb in SelectedModelGallery)
        {
            thumb.Label = Loc.GetString(thumb.LabelResourceKey);
        }

        NotifyGalleryPresentationChanged();
        if (SelectedModel is not null)
        {
            OnPropertyChanged(nameof(SelectedModel));
        }
    }

    [RelayCommand]
    public async Task LoadCatalogAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var categoryQuery = string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : SelectedCategory;
            var searchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            var modelsTask = _apiClient.ListMotorcyclesAsync(
                pageNumber: 1,
                pageSize: 50,
                category: categoryQuery,
                search: searchQuery);
            var dealersTask = _apiClient.ListDealersAsync();

            await Task.WhenAll(modelsTask, dealersTask).ConfigureAwait(false);

            var result = modelsTask.Result;
            var dealersResult = dealersTask.Result;

            int activeDealerCount;
            if (dealersResult.IsSuccess && dealersResult.Data is not null)
            {
                await _offlineDb.SaveDealersAsync(dealersResult.Data).ConfigureAwait(false);
                activeDealerCount = dealersResult.Data.Count(d => d.IsActive);
            }
            else
            {
                var cachedDealers = await _offlineDb.GetCachedDealersAsync().ConfigureAwait(false);
                activeDealerCount = cachedDealers.Count(d => d.IsActive);
            }

            if (result.IsSuccess && result.Data is not null)
            {
                if (categoryQuery is null && searchQuery is null)
                {
                    await _offlineDb.SaveMotorcycleModelsAsync(result.Data).ConfigureAwait(false);
                }

                IsOfflineBannerVisible = false;
                _lastSyncUtc = DateTimeOffset.UtcNow;
                SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
                OnPropertyChanged(nameof(OfflineCatalogBannerText));
                await PopulateModelsAsync(result.Data, activeDealerCount).ConfigureAwait(false);
                return;
            }

            // Graceful offline fallback from encrypted SQLCipher public cache
            var cached = await _offlineDb.GetCachedMotorcycleModelsAsync().ConfigureAwait(false);
            var filtered = FilterLocally(cached, categoryQuery, searchQuery);
            var syncState = await _offlineDb.GetSyncStateAsync(MobileOfflineDb.PublicOwnerScope, "motorcycles")
                .ConfigureAwait(false);

            IsOfflineBannerVisible = true;
            _lastSyncUtc = syncState?.LastSyncedUtc;
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
            OnPropertyChanged(nameof(OfflineCatalogBannerText));
            await PopulateModelsAsync(filtered, activeDealerCount).ConfigureAwait(false);

            if (filtered.Count == 0)
            {
                ErrorMessage = FormatApiError(result);
            }
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task SelectCategoryAsync(string category)
    {
        SelectedCategory = string.IsNullOrWhiteSpace(category) ? "All" : category;
        TrackUserAction("Showroom.FilterCategory", "Showroom", SelectedCategory);
        await LoadCatalogAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void SelectModel(MotorcycleModelDto? model)
    {
        SelectedModel = model;
        if (model is not null)
        {
            TrackUserAction("Showroom.SelectModel", "Showroom", $"{model.Brand} {model.ModelName} ({model.ModelCode})");
        }
    }

    [RelayCommand]
    private void SelectGalleryAngle(ShowroomGalleryThumb? thumb)
    {
        if (thumb is null || string.IsNullOrWhiteSpace(thumb.AngleKey))
        {
            return;
        }

        SelectedGalleryAngleKey = thumb.AngleKey;
        TrackUserAction("Showroom.SelectAngle", "Showroom", $"{SelectedModel?.ModelCode}:{thumb.AngleKey}");
    }

    [RelayCommand]
    private void NextGalleryAngle()
    {
        StepGalleryAngle(1);
    }

    [RelayCommand]
    private void PreviousGalleryAngle()
    {
        StepGalleryAngle(-1);
    }

    private void StepGalleryAngle(int delta)
    {
        if (SelectedModelGallery.Count <= 1)
        {
            return;
        }

        var current = GetActiveGalleryThumb()?.Index ?? 0;
        var nextIndex = ((current + delta) % SelectedModelGallery.Count + SelectedModelGallery.Count) % SelectedModelGallery.Count;
        SelectedGalleryAngleKey = SelectedModelGallery[nextIndex].AngleKey;
    }

    private ShowroomGalleryThumb? GetActiveGalleryThumb()
    {
        if (SelectedModelGallery.Count == 0)
        {
            return null;
        }

        return SelectedModelGallery.FirstOrDefault(
                   g => string.Equals(g.AngleKey, SelectedGalleryAngleKey, StringComparison.OrdinalIgnoreCase))
               ?? SelectedModelGallery[0];
    }

    private void RebuildSelectedModelGallery(bool resetToFront)
    {
        var previousKey = SelectedGalleryAngleKey;
        SelectedModelGallery.Clear();

        if (SelectedModel is not null)
        {
            var resolved = _imageResolver.ResolveGalleryForModel(SelectedModel);
            for (var i = 0; i < resolved.Count; i++)
            {
                var item = resolved[i];
                SelectedModelGallery.Add(new ShowroomGalleryThumb(
                    item.AngleKey,
                    item.ImageSource,
                    item.LabelResourceKey,
                    Loc.GetString(item.LabelResourceKey),
                    i));
            }
        }

        var targetKey = resetToFront || string.IsNullOrWhiteSpace(previousKey)
            ? (SelectedModelGallery.FirstOrDefault()?.AngleKey ?? "front")
            : (SelectedModelGallery.Any(g => string.Equals(g.AngleKey, previousKey, StringComparison.OrdinalIgnoreCase))
                ? previousKey
                : (SelectedModelGallery.FirstOrDefault()?.AngleKey ?? "front"));

        SelectedGalleryAngleKey = targetKey;
        NotifyGalleryPresentationChanged();
    }

    private void NotifyGalleryPresentationChanged()
    {
        OnPropertyChanged(nameof(SelectedModelImageSource));
        OnPropertyChanged(nameof(SelectedModelStockStatus));
        OnPropertyChanged(nameof(HasMultipleGalleryAngles));
        OnPropertyChanged(nameof(SelectedGalleryCounter));
        OnPropertyChanged(nameof(SelectedGalleryAngleLabel));
        OnPropertyChanged(nameof(SelectedModelEstimatedPointsLabel));
    }

    [RelayCommand]
    private async Task BookTestDriveForSelectedModelAsync()
    {
        TrackUserAction("Showroom.TapBookCta", "Showroom", SelectedModel?.ModelName ?? "General");
        if (!_sessionState.IsAuthenticated)
        {
            await _navigationService.GoToAsync(MobileRoutes.LoginRegister).ConfigureAwait(false);
            return;
        }

        if (_sessionState.IsCustomer)
        {
            var parameters = SelectedModel is not null
                ? new Dictionary<string, object> { ["modelId"] = SelectedModel.Id.ToString("D") }
                : null;
            await _navigationService.GoToAsync(MobileRoutes.BookingWizard, parameters).ConfigureAwait(false);
        }
    }

    private Task PopulateModelsAsync(IReadOnlyList<MotorcycleModelDto> items, int activeDealerCount) =>
        _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
        {
            Models.Clear();
            foreach (var item in items)
            {
                Models.Add(item);
            }

            ActiveDealerCount = activeDealerCount;
            TotalModelsCount = items.Count;
            if (SelectedModel is null || !items.Any(m => m.Id == SelectedModel.Id))
            {
                SelectedModel = items.FirstOrDefault();
            }

            return Task.CompletedTask;
        });

    private static IReadOnlyList<MotorcycleModelDto> FilterLocally(
        IReadOnlyList<MotorcycleModelDto> source,
        string? category,
        string? search)
    {
        IEnumerable<MotorcycleModelDto> query = source;
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(m => string.Equals(m.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                m.ModelName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.Brand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.ModelCode.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToArray();
    }
}
