using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Public;

public sealed partial class DealerLocatorViewModel : MobileViewModelBase
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly INavigationService _navigationService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;

    private DateTimeOffset? _lastSyncUtc;

    public DealerLocatorViewModel(
        IDealerMobileApiClient apiClient,
        IMobileOfflineDb offlineDb,
        INavigationService navigationService,
        IMainThreadDispatcher mainThreadDispatcher,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));

        SelectedCity = "All";
    }

    public ObservableCollection<DealerDto> Dealers { get; } = [];

    public ObservableCollection<string> CityFilters { get; } = ["All"];

    [ObservableProperty]
    public partial string SelectedCity { get; set; }

    [ObservableProperty]
    public partial DealerDto? SelectedDealer { get; set; }

    public string OfflineDirectoryBannerText => Loc.Format("DealerOfflineBanner", SyncAgeLabel);

    protected override void OnCultureChanged(string cultureCode)
    {
        if (_lastSyncUtc.HasValue || IsOfflineBannerVisible)
        {
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
        }

        OnPropertyChanged(nameof(OfflineDirectoryBannerText));

        var selectedCity = SelectedCity;
        var selectedDealer = SelectedDealer;
        RefreshCollection(CityFilters);
        RefreshCollection(Dealers);
        SelectedCity = selectedCity;
        OnPropertyChanged(nameof(SelectedCity));
        SelectedDealer = selectedDealer;
    }

    [RelayCommand]
    public async Task LoadDealersAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var cityFilter = string.Equals(SelectedCity, "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : SelectedCity;

            var result = await _apiClient.ListDealersAsync(cityFilter).ConfigureAwait(false);
            if (result.IsSuccess && result.Data is not null)
            {
                if (cityFilter is null)
                {
                    await _offlineDb.SaveDealersAsync(result.Data).ConfigureAwait(false);
                }

                IsOfflineBannerVisible = false;
                _lastSyncUtc = DateTimeOffset.UtcNow;
                SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
                OnPropertyChanged(nameof(OfflineDirectoryBannerText));
                await PopulateAsync(result.Data, cityFilter is null ? result.Data : null).ConfigureAwait(false);
                return;
            }

            var cached = await _offlineDb.GetCachedDealersAsync().ConfigureAwait(false);
            var filtered = string.IsNullOrWhiteSpace(cityFilter)
                ? cached
                : cached.Where(d => d.City.Contains(cityFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
            var syncState = await _offlineDb.GetSyncStateAsync(MobileOfflineDb.PublicOwnerScope, "dealers")
                .ConfigureAwait(false);

            IsOfflineBannerVisible = true;
            _lastSyncUtc = syncState?.LastSyncedUtc;
            SyncAgeLabel = VietnamFormatting.FormatSyncAge(_lastSyncUtc, culture: Loc.CurrentCulture);
            OnPropertyChanged(nameof(OfflineDirectoryBannerText));
            await PopulateAsync(filtered, cached).ConfigureAwait(false);

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
    private async Task SelectCityAsync(string city)
    {
        SelectedCity = string.IsNullOrWhiteSpace(city) ? "All" : city;
        await LoadDealersAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task OpenDirectionsAsync(DealerDto? dealer)
    {
        var target = dealer ?? SelectedDealer;
        if (target is null)
        {
            return;
        }

        var url = target.Latitude.HasValue && target.Longitude.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"https://www.google.com/maps/search/?api=1&query={target.Latitude.Value},{target.Longitude.Value}")
            : $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString($"{target.Name} {target.Address}")}";

        await _navigationService.OpenExternalUrlAsync(url).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task CallDealerAsync(DealerDto? dealer)
    {
        var target = dealer ?? SelectedDealer;
        if (target is null || string.IsNullOrWhiteSpace(target.PhoneNumber))
        {
            return;
        }

        await _navigationService.OpenExternalUrlAsync($"tel:{target.PhoneNumber.Trim()}").ConfigureAwait(false);
    }

    private Task PopulateAsync(
        IReadOnlyList<DealerDto> items,
        IReadOnlyList<DealerDto>? allDealersForCities = null) =>
        _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
        {
            if (allDealersForCities is not null)
            {
                var distinctCities = allDealersForCities
                    .Select(d => d.City?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                CityFilters.Clear();
                CityFilters.Add("All");
                foreach (var city in distinctCities)
                {
                    CityFilters.Add(city);
                }
            }

            Dealers.Clear();
            foreach (var d in items)
            {
                Dealers.Add(d);
            }

            if (SelectedDealer is null && items.Count > 0)
            {
                SelectedDealer = items[0];
            }

            return Task.CompletedTask;
        });
}
