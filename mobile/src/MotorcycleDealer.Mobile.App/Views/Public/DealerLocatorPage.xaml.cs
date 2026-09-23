using MotorcycleDealer.Mobile.Core.ViewModels.Public;

namespace MotorcycleDealer.Mobile.App.Views.Public;

public partial class DealerLocatorPage : ContentPage
{
    public DealerLocatorViewModel ViewModel { get; }

    public DealerLocatorPage(DealerLocatorViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel.Dealers.Count == 0)
        {
            await ViewModel.LoadDealersCommand.ExecuteAsync(null);
        }
    }
}
