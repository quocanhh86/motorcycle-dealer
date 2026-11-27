using MotorcycleDealer.Mobile.App.Motion;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Legacy single-card shimmer skeleton preserved for backward compatibility.
/// Automatically pauses pulse animation on IsVisible=False, Unloaded, or OS reduced-motion.
/// </summary>
public partial class ShimmerCardView : ContentView
{
    private const string AnimationName = "ShimmerPulse";

    public static readonly BindableProperty CardHeightProperty = BindableProperty.Create(
        nameof(CardHeight),
        typeof(double),
        typeof(ShimmerCardView),
        defaultValue: 112d);

    public double CardHeight
    {
        get => (double)GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    public ShimmerCardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            UpdateAnimationState();
        }
    }

    private bool ShouldAnimate => IsLoaded && IsVisible && !MotionHelper.IsReducedMotionEnabled;

    private void OnLoaded(object? sender, EventArgs e)
    {
        UpdateAnimationState();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        StopPulse();
    }

    private void UpdateAnimationState()
    {
        if (!ShouldAnimate)
        {
            StopPulse();
            return;
        }

        PulseContainer.AbortAnimation(AnimationName);
        var pulse = new Animation
        {
            { 0.0, 0.5, new Animation(v => PulseContainer.Opacity = v, 1.0, 0.42) },
            { 0.5, 1.0, new Animation(v => PulseContainer.Opacity = v, 0.42, 1.0) }
        };

        pulse.Commit(PulseContainer, AnimationName, rate: 24, length: 1400, repeat: () => ShouldAnimate);
    }

    private void StopPulse()
    {
        PulseContainer.AbortAnimation(AnimationName);
        PulseContainer.Opacity = 1.0;
    }
}
