using MotorcycleDealer.Mobile.Core.ViewModels.Staff;

namespace MotorcycleDealer.Mobile.App.Views.Staff;

public partial class ShowroomVinSalesPage : ContentPage
{
    public ShowroomVinSalesViewModel ViewModel { get; }

    public ShowroomVinSalesPage(ShowroomVinSalesViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadSalesDeskCommand.ExecuteAsync(null);
    }
}
