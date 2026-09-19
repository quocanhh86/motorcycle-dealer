using MotorcycleDealer.Mobile.Core.ViewModels;
using MotorcycleDealer.Mobile.Core.ViewModels.Customer;

namespace MotorcycleDealer.Mobile.App.Views.Customer;

public partial class CustomerHomePage : ContentPage
{
    private readonly AppShellViewModel _shellViewModel;

    public CustomerHomeViewModel ViewModel { get; }

    public CustomerHomePage(CustomerHomeViewModel viewModel, AppShellViewModel shellViewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadDashboardCommand.ExecuteAsync(null);
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
        => await _shellViewModel.SignOutCommand.ExecuteAsync(null);
}
