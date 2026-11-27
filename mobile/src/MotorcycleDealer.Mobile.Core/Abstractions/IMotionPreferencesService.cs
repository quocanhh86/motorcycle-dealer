namespace MotorcycleDealer.Mobile.Core.Abstractions;

/// <summary>
/// Pure-Core OS accessibility reduced-motion contract.
/// Mobile.App implements this using Android AnimatorDurationScale and iOS UIAccessibility.IsReduceMotionEnabled.
/// </summary>
public interface IMotionPreferencesService
{
    bool IsReducedMotionEnabled { get; }
}

/// <summary>
/// Default pure-Core motion preferences fallback (animations enabled) registered via TryAddSingleton.
/// </summary>
public sealed class DefaultMotionPreferencesService : IMotionPreferencesService
{
    public static DefaultMotionPreferencesService Instance { get; } = new();

    public bool IsReducedMotionEnabled => false;
}
