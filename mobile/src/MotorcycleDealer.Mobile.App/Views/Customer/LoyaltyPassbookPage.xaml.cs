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
        Unloaded += OnPageUnloaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            PassbookCard.ResumeSheen();
            await ViewModel.LoadPassbookCommand.ExecuteAsync(null);
        }
        catch
        {
            // Keep async void page lifecycle handler crash-safe
        }
    }

    protected override void OnDisappearing()
    {
        PassbookCard.PauseSheen();
        base.OnDisappearing();
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
        => PassbookCard.PauseSheen();
}
