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
        ViewModel.AttachEventSubscriptions();
        await ViewModel.LoadNotificationsCommand.ExecuteAsync(null);
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
        if (IsLoaded)
        {
            await ViewModel.LoadNotificationsCommand.ExecuteAsync(null);
        }
    }
}
