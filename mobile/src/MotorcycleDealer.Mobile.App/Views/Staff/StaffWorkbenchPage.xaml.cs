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
        await ViewModel.LoadWorkbenchCommand.ExecuteAsync(null);
    }

    private async void OnAlertsClicked(object? sender, EventArgs e)
        => await _shellViewModel.OpenNotificationsCommand.ExecuteAsync(null);

    private async void OnSignOutClicked(object? sender, EventArgs e)
        => await _shellViewModel.SignOutCommand.ExecuteAsync(null);
}
