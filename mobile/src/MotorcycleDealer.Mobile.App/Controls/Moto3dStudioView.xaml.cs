using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using MotorcycleDealer.Mobile.App.Motion;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Media;
using MotorcycleDealer.Mobile.Core.Messaging;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Hardware-accelerated real-life HD DSLR 360-degree showroom showcase &amp; OEM component inspector
/// for Android 15 &amp; iOS 15+. Replaces procedural 3D meshes with bundled 1280x800 real dealership
/// photographs supporting horizontal swipe scrubbing, smooth GPU zoom/pan up to 2.4x, proportional
/// counter-scaled interactive OEM hotspot pins, showroom lighting atmosphere modes, and instant VI/EN + Dark/Light sync.
/// </summary>
public partial class Moto3dStudioView : ContentView
{
    private readonly record struct PinCoord(double X, double Y);

    private readonly record struct PartSpec(
        string Id,
        string ViName,
        string EnName,
        string ViDesc,
        string EnDesc,
        string BestAngle,
        double FocusZoom,
        IReadOnlyDictionary<string, PinCoord> Pins);

    private readonly record struct AtmosphereMode(string Key, string ViLabel, string EnLabel, Color TintColor, double Opacity);

    private static readonly AtmosphereMode[] AtmosphereModes =
    [
        new("daylight", "Ánh sáng Showroom", "Natural Showroom", Colors.Transparent, 0.0),
        new("golden", "Nắng chiều ấm", "Warm Daylight", Color.FromArgb("#F59E0B"), 0.08),
        new("spotlight", "Tiêu điểm Đêm", "Evening Spotlight", Color.FromArgb("#020617"), 0.22)
    ];

    private static readonly PartSpec[] PartCatalog =
    [
        new("headlight", "Đèn LED", "LED Head",
            "Cụm đèn pha Full-LED cường độ cao kết hợp dải định vị DRL ban ngày cho tầm nhìn sắc nét.",
            "High-output Full-LED headlamp with signature daytime running light optics.",
            "front", 1.85,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["front"] = new(0.61, 0.41),
                ["cockpit"] = new(0.50, 0.24)
            }),
        new("suspension", "Phuộc & ABS", "Forks & ABS",
            "Giảm xóc trước ổn định thân xe kết hợp đĩa phanh khoan lỗ tản nhiệt & chống bó cứng ABS.",
            "Precision-damped front forks paired with drilled stainless disc rotor and ABS caliper.",
            "front", 1.95,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["front"] = new(0.60, 0.65),
                ["side"] = new(0.69, 0.60)
            }),
        new("bodywork", "Dàn áo", "Bodywork",
            "Dàn áo khí động học phủ sơn Clear-Coat tiêu chuẩn nhà máy chống tia UV và bền màu.",
            "Sculpted aerodynamic fairings finished in factory automotive clear-coat.",
            "front", 1.70,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["front"] = new(0.52, 0.46),
                ["side"] = new(0.55, 0.44)
            }),
        new("engine", "Động cơ", "Engine",
            "Khối động cơ {0} cc hợp kim nhôm đúc áp lực tản nhiệt tối ưu, bảo dưỡng chuẩn hãng mỗi 4.000 km.",
            "{0} cc die-cast alloy OEM engine block; recommended OEM oil service every 4,000 km.",
            "side", 1.95,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["front"] = new(0.45, 0.61),
                ["side"] = new(0.52, 0.63)
            }),
        new("exhaust", "Ống xả", "Exhaust",
            "Cổ pô chống ăn mòn nối liền lon pô đạt chuẩn khí thải cùng ốp chống nóng an toàn.",
            "Corrosion-resistant header paired with emissions-compliant OEM silencer and heat shield.",
            "side", 1.95,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["front"] = new(0.35, 0.63),
                ["side"] = new(0.34, 0.66),
                ["rear"] = new(0.63, 0.64)
            }),
        new("wheels", "Mâm & Lốp", "Wheels",
            "Mâm đúc hợp kim đa chấu siêu nhẹ kết hợp lốp không săm rãnh sâu tăng độ bám đường.",
            "Lightweight multi-spoke cast alloy rims wrapped in deep-tread tubeless tires.",
            "side", 1.85,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["front"] = new(0.62, 0.77),
                ["side"] = new(0.72, 0.74),
                ["rear"] = new(0.48, 0.74)
            }),
        new("tailSeat", "Yên & Đuôi", "Seat & Tail",
            "Yên da công thái học chống trượt kết hợp cụm đèn hậu LED nhận diện nổi bật.",
            "Anti-slip ergonomic stepped saddle paired with sharp LED rear brake lighting.",
            "rear", 1.80,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["side"] = new(0.37, 0.43),
                ["rear"] = new(0.48, 0.45)
            }),
        new("cockpit", "Đồng hồ", "Cockpit",
            "Cụm đồng hồ kỹ thuật số độ tương phản cao hiển thị tốc độ, tua máy, cấp số & ABS.",
            "High-contrast digital instrument cluster displaying speed, RPM, gear & ABS telemetry.",
            "cockpit", 1.65,
            new Dictionary<string, PinCoord>(StringComparer.Ordinal)
            {
                ["cockpit"] = new(0.50, 0.50)
            })
    ];

    public static readonly BindableProperty ModelCodeProperty = BindableProperty.Create(
        nameof(ModelCode), typeof(string), typeof(Moto3dStudioView), defaultValue: "HND-CB300R-2026",
        propertyChanged: (b, _, _) => ((Moto3dStudioView)b).OnModelChanged());

    public static readonly BindableProperty ImageUrlProperty = BindableProperty.Create(
        nameof(ImageUrl), typeof(string), typeof(Moto3dStudioView), defaultValue: null,
        propertyChanged: (b, _, _) => ((Moto3dStudioView)b).OnModelChanged());

    public static readonly BindableProperty ModelNameProperty = BindableProperty.Create(
        nameof(ModelName), typeof(string), typeof(Moto3dStudioView), defaultValue: "Apex Moto");

    public static readonly BindableProperty CategoryProperty = BindableProperty.Create(
        nameof(Category), typeof(string), typeof(Moto3dStudioView), defaultValue: "Naked");

    public static readonly BindableProperty EngineDisplacementCcProperty = BindableProperty.Create(
        nameof(EngineDisplacementCc), typeof(int), typeof(Moto3dStudioView), defaultValue: 160,
        propertyChanged: (b, _, _) => ((Moto3dStudioView)b).RefreshInspectorBindings());

    public string ModelCode { get => (string)GetValue(ModelCodeProperty); set => SetValue(ModelCodeProperty, value); }
    public string? ImageUrl { get => (string?)GetValue(ImageUrlProperty); set => SetValue(ImageUrlProperty, value); }
    public string ModelName { get => (string)GetValue(ModelNameProperty); set => SetValue(ModelNameProperty, value); }
    public string Category { get => (string)GetValue(CategoryProperty); set => SetValue(CategoryProperty, value); }
    public int EngineDisplacementCc { get => (int)GetValue(EngineDisplacementCcProperty); set => SetValue(EngineDisplacementCcProperty, value); }

    private IReadOnlyList<MotorcycleGalleryAngle> _angles = [];
    private int _activeAngleIndex;
    private double _zoom = 1.0;
    private double _panX;
    private double _panY;
    private double _panStartX;
    private double _panStartY;
    private double _lastScrubTotalX;
    private bool _autoTour = true;
    private bool _showHotspots = true;
    private int _atmosphereIndex;
    private string? _selectedPartId;
    private IDispatcherTimer? _tourTimer;
    private bool _stripsBuilt;
    private int _angleAnimEpoch;
    private int _zoomAnimEpoch;

    private readonly List<(Button Button, string AngleKey)> _angleChipRefs = [];
    private readonly List<(Button Button, Func<string> LabelFactory, Func<bool> StateGetter)> _toggleChipRefs = [];
    private readonly List<(Button Button, PartSpec Part)> _partChipRefs = [];
    private readonly List<VisualElement> _hotspotPinRefs = [];

    public string ActivePhotoSource =>
        _angles.Count > 0 && _activeAngleIndex >= 0 && _activeAngleIndex < _angles.Count
            ? _angles[_activeAngleIndex].ImageSource
            : MotorcycleImageResolver.HeroShowroomImage;

    public bool HasSelectedPart => !string.IsNullOrWhiteSpace(_selectedPartId);
    public string HudBadgeText => IsVietnamese ? "HD 360° • XE THẬT" : "REAL HD 360°";
    public string ZoomOutA11yLabel => IsVietnamese ? "Thu nhỏ ảnh xe" : "Zoom out motorcycle photo";
    public string ZoomInA11yLabel => IsVietnamese ? "Phóng to ảnh xe" : "Zoom in motorcycle photo";
    public string ResetZoomA11yLabel => IsVietnamese ? "Đặt lại mức phóng 1x" : "Reset zoom to 1x";
    public string PrevAngleA11yLabel => LocalizationService.Current.ShowroomPrevAngle;
    public string NextAngleA11yLabel => LocalizationService.Current.ShowroomNextAngle;
    public string ClearPartA11yLabel => IsVietnamese ? "Đóng chi tiết linh kiện" : "Close component inspection";

    public string HintText
    {
        get
        {
            string angleLabel = GetAngleLabel(CurrentAngleKey);
            int total = Math.Max(1, _angles.Count);
            int current = Math.Clamp(_activeAngleIndex + 1, 1, total);
            return IsVietnamese
                ? $"{current}/{total} • {angleLabel} • {_zoom:0.0}x • Vuốt đổi góc hoặc chạm ghim đỏ"
                : $"{current}/{total} • {angleLabel} • {_zoom:0.0}x • Swipe to orbit or tap pins";
        }
    }

    public string SelectedPartTitle
    {
        get
        {
            PartSpec? spec = FindPart(_selectedPartId);
            return spec is null ? string.Empty : (IsVietnamese ? spec.Value.ViName : spec.Value.EnName);
        }
    }

    public string SelectedPartDescription
    {
        get
        {
            PartSpec? spec = FindPart(_selectedPartId);
            if (spec is null)
            {
                return string.Empty;
            }

            string raw = IsVietnamese ? spec.Value.ViDesc : spec.Value.EnDesc;
            return string.Format(raw, EngineDisplacementCc > 0 ? EngineDisplacementCc : 160);
        }
    }

    private string CurrentAngleKey =>
        _angles.Count > 0 && _activeAngleIndex >= 0 && _activeAngleIndex < _angles.Count
            ? _angles[_activeAngleIndex].AngleKey
            : "front";

    private static bool IsVietnamese =>
        LocalizationService.Current.CurrentCulture.TwoLetterISOLanguageName.Equals("vi", StringComparison.OrdinalIgnoreCase);

    private static bool IsDarkTheme =>
        Application.Current?.UserAppTheme == AppTheme.Dark ||
        (Application.Current?.UserAppTheme == AppTheme.Unspecified && Application.Current?.RequestedTheme == AppTheme.Dark);

    public Moto3dStudioView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            if (IsVisible)
            {
                ReloadGalleryForCurrentModel(resetAngle: false);
                EnsureControlStripsBuilt();
                RefreshControlStripStates();
                RebuildHotspotPins();
                ApplyAtmosphereOverlay();
            }

            UpdateTourTimerState();
        }
    }

    private void OnViewLoaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<Moto3dStudioView, CultureChangedMessage>(
            this,
            static (r, _) => MainThread.BeginInvokeOnMainThread(r.OnCultureUpdated));

        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<Moto3dStudioView, ThemeChangedMessage>(
            this,
            static (r, msg) =>
            {
                bool isDark = string.Equals(msg.ThemeMode?.Trim(), "dark", StringComparison.OrdinalIgnoreCase);
                MainThread.BeginInvokeOnMainThread(() => r.OnInAppThemeUpdated(isDark));
            });

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }

        if (MotionHelper.IsReducedMotionEnabled)
        {
            _autoTour = false;
        }

        ReloadGalleryForCurrentModel(resetAngle: true);
        if (IsVisible)
        {
            EnsureControlStripsBuilt();
            RefreshControlStripStates();
            RebuildHotspotPins();
            ApplyAtmosphereOverlay();
        }

        UpdateTourTimerState();
    }

    private void OnViewUnloaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        }

        if (_tourTimer is not null)
        {
            _tourTimer.Stop();
            _tourTimer.Tick -= OnTourTick;
            _tourTimer = null;
        }

        _angleAnimEpoch++;
        _zoomAnimEpoch++;
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        if (!_stripsBuilt && !IsVisible)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => RefreshControlStripStates());
    }

    private void OnInAppThemeUpdated(bool isDark)
    {
        if (!_stripsBuilt && !IsVisible)
        {
            return;
        }

        RefreshControlStripStates(isDark);
    }

    private void OnCultureUpdated()
    {
        OnPropertyChanged(nameof(HudBadgeText));
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(ZoomOutA11yLabel));
        OnPropertyChanged(nameof(ZoomInA11yLabel));
        OnPropertyChanged(nameof(ResetZoomA11yLabel));
        OnPropertyChanged(nameof(PrevAngleA11yLabel));
        OnPropertyChanged(nameof(NextAngleA11yLabel));
        OnPropertyChanged(nameof(ClearPartA11yLabel));
        RefreshInspectorBindings();
        if (_stripsBuilt)
        {
            RefreshControlStripStates();
        }

        if (IsVisible)
        {
            RebuildHotspotPins();
        }
    }

    private void OnModelChanged()
    {
        ReloadGalleryForCurrentModel(resetAngle: true);
        _selectedPartId = null;
        ResetZoomAndPanImmediate();
        RefreshInspectorBindings();

        if (_stripsBuilt)
        {
            RebuildAngleChips();
            RefreshControlStripStates();
        }

        if (IsVisible)
        {
            RebuildHotspotPins();
        }

        UpdateTourTimerState();
    }

    private void ReloadGalleryForCurrentModel(bool resetAngle)
    {
        _angles = MotorcycleImageResolver.ResolveModelGallery(ModelCode, ImageUrl);
        if (resetAngle || _activeAngleIndex >= _angles.Count)
        {
            _activeAngleIndex = 0;
        }

        OnPropertyChanged(nameof(ActivePhotoSource));
        OnPropertyChanged(nameof(HintText));
    }

    private void UpdateTourTimerState()
    {
        if (!IsVisible || !_autoTour || _angles.Count <= 1 || MotionHelper.IsReducedMotionEnabled)
        {
            StopTourTimer();
            return;
        }

        if (_tourTimer is null && Dispatcher is not null)
        {
            _tourTimer = Dispatcher.CreateTimer();
            _tourTimer.Interval = TimeSpan.FromMilliseconds(3600);
            _tourTimer.Tick += OnTourTick;
        }

        if (_tourTimer is not null && !_tourTimer.IsRunning)
        {
            _tourTimer.Start();
        }
    }

    private void StopTourTimer()
    {
        if (_tourTimer is not null && _tourTimer.IsRunning)
        {
            _tourTimer.Stop();
        }
    }

    private void OnTourTick(object? sender, EventArgs e)
    {
        if (!IsVisible || !_autoTour || HasSelectedPart || _angles.Count <= 1)
        {
            return;
        }

        _ = SwitchAngleAsync((_activeAngleIndex + 1) % _angles.Count, clearSelection: true);
    }

    private async Task SwitchAngleAsync(int targetIndex, bool clearSelection)
    {
        if (_angles.Count == 0)
        {
            return;
        }

        int epoch = ++_angleAnimEpoch;
        int normalized = ((targetIndex % _angles.Count) + _angles.Count) % _angles.Count;
        bool changed = normalized != _activeAngleIndex;
        _activeAngleIndex = normalized;

        if (clearSelection)
        {
            _selectedPartId = null;
            RefreshInspectorBindings();
        }

        try
        {
            if (changed && !MotionHelper.IsReducedMotionEnabled && ActivePhotoImage is not null)
            {
                await ActivePhotoImage.FadeToAsync(0.55, 90, Easing.CubicOut);
                if (epoch != _angleAnimEpoch)
                {
                    return;
                }

                OnPropertyChanged(nameof(ActivePhotoSource));
                await ActivePhotoImage.FadeToAsync(1.0, 150, Easing.CubicIn);
            }
            else
            {
                OnPropertyChanged(nameof(ActivePhotoSource));
            }
        }
        catch
        {
            OnPropertyChanged(nameof(ActivePhotoSource));
        }
        finally
        {
            if (epoch == _angleAnimEpoch && ActivePhotoImage is not null && ActivePhotoImage.Opacity < 0.99)
            {
                ActivePhotoImage.Opacity = 1.0;
            }
        }

        if (epoch != _angleAnimEpoch)
        {
            return;
        }

        OnPropertyChanged(nameof(HintText));
        RebuildHotspotPins();
        RefreshControlStripStates();
    }

    private async Task ApplyZoomAndPanAsync(double targetZoom, double targetPanX, double targetPanY, bool animate)
    {
        int epoch = ++_zoomAnimEpoch;
        double prevZoom = _zoom;
        _zoom = Math.Clamp(targetZoom, 1.0, 2.4);

        double vpW = PhotoViewport.Width > 0 ? PhotoViewport.Width : 360;
        double vpH = PhotoViewport.Height > 0 ? PhotoViewport.Height : 276;
        double maxX = Math.Max(0, (vpW * (_zoom - 1.0)) * 0.48);
        double maxY = Math.Max(0, (vpH * (_zoom - 1.0)) * 0.48);
        _panX = _zoom <= 1.02 ? 0 : Math.Clamp(targetPanX, -maxX, maxX);
        _panY = _zoom <= 1.02 ? 0 : Math.Clamp(targetPanY, -maxY, maxY);

        if (Math.Abs(_zoom - prevZoom) >= 0.01)
        {
            OnPropertyChanged(nameof(HintText));
            UpdateHotspotPinScales();
        }

        if (ZoomLayer is null)
        {
            return;
        }

        try
        {
            if (animate && !MotionHelper.IsReducedMotionEnabled)
            {
                await Task.WhenAll(
                    ZoomLayer.ScaleToAsync(_zoom, 240, Easing.CubicOut),
                    ZoomLayer.TranslateToAsync(_panX, _panY, 240, Easing.CubicOut));
            }
            else
            {
                ZoomLayer.Scale = _zoom;
                ZoomLayer.TranslationX = _panX;
                ZoomLayer.TranslationY = _panY;
            }
        }
        catch
        {
            if (epoch == _zoomAnimEpoch && ZoomLayer is not null)
            {
                ZoomLayer.Scale = _zoom;
                ZoomLayer.TranslationX = _panX;
                ZoomLayer.TranslationY = _panY;
            }
        }
    }

    private void ResetZoomAndPanImmediate()
    {
        _zoomAnimEpoch++;
        _zoom = 1.0;
        _panX = 0;
        _panY = 0;
        if (ZoomLayer is not null)
        {
            ZoomLayer.Scale = 1.0;
            ZoomLayer.TranslationX = 0;
            ZoomLayer.TranslationY = 0;
        }

        UpdateHotspotPinScales();
        OnPropertyChanged(nameof(HintText));
    }

    private void UpdateHotspotPinScales()
    {
        double invScale = 1.0 / Math.Max(1.0, _zoom);
        for (int i = 0; i < _hotspotPinRefs.Count; i++)
        {
            _hotspotPinRefs[i].Scale = invScale;
        }
    }

    private void ApplyAtmosphereOverlay()
    {
        if (AtmosphereOverlay is null)
        {
            return;
        }

        AtmosphereMode mode = AtmosphereModes[_atmosphereIndex % AtmosphereModes.Length];
        AtmosphereOverlay.Color = mode.TintColor;
        AtmosphereOverlay.Opacity = mode.Opacity;
    }

    private void RebuildHotspotPins()
    {
        if (HotspotLayer is null)
        {
            return;
        }

        HotspotLayer.Children.Clear();
        _hotspotPinRefs.Clear();
        if (!_showHotspots)
        {
            return;
        }

        string angleKey = CurrentAngleKey;
        double invScale = 1.0 / Math.Max(1.0, _zoom);

        foreach (PartSpec part in PartCatalog)
        {
            if (!part.Pins.TryGetValue(angleKey, out PinCoord coord))
            {
                continue;
            }

            bool isSelected = string.Equals(_selectedPartId, part.Id, StringComparison.Ordinal);
            double visualSize = isSelected ? 26 : 22;

            var pinOuter = new Border
            {
                WidthRequest = visualSize,
                HeightRequest = visualSize,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(visualSize / 2.0) },
                BackgroundColor = isSelected ? Color.FromArgb("#E6DC2626") : Color.FromArgb("#B8DC2626"),
                Stroke = Colors.White,
                StrokeThickness = isSelected ? 2.5 : 2.0,
                Padding = 0,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true
            };

            var pinDot = new BoxView
            {
                WidthRequest = isSelected ? 8 : 6,
                HeightRequest = isSelected ? 8 : 6,
                CornerRadius = 4,
                Color = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true
            };
            pinOuter.Content = pinDot;

            // 44x44 accessible tap wrapper with inverse zoom scaling so pins stay crisp & easy to tap up to 2.4x
            var hitWrapper = new Grid
            {
                WidthRequest = 44,
                HeightRequest = 44,
                BackgroundColor = Colors.Transparent,
                Scale = invScale
            };
            string partName = IsVietnamese ? part.ViName : part.EnName;
            SemanticProperties.SetDescription(
                hitWrapper,
                IsVietnamese ? $"Soi linh kiện: {partName}" : $"Inspect component: {partName}");
            hitWrapper.Children.Add(pinOuter);

            string partId = part.Id;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => _ = FocusPartAsync(partId, stayOnCurrentAngle: true);
            hitWrapper.GestureRecognizers.Add(tap);

            AbsoluteLayout.SetLayoutFlags(hitWrapper, AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(hitWrapper, new Rect(coord.X, coord.Y, 44, 44));
            _hotspotPinRefs.Add(hitWrapper);
            HotspotLayer.Children.Add(hitWrapper);
        }
    }

    private async Task FocusPartAsync(string partId, bool stayOnCurrentAngle)
    {
        PartSpec? found = FindPart(partId);
        if (found is null)
        {
            return;
        }

        _autoTour = false;
        UpdateTourTimerState();
        _showHotspots = true;

        string targetAngleKey = stayOnCurrentAngle && found.Value.Pins.ContainsKey(CurrentAngleKey)
            ? CurrentAngleKey
            : found.Value.BestAngle;

        int targetIdx = _activeAngleIndex;
        for (int i = 0; i < _angles.Count; i++)
        {
            if (string.Equals(_angles[i].AngleKey, targetAngleKey, StringComparison.OrdinalIgnoreCase))
            {
                targetIdx = i;
                break;
            }
        }

        _selectedPartId = found.Value.Id;
        RefreshInspectorBindings();
        await SwitchAngleAsync(targetIdx, clearSelection: false);

        PinCoord coord = found.Value.Pins.TryGetValue(CurrentAngleKey, out PinCoord activeCoord)
            ? activeCoord
            : (found.Value.Pins.TryGetValue(found.Value.BestAngle, out PinCoord bestCoord) ? bestCoord : new PinCoord(0.5, 0.5));

        double vpW = PhotoViewport.Width > 0 ? PhotoViewport.Width : 360;
        double vpH = PhotoViewport.Height > 0 ? PhotoViewport.Height : 276;
        double targetPanX = (0.5 - coord.X) * vpW * (found.Value.FocusZoom - 1.0) * 1.1;
        double targetPanY = (0.5 - coord.Y) * vpH * (found.Value.FocusZoom - 1.0) * 1.1;

        await ApplyZoomAndPanAsync(found.Value.FocusZoom, targetPanX, targetPanY, animate: true);
    }

    private void OnViewportPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            _panStartX = _panX;
            _panStartY = _panY;
            _lastScrubTotalX = 0;
            if (_autoTour)
            {
                _autoTour = false;
                UpdateTourTimerState();
                RefreshControlStripStates();
            }

            return;
        }

        if (e.StatusType == GestureStatus.Running)
        {
            if (_zoom > 1.05)
            {
                _ = ApplyZoomAndPanAsync(_zoom, _panStartX + e.TotalX, _panStartY + e.TotalY, animate: false);
            }
            else if (_angles.Count > 1)
            {
                double delta = e.TotalX - _lastScrubTotalX;
                if (Math.Abs(delta) >= 64)
                {
                    _lastScrubTotalX = e.TotalX;
                    int step = delta > 0 ? -1 : 1;
                    _ = SwitchAngleAsync(_activeAngleIndex + step, clearSelection: true);
                }
            }
        }
    }

    private void OnViewportDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_zoom > 1.15)
        {
            _selectedPartId = null;
            RefreshInspectorBindings();
            _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: true);
        }
        else
        {
            _autoTour = false;
            UpdateTourTimerState();
            _ = ApplyZoomAndPanAsync(1.85, 0, 0, animate: true);
        }

        RefreshControlStripStates();
    }

    private void OnZoomInClicked(object? sender, EventArgs e)
    {
        _autoTour = false;
        UpdateTourTimerState();
        _ = ApplyZoomAndPanAsync(_zoom + 0.35, _panX, _panY, animate: true);
        RefreshControlStripStates();
    }

    private void OnZoomOutClicked(object? sender, EventArgs e)
    {
        double next = _zoom - 0.35;
        if (next <= 1.05)
        {
            _selectedPartId = null;
            RefreshInspectorBindings();
            _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: true);
        }
        else
        {
            _ = ApplyZoomAndPanAsync(next, _panX, _panY, animate: true);
        }

        RefreshControlStripStates();
    }

    private void OnResetCameraClicked(object? sender, EventArgs e)
    {
        _selectedPartId = null;
        RefreshInspectorBindings();
        _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: true);
        RebuildHotspotPins();
        RefreshControlStripStates();
    }

    private void OnPreviousAngleClicked(object? sender, EventArgs e)
    {
        _autoTour = false;
        UpdateTourTimerState();
        _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: false);
        _ = SwitchAngleAsync(_activeAngleIndex - 1, clearSelection: true);
    }

    private void OnNextAngleClicked(object? sender, EventArgs e)
    {
        _autoTour = false;
        UpdateTourTimerState();
        _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: false);
        _ = SwitchAngleAsync(_activeAngleIndex + 1, clearSelection: true);
    }

    private void OnClearPartClicked(object? sender, EventArgs e)
    {
        _selectedPartId = null;
        RefreshInspectorBindings();
        _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: true);
        RebuildHotspotPins();
        RefreshControlStripStates();
    }

    private void EnsureControlStripsBuilt()
    {
        if (_stripsBuilt)
        {
            return;
        }

        _stripsBuilt = true;
        RebuildAngleChips();

        FeatureToggleRow.Children.Clear();
        _toggleChipRefs.Clear();

        AddToggleChip(
            () => IsVietnamese ? "Tự quay 360°" : "Auto-Tour",
            () => _autoTour,
            () =>
            {
                _autoTour = !_autoTour;
                if (_autoTour)
                {
                    _selectedPartId = null;
                    RefreshInspectorBindings();
                    _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: true);
                }

                UpdateTourTimerState();
            });

        AddToggleChip(
            () => IsVietnamese ? "Ghim linh kiện" : "Hotspots",
            () => _showHotspots,
            () =>
            {
                _showHotspots = !_showHotspots;
                RebuildHotspotPins();
            });

        AddToggleChip(
            () =>
            {
                AtmosphereMode mode = AtmosphereModes[_atmosphereIndex % AtmosphereModes.Length];
                return IsVietnamese ? mode.ViLabel : mode.EnLabel;
            },
            () => _atmosphereIndex > 0,
            () =>
            {
                _atmosphereIndex = (_atmosphereIndex + 1) % AtmosphereModes.Length;
                ApplyAtmosphereOverlay();
            });

        ComponentChipRow.Children.Clear();
        _partChipRefs.Clear();
        foreach (PartSpec part in PartCatalog)
        {
            PartSpec captured = part;
            var btn = CreateChipButton();
            btn.Clicked += (_, _) => _ = FocusPartAsync(captured.Id, stayOnCurrentAngle: false);
            _partChipRefs.Add((btn, captured));
            ComponentChipRow.Children.Add(btn);
        }
    }

    private void RebuildAngleChips()
    {
        AngleChipRow.Children.Clear();
        _angleChipRefs.Clear();
        for (int i = 0; i < _angles.Count; i++)
        {
            int capturedIndex = i;
            string angleKey = _angles[i].AngleKey;
            var btn = CreateChipButton();
            btn.Clicked += (_, _) =>
            {
                _autoTour = false;
                UpdateTourTimerState();
                _ = ApplyZoomAndPanAsync(1.0, 0, 0, animate: false);
                _ = SwitchAngleAsync(capturedIndex, clearSelection: true);
            };
            _angleChipRefs.Add((btn, angleKey));
            AngleChipRow.Children.Add(btn);
        }
    }

    private void AddToggleChip(Func<string> labelFactory, Func<bool> stateGetter, Action toggleAction)
    {
        var btn = CreateChipButton();
        btn.Clicked += (_, _) =>
        {
            toggleAction();
            RefreshControlStripStates();
        };
        _toggleChipRefs.Add((btn, labelFactory, stateGetter));
        FeatureToggleRow.Children.Add(btn);
    }

    private static Button CreateChipButton() => new()
    {
        HeightRequest = 34,
        MinimumHeightRequest = 34,
        CornerRadius = 17,
        Padding = new Thickness(12, 0),
        FontFamily = "BeVietnamProSemiBold",
        FontSize = 11,
        BorderWidth = 1
    };

    private void RefreshControlStripStates(bool? isDarkOverride = null)
    {
        bool dark = isDarkOverride ?? IsDarkTheme;
        Color inactiveBg = dark ? Color.FromArgb("#1E293B") : Color.FromArgb("#F1F5F9");
        Color inactiveFg = dark ? Color.FromArgb("#E2E8F0") : Color.FromArgb("#0F172A");
        Color inactiveBorder = dark ? Color.FromArgb("#334155") : Color.FromArgb("#CBD5E1");
        Color activeBg = Color.FromArgb("#DC2626");

        string currentAngle = CurrentAngleKey;
        foreach (var (btn, angleKey) in _angleChipRefs)
        {
            bool active = string.Equals(angleKey, currentAngle, StringComparison.OrdinalIgnoreCase);
            btn.Text = GetAngleLabel(angleKey);
            btn.BackgroundColor = active ? activeBg : inactiveBg;
            btn.TextColor = active ? Colors.White : inactiveFg;
            btn.BorderColor = active ? activeBg : inactiveBorder;
        }

        foreach (var (btn, labelFactory, stateGetter) in _toggleChipRefs)
        {
            bool active = stateGetter();
            btn.Text = labelFactory();
            btn.BackgroundColor = active ? activeBg : inactiveBg;
            btn.TextColor = active ? Colors.White : inactiveFg;
            btn.BorderColor = active ? activeBg : inactiveBorder;
        }

        foreach (var (btn, part) in _partChipRefs)
        {
            bool active = string.Equals(_selectedPartId, part.Id, StringComparison.Ordinal);
            btn.Text = IsVietnamese ? part.ViName : part.EnName;
            btn.BackgroundColor = active ? activeBg : inactiveBg;
            btn.TextColor = active ? Colors.White : inactiveFg;
            btn.BorderColor = active ? activeBg : inactiveBorder;
        }
    }

    private void RefreshInspectorBindings()
    {
        OnPropertyChanged(nameof(HasSelectedPart));
        OnPropertyChanged(nameof(SelectedPartTitle));
        OnPropertyChanged(nameof(SelectedPartDescription));
    }

    private static string GetAngleLabel(string angleKey) => angleKey.ToLowerInvariant() switch
    {
        "side" => LocalizationService.Current.ShowroomAngleSide,
        "rear" => LocalizationService.Current.ShowroomAngleRear,
        "cockpit" => LocalizationService.Current.ShowroomAngleCockpit,
        _ => LocalizationService.Current.ShowroomAngleFront
    };

    private static PartSpec? FindPart(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        foreach (PartSpec part in PartCatalog)
        {
            if (string.Equals(part.Id, id, StringComparison.Ordinal))
            {
                return part;
            }
        }

        return null;
    }
}
