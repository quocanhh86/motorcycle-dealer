using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;

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

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ApplyCarbonStatusBar();
        Window?.DecorView?.Post(ApplyCarbonStatusBar);
        RequestNotificationPermissionIfRequired();
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
        ApplyCarbonStatusBarToWindow(Window);
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
                    ApplyCarbonStatusBarToWindow(dialogWindow);
                    dialogWindow.DecorView?.Post(() => ApplyCarbonStatusBarToWindow(dialogWindow));
                }
            }

            if (fragment.IsAdded)
            {
                ApplyCarbonStatusBarToDialogFragments(fragment.ChildFragmentManager);
            }
        }
    }

    private void ApplyCarbonStatusBarToWindow(Android.Views.Window? window)
    {
        if (window is null)
        {
            return;
        }

        Android.Graphics.Color carbonColor = Android.Graphics.Color.ParseColor("#020617");
        window.DecorView.SetBackgroundColor(carbonColor);

        if (OperatingSystem.IsAndroidVersionAtLeast(29) && !OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            window.StatusBarContrastEnforced = false;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            window.SetStatusBarColor(carbonColor);
        }
        else
        {
#pragma warning disable CA1422, CS0618
            window.StatusBarContrastEnforced = false;
            window.SetStatusBarColor(carbonColor);
#pragma warning restore CA1422, CS0618
        }

        WindowInsetsControllerCompat? controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is not null)
        {
            // Dark #020617 Carbon header/status bar in BOTH Light & Dark modes -> always render crisp WHITE icons
            controller.AppearanceLightStatusBars = false;

            bool isNight = (Resources?.Configuration?.UiMode & UiMode.NightMask) == UiMode.NightYes;
            controller.AppearanceLightNavigationBars = !isNight;
        }
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
