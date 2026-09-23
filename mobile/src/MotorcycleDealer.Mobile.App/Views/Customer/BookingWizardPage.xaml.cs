using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.ViewModels.Customer;

namespace MotorcycleDealer.Mobile.App.Views.Customer;

public partial class BookingWizardPage : ContentPage
{
    public BookingWizardViewModel ViewModel { get; }

    public BookingWizardPage(BookingWizardViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SlotDatePicker.Date = ViewModel.SelectedDate.ToDateTime(TimeOnly.MinValue);
        await ViewModel.LoadWizardDataCommand.ExecuteAsync(null);
    }

    private void OnStep1Clicked(object? sender, EventArgs e) => ViewModel.CurrentStep = 1;
    private void OnStep2Clicked(object? sender, EventArgs e) => ViewModel.CurrentStep = 2;
    private void OnStep3Clicked(object? sender, EventArgs e) => ViewModel.CurrentStep = 3;

    private async void OnDealerChanged(object? sender, EventArgs e)
    {
        if (IsLoaded && ViewModel.SelectedDealer is not null)
        {
            await ViewModel.LoadAvailableSlotsCommand.ExecuteAsync(null);
        }
    }

    private async void OnSlotDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (e.NewDate is DateTime dt)
        {
            ViewModel.SelectedDate = DateOnly.FromDateTime(dt);
            if (IsLoaded)
            {
                await ViewModel.LoadAvailableSlotsCommand.ExecuteAsync(null);
            }
        }
    }

    private void OnPickInvoiceForReviewClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ServiceInvoiceDto invoice)
        {
            ViewModel.SelectedInvoiceForReview = invoice;
            ViewModel.ReviewRating = 5;
        }
    }

    private void OnRate1Clicked(object? sender, EventArgs e) => ViewModel.ReviewRating = 1;
    private void OnRate2Clicked(object? sender, EventArgs e) => ViewModel.ReviewRating = 2;
    private void OnRate3Clicked(object? sender, EventArgs e) => ViewModel.ReviewRating = 3;
    private void OnRate4Clicked(object? sender, EventArgs e) => ViewModel.ReviewRating = 4;
    private void OnRate5Clicked(object? sender, EventArgs e) => ViewModel.ReviewRating = 5;
}
