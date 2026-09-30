using MotorcycleDealer.Mobile.App.Icons;
using MotorcycleDealer.Mobile.App.Motion;
using SkiaSharp.Extended.UI.Controls;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Renders a self-hosted SkiaSharp SKLottieView animation from Resources/Raw when motion is enabled,
/// automatically falling back to a crisp Phosphor vector icon under OS reduced-motion or load errors,
/// and halting the Skia render loop cleanly on Unloaded / IsVisible=False.
/// </summary>
public partial class LottieOrIconView : ContentView
{
    private string _loadedLottieFile = string.Empty;
    private bool _lottieFailed;

    public static readonly BindableProperty LottieFileProperty = BindableProperty.Create(
        nameof(LottieFile),
        typeof(string),
        typeof(LottieOrIconView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((LottieOrIconView)b).OnLottieFileChanged());

    public static readonly BindableProperty FallbackGlyphProperty = BindableProperty.Create(
        nameof(FallbackGlyph),
        typeof(string),
        typeof(LottieOrIconView),
        defaultValue: PhosphorIcons.Sparkle);

    public static readonly BindableProperty FallbackColorProperty = BindableProperty.Create(
        nameof(FallbackColor),
        typeof(Color),
        typeof(LottieOrIconView),
        defaultValue: Color.FromArgb("#DC2626"));

    public static readonly BindableProperty RepeatCountProperty = BindableProperty.Create(
        nameof(RepeatCount),
        typeof(int),
        typeof(LottieOrIconView),
        defaultValue: -1,
        propertyChanged: (b, _, _) => ((LottieOrIconView)b).SyncVisualState());

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size),
        typeof(double),
        typeof(LottieOrIconView),
        defaultValue: 48d,
        propertyChanged: (b, _, _) => ((LottieOrIconView)b).OnPropertyChanged(nameof(IconFontSize)));

    public string LottieFile
    {
        get => (string)GetValue(LottieFileProperty);
        set => SetValue(LottieFileProperty, value);
    }

    public string FallbackGlyph
    {
        get => (string)GetValue(FallbackGlyphProperty);
        set => SetValue(FallbackGlyphProperty, value);
    }

    public Color FallbackColor
    {
        get => (Color)GetValue(FallbackColorProperty);
        set => SetValue(FallbackColorProperty, value);
    }

    public int RepeatCount
    {
        get => (int)GetValue(RepeatCountProperty);
        set => SetValue(RepeatCountProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double IconFontSize => Math.Max(18d, Size * 0.62d);

    public LottieOrIconView()
    {
        InitializeComponent();
        LottiePlayer.AnimationFailed += OnLottieAnimationFailed;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SyncVisualState();
    }

    public void Replay()
    {
        if (MotionHelper.IsReducedMotionEnabled || _lottieFailed || string.IsNullOrWhiteSpace(LottieFile))
        {
            return;
        }

        try
        {
            LottiePlayer.IsAnimationEnabled = false;
            LottiePlayer.Progress = TimeSpan.Zero;
            LottiePlayer.IsAnimationEnabled = IsLoaded && IsVisible;
        }
        catch
        {
            SwitchToFallbackIcon();
        }
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            SyncVisualState();
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        SyncVisualState();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        try
        {
            LottiePlayer.IsAnimationEnabled = false;
        }
        catch
        {
            // Defensive on platform handler teardown
        }
    }

    private void OnLottieFileChanged()
    {
        _lottieFailed = false;
        SyncVisualState();
    }

    private void OnLottieAnimationFailed(object? sender, SKLottieAnimationFailedEventArgs e)
    {
        _lottieFailed = true;
        MainThread.BeginInvokeOnMainThread(SwitchToFallbackIcon);
    }

    private void SyncVisualState()
    {
        string file = (LottieFile ?? string.Empty).Trim();
        bool canUseLottie = !MotionHelper.IsReducedMotionEnabled
            && !string.IsNullOrWhiteSpace(file)
            && !_lottieFailed;

        if (!canUseLottie)
        {
            SwitchToFallbackIcon();
            return;
        }

        try
        {
            if (!string.Equals(_loadedLottieFile, file, StringComparison.Ordinal))
            {
                _loadedLottieFile = file;
                LottiePlayer.Source = new SKFileLottieImageSource { File = file };
            }

            LottiePlayer.RepeatCount = RepeatCount;
            LottiePlayer.RepeatMode = SKLottieRepeatMode.Restart;
            LottiePlayer.IsVisible = true;
            FallbackIconLabel.IsVisible = false;
            LottiePlayer.IsAnimationEnabled = IsLoaded && IsVisible;
        }
        catch
        {
            _lottieFailed = true;
            SwitchToFallbackIcon();
        }
    }

    private void SwitchToFallbackIcon()
    {
        try
        {
            LottiePlayer.IsAnimationEnabled = false;
        }
        catch
        {
            // Ignore teardown race
        }

        LottiePlayer.IsVisible = false;
        FallbackIconLabel.IsVisible = true;
    }
}
