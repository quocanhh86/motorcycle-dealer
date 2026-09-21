using MotorcycleDealer.Mobile.Core.ViewModels.Staff;

namespace MotorcycleDealer.Mobile.App.Views.Staff;

public partial class StaffTestDrivePage : ContentPage
{
    public StaffTestDriveViewModel ViewModel { get; }

    public StaffTestDrivePage(StaffTestDriveViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadTestDrivesCommand.ExecuteAsync(null);
    }
}
