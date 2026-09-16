using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Public;

public sealed partial class ShowroomCatalogViewModel : MobileViewModelBase
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly INavigationService _navigationService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;

    public ShowroomCatalogViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        INavigationService navigationService,
        IMainThreadDispatcher mainThreadDispatcher)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));

        SearchText = string.Empty;
        SelectedCategory = "All";
    }

    public ObservableCollection<MotorcycleModelDto> Models { get; } = [];

    public IReadOnlyList<string> CategoryFilters { get; } =
        ["All", "Scooter", "Sport", "Naked", "Cruiser", "Adventure", "Underbone"];

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
            ? $"{"model".ToQuantity(TotalModelsCount)} across {"branch".ToQuantity(ActiveDealerCount)}"
            : $"{"model".ToQuantity(TotalModelsCount)} available";

    partial void OnTotalModelsCountChanged(int value) => OnPropertyChanged(nameof(ShowroomSummarySubtitle));
    partial void OnActiveDealerCountChanged(int value) => OnPropertyChanged(nameof(ShowroomSummarySubtitle));

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
                SyncAgeLabel = VietnamFormatting.FormatSyncAge(DateTimeOffset.UtcNow);
                await PopulateModelsAsync(result.Data, activeDealerCount).ConfigureAwait(false);
                return;
            }

            // Graceful offline fallback from encrypted SQLCipher public cache
            var cached = await _offlineDb.GetCachedMotorcycleModelsAsync().ConfigureAwait(false);
            var filtered = FilterLocally(cached, categoryQuery, searchQuery);
            var syncState = await _offlineDb.GetSyncStateAsync(MobileOfflineDb.PublicOwnerScope, "motorcycles")
                .ConfigureAwait(false);

            IsOfflineBannerVisible = true;
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(syncState?.LastSyncedUtc);
            await PopulateModelsAsync(filtered, activeDealerCount).ConfigureAwait(false);

            if (filtered.Count == 0)
            {
                ErrorMessage = result.ErrorMessage;
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
