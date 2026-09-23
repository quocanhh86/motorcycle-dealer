using MotorcycleDealer.Mobile.Core.Logging;
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
