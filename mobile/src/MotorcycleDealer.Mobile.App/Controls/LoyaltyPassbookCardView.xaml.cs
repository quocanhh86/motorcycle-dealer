using System.Globalization;

namespace MotorcycleDealer.Mobile.App.Controls;

public partial class LoyaltyPassbookCardView : ContentView
{
    private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

    public static readonly BindableProperty MemberNameProperty = BindableProperty.Create(
        nameof(MemberName), typeof(string), typeof(LoyaltyPassbookCardView), string.Empty);

    public static readonly BindableProperty TierNameProperty = BindableProperty.Create(
        nameof(TierName), typeof(string), typeof(LoyaltyPassbookCardView), string.Empty,
        propertyChanged: (b, _, _) => ((LoyaltyPassbookCardView)b).RecalculateVisuals());

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
        coerceValue: (_, value) => Math.Clamp(value is double d ? d : 0d, 0d, 1d));

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

    public Brush CardBackgroundBrush { get; private set; } = BuildBrush("#1E293B", "#0F172A");
    public Color CardAccentBorder { get; private set; } = Color.FromArgb("#F59E0B");
    public Color TierBadgeBg { get; private set; } = Color.FromArgb("#FEF3C7");
    public Color TierBadgeFg { get; private set; } = Color.FromArgb("#78350F");
    public string TierPillText { get; private set; } = "—";
    public string AvailablePointsText { get; private set; } = "0 pts";
    public string QualifyingPointsText { get; private set; } = "0 pts";

    public LoyaltyPassbookCardView()
    {
        InitializeComponent();
        RecalculateVisuals();
    }

    private void RecalculateVisuals()
    {
        string tier = (TierName ?? string.Empty).Trim();
        string upper = tier.ToUpperInvariant();

        (string c1, string c2, string accent, string pillBg, string pillFg) = upper switch
        {
            "PLATINUM" => ("#0F172A", "#1E293B", "#38BDF8", "#E0F2FE", "#0C4A6E"),
            "GOLD"     => ("#422006", "#1C1917", "#EAB308", "#FEF9C3", "#713F12"),
            "SILVER"   => ("#1E293B", "#334155", "#CBD5E1", "#E2E8F0", "#1E293B"),
            _          => ("#291509", "#0F172A", "#F59E0B", "#FEF3C7", "#78350F")
        };

        CardBackgroundBrush = BuildBrush(c1, c2);
        CardAccentBorder = Color.FromArgb(accent);
        TierBadgeBg = Color.FromArgb(pillBg);
        TierBadgeFg = Color.FromArgb(pillFg);
        TierPillText = string.IsNullOrEmpty(upper)
            ? "—"
            : $"{upper} · {TierMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x";
        AvailablePointsText = $"{AvailablePoints.ToString("N0", ViVn)} pts";
        QualifyingPointsText = $"{LifetimeQualifyingPoints.ToString("N0", ViVn)} pts";

        OnPropertyChanged(nameof(CardBackgroundBrush));
        OnPropertyChanged(nameof(CardAccentBorder));
        OnPropertyChanged(nameof(TierBadgeBg));
        OnPropertyChanged(nameof(TierBadgeFg));
        OnPropertyChanged(nameof(TierPillText));
        OnPropertyChanged(nameof(AvailablePointsText));
        OnPropertyChanged(nameof(QualifyingPointsText));
    }

    private static LinearGradientBrush BuildBrush(string startHex, string endHex)
        => new(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(startHex), 0.0f),
                new GradientStop(Color.FromArgb(endHex), 1.0f)
            },
            new Point(0, 0),
            new Point(1, 1));
}
