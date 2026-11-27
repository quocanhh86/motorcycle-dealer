using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.ViewModels;

namespace MotorcycleDealer.Mobile.App;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        InitializeComponent();

        // Hydrate persisted Light Showroom / Dark Pit-Lane preference before creating the root window
        IAppPreferencesStore preferencesStore = _serviceProvider.GetRequiredService<IAppPreferencesStore>();
        string savedTheme = preferencesStore.GetString(AppPreferenceKeys.Theme, "light");
        UserAppTheme = ResolveAppTheme(savedTheme);

        // Hot-switch AppTheme live across all pages and controls without restarting the app
        WeakReferenceMessenger.Default.Register<App, ThemeChangedMessage>(
            this,
            static (recipient, message) => recipient.ApplyThemeMode(message.ThemeMode));

        RequestedThemeChanged += (_, _) => ReapplyPlatformStatusBar();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        AppShell shell = _serviceProvider.GetRequiredService<AppShell>();
        ISignalRMobileService signalR = _serviceProvider.GetRequiredService<ISignalRMobileService>();
        AppShellViewModel shellViewModel = _serviceProvider.GetRequiredService<AppShellViewModel>();
        IUserActionLogger actionLogger = _serviceProvider.GetRequiredService<IUserActionLogger>();

        var window = new Window(shell)
        {
            Title = "Apex Moto"
        };

        window.ModalPushed += (_, _) => ReapplyPlatformStatusBar();
        window.ModalPopped += (_, _) => ReapplyPlatformStatusBar();

        window.Created += async (_, _) =>
        {
            ReapplyPlatformStatusBar();
            actionLogger.Track("App.Started", "Lifecycle", actionLogger.ClientPlatform);
            try
            {
                await shellViewModel.BootstrapSessionCommand.ExecuteAsync(null);
            }
            catch
            {
                // Startup errors are surfaced gracefully via ViewModel banners
            }
        };

        window.Stopped += async (_, _) =>
        {
            actionLogger.Track("App.Backgrounded", "Lifecycle", actionLogger.CurrentScreen);
            try
            {
                await actionLogger.FlushPendingAsync();
                await signalR.OnAppBackgroundedAsync();
            }
            catch
            {
                // Keep Android/iOS background suspension resilient
            }
        };

        window.Resumed += async (_, _) =>
        {
            ReapplyPlatformStatusBar();
            actionLogger.Track("App.Resumed", "Lifecycle", actionLogger.CurrentScreen);
            try
            {
                await signalR.OnAppResumedAsync();
            }
            catch
            {
                // Keep Android/iOS resume & sinceUtc watermark recovery resilient
            }
        };

        return window;
    }

    private void ApplyThemeMode(string? themeMode)
    {
        AppTheme target = ResolveAppTheme(themeMode);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UserAppTheme = target;
            ReapplyPlatformStatusBar();
        });
    }

    private static AppTheme ResolveAppTheme(string? rawTheme) =>
        string.Equals(rawTheme?.Trim(), "dark", StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Dark
            : AppTheme.Light;

    private static void ReapplyPlatformStatusBar()
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Platform.CurrentActivity is MainActivity mainActivity)
            {
                mainActivity.ApplyCarbonStatusBar();
            }
        });
#endif
    }
}
