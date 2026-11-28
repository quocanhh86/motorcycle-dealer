using Foundation;
using MotorcycleDealer.Mobile.Core.ViewModels;
using UIKit;
using UserNotifications;

namespace MotorcycleDealer.Mobile.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private readonly ApexIosNotificationDelegate _notificationDelegate = new();

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        UNUserNotificationCenter.Current.Delegate = _notificationDelegate;
        return base.FinishedLaunching(application, launchOptions);
    }

    private sealed class ApexIosNotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            completionHandler(
                UNNotificationPresentationOptions.Banner |
                UNNotificationPresentationOptions.List |
                UNNotificationPresentationOptions.Sound |
                UNNotificationPresentationOptions.Badge);
        }

        public override void DidReceiveNotificationResponse(
            UNUserNotificationCenter center,
            UNNotificationResponse response,
            Action completionHandler)
        {
            try
            {
                NSDictionary? userInfo = response?.Notification?.Request?.Content?.UserInfo;
                NSObject? rawRoute = userInfo is not null ? userInfo.ObjectForKey((NSString)"target_route") : null;
                string? targetRoute = rawRoute?.ToString();
                if (!string.IsNullOrWhiteSpace(targetRoute))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            AppShellViewModel? shellViewModel = IPlatformApplication.Current?.Services.GetService<AppShellViewModel>();
                            if (shellViewModel is not null)
                            {
                                await shellViewModel.HandleExternalDeepLinkAsync(targetRoute);
                            }
                        }
                        catch
                        {
                            // Ignore deep-link routing failures during cold start or logged-out state
                        }
                    });
                }
            }
            catch
            {
                // Always invoke completionHandler
            }

            completionHandler();
        }
    }
}
