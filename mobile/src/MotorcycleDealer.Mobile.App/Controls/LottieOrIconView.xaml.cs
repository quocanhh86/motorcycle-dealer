using System.ComponentModel;
using MotorcycleDealer.Mobile.App.Icons;
using MotorcycleDealer.Mobile.App.Motion;
using SkiaSharp.Extended.UI.Controls;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Renders a self-hosted SkiaSharp SKLottieView animation from Resources/Raw when motion is enabled,
/// automatically falling back to a crisp Phosphor vector icon under OS reduced-motion or load errors,
/// halting the Skia render loop cleanly on Unloaded / IsVisible=False / hidden visual ancestors,
/// and auto-replaying 1-shot (RepeatCount == 0) animations when becoming visible.
/// </summary>
public partial class LottieOrIconView : ContentView
{
    private readonly List<VisualElement> _observedAncestors = [];
    private string _loadedLottieFile = string.Empty;
    private bool _lottieFailed;
    private bool _wasEffectivelyVisible;

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

    private bool IsEffectivelyVisible => IsLoaded && IsVisible && AreAllAncestorsVisible();

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
        string file = (LottieFile ?? string.Empty).Trim();
        if (MotionHelper.IsReducedMotionEnabled || _lottieFailed || string.IsNullOrWhiteSpace(file))
        {
            return;
        }

        try
        {
            EnsureLottieSourceLoaded(file);
            LottiePlayer.RepeatCount = RepeatCount;
            LottiePlayer.RepeatMode = SKLottieRepeatMode.Restart;
            LottiePlayer.IsVisible = true;
            FallbackIconLabel.IsVisible = false;

            bool canPlay = IsEffectivelyVisible;
            _wasEffectivelyVisible = canPlay;
            LottiePlayer.IsAnimationEnabled = false;
            LottiePlayer.Progress = TimeSpan.Zero;
            LottiePlayer.IsAnimationEnabled = canPlay;
        }
        catch
        {
            _lottieFailed = true;
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

    protected override void OnParentChanged()
    {
        base.OnParentChanged();
        if (Parent is null)
        {
            DetachAncestorVisibilityWatchers();
        }
        else if (IsLoaded)
        {
            AttachAncestorVisibilityWatchers();
        }

        SyncVisualState();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        AttachAncestorVisibilityWatchers();
        SyncVisualState();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        DetachAncestorVisibilityWatchers();
        _wasEffectivelyVisible = false;
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

    private void OnAncestorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == IsVisibleProperty.PropertyName)
        {
            RunOnMainThread(SyncVisualState);
        }
        else if (string.Equals(e.PropertyName, nameof(Parent), StringComparison.Ordinal))
        {
            RunOnMainThread(() =>
            {
                if (IsLoaded)
                {
                    AttachAncestorVisibilityWatchers();
                }

                SyncVisualState();
            });
        }
    }

    private bool AreAllAncestorsVisible()
    {
        Element? current = Parent;
        while (current is not null)
        {
            if (current is VisualElement visual && !visual.IsVisible)
            {
                return false;
            }

            current = current.Parent;
        }

        return true;
    }

    private void AttachAncestorVisibilityWatchers()
    {
        DetachAncestorVisibilityWatchers();

        Element? current = Parent;
        while (current is not null)
        {
            if (current is VisualElement visual)
            {
                visual.PropertyChanged += OnAncestorPropertyChanged;
                _observedAncestors.Add(visual);
            }

            current = current.Parent;
        }
    }

    private void DetachAncestorVisibilityWatchers()
    {
        for (int i = 0; i < _observedAncestors.Count; i++)
        {
            _observedAncestors[i].PropertyChanged -= OnAncestorPropertyChanged;
        }

        _observedAncestors.Clear();
    }

    private void EnsureLottieSourceLoaded(string file)
    {
        if (!string.Equals(_loadedLottieFile, file, StringComparison.Ordinal))
        {
            _loadedLottieFile = file;
            LottiePlayer.Source = new SKFileLottieImageSource { File = file };
        }
    }

    private void SyncVisualState()
    {
        string file = (LottieFile ?? string.Empty).Trim();
        bool canUseLottie = !MotionHelper.IsReducedMotionEnabled
            && !string.IsNullOrWhiteSpace(file)
            && !_lottieFailed;

        if (!canUseLottie)
        {
            _wasEffectivelyVisible = false;
            SwitchToFallbackIcon();
            return;
        }

        try
        {
            EnsureLottieSourceLoaded(file);

            LottiePlayer.RepeatCount = RepeatCount;
            LottiePlayer.RepeatMode = SKLottieRepeatMode.Restart;
            LottiePlayer.IsVisible = true;
            FallbackIconLabel.IsVisible = false;

            bool isEffectivelyVisible = IsEffectivelyVisible;
            bool becameVisible = isEffectivelyVisible && !_wasEffectivelyVisible;
            _wasEffectivelyVisible = isEffectivelyVisible;

            if (!isEffectivelyVisible)
            {
                LottiePlayer.IsAnimationEnabled = false;
                return;
            }

            if (becameVisible && RepeatCount == 0)
            {
                LottiePlayer.IsAnimationEnabled = false;
                LottiePlayer.Progress = TimeSpan.Zero;
            }

            LottiePlayer.IsAnimationEnabled = true;
        }
        catch
        {
            _lottieFailed = true;
            _wasEffectivelyVisible = false;
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

    private static void RunOnMainThread(Action action)
    {
        if (MainThread.IsMainThread)
        {
            action();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }
}
