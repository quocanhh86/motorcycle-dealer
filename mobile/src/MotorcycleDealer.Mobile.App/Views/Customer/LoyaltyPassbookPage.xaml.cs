using MotorcycleDealer.Mobile.Core.ViewModels.Customer;

namespace MotorcycleDealer.Mobile.App.Views.Customer;

public partial class LoyaltyPassbookPage : ContentPage
{
    public LoyaltyPassbookViewModel ViewModel { get; }

    public LoyaltyPassbookPage(LoyaltyPassbookViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadPassbookCommand.ExecuteAsync(null);
    }
}
