using MotorcycleDealer.Mobile.Core.ViewModels.Public;

namespace MotorcycleDealer.Mobile.App.Views.Public;

public partial class ShowroomCatalogPage : ContentPage
{
    public ShowroomCatalogViewModel ViewModel { get; }

    public ShowroomCatalogPage(ShowroomCatalogViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel.Models.Count == 0)
        {
            await ViewModel.LoadCatalogCommand.ExecuteAsync(null);
        }
    }

    private async void OnSearchCompleted(object? sender, EventArgs e)
        => await ViewModel.LoadCatalogCommand.ExecuteAsync(null);
}
