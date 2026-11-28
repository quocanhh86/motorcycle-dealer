using MotorcycleDealer.Mobile.Core.ViewModels.Customer;

namespace MotorcycleDealer.Mobile.App.Views.Customer;

public partial class CustomerWarrantyPage : ContentPage
{
    public CustomerWarrantyViewModel ViewModel { get; }

    public CustomerWarrantyPage(CustomerWarrantyViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await ViewModel.LoadWarrantyDataCommand.ExecuteAsync(null);
        }
        catch
        {
            // Keep async void page lifecycle handler crash-safe
        }
    }
}
