using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.ViewModels;
using MotorcycleDealer.Mobile.Core.ViewModels.Staff;

namespace MotorcycleDealer.Mobile.App.Views.Staff;

public partial class StaffWorkbenchPage : ContentPage
{
    private readonly AppShellViewModel _shellViewModel;

    public StaffWorkbenchViewModel ViewModel { get; }

    public StaffWorkbenchPage(StaffWorkbenchViewModel viewModel, AppShellViewModel shellViewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Register<StaffWorkbenchPage, CultureChangedMessage>(
            this,
            static (page, _) => page.RefreshLocalizedChips());
        await ViewModel.LoadWorkbenchCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        base.OnDisappearing();
    }

    private void RefreshLocalizedChips()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BindableLayout.SetItemsSource(LaneChipContainer, null);
            BindableLayout.SetItemsSource(LaneChipContainer, ViewModel.StatusLanes);
        });
    }

    private async void OnAlertsClicked(object? sender, EventArgs e)
        => await _shellViewModel.OpenNotificationsCommand.ExecuteAsync(null);

    private async void OnSignOutClicked(object? sender, EventArgs e)
        => await _shellViewModel.SignOutCommand.ExecuteAsync(null);
}
