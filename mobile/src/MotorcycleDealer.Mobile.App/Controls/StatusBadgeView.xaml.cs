namespace MotorcycleDealer.Mobile.App.Controls;

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
    public Color BadgeBackground { get; private set; } = Color.FromArgb("#F1F5F9");
    public Color BadgeForeground { get; private set; } = Color.FromArgb("#334155");
    public Color BadgeBorder { get; private set; } = Color.FromArgb("#CBD5E1");

    public StatusBadgeView()
    {
        InitializeComponent();
        ApplyPalette();
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }

        Unloaded += OnUnloaded;
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplyPalette();

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        }
    }

    private void ApplyPalette()
    {
        string raw = (Status ?? string.Empty).Trim();
        DisplayText = !string.IsNullOrWhiteSpace(LabelOverride)
            ? LabelOverride.Trim()
            : FormatLabel(raw);

        bool dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        string key = raw.ToUpperInvariant().Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);

        (string bg, string fg, string bd) = key switch
        {
            "COMPLETED" or "APPROVED" or "ACTIVE" or "AVAILABLE" or "CONNECTED" or "UPTODATE" or "HEALTHY" or "PAID" or "ELIGIBLE"
                => dark ? ("#052E16", "#86EFAC", "#166534") : ("#DCFCE7", "#15803D", "#BBF7D0"),

            "PENDING" or "DUESOON" or "SUBMITTED" or "UNDERREVIEW" or "RESERVED" or "RECONNECTING" or "QUEUED" or "EXPIRINGSOON"
                => dark ? ("#451A03", "#FDE68A", "#92400E") : ("#FEF3C7", "#92400E", "#FDE68A"),

            "CANCELLED" or "REJECTED" or "OVERDUE" or "EXPIRED" or "NOSHOW" or "DISCONNECTED" or "OFFLINE" or "OUTOFCOVERAGE" or "LOCKED" or "SOLD"
                => dark ? ("#450A0A", "#FCA5A5", "#991B1B") : ("#FEE2E2", "#991B1B", "#FECACA"),

            "CHECKEDIN"
                => dark ? ("#083344", "#67E8F9", "#155E75") : ("#CFFAFE", "#155E75", "#A5F3FC"),

            "INPROGRESS" or "INSERVICE"
                => dark ? ("#1E3A8A", "#93C5FD", "#3B82F6") : ("#EFF6FF", "#1D4ED8", "#93C5FD"),

            "CONFIRMED" or "INFO" or "SYNCED" or "BRONZE" or "SILVER" or "GOLD" or "PLATINUM"
                => dark ? ("#172554", "#93C5FD", "#1D4ED8") : ("#DBEAFE", "#1E40AF", "#BFDBFE"),

            _ => dark ? ("#1E293B", "#CBD5E1", "#475569") : ("#F1F5F9", "#334155", "#CBD5E1")
        };

        BadgeBackground = Color.FromArgb(bg);
        BadgeForeground = Color.FromArgb(fg);
        BadgeBorder = Color.FromArgb(bd);

        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(BadgeBackground));
        OnPropertyChanged(nameof(BadgeForeground));
        OnPropertyChanged(nameof(BadgeBorder));
    }

    private static string FormatLabel(string raw)
        => raw switch
        {
            "" => "UNKNOWN",
            "CheckedIn" => "Checked In",
            "InProgress" => "In Progress",
            "NoShow" => "No Show",
            "UnderReview" => "Under Review",
            "DueSoon" => "Due Soon",
            "UpToDate" => "Up To Date",
            _ => raw
        };
}
