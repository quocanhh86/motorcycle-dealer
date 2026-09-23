using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.ViewModels.Staff;

namespace MotorcycleDealer.Mobile.App.Views.Staff;

public partial class StaffWarrantyCsatPage : ContentPage
{
    public StaffWarrantyCsatViewModel ViewModel { get; }

    public StaffWarrantyCsatPage(StaffWarrantyCsatViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadDeskCommand.ExecuteAsync(null);
    }

    private void OnSelectReviewClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ServiceReviewDto review)
        {
            ViewModel.SelectedReview = review;
        }
    }
}
