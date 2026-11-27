using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Localization;
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
        Unloaded += OnPageUnloaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<DealerLocatorPage, CultureChangedMessage>(
            this,
            static (page, _) => page.RefreshLocalizedChips());
        if (ViewModel.Dealers.Count == 0)
        {
            await ViewModel.LoadDealersCommand.ExecuteAsync(null);
        }
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
            BindableLayout.SetItemsSource(CityChipContainer, null);
            BindableLayout.SetItemsSource(CityChipContainer, ViewModel.CityFilters);
        });
    }
}
