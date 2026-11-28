using MotorcycleDealer.Mobile.Core.ViewModels;

namespace MotorcycleDealer.Mobile.App.Views.Shared;

public partial class NotificationsFeedPage : ContentPage
{
    public NotificationsFeedViewModel ViewModel { get; }

    public NotificationsFeedPage(NotificationsFeedViewModel viewModel)
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
            ViewModel.AttachEventSubscriptions();
            await ViewModel.LoadNotificationsCommand.ExecuteAsync(null);
        }
        catch
        {
            // Keep async void page lifecycle handler crash-safe
        }
    }

    protected override void OnDisappearing()
    {
        ViewModel.DetachEventSubscriptions();
        base.OnDisappearing();
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
        => ViewModel.DetachEventSubscriptions();

    private async void OnFilterToggled(object? sender, ToggledEventArgs e)
    {
        try
        {
            if (IsLoaded)
            {
                await ViewModel.LoadNotificationsCommand.ExecuteAsync(null);
            }
        }
        catch
        {
            // Keep async void event handler crash-safe
        }
    }
}
