using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MotorcycleDealer.Mobile.App.Services;
using MotorcycleDealer.Mobile.App.Views.Customer;
using MotorcycleDealer.Mobile.App.Views.Public;
using MotorcycleDealer.Mobile.App.Views.Shared;
using MotorcycleDealer.Mobile.App.Views.Staff;
using MotorcycleDealer.Mobile.Core.DependencyInjection;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;
using ZXing.Net.Maui.Controls;

#if ANDROID
using MotorcycleDealer.Mobile.App.Platforms.Android.Services;
#elif IOS
using MotorcycleDealer.Mobile.App.Platforms.iOS.Services;
#endif

namespace MotorcycleDealer.Mobile.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Cross-platform MAUI Essentials & CommunityToolkit.Maui services (Android Keystore + iOS Keychain)
        builder.Services.AddSingleton<ISecureTokenStore, MauiSecureTokenStore>();
        builder.Services.AddSingleton<IMainThreadDispatcher, MauiMainThreadDispatcher>();
        builder.Services.AddSingleton<IConnectivityMonitor, MauiConnectivityMonitor>();
        builder.Services.AddSingleton<IHapticService, MauiHapticService>();
        builder.Services.AddSingleton<IShareService, MauiShareService>();
        builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
        builder.Services.AddSingleton<IDatabasePathProvider, MauiDatabasePathProvider>();
        builder.Services.AddSingleton<IUserFeedbackService, MauiUserFeedbackService>();

        // Native platform notification service, platform logcat/OSLog sink & default local API endpoint
#if ANDROID
        builder.Services.AddSingleton<ILocalNotificationService, AndroidLocalNotificationService>();
        builder.Services.AddSingleton<IAppPlatformInfo>(_ => new DefaultAppPlatformInfo("MobileAndroid"));
        builder.Services.AddSingleton<IUserActionPlatformSink, AndroidLogcatUserActionSink>();
        string defaultApiBaseUrl = MobileEndpointDefaults.EmulatorBaseUrl;
#elif IOS
        builder.Services.AddSingleton<ILocalNotificationService, IosLocalNotificationService>();
        builder.Services.AddSingleton<IAppPlatformInfo>(_ => new DefaultAppPlatformInfo("MobileIos"));
        builder.Services.AddSingleton<IUserActionPlatformSink, IosConsoleUserActionSink>();
        string defaultApiBaseUrl = MobileEndpointDefaults.IosSimulatorBaseUrl;
#else
        builder.Services.AddSingleton<IAppPlatformInfo>(_ => new DefaultAppPlatformInfo("MobileCore"));
        string defaultApiBaseUrl = MobileEndpointDefaults.AdbReverseLocalhostBaseUrl;
#endif

        // Register pure net10.0 Mobile.Core services, Polly v8 resilience, FluentValidation, SQLCipher DB, SignalR hub & 17 ViewModels
        builder.Services.AddMotorcycleDealerMobileCore(defaultApiBaseUrl);

        // Role-adaptive Shell
        builder.Services.AddSingleton<AppShell>();

        // Public Views (4)
        builder.Services.AddTransient<ShowroomCatalogPage>();
        builder.Services.AddTransient<DealerLocatorPage>();
        builder.Services.AddTransient<LoginRegisterPage>();
        builder.Services.AddTransient<AdminWebConsoleNoticePage>();

        // Customer Views (5)
        builder.Services.AddTransient<CustomerHomePage>();
        builder.Services.AddTransient<VinGaragePage>();
        builder.Services.AddTransient<BookingWizardPage>();
        builder.Services.AddTransient<LoyaltyPassbookPage>();
        builder.Services.AddTransient<CustomerWarrantyPage>();

        // DealerStaff Views (6)
        builder.Services.AddTransient<StaffWorkbenchPage>();
        builder.Services.AddTransient<MobileInvoiceBuilderPage>();
        builder.Services.AddTransient<ShowroomVinSalesPage>();
        builder.Services.AddTransient<StaffTestDrivePage>();
        builder.Services.AddTransient<StaffWarrantyCsatPage>();
        builder.Services.AddTransient<SlotCapacityPage>();

        // Shared Views (1)
        builder.Services.AddTransient<NotificationsFeedPage>();

#if ANDROID
        ConfigureAndroidBorderlessInputs();
#elif IOS
        ConfigureIosNativeStyling();
#endif

        return builder.Build();
    }

#if ANDROID
    private static void ConfigureAndroidBorderlessInputs()
    {
        Android.Content.Res.ColorStateList transparentTint =
            Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.BackgroundTintList = transparentTint;
            handler.PlatformView.Gravity = Android.Views.GravityFlags.CenterVertical | Android.Views.GravityFlags.Start;
            handler.PlatformView.SetPadding(0, 0, 0, 0);
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.BackgroundTintList = transparentTint;
            handler.PlatformView.SetPadding(0, 14, 0, 14);
        });

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.BackgroundTintList = transparentTint;
            handler.PlatformView.Gravity = Android.Views.GravityFlags.CenterVertical | Android.Views.GravityFlags.Start;
            handler.PlatformView.SetPadding(0, 0, 0, 0);
        });

        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.BackgroundTintList = transparentTint;
            handler.PlatformView.Gravity = Android.Views.GravityFlags.CenterVertical | Android.Views.GravityFlags.Start;
            handler.PlatformView.SetPadding(0, 0, 0, 0);
        });
    }
#elif IOS
    private static void ConfigureIosNativeStyling()
    {
        UIKit.UIColor crimsonTint = UIKit.UIColor.FromRGB(0xDC, 0x26, 0x26);
        UIKit.UIColor carbonBg = UIKit.UIColor.FromRGB(0x02, 0x06, 0x17);
        UIKit.UIColor slateWhite = UIKit.UIColor.FromRGB(0xF8, 0xFA, 0xFC);

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.TintColor = crimsonTint;
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.Layer.BorderWidth = 0;
            handler.PlatformView.TintColor = crimsonTint;
        });

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.TintColor = crimsonTint;
        });

        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("BorderlessInput", (handler, _) =>
        {
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.TintColor = crimsonTint;
        });

        var navAppearance = new UIKit.UINavigationBarAppearance();
        navAppearance.ConfigureWithOpaqueBackground();
        navAppearance.BackgroundColor = carbonBg;
        navAppearance.TitleTextAttributes = new UIKit.UIStringAttributes
        {
            ForegroundColor = slateWhite
        };
        navAppearance.LargeTitleTextAttributes = new UIKit.UIStringAttributes
        {
            ForegroundColor = slateWhite
        };

        UIKit.UINavigationBar.Appearance.StandardAppearance = navAppearance;
        UIKit.UINavigationBar.Appearance.ScrollEdgeAppearance = navAppearance;
        UIKit.UINavigationBar.Appearance.CompactAppearance = navAppearance;
        UIKit.UINavigationBar.Appearance.TintColor = crimsonTint;
    }
#endif

#if ANDROID
    private sealed class AndroidLogcatUserActionSink : IUserActionPlatformSink
    {
        private const string LogTag = "MotorcycleDealer.UserAction";

        public void WriteLine(string level, string message)
        {
            if (string.Equals(level, "ERROR", StringComparison.OrdinalIgnoreCase))
            {
                Android.Util.Log.Error(LogTag, message);
            }
            else if (string.Equals(level, "WARN", StringComparison.OrdinalIgnoreCase))
            {
                Android.Util.Log.Warn(LogTag, message);
            }
            else
            {
                Android.Util.Log.Info(LogTag, message);
            }
        }
    }
#elif IOS
    private sealed class IosConsoleUserActionSink : IUserActionPlatformSink
    {
        public void WriteLine(string level, string message)
        {
            Console.WriteLine($"[MotorcycleDealer.UserAction][{level}] {message}");
        }
    }
#endif
}
