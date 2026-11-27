using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.App.Motion;
using MotorcycleDealer.Mobile.Core.Localization;
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
        Unloaded += OnPageUnloaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ShowroomCatalogPage, CultureChangedMessage>(
            this,
            static (page, _) => page.RefreshLocalizedChips());
        if (ViewModel.Models.Count == 0)
        {
            await ViewModel.LoadCatalogCommand.ExecuteAsync(null);
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
            BindableLayout.SetItemsSource(CategoryChipContainer, null);
            BindableLayout.SetItemsSource(CategoryChipContainer, ViewModel.CategoryFilters);
        });
    }

    private async void OnSearchCompleted(object? sender, EventArgs e)
        => await ViewModel.LoadCatalogCommand.ExecuteAsync(null);

    private void OnOemExpanderChanged(object? sender, ExpandedChangedEventArgs e)
        => _ = MotionHelper.RotateChevronAsync(OemChevron, e.IsExpanded);
}
