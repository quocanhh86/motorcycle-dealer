using MotorcycleDealer.Mobile.App.Services;
using MotorcycleDealer.Mobile.Core.Abstractions;

namespace MotorcycleDealer.Mobile.App.Motion;

/// <summary>
/// Motorsport-tuned easing curves matching the Web SPA Quiet-Chrome deceleration profile.
/// </summary>
public static class MotorEasing
{
    /// <summary>Exponential ease-out (fast launch, smooth precision settle).</summary>
    public static readonly Easing ExpoOut = new(t => t >= 1d ? 1d : 1d - Math.Pow(2d, -10d * Math.Clamp(t, 0d, 1d)));

    /// <summary>Cubic ease-out for short micro-interactions (chevron rotation, badge swaps).</summary>
    public static readonly Easing SnappyOut = new(t => 1d - Math.Pow(1d - Math.Clamp(t, 0d, 1d), 3d));
}

/// <summary>
/// Reduced-motion-aware MAUI animation primitives for staggered entrances, chevron toggles, and safe teardown.
/// </summary>
public static class MotionHelper
{
    private const string EntranceAnimationName = "ApexEntrance";
    private const string ChevronAnimationName = "ApexChevron";
    private const string SlideFadeAnimationName = "ApexSlideFade";

    public static IMotionPreferencesService MotionPreferences { get; set; } = new MauiMotionPreferencesService();

    public static bool IsReducedMotionEnabled => MotionPreferences.IsReducedMotionEnabled;

    public static async Task AnimateEntranceAsync(
        VisualElement? element,
        int staggerIndex = 0,
        uint durationMs = 280,
        double initialOffsetY = 14d,
        CancellationToken cancellationToken = default)
    {
        if (element is null)
        {
            return;
        }

        AbortAll(element);

        if (IsReducedMotionEnabled)
        {
            element.Opacity = 1d;
            element.TranslationY = 0d;
            return;
        }

        element.Opacity = 0d;
        element.TranslationY = initialOffsetY;

        int delayMs = Math.Clamp(staggerIndex, 0, 6) * 45;
        if (delayMs > 0)
        {
            try
            {
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                element.Opacity = 1d;
                element.TranslationY = 0d;
                return;
            }
        }

        if (cancellationToken.IsCancellationRequested || !element.IsLoaded)
        {
            element.Opacity = 1d;
            element.TranslationY = 0d;
            return;
        }

        try
        {
            await Task.WhenAll(
                element.FadeToAsync(1d, durationMs, MotorEasing.ExpoOut),
                element.TranslateToAsync(0d, 0d, durationMs, MotorEasing.ExpoOut));
        }
        catch
        {
            element.Opacity = 1d;
            element.TranslationY = 0d;
        }
    }

    public static async Task RotateChevronAsync(
        VisualElement? chevron,
        bool expanded,
        uint durationMs = 200)
    {
        if (chevron is null)
        {
            return;
        }

        double targetDegrees = expanded ? 180d : 0d;
        chevron.AbortAnimation(ChevronAnimationName);

        if (IsReducedMotionEnabled || !chevron.IsLoaded)
        {
            chevron.Rotation = targetDegrees;
            return;
        }

        try
        {
            await chevron.RotateToAsync(targetDegrees, durationMs, MotorEasing.SnappyOut);
        }
        catch
        {
            chevron.Rotation = targetDegrees;
        }
    }

    public static void AbortAll(VisualElement? element)
    {
        if (element is null)
        {
            return;
        }

        try
        {
            element.CancelAnimations();
            element.AbortAnimation(EntranceAnimationName);
            element.AbortAnimation(ChevronAnimationName);
            element.AbortAnimation(SlideFadeAnimationName);
        }
        catch
        {
            // Ignore teardown races if handler is already detached
        }
    }
}
