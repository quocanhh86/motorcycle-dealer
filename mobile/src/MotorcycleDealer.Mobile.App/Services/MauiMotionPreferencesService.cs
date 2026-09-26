using MotorcycleDealer.Mobile.Core.Abstractions;

namespace MotorcycleDealer.Mobile.App.Services;

/// <summary>
/// Reads live OS accessibility reduced-motion settings on Android and iOS
/// with an optional in-app preference override stored in IAppPreferencesStore.
/// </summary>
public sealed class MauiMotionPreferencesService : IMotionPreferencesService
{
    private readonly IAppPreferencesStore? _preferencesStore;

    public MauiMotionPreferencesService(IAppPreferencesStore? preferencesStore = null)
    {
        _preferencesStore = preferencesStore;
    }

    public bool IsReducedMotionEnabled
    {
        get
        {
            if (IsOsReducedMotionEnabled())
            {
                return true;
            }

            string? saved = _preferencesStore?.GetString(AppPreferenceKeys.ReducedMotion, "false");
            return string.Equals(saved?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool IsOsReducedMotionEnabled()
    {
#if ANDROID
        try
        {
            Android.Content.ContentResolver? resolver = Android.App.Application.Context.ContentResolver;
            if (resolver is not null)
            {
                float scale = Android.Provider.Settings.Global.GetFloat(
                    resolver,
                    Android.Provider.Settings.Global.AnimatorDurationScale,
                    1f);
                return scale <= 0f;
            }
        }
        catch
        {
            // Ignore restricted settings providers on custom Android builds
        }
#elif IOS
        try
        {
            return UIKit.UIAccessibility.IsReduceMotionEnabled;
        }
        catch
        {
            // Ignore simulator accessibility edge cases
        }
#endif
        return false;
    }
}
