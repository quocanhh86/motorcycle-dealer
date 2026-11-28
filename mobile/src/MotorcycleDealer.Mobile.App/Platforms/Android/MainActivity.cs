using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using MotorcycleDealer.Mobile.Core.ViewModels;

namespace MotorcycleDealer.Mobile.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int PostNotificationsRequestCode = 1033;
    private const string StatusBarScrimTag = "apex_carbon_status_bar_scrim";

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ApplyCarbonStatusBar();
        Window?.DecorView?.Post(ApplyCarbonStatusBar);
        RequestNotificationPermissionIfRequired();
        HandleNotificationDeepLinkIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleNotificationDeepLinkIntent(intent);
    }

    protected override void OnPostCreate(Bundle? savedInstanceState)
    {
        base.OnPostCreate(savedInstanceState);
        ApplyCarbonStatusBar();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplyCarbonStatusBar();
        Window?.DecorView?.Post(ApplyCarbonStatusBar);
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            ApplyCarbonStatusBar();
        }
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        ApplyCarbonStatusBar();
        Window?.DecorView?.Post(ApplyCarbonStatusBar);
    }

    public void ApplyCarbonStatusBar()
    {
        ApplyCarbonStatusBarToWindow(Window, includeApi35Scrim: true);
        ApplyCarbonStatusBarToDialogFragments(SupportFragmentManager);
    }

    private void ApplyCarbonStatusBarToDialogFragments(AndroidX.Fragment.App.FragmentManager? fragmentManager)
    {
        if (fragmentManager is null)
        {
            return;
        }

        foreach (AndroidX.Fragment.App.Fragment fragment in fragmentManager.Fragments)
        {
            if (fragment is AndroidX.Fragment.App.DialogFragment dialogFragment)
            {
                Android.Views.Window? dialogWindow = dialogFragment.Dialog?.Window;
                if (dialogWindow is not null)
                {
                    ApplyCarbonStatusBarToWindow(dialogWindow, includeApi35Scrim: false);
                    dialogWindow.DecorView?.Post(() => ApplyCarbonStatusBarToWindow(dialogWindow, includeApi35Scrim: false));
                }
            }

            if (fragment.IsAdded)
            {
                ApplyCarbonStatusBarToDialogFragments(fragment.ChildFragmentManager);
            }
        }
    }

    private void ApplyCarbonStatusBarToWindow(Android.Views.Window? window, bool includeApi35Scrim)
    {
        if (window is null)
        {
            return;
        }

        Microsoft.Maui.Controls.Application? mauiApp = Microsoft.Maui.Controls.Application.Current;
        bool isDark = mauiApp is not null
            ? (mauiApp.UserAppTheme != AppTheme.Unspecified
                ? mauiApp.UserAppTheme == AppTheme.Dark
                : mauiApp.RequestedTheme == AppTheme.Dark)
            : (Resources?.Configuration?.UiMode & UiMode.NightMask) == UiMode.NightYes;

        Android.Graphics.Color carbonColor = Android.Graphics.Color.ParseColor("#020617");
        Android.Graphics.Color lightSurfaceColor = Android.Graphics.Color.ParseColor("#F8FAFC");
        Android.Graphics.Color navSurfaceColor = isDark ? carbonColor : lightSurfaceColor;

        // Paint main Activity DecorView with theme-matched surface so Android 15 edge-to-edge bottom nav bar stays
        // legible in Light mode, while never painting over floating DialogFragment windows.
        if (includeApi35Scrim)
        {
            window.DecorView.SetBackgroundColor(navSurfaceColor);
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(29) && !OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            window.StatusBarContrastEnforced = false;
            window.NavigationBarContrastEnforced = false;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            window.SetStatusBarColor(carbonColor);
            window.SetNavigationBarColor(navSurfaceColor);
        }
        else
        {
#pragma warning disable CA1422, CS0618
            window.StatusBarContrastEnforced = false;
            window.NavigationBarContrastEnforced = false;
            window.SetStatusBarColor(carbonColor);
            window.SetNavigationBarColor(navSurfaceColor);
#pragma warning restore CA1422, CS0618
            if (includeApi35Scrim)
            {
                EnsureApi35CarbonStatusBarScrim(window, carbonColor);
            }
        }

        WindowInsetsControllerCompat? controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is not null)
        {
            // Dark #020617 Carbon header/status bar in BOTH Light & Dark modes -> always render crisp WHITE icons
            controller.AppearanceLightStatusBars = false;
            // Theme-matched bottom navigation bar -> DARK icons on #F8FAFC Light, WHITE icons on #020617 Dark
            controller.AppearanceLightNavigationBars = !isDark;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android35.0")]
    private static void EnsureApi35CarbonStatusBarScrim(Android.Views.Window window, Android.Graphics.Color carbonColor)
    {
        if (window.DecorView is not ViewGroup decorGroup)
        {
            return;
        }

        Android.Views.View? scrim = decorGroup.FindViewWithTag(StatusBarScrimTag);
        WindowInsetsCompat? rootInsets = ViewCompat.GetRootWindowInsets(decorGroup);
        int topInsetPx = rootInsets?.GetInsets(WindowInsetsCompat.Type.StatusBars())?.Top ?? 0;

        if (scrim is null)
        {
            scrim = new Android.Views.View(decorGroup.Context)
            {
                Tag = StatusBarScrimTag,
                Clickable = false,
                Focusable = false,
                ImportantForAccessibility = ImportantForAccessibility.No
            };
            scrim.SetBackgroundColor(carbonColor);
            // Attach inset listener directly to the scrim so rotation/split-screen updates height dynamically
            // without overwriting MAUI's own DecorView WindowInsets listener.
            scrim.SetOnApplyWindowInsetsListener(Api35ScrimInsetsListener.Instance);
            decorGroup.AddView(
                scrim,
                new Android.Widget.FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    Math.Max(0, topInsetPx),
                    GravityFlags.Top));
            scrim.RequestApplyInsets();
        }
        else
        {
            scrim.SetBackgroundColor(carbonColor);
            if (scrim.LayoutParameters is ViewGroup.LayoutParams lp && lp.Height != Math.Max(0, topInsetPx))
            {
                lp.Height = Math.Max(0, topInsetPx);
                scrim.LayoutParameters = lp;
            }
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android35.0")]
    private sealed class Api35ScrimInsetsListener : Java.Lang.Object, Android.Views.View.IOnApplyWindowInsetsListener
    {
        public static Api35ScrimInsetsListener Instance { get; } = new();

        public WindowInsets OnApplyWindowInsets(Android.Views.View? v, WindowInsets? insets)
        {
            if (v is not null && insets is not null)
            {
                int topInsetPx = Math.Max(0, insets.GetInsets(WindowInsets.Type.StatusBars()).Top);
                if (v.LayoutParameters is ViewGroup.LayoutParams lp && lp.Height != topInsetPx)
                {
                    lp.Height = topInsetPx;
                    v.LayoutParameters = lp;
                }
            }

            return insets ?? new WindowInsets.Builder().Build();
        }
    }

    private void HandleNotificationDeepLinkIntent(Intent? intent)
    {
        string? targetRoute = intent?.GetStringExtra("target_route");
        if (string.IsNullOrWhiteSpace(targetRoute))
        {
            return;
        }

        intent?.RemoveExtra("target_route");
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

    private void RequestNotificationPermissionIfRequired()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return;
        }

        string permission = Android.Manifest.Permission.PostNotifications;
        if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
        {
            ActivityCompat.RequestPermissions(this, [permission], PostNotificationsRequestCode);
        }
    }
}
