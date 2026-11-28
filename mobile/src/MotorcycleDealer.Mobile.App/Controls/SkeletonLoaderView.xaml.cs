using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.Shapes;
using MotorcycleDealer.Mobile.App.Motion;
using MotorcycleDealer.Mobile.Core.Messaging;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Multi-variant motorsport skeleton loader supporting ShowroomCard, KpiBento, ListRow, DetailHero, and PassbookCard.
/// Runs a single coordinated 1400ms GPU-friendly shimmer sweep that pauses automatically on IsVisible=False,
/// Unloaded, or OS reduced-motion (falling back to a crisp static surface).
/// </summary>
public partial class SkeletonLoaderView : ContentView
{
    private const string ShimmerAnimationName = "ApexSkeletonShimmerSweep";
    private readonly List<VisualElement> _sweepOverlays = [];
    private bool _themeDirty;

    public static readonly BindableProperty VariantProperty = BindableProperty.Create(
        nameof(Variant),
        typeof(string),
        typeof(SkeletonLoaderView),
        defaultValue: "ShowroomCard",
        propertyChanged: (b, _, _) => ((SkeletonLoaderView)b).RebuildSkeletons());

    public static readonly BindableProperty CountProperty = BindableProperty.Create(
        nameof(Count),
        typeof(int),
        typeof(SkeletonLoaderView),
        defaultValue: 2,
        coerceValue: (_, v) => Math.Clamp(v is int count ? count : 2, 1, 8),
        propertyChanged: (b, _, _) => ((SkeletonLoaderView)b).RebuildSkeletons());

    public string Variant
    {
        get => (string)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public SkeletonLoaderView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RebuildSkeletons();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            if (IsVisible && IsLoaded && _themeDirty)
            {
                _themeDirty = false;
                RebuildSkeletons();
                return;
            }

            UpdateAnimationState();
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<SkeletonLoaderView, ThemeChangedMessage>(
            this,
            static (view, msg) =>
            {
                bool isDark = string.Equals(msg.ThemeMode?.Trim(), "dark", StringComparison.OrdinalIgnoreCase);
                view.HandleThemeUpdated(isDark);
            });

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        if (_themeDirty && IsVisible)
        {
            _themeDirty = false;
            RebuildSkeletons();
            return;
        }

        UpdateAnimationState();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        StopShimmer();
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        => HandleThemeUpdated(isDarkOverride: null);

    private void HandleThemeUpdated(bool? isDarkOverride)
    {
        if (!IsLoaded || !IsVisible)
        {
            _themeDirty = true;
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!IsLoaded || !IsVisible)
            {
                _themeDirty = true;
                return;
            }

            _themeDirty = false;
            RebuildSkeletons(isDarkOverride);
        });
    }

    private bool ShouldAnimate => IsLoaded && IsVisible && !MotionHelper.IsReducedMotionEnabled && _sweepOverlays.Count > 0;

    private static bool ResolveIsDarkTheme() =>
        Application.Current?.UserAppTheme == AppTheme.Dark ||
        (Application.Current?.UserAppTheme == AppTheme.Unspecified && Application.Current?.RequestedTheme == AppTheme.Dark);

    private void RebuildSkeletons(bool? isDarkOverride = null)
    {
        StopShimmer();
        ItemsHost.Children.Clear();
        _sweepOverlays.Clear();

        bool isDark = isDarkOverride ?? ResolveIsDarkTheme();
        Color cardBg = isDark ? Color.FromArgb("#0F172A") : Color.FromArgb("#FFFFFF");
        Color cardStroke = isDark ? Color.FromArgb("#334155") : Color.FromArgb("#E2E8F0");
        Color boneColor = isDark ? Color.FromArgb("#1E293B") : Color.FromArgb("#E2E8F0");
        Color boneAccent = isDark ? Color.FromArgb("#334155") : Color.FromArgb("#CBD5E1");
        Color sheenMid = isDark ? Color.FromArgb("#4294A3B8") : Color.FromArgb("#B8FFFFFF");

        string normalized = (Variant ?? "ShowroomCard").Trim();
        int itemCount = Math.Clamp(Count, 1, 8);

        if (string.Equals(normalized, "KpiBento", StringComparison.OrdinalIgnoreCase))
        {
            int rows = Math.Max(1, (itemCount + 1) / 2);
            for (int r = 0; r < rows; r++)
            {
                var rowGrid = new Grid
                {
                    ColumnDefinitions =
                    [
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star)
                    ],
                    ColumnSpacing = 10
                };

                rowGrid.Add(WrapWithShimmer(BuildKpiTileContent(boneColor, boneAccent), cardBg, cardStroke, sheenMid, 12, 14), 0, 0);
                if ((r * 2) + 1 < itemCount || itemCount == 1)
                {
                    rowGrid.Add(WrapWithShimmer(BuildKpiTileContent(boneColor, boneAccent), cardBg, cardStroke, sheenMid, 12, 14), 1, 0);
                }

                ItemsHost.Children.Add(rowGrid);
            }
        }
        else
        {
            for (int i = 0; i < itemCount; i++)
            {
                View content = normalized.ToLowerInvariant() switch
                {
                    "listrow" => BuildListRowContent(boneColor, boneAccent),
                    "detailhero" => BuildDetailHeroContent(boneColor, boneAccent),
                    "passbookcard" => BuildPassbookCardContent(boneColor, boneAccent),
                    _ => BuildShowroomCardContent(boneColor, boneAccent)
                };

                double radius = string.Equals(normalized, "PassbookCard", StringComparison.OrdinalIgnoreCase) ? 16 : 12;
                ItemsHost.Children.Add(WrapWithShimmer(content, cardBg, cardStroke, sheenMid, radius, 14));
            }
        }

        UpdateAnimationState();
    }

    private Border WrapWithShimmer(View content, Color cardBg, Color cardStroke, Color sheenMid, double cornerRadius, double padding)
    {
        var sweepOverlay = new Border
        {
            WidthRequest = 140,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Fill,
            StrokeThickness = 0,
            InputTransparent = true,
            IsVisible = !MotionHelper.IsReducedMotionEnabled,
            TranslationX = -160,
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.Transparent, 0.0f),
                    new GradientStop(sheenMid, 0.5f),
                    new GradientStop(Colors.Transparent, 1.0f)
                },
                new Point(0, 0.2),
                new Point(1, 0.8))
        };

        _sweepOverlays.Add(sweepOverlay);

        var hostGrid = new Grid
        {
            IsClippedToBounds = true
        };
        hostGrid.Children.Add(content);
        hostGrid.Children.Add(sweepOverlay);

        return new Border
        {
            BackgroundColor = cardBg,
            Stroke = cardStroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(cornerRadius) },
            Padding = new Thickness(padding),
            Content = hostGrid
        };
    }

    private static View BuildShowroomCardContent(Color bone, Color accent)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(Bone(height: 148, width: -1, radius: 10, color: bone));

        var titleRow = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            ],
            ColumnSpacing = 12
        };
        titleRow.Add(Bone(height: 16, width: 170, radius: 4, color: accent), 0, 0);
        titleRow.Add(Bone(height: 22, width: 72, radius: 6, color: bone), 1, 0);
        stack.Children.Add(titleRow);

        stack.Children.Add(Bone(height: 12, width: 220, radius: 4, color: bone));

        var metaRow = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            ],
            ColumnSpacing = 10
        };
        metaRow.Add(Bone(height: 28, width: -1, radius: 6, color: bone), 0, 0);
        metaRow.Add(Bone(height: 28, width: -1, radius: 6, color: accent), 1, 0);
        stack.Children.Add(metaRow);

        return stack;
    }

    private static View BuildKpiTileContent(Color bone, Color accent)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        var topRow = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            ],
            ColumnSpacing = 8
        };
        topRow.Add(Bone(height: 24, width: 24, radius: 6, color: accent), 0, 0);
        topRow.Add(Bone(height: 11, width: 82, radius: 4, color: bone, verticalOptions: LayoutOptions.Center), 1, 0);
        stack.Children.Add(topRow);
        stack.Children.Add(Bone(height: 24, width: 96, radius: 6, color: accent));
        stack.Children.Add(Bone(height: 10, width: 110, radius: 4, color: bone));
        return stack;
    }

    private static View BuildListRowContent(Color bone, Color accent)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            ],
            ColumnSpacing = 12
        };

        grid.Add(Bone(height: 52, width: 52, radius: 8, color: bone, verticalOptions: LayoutOptions.Center), 0, 0);

        var center = new VerticalStackLayout { Spacing = 7, VerticalOptions = LayoutOptions.Center };
        center.Children.Add(Bone(height: 14, width: 150, radius: 4, color: accent));
        center.Children.Add(Bone(height: 11, width: 190, radius: 4, color: bone));
        grid.Add(center, 1, 0);

        grid.Add(Bone(height: 24, width: 64, radius: 6, color: bone, verticalOptions: LayoutOptions.Center), 2, 0);
        return grid;
    }

    private static View BuildDetailHeroContent(Color bone, Color accent)
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(Bone(height: 176, width: -1, radius: 10, color: bone));
        stack.Children.Add(Bone(height: 20, width: 210, radius: 5, color: accent));
        stack.Children.Add(Bone(height: 13, width: -1, radius: 4, color: bone));
        stack.Children.Add(Bone(height: 13, width: 240, radius: 4, color: bone));

        var chipRow = new HorizontalStackLayout { Spacing = 8 };
        chipRow.Children.Add(Bone(height: 28, width: 84, radius: 6, color: bone));
        chipRow.Children.Add(Bone(height: 28, width: 96, radius: 6, color: bone));
        chipRow.Children.Add(Bone(height: 28, width: 76, radius: 6, color: accent));
        stack.Children.Add(chipRow);

        return stack;
    }

    private static View BuildPassbookCardContent(Color bone, Color accent)
    {
        var stack = new VerticalStackLayout { Spacing = 14 };
        var header = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            ]
        };
        var left = new VerticalStackLayout { Spacing = 6 };
        left.Children.Add(Bone(height: 10, width: 116, radius: 4, color: bone));
        left.Children.Add(Bone(height: 18, width: 164, radius: 5, color: accent));
        header.Add(left, 0, 0);
        header.Add(Bone(height: 28, width: 92, radius: 6, color: accent), 1, 0);
        stack.Children.Add(header);

        var wells = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            ],
            ColumnSpacing = 12
        };
        wells.Add(Bone(height: 58, width: -1, radius: 8, color: bone), 0, 0);
        wells.Add(Bone(height: 58, width: -1, radius: 8, color: bone), 1, 0);
        stack.Children.Add(wells);

        stack.Children.Add(Bone(height: 11, width: 196, radius: 4, color: bone));
        stack.Children.Add(Bone(height: 6, width: -1, radius: 3, color: accent));
        return stack;
    }

    private static BoxView Bone(
        double height,
        double width,
        double radius,
        Color color,
        LayoutOptions? verticalOptions = null)
    {
        var box = new BoxView
        {
            HeightRequest = height,
            CornerRadius = radius,
            Color = color,
            HorizontalOptions = width < 0 ? LayoutOptions.Fill : LayoutOptions.Start,
            VerticalOptions = verticalOptions ?? LayoutOptions.Start
        };

        if (width >= 0)
        {
            box.WidthRequest = width;
        }

        return box;
    }

    private void UpdateAnimationState()
    {
        if (!ShouldAnimate)
        {
            StopShimmer();
            return;
        }

        StartShimmer();
    }

    private void StartShimmer()
    {
        this.AbortAnimation(ShimmerAnimationName);

        foreach (VisualElement overlay in _sweepOverlays)
        {
            overlay.IsVisible = true;
            overlay.TranslationX = -160;
        }

        var sweep = new Animation(
            callback: progress =>
            {
                double span = Width > 100d ? Width + 180d : 480d;
                double x = -160d + (progress * span);
                for (int i = 0; i < _sweepOverlays.Count; i++)
                {
                    _sweepOverlays[i].TranslationX = x;
                }
            },
            start: 0d,
            end: 1d,
            easing: Easing.SinInOut);

        sweep.Commit(
            owner: this,
            name: ShimmerAnimationName,
            rate: 16,
            length: 1400,
            repeat: () => ShouldAnimate);
    }

    private void StopShimmer()
    {
        this.AbortAnimation(ShimmerAnimationName);
        for (int i = 0; i < _sweepOverlays.Count; i++)
        {
            _sweepOverlays[i].IsVisible = false;
            _sweepOverlays[i].TranslationX = -160;
        }
    }
}
