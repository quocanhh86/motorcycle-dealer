using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Localization;
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
        Unloaded += OnPageUnloaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<StaffTestDrivePage, CultureChangedMessage>(
            this,
            static (page, _) => page.RefreshLocalizedChips());
        await ViewModel.LoadTestDrivesCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        base.OnDisappearing();
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
        => WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);

    private void RefreshLocalizedChips()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BindableLayout.SetItemsSource(StatusFilterContainer, null);
            BindableLayout.SetItemsSource(StatusFilterContainer, ViewModel.StatusFilters);
        });
    }
}
