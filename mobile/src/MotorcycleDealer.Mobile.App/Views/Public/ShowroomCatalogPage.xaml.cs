using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.App.Motion;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.ViewModels.Public;

namespace MotorcycleDealer.Mobile.App.Views.Public;

public partial class ShowroomCatalogPage : ContentPage
{
    private bool _is3dMode;

    public ShowroomCatalogViewModel ViewModel { get; }

    public ShowroomCatalogPage(ShowroomCatalogViewModel viewModel)
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
        WeakReferenceMessenger.Default.Register<ShowroomCatalogPage, CultureChangedMessage>(
            this,
            static (page, _) => page.RefreshLocalizedChips());
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ShowroomCatalogPage, ThemeChangedMessage>(
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
            if (ViewModel.Models.Count == 0)
            {
                await ViewModel.LoadCatalogCommand.ExecuteAsync(null);
            }
        }
        catch
        {
            // Catalog load failures are surfaced via ViewModel.ErrorMessage
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

    private void RefreshLocalizedChips()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BindableLayout.SetItemsSource(CategoryChipContainer, null);
            BindableLayout.SetItemsSource(CategoryChipContainer, ViewModel.CategoryFilters);
            UpdateMediaModeUi();
        });
    }

    private void OnShowroom2dModeClicked(object? sender, EventArgs e)
    {
        _is3dMode = false;
        UpdateMediaModeUi();
    }

    private void OnShowroom3dModeClicked(object? sender, EventArgs e)
    {
        _is3dMode = true;
        UpdateMediaModeUi();
    }

    private void UpdateMediaModeUi(bool? isDarkOverride = null)
    {
        bool isVi = !LocalizationService.Current.CurrentCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        bool isDark = isDarkOverride ?? (Application.Current?.UserAppTheme == AppTheme.Dark ||
                      (Application.Current?.UserAppTheme == AppTheme.Unspecified && Application.Current?.RequestedTheme == AppTheme.Dark));

        Showroom2dMediaBlock.IsVisible = !_is3dMode;
        Showroom3dStudio.IsVisible = _is3dMode;

        ShowroomMode2dButton.Text = isVi ? "Ảnh Thật 4 Góc" : "4-Angle Real HD";
        ShowroomMode3dButton.Text = isVi ? "Soi HD 360°" : "Real HD 360°";

        Color inactiveBg = isDark ? Color.FromArgb("#1E293B") : Color.FromArgb("#F1F5F9");
        Color inactiveFg = isDark ? Color.FromArgb("#E2E8F0") : Color.FromArgb("#0F172A");
        Color inactiveBorder = isDark ? Color.FromArgb("#334155") : Color.FromArgb("#CBD5E1");
        Color activeBg = Color.FromArgb("#DC2626");

        ShowroomMode2dButton.BackgroundColor = _is3dMode ? inactiveBg : activeBg;
        ShowroomMode2dButton.TextColor = _is3dMode ? inactiveFg : Colors.White;
        ShowroomMode2dButton.BorderColor = _is3dMode ? inactiveBorder : activeBg;

        ShowroomMode3dButton.BackgroundColor = _is3dMode ? activeBg : inactiveBg;
        ShowroomMode3dButton.TextColor = _is3dMode ? Colors.White : inactiveFg;
        ShowroomMode3dButton.BorderColor = _is3dMode ? activeBg : inactiveBorder;
    }

    private async void OnSearchCompleted(object? sender, EventArgs e)
    {
        try
        {
            await ViewModel.LoadCatalogCommand.ExecuteAsync(null);
        }
        catch
        {
            // Catalog search errors are surfaced via ViewModel.ErrorMessage
        }
    }

    private void OnOemExpanderChanged(object? sender, ExpandedChangedEventArgs e)
        => _ = MotionHelper.RotateChevronAsync(OemChevron, e.IsExpanded);
}
