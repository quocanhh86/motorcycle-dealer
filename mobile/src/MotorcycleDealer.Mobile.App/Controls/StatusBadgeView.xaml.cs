using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.App.Icons;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Semantic WCAG AA status pill combining a crisp Phosphor vector icon with
/// live localized enum text via LocalizationService.GetEnumLabel.
/// </summary>
public partial class StatusBadgeView : ContentView
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status),
        typeof(string),
        typeof(StatusBadgeView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((StatusBadgeView)b).ApplyPalette());

    public static readonly BindableProperty LabelOverrideProperty = BindableProperty.Create(
        nameof(LabelOverride),
        typeof(string),
        typeof(StatusBadgeView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((StatusBadgeView)b).ApplyPalette());

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string LabelOverride
    {
        get => (string)GetValue(LabelOverrideProperty);
        set => SetValue(LabelOverrideProperty, value);
    }

    public string DisplayText { get; private set; } = "—";
    public string StatusIconGlyph { get; private set; } = PhosphorIcons.Info;
    public Color BadgeBackground { get; private set; } = Color.FromArgb("#F1F5F9");
    public Color BadgeForeground { get; private set; } = Color.FromArgb("#334155");
    public Color BadgeBorder { get; private set; } = Color.FromArgb("#CBD5E1");

    public StatusBadgeView()
    {
        InitializeComponent();
        ApplyPalette();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }

        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<StatusBadgeView, CultureChangedMessage>(
            this,
            static (recipient, _) => MainThread.BeginInvokeOnMainThread(recipient.ApplyPalette));

        ApplyPalette();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(ApplyPalette);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        }

        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
    }

    private void ApplyPalette()
    {
        string raw = (Status ?? string.Empty).Trim();
        LocalizationService loc = LocalizationService.Current;

        DisplayText = !string.IsNullOrWhiteSpace(LabelOverride)
            ? LabelOverride.Trim()
            : string.IsNullOrEmpty(raw)
                ? loc.Common_Dash
                : loc.GetEnumLabel(string.Empty, raw);

        bool dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        string key = raw.ToUpperInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);

        (string bg, string fg, string bd, string glyph) = key switch
        {
            "COMPLETED" or "APPROVED" or "ACTIVE" or "AVAILABLE" or "CONNECTED" or "UPTODATE" or "HEALTHY" or "PAID" or "ELIGIBLE" or "INCOVERAGE" or "SUCCESS"
                => dark
                    ? ("#052E16", "#86EFAC", "#166534", PhosphorIcons.CheckCircle)
                    : ("#DCFCE7", "#15803D", "#BBF7D0", PhosphorIcons.CheckCircle),

            "UNDERREVIEW"
                => dark
                    ? ("#451A03", "#FDE68A", "#92400E", PhosphorIcons.MagnifyingGlass)
                    : ("#FEF3C7", "#92400E", "#FDE68A", PhosphorIcons.MagnifyingGlass),

            "DUESOON" or "EXPIRINGSOON" or "WARNING"
                => dark
                    ? ("#451A03", "#FDE68A", "#92400E", PhosphorIcons.Warning)
                    : ("#FEF3C7", "#92400E", "#FDE68A", PhosphorIcons.Warning),

            "RESERVED" or "RESERVEDONBOOKING"
                => dark
                    ? ("#451A03", "#FDE68A", "#92400E", PhosphorIcons.Ticket)
                    : ("#FEF3C7", "#92400E", "#FDE68A", PhosphorIcons.Ticket),

            "PENDING" or "SUBMITTED" or "REQUESTED" or "RECONNECTING" or "CONNECTING" or "QUEUED"
                => dark
                    ? ("#451A03", "#FDE68A", "#92400E", PhosphorIcons.Clock)
                    : ("#FEF3C7", "#92400E", "#FDE68A", PhosphorIcons.Clock),

            "DISCONNECTED" or "OFFLINE" or "UNAVAILABLE"
                => dark
                    ? ("#450A0A", "#FCA5A5", "#991B1B", PhosphorIcons.WifiSlash)
                    : ("#FEE2E2", "#991B1B", "#FECACA", PhosphorIcons.WifiSlash),

            "CANCELLED" or "REJECTED" or "OVERDUE" or "EXPIRED" or "NOSHOW" or "OUTOFCOVERAGE" or "LOCKED" or "SOLD" or "BLOCKED" or "DENIED" or "CLIENTERROR" or "FAILED"
                => dark
                    ? ("#450A0A", "#FCA5A5", "#991B1B", PhosphorIcons.XCircle)
                    : ("#FEE2E2", "#991B1B", "#FECACA", PhosphorIcons.XCircle),

            "CHECKEDIN"
                => dark
                    ? ("#083344", "#67E8F9", "#155E75", PhosphorIcons.Garage)
                    : ("#CFFAFE", "#155E75", "#A5F3FC", PhosphorIcons.Garage),

            // Distinct InProgress Violet (1-to-1 with MotorsportColors.xaml & Web design-tokens.css)
            "INPROGRESS" or "INSERVICE"
                => dark
                    ? ("#2E1065", "#C4B5FD", "#6D28D9", PhosphorIcons.Wrench)
                    : ("#EDE9FE", "#5B21B6", "#DDD6FE", PhosphorIcons.Wrench),

            "BRONZE"
                => dark
                    ? ("#451A03", "#FDE68A", "#D97706", PhosphorIcons.Crown)
                    : ("#FEF3C7", "#78350F", "#F59E0B", PhosphorIcons.Crown),

            "SILVER"
                => dark
                    ? ("#1E293B", "#E2E8F0", "#64748B", PhosphorIcons.Crown)
                    : ("#E2E8F0", "#1E293B", "#94A3B8", PhosphorIcons.Crown),

            "GOLD"
                => dark
                    ? ("#422006", "#FEF08A", "#CA8A04", PhosphorIcons.Crown)
                    : ("#FEF9C3", "#713F12", "#EAB308", PhosphorIcons.Crown),

            // Unified Titanium Ice-Cyan Platinum
            "PLATINUM"
                => dark
                    ? ("#0C4A6E", "#E0F2FE", "#38BDF8", PhosphorIcons.Crown)
                    : ("#E0F2FE", "#0C4A6E", "#38BDF8", PhosphorIcons.Crown),

            "CONFIRMED" or "SYNCED" or "BOOKED"
                => dark
                    ? ("#172554", "#93C5FD", "#1D4ED8", PhosphorIcons.Check)
                    : ("#DBEAFE", "#1E40AF", "#BFDBFE", PhosphorIcons.Check),

            "INFO" or "SENT" or "UPCOMING"
                => dark
                    ? ("#172554", "#93C5FD", "#1D4ED8", PhosphorIcons.Info)
                    : ("#DBEAFE", "#1E40AF", "#BFDBFE", PhosphorIcons.Info),

            _ => dark
                ? ("#1E293B", "#CBD5E1", "#475569", PhosphorIcons.Info)
                : ("#F1F5F9", "#334155", "#CBD5E1", PhosphorIcons.Info)
        };

        BadgeBackground = Color.FromArgb(bg);
        BadgeForeground = Color.FromArgb(fg);
        BadgeBorder = Color.FromArgb(bd);
        StatusIconGlyph = glyph;

        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(StatusIconGlyph));
        OnPropertyChanged(nameof(BadgeBackground));
        OnPropertyChanged(nameof(BadgeForeground));
        OnPropertyChanged(nameof(BadgeBorder));
    }
}
