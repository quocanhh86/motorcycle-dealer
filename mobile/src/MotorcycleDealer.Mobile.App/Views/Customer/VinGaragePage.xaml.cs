using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.ViewModels.Customer;

namespace MotorcycleDealer.Mobile.App.Views.Customer;

public partial class VinGaragePage : ContentPage
{
    private bool _is3dMode;

    public VinGarageViewModel ViewModel { get; }

    public VinGaragePage(VinGarageViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
        UpdateMediaModeUi();
        Unloaded += OnPageUnloaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<VinGaragePage, CultureChangedMessage>(
            this,
            static (page, _) => MainThread.BeginInvokeOnMainThread(() => page.UpdateMediaModeUi()));
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<VinGaragePage, ThemeChangedMessage>(
            this,
            static (page, msg) =>
            {
                bool isDark = string.Equals(msg.ThemeMode, "dark", StringComparison.OrdinalIgnoreCase);
                MainThread.BeginInvokeOnMainThread(() => page.UpdateMediaModeUi(isDark));
            });
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        UpdateMediaModeUi();
        try
        {
            await ViewModel.LoadGarageCommand.ExecuteAsync(null);
        }
        catch
        {
            // Garage load errors are surfaced via ViewModel.ErrorMessage
        }
    }

    protected override void OnDisappearing()
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        base.OnDisappearing();
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(() => UpdateMediaModeUi());

    private void OnGarage2dModeClicked(object? sender, EventArgs e)
    {
        _is3dMode = false;
        UpdateMediaModeUi();
    }

    private void OnGarage3dModeClicked(object? sender, EventArgs e)
    {
        _is3dMode = true;
        UpdateMediaModeUi();
    }

    private void UpdateMediaModeUi(bool? isDarkOverride = null)
    {
        bool isVi = !LocalizationService.Current.CurrentCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        bool isDark = isDarkOverride ?? (Application.Current?.UserAppTheme == AppTheme.Dark ||
                      (Application.Current?.UserAppTheme == AppTheme.Unspecified && Application.Current?.RequestedTheme == AppTheme.Dark));

        Garage2dMediaBlock.IsVisible = !_is3dMode;
        Garage3dStudio.IsVisible = _is3dMode;

        GarageMode2dButton.Text = isVi ? "Ảnh Thật 4 Góc" : "4-Angle Real HD";
        GarageMode3dButton.Text = isVi ? "Soi HD 360°" : "Real HD 360°";

        Color inactiveBg = isDark ? Color.FromArgb("#1E293B") : Color.FromArgb("#F1F5F9");
        Color inactiveFg = isDark ? Color.FromArgb("#E2E8F0") : Color.FromArgb("#0F172A");
        Color inactiveBorder = isDark ? Color.FromArgb("#334155") : Color.FromArgb("#CBD5E1");
        Color activeBg = Color.FromArgb("#DC2626");

        GarageMode2dButton.BackgroundColor = _is3dMode ? inactiveBg : activeBg;
        GarageMode2dButton.TextColor = _is3dMode ? inactiveFg : Colors.White;
        GarageMode2dButton.BorderColor = _is3dMode ? inactiveBorder : activeBg;

        GarageMode3dButton.BackgroundColor = _is3dMode ? activeBg : inactiveBg;
        GarageMode3dButton.TextColor = _is3dMode ? Colors.White : inactiveFg;
        GarageMode3dButton.BorderColor = _is3dMode ? activeBg : inactiveBorder;
    }
}
