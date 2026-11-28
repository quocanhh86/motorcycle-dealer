using CommunityToolkit.Maui.Core;
using MotorcycleDealer.Mobile.App.Icons;
using MotorcycleDealer.Mobile.App.Motion;
using MotorcycleDealer.Mobile.Core.ViewModels.Public;

namespace MotorcycleDealer.Mobile.App.Views.Public;

public partial class LoginRegisterPage : ContentPage
{
    public LoginRegisterViewModel ViewModel { get; }

    public LoginRegisterPage(LoginRegisterViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    private void OnTogglePasswordVisibilityClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordToggleBtn.Text = PasswordEntry.IsPassword ? PhosphorIcons.Eye : PhosphorIcons.EyeSlash;
    }

    private void OnEndpointExpanderChanged(object? sender, ExpandedChangedEventArgs e)
        => _ = MotionHelper.RotateChevronAsync(EndpointChevron, e.IsExpanded);
}
