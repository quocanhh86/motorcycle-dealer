using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.App.Motion;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// 4-Tier Metallic Foil Loyalty Passbook card (Bronze, Silver, Gold, Titanium Ice-Cyan Platinum)
/// with diagonal specular sheen, 1-shot Lottie tier sparkle (reduced-motion safe),
/// smooth ProgressTo bar transitions, and reactive VI/EN localization.
/// </summary>
public partial class LoyaltyPassbookCardView : ContentView
{
    private const string SheenAnimationName = "PassbookFoilSheen";
    private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");
    private string _lastTierCode = string.Empty;

    public static readonly BindableProperty MemberNameProperty = BindableProperty.Create(
        nameof(MemberName), typeof(string), typeof(LoyaltyPassbookCardView), string.Empty);

    public static readonly BindableProperty TierNameProperty = BindableProperty.Create(
        nameof(TierName), typeof(string), typeof(LoyaltyPassbookCardView), string.Empty,
        propertyChanged: (b, _, _) => ((LoyaltyPassbookCardView)b).OnTierVisualChanged());

    public static readonly BindableProperty TierMultiplierProperty = BindableProperty.Create(
        nameof(TierMultiplier), typeof(decimal), typeof(LoyaltyPassbookCardView), 1.00m,
        propertyChanged: (b, _, _) => ((LoyaltyPassbookCardView)b).RecalculateVisuals());

    public static readonly BindableProperty AvailablePointsProperty = BindableProperty.Create(
        nameof(AvailablePoints), typeof(int), typeof(LoyaltyPassbookCardView), 0,
        propertyChanged: (b, _, _) => ((LoyaltyPassbookCardView)b).RecalculateVisuals());

    public static readonly BindableProperty LifetimeQualifyingPointsProperty = BindableProperty.Create(
        nameof(LifetimeQualifyingPoints), typeof(int), typeof(LoyaltyPassbookCardView), 0,
        propertyChanged: (b, _, _) => ((LoyaltyPassbookCardView)b).RecalculateVisuals());

    public static readonly BindableProperty TierProgressRatioProperty = BindableProperty.Create(
        nameof(TierProgressRatio), typeof(double), typeof(LoyaltyPassbookCardView), 0d,
        coerceValue: (_, value) => Math.Clamp(value is double d ? d : 0d, 0d, 1d),
        propertyChanged: (b, _, _) => _ = ((LoyaltyPassbookCardView)b).AnimateProgressAsync());

    public static readonly BindableProperty NextTierHintProperty = BindableProperty.Create(
        nameof(NextTierHint), typeof(string), typeof(LoyaltyPassbookCardView), string.Empty);

    public static readonly BindableProperty SyncCaptionProperty = BindableProperty.Create(
        nameof(SyncCaption), typeof(string), typeof(LoyaltyPassbookCardView), string.Empty);

    public string MemberName
    {
        get => (string)GetValue(MemberNameProperty);
        set => SetValue(MemberNameProperty, value);
    }

    public string TierName
    {
        get => (string)GetValue(TierNameProperty);
        set => SetValue(TierNameProperty, value);
    }

    public decimal TierMultiplier
    {
        get => (decimal)GetValue(TierMultiplierProperty);
        set => SetValue(TierMultiplierProperty, value);
    }

    public int AvailablePoints
    {
        get => (int)GetValue(AvailablePointsProperty);
        set => SetValue(AvailablePointsProperty, value);
    }

    public int LifetimeQualifyingPoints
    {
        get => (int)GetValue(LifetimeQualifyingPointsProperty);
        set => SetValue(LifetimeQualifyingPointsProperty, value);
    }

    public double TierProgressRatio
    {
        get => (double)GetValue(TierProgressRatioProperty);
        set => SetValue(TierProgressRatioProperty, value);
    }

    public string NextTierHint
    {
        get => (string)GetValue(NextTierHintProperty);
        set => SetValue(NextTierHintProperty, value);
    }

    public string SyncCaption
    {
        get => (string)GetValue(SyncCaptionProperty);
        set => SetValue(SyncCaptionProperty, value);
    }

    public LocalizationService Loc => LocalizationService.Current;

    public Brush CardBackgroundBrush { get; private set; } = BuildFoilBrush("#451A03", "#92400E", "#B45309");
    public Color CardAccentBorder { get; private set; } = Color.FromArgb("#F59E0B");
    public Color TierBadgeBg { get; private set; } = Color.FromArgb("#FEF3C7");
    public Color TierBadgeFg { get; private set; } = Color.FromArgb("#78350F");
    public string TierPillText { get; private set; } = "—";
    public string AvailablePointsText { get; private set; } = "0";
    public string QualifyingPointsText { get; private set; } = "0";

    public LoyaltyPassbookCardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RecalculateVisuals();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            UpdateSheenAnimation();
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<LoyaltyPassbookCardView, CultureChangedMessage>(
            this,
            static (recipient, _) => MainThread.BeginInvokeOnMainThread(recipient.RecalculateVisuals));

        RecalculateVisuals();
        _ = AnimateProgressAsync();
        UpdateSheenAnimation();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        StopSheenAnimation();
    }

    private void OnTierVisualChanged()
    {
        string normalizedTier = (TierName ?? string.Empty).Trim().ToUpperInvariant();
        bool changed = !string.IsNullOrEmpty(normalizedTier)
            && !string.Equals(_lastTierCode, normalizedTier, StringComparison.Ordinal);

        _lastTierCode = normalizedTier;
        RecalculateVisuals();

        if (changed && IsLoaded && IsVisible)
        {
            TierSparkle.Replay();
        }
    }

    private void RecalculateVisuals()
    {
        string tier = (TierName ?? string.Empty).Trim();
        string upper = tier.ToUpperInvariant();

        // Canonical 3-stop metallic foil gradients matching MotorsportColors.xaml Foil* tokens
        (string c1, string c2, string c3, string rim, string pillBg, string pillFg) = upper switch
        {
            "PLATINUM" => ("#082F49", "#0C4A6E", "#0369A1", "#38BDF8", "#E0F2FE", "#0C4A6E"),
            "GOLD"     => ("#422006", "#A16207", "#CA8A04", "#FACC15", "#FEF9C3", "#713F12"),
            "SILVER"   => ("#0F172A", "#334155", "#64748B", "#CBD5E1", "#E2E8F0", "#1E293B"),
            _          => ("#451A03", "#92400E", "#B45309", "#F59E0B", "#FEF3C7", "#78350F")
        };

        LocalizationService loc = LocalizationService.Current;
        string ptsUnit = loc.Common_PointsShort;

        CardBackgroundBrush = BuildFoilBrush(c1, c2, c3);
        CardAccentBorder = Color.FromArgb(rim);
        TierBadgeBg = Color.FromArgb(pillBg);
        TierBadgeFg = Color.FromArgb(pillFg);
        TierPillText = string.IsNullOrEmpty(upper)
            ? loc.Common_Dash
            : loc.Format("LoyaltyTierPillFormat", loc.GetEnumLabel("LoyaltyTier", tier).ToUpper(loc.CurrentCulture), TierMultiplier);
        AvailablePointsText = $"{AvailablePoints.ToString("N0", ViVn)} {ptsUnit}";
        QualifyingPointsText = $"{LifetimeQualifyingPoints.ToString("N0", ViVn)} {ptsUnit}";

        OnPropertyChanged(nameof(CardBackgroundBrush));
        OnPropertyChanged(nameof(CardAccentBorder));
        OnPropertyChanged(nameof(TierBadgeBg));
        OnPropertyChanged(nameof(TierBadgeFg));
        OnPropertyChanged(nameof(TierPillText));
        OnPropertyChanged(nameof(AvailablePointsText));
        OnPropertyChanged(nameof(QualifyingPointsText));
    }

    private async Task AnimateProgressAsync()
    {
        double target = Math.Clamp(TierProgressRatio, 0d, 1d);
        if (MotionHelper.IsReducedMotionEnabled || !IsLoaded)
        {
            TierProgressBar.Progress = target;
            return;
        }

        try
        {
            await TierProgressBar.ProgressTo(target, 550, MotorEasing.ExpoOut);
        }
        catch
        {
            TierProgressBar.Progress = target;
        }
    }

    private bool ShouldAnimateSheen => IsLoaded && IsVisible && !MotionHelper.IsReducedMotionEnabled;

    private void UpdateSheenAnimation()
    {
        if (!ShouldAnimateSheen)
        {
            StopSheenAnimation();
            return;
        }

        StopSheenAnimation();
        SpecularSheenOverlay.IsVisible = true;
        SpecularSheenOverlay.TranslationX = -180;

        // Glides specular sheen across card during the first 35% of the cycle, then rests cleanly
        var sheen = new Animation();
        sheen.Add(0.0, 0.36, new Animation(
            v =>
            {
                double span = Width > 100d ? Width + 240d : 540d;
                SpecularSheenOverlay.TranslationX = -180d + (v * span);
            },
            0d,
            1d,
            Easing.SinInOut));

        sheen.Commit(
            owner: this,
            name: SheenAnimationName,
            rate: 16,
            length: 4400,
            repeat: () => ShouldAnimateSheen);
    }

    private void StopSheenAnimation()
    {
        this.AbortAnimation(SheenAnimationName);
        SpecularSheenOverlay.IsVisible = false;
        SpecularSheenOverlay.TranslationX = -180;
    }

    private static LinearGradientBrush BuildFoilBrush(string startHex, string midHex, string endHex)
        => new(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(startHex), 0.0f),
                new GradientStop(Color.FromArgb(midHex), 0.52f),
                new GradientStop(Color.FromArgb(endHex), 1.0f)
            },
            new Point(0, 0),
            new Point(1, 1));
}
