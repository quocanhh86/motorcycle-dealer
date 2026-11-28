using System.Windows.Input;
using MotorcycleDealer.Mobile.App.Motion;

namespace MotorcycleDealer.Mobile.App.Behaviors;

/// <summary>
/// Drop-in theme-safe and Android-15-safe tactile press behavior replacing CommunityToolkit.Maui TouchBehavior.
/// Uses native TapGestureRecognizer + GPU ScaleToAsync/FadeToAsync only: never writes BackgroundColor (preserving
/// AppThemeBinding in Light &amp; Dark modes) and avoids CommunityToolkit.Maui TouchBehavior+AccessibilityListener
/// JNI activation crashes on Android 15 (Issue #2950).
/// </summary>
public sealed class ApexPressBehavior : Behavior<View>
{
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command),
        typeof(ICommand),
        typeof(ApexPressBehavior));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter),
        typeof(object),
        typeof(ApexPressBehavior));

    public static readonly BindableProperty PressedScaleProperty = BindableProperty.Create(
        nameof(PressedScale),
        typeof(double),
        typeof(ApexPressBehavior),
        defaultValue: 0.97d);

    public static readonly BindableProperty PressedOpacityProperty = BindableProperty.Create(
        nameof(PressedOpacity),
        typeof(double),
        typeof(ApexPressBehavior),
        defaultValue: 0.90d);

    public static readonly BindableProperty PressedAnimationDurationProperty = BindableProperty.Create(
        nameof(PressedAnimationDuration),
        typeof(int),
        typeof(ApexPressBehavior),
        defaultValue: 110);

    public static readonly BindableProperty ShouldMakeChildrenInputTransparentProperty = BindableProperty.Create(
        nameof(ShouldMakeChildrenInputTransparent),
        typeof(bool),
        typeof(ApexPressBehavior),
        defaultValue: false);

    private View? _associatedView;
    private TapGestureRecognizer? _tapGesture;
    private bool _isHandlingTap;

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public double PressedScale
    {
        get => (double)GetValue(PressedScaleProperty);
        set => SetValue(PressedScaleProperty, value);
    }

    public double PressedOpacity
    {
        get => (double)GetValue(PressedOpacityProperty);
        set => SetValue(PressedOpacityProperty, value);
    }

    public int PressedAnimationDuration
    {
        get => (int)GetValue(PressedAnimationDurationProperty);
        set => SetValue(PressedAnimationDurationProperty, value);
    }

    public bool ShouldMakeChildrenInputTransparent
    {
        get => (bool)GetValue(ShouldMakeChildrenInputTransparentProperty);
        set => SetValue(ShouldMakeChildrenInputTransparentProperty, value);
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedView = bindable;
        BindingContext = bindable.BindingContext;
        bindable.BindingContextChanged += OnBindableBindingContextChanged;

        if (ShouldMakeChildrenInputTransparent && bindable is Border border && border.Content is VisualElement child)
        {
            child.InputTransparent = true;
        }

        _tapGesture = new TapGestureRecognizer();
        _tapGesture.Tapped += OnViewTapped;
        bindable.GestureRecognizers.Add(_tapGesture);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.BindingContextChanged -= OnBindableBindingContextChanged;
        bindable.CancelAnimations();
        bindable.Scale = 1.0d;
        bindable.Opacity = 1.0d;
        _isHandlingTap = false;

        if (_tapGesture is not null)
        {
            _tapGesture.Tapped -= OnViewTapped;
            bindable.GestureRecognizers.Remove(_tapGesture);
            _tapGesture = null;
        }

        _associatedView = null;
        BindingContext = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnBindableBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is BindableObject bindable)
        {
            BindingContext = bindable.BindingContext;
        }
    }

    private async void OnViewTapped(object? sender, TappedEventArgs e)
    {
        View? view = _associatedView;
        if (view is null || !view.IsEnabled || _isHandlingTap)
        {
            return;
        }

        ICommand? command = Command;
        object? parameter = CommandParameter;
        if (command is null || !command.CanExecute(parameter))
        {
            return;
        }

        _isHandlingTap = true;
        try
        {
            if (!MotionHelper.IsReducedMotionEnabled)
            {
                uint downMs = (uint)Math.Clamp(PressedAnimationDuration / 2, 45, 95);
                uint upMs = (uint)Math.Clamp(PressedAnimationDuration, 75, 160);
                double targetScale = Math.Clamp(PressedScale, 0.90d, 1.0d);
                double targetOpacity = Math.Clamp(PressedOpacity, 0.60d, 1.0d);

                try
                {
                    await Task.WhenAll(
                        view.ScaleToAsync(targetScale, downMs, Easing.CubicOut),
                        view.FadeToAsync(targetOpacity, downMs, Easing.CubicOut));
                }
                catch
                {
                    // View detached during press animation
                }

                try
                {
                    if (_associatedView == view && view.IsEnabled && command.CanExecute(parameter))
                    {
                        command.Execute(parameter);
                    }
                }
                finally
                {
                    try
                    {
                        if (_associatedView == view)
                        {
                            await Task.WhenAll(
                                view.ScaleToAsync(1.0d, upMs, Easing.CubicOut),
                                view.FadeToAsync(1.0d, upMs, Easing.CubicOut));
                        }
                    }
                    catch
                    {
                        // View detached during release animation
                    }
                }
            }
            else
            {
                command.Execute(parameter);
            }
        }
        catch
        {
            // Never allow unhandled async void exception from a UI gesture
        }
        finally
        {
            _isHandlingTap = false;
        }
    }
}
