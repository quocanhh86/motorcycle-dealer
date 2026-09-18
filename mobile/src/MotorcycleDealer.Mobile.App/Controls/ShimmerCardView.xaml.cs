namespace MotorcycleDealer.Mobile.App.Controls;

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

    private void OnLoaded(object? sender, EventArgs e)
    {
        var pulse = new Animation
        {
            { 0.0, 0.5, new Animation(v => PulseContainer.Opacity = v, 1.0, 0.35) },
            { 0.5, 1.0, new Animation(v => PulseContainer.Opacity = v, 0.35, 1.0) }
        };

        pulse.Commit(PulseContainer, AnimationName, rate: 32, length: 1200, repeat: () => IsLoaded);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        PulseContainer.AbortAnimation(AnimationName);
    }
}
