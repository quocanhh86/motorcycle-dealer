using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Staff;

public sealed partial class SlotCapacityViewModel : MobileViewModelBase
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;

    public SlotCapacityViewModel(
        IDealerMobileApiClient apiClient,
        IHapticService hapticService,
        IMainThreadDispatcher mainThreadDispatcher,
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
        : base(localizationService, preferencesStore, messenger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));

        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        FromDate = todayVn;
        ToDate = todayVn.AddDays(6);
        GenerateDurationMinutes = 60;
        GenerateMaxCapacity = 4;
    }

    public ObservableCollection<ServiceTimeSlotDto> Slots { get; } = [];

    [ObservableProperty]
    public partial DateOnly FromDate { get; set; }

    [ObservableProperty]
    public partial DateOnly ToDate { get; set; }

    [ObservableProperty]
    public partial int GenerateDurationMinutes { get; set; }

    [ObservableProperty]
    public partial int GenerateMaxCapacity { get; set; }

    [RelayCommand]
    public async Task LoadSlotsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var result = await _apiClient.ListDealerTimeSlotsAsync(
                fromDate: FromDate,
                toDate: ToDate).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                return;
            }

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                Slots.Clear();
                foreach (var s in result.Data) Slots.Add(s);
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
    public async Task GenerateRollingSlotsAsync()
    {
        ClearMessages();
        IsBusy = true;
        try
        {
            var result = await _apiClient.GenerateTimeSlotsAsync(new GenerateTimeSlotsRequest
            {
                FromDate = FromDate,
                ToDate = ToDate,
                SlotDurationMinutes = Math.Clamp(GenerateDurationMinutes, 30, 180),
                MaxCapacityPerSlot = Math.Clamp(GenerateMaxCapacity, 1, 20)
            }).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
            {
                ErrorMessage = FormatApiError(result);
                return;
            }

            SuccessMessage = Loc.Format("SlotsGeneratedSuccess", result.Data.Count);
            _hapticService.Perform(HapticKind.Success);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadSlotsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task ToggleBlockSlotAsync(ServiceTimeSlotDto? slot)
    {
        if (slot is null)
        {
            return;
        }

        ClearMessages();
        var result = await _apiClient.UpdateTimeSlotAsync(
            slot.Id,
            new UpdateTimeSlotRequest
            {
                IsBlocked = !slot.IsBlocked,
                MaxCapacity = slot.MaxCapacity,
                RowVersion = slot.RowVersion
            }).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        SuccessMessage = result.Data.IsBlocked
            ? Loc.Format("SlotsBlockedSuccess", result.Data.StartTime)
            : Loc.Format("SlotsReopenedSuccess", result.Data.StartTime, Loc.FormatQuantity("bay", result.Data.RemainingCapacity));
        _hapticService.Perform(HapticKind.Success);
        await LoadSlotsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task IncrementSlotCapacityAsync(ServiceTimeSlotDto? slot)
    {
        if (slot is null)
        {
            return;
        }

        ClearMessages();
        var result = await _apiClient.UpdateTimeSlotAsync(
            slot.Id,
            new UpdateTimeSlotRequest
            {
                MaxCapacity = slot.MaxCapacity + 1,
                IsBlocked = slot.IsBlocked,
                RowVersion = slot.RowVersion
            }).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            return;
        }

        SuccessMessage = Loc.Format("SlotsCapacityUpdatedSuccess", result.Data.MaxCapacity);
        await LoadSlotsAsync().ConfigureAwait(false);
    }
}
