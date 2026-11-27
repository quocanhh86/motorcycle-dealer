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

public sealed partial class StaffTestDriveViewModel : MobileViewModelBase, IDisposable
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IHapticService _hapticService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private bool _isSelfBroadcasting;

    public StaffTestDriveViewModel(
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

        SelectedStatusFilter = "All";
        StaffNotesInput = string.Empty;

        _messenger.Register<StaffTestDriveViewModel, TestDriveMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
        _messenger.Register<StaffTestDriveViewModel, VehicleMutatedMessage>(this, static (r, _) => r.TriggerCrossTabRefresh());
    }

    public IReadOnlyList<string> StatusFilters { get; } = ["All", "Requested", "Confirmed", "Completed", "Cancelled", "NoShow"];

    public ObservableCollection<TestDriveBookingDto> TestDrives { get; } = [];
    public ObservableCollection<DealerInventoryDto> DemoUnits { get; } = [];

    [ObservableProperty]
    public partial string SelectedStatusFilter { get; set; }

    [ObservableProperty]
    public partial TestDriveBookingDto? SelectedTestDrive { get; set; }

    [ObservableProperty]
    public partial DealerInventoryDto? SelectedDemoInventory { get; set; }

    [ObservableProperty]
    public partial string StaffNotesInput { get; set; }

    [RelayCommand]
    public async Task LoadTestDrivesAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var statusParam = string.Equals(SelectedStatusFilter, "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : SelectedStatusFilter;

            var listTask = _apiClient.ListDealerTestDrivesAsync(status: statusParam, pageSize: 40);
            var stockTask = _apiClient.ListInventoriesAsync(stockStatus: "Available", pageSize: 50);
            await Task.WhenAll(listTask, stockTask).ConfigureAwait(false);

            if (!listTask.Result.IsSuccess || listTask.Result.Data is null)
            {
                ErrorMessage = FormatApiError(listTask.Result);
                return;
            }

            await _mainThreadDispatcher.InvokeOnMainThreadAsync(() =>
            {
                TestDrives.Clear();
                foreach (var td in listTask.Result.Data) TestDrives.Add(td);

                DemoUnits.Clear();
                if (stockTask.Result.IsSuccess && stockTask.Result.Data is not null)
                {
                    foreach (var unit in stockTask.Result.Data) DemoUnits.Add(unit);
                }

                if (SelectedTestDrive is null && TestDrives.Count > 0)
                {
                    SelectedTestDrive = TestDrives[0];
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
    private async Task SelectStatusFilterAsync(string status)
    {
        SelectedStatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;
        await LoadTestDrivesAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public Task ConfirmTestDriveAsync(TestDriveBookingDto? item) =>
        UpdateStatusAsync(item, "Confirmed", SelectedDemoInventory?.Id);

    [RelayCommand]
    public Task CompleteTestDriveAsync(TestDriveBookingDto? item) =>
        UpdateStatusAsync(item, "Completed", item?.AssignedInventoryId);

    [RelayCommand]
    public Task MarkNoShowAsync(TestDriveBookingDto? item) =>
        UpdateStatusAsync(item, "NoShow", null);

    [RelayCommand]
    public Task CancelTestDriveAsync(TestDriveBookingDto? item) =>
        UpdateStatusAsync(item, "Cancelled", null);

    private async Task UpdateStatusAsync(TestDriveBookingDto? item, string nextStatus, Guid? assignedInventoryId)
    {
        var target = item ?? SelectedTestDrive;
        if (target is null)
        {
            return;
        }

        ClearMessages();
        var result = await _apiClient.UpdateTestDriveStatusAsync(
            target.TestDriveId,
            new UpdateTestDriveStatusRequest
            {
                NewStatus = nextStatus,
                AssignedInventoryId = assignedInventoryId,
                StaffNotes = string.IsNullOrWhiteSpace(StaffNotesInput) ? null : StaffNotesInput.Trim(),
                CancellationReason = nextStatus == "Cancelled"
                    ? (string.IsNullOrWhiteSpace(StaffNotesInput) ? Loc.TestDriveCancelledReasonDefault : StaffNotesInput.Trim())
                    : null,
                RowVersion = target.RowVersion
            }).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = FormatApiError(result);
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        StaffNotesInput = string.Empty;
        var statusLabel = Loc.GetEnumLabel("TestDriveStatus", result.Data.Status);
        SuccessMessage = Loc.Format("TestDriveUpdatedSuccess", result.Data.BookingNumber, statusLabel);
        _hapticService.Perform(HapticKind.Success);
        _isSelfBroadcasting = true;
        try
        {
            _messenger.Send(new TestDriveMutatedMessage(result.Data.TestDriveId));
        }
        finally
        {
            _isSelfBroadcasting = false;
        }

        await _feedbackService.ShowToastAsync(Loc.Format("TestDriveUpdatedToast", result.Data.BookingNumber, statusLabel)).ConfigureAwait(false);
        await LoadTestDrivesAsync().ConfigureAwait(false);
    }

    private async void TriggerCrossTabRefresh()
    {
        if (_isSelfBroadcasting || IsBusy)
        {
            return;
        }

        try
        {
            await LoadTestDrivesAsync().ConfigureAwait(false);
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
