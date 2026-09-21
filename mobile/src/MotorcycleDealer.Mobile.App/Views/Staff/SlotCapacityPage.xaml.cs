using MotorcycleDealer.Mobile.Core.ViewModels.Staff;

namespace MotorcycleDealer.Mobile.App.Views.Staff;

public partial class SlotCapacityPage : ContentPage
{
    public SlotCapacityViewModel ViewModel { get; }

    public SlotCapacityPage(SlotCapacityViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        FromDatePicker.Date = ViewModel.FromDate.ToDateTime(TimeOnly.MinValue);
        ToDatePicker.Date = ViewModel.ToDate.ToDateTime(TimeOnly.MinValue);
        await ViewModel.LoadSlotsCommand.ExecuteAsync(null);
    }

    private void OnFromDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (e.NewDate is DateTime dt)
        {
            ViewModel.FromDate = DateOnly.FromDateTime(dt);
        }
    }

    private void OnToDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (e.NewDate is DateTime dt)
        {
            ViewModel.ToDate = DateOnly.FromDateTime(dt);
        }
    }
}
