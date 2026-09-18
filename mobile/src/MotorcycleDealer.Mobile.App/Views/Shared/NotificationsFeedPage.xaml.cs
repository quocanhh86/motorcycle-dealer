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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadNotificationsCommand.ExecuteAsync(null);
    }

    private async void OnFilterToggled(object? sender, ToggledEventArgs e)
    {
        if (IsLoaded)
        {
            await ViewModel.LoadNotificationsCommand.ExecuteAsync(null);
        }
    }
}
