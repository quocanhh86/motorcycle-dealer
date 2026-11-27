using MotorcycleDealer.Mobile.Core.ViewModels.Public;

namespace MotorcycleDealer.Mobile.App.Views.Public;

public partial class AdminWebConsoleNoticePage : ContentPage
{
    public AdminWebConsoleNoticeViewModel ViewModel { get; }

    public AdminWebConsoleNoticePage(AdminWebConsoleNoticeViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }
}
