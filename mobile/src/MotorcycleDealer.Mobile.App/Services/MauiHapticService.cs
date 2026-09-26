using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.App.Services;

/// <summary>
/// Platform-native haptic feedback service using UIKit UINotificationFeedbackGenerator on iOS
/// and native Android View.PerformHapticFeedback (Confirm/Reject on API 30+) with safe MAUI fallback.
/// </summary>
public sealed class MauiHapticService : IHapticService
{
    public void Perform(HapticKind kind)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() => PerformOnMainThread(kind));
        }
        catch
        {
            // Ignore on headless hosts or emulators without vibrator hardware
        }
    }

    private static void PerformOnMainThread(HapticKind kind)
    {
        try
        {
#if IOS
            if (kind is HapticKind.Success or HapticKind.Warning)
            {
                using var generator = new UIKit.UINotificationFeedbackGenerator();
                generator.Prepare();
                generator.NotificationOccurred(kind == HapticKind.Success
                    ? UIKit.UINotificationFeedbackType.Success
                    : UIKit.UINotificationFeedbackType.Warning);
                return;
            }
#elif ANDROID
            if (kind is HapticKind.Success or HapticKind.Warning &&
                OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                Android.Views.View? decorView = Platform.CurrentActivity?.Window?.DecorView;
                if (decorView is not null)
                {
                    Android.Views.FeedbackConstants constant = kind == HapticKind.Success
                        ? Android.Views.FeedbackConstants.Confirm
                        : Android.Views.FeedbackConstants.Reject;
                    if (decorView.PerformHapticFeedback(constant))
                    {
                        return;
                    }
                }
            }
#endif
            if (!HapticFeedback.Default.IsSupported)
            {
                return;
            }

            HapticFeedbackType nativeType = kind switch
            {
                HapticKind.LongPress or HapticKind.Warning => HapticFeedbackType.LongPress,
                _ => HapticFeedbackType.Click
            };

            HapticFeedback.Default.Perform(nativeType);
        }
        catch
        {
            // Ignore on emulators/simulators without haptic actuator hardware
        }
    }
}
