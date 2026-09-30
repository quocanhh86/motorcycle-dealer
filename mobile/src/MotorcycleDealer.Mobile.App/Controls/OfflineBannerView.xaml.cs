using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.App.Motion;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Offline SQLCipher cache notice banner featuring Phosphor WifiSlash icon,
/// live VI/EN localized copy, and smooth slide-fade transitions honoring OS reduced-motion.
/// </summary>
public partial class OfflineBannerView : ContentView
{
    public static readonly BindableProperty IsOfflineProperty = BindableProperty.Create(
        nameof(IsOffline),
        typeof(bool),
        typeof(OfflineBannerView),
        defaultValue: false,
        propertyChanged: (b, _, _) => _ = ((OfflineBannerView)b).AnimateVisibilityAsync());

    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message),
        typeof(string),
        typeof(OfflineBannerView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((OfflineBannerView)b).OnPropertyChanged(nameof(DisplayMessage)));

    public static readonly BindableProperty SyncCaptionProperty = BindableProperty.Create(
        nameof(SyncCaption),
        typeof(string),
        typeof(OfflineBannerView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) =>
        {
            var view = (OfflineBannerView)b;
            view.OnPropertyChanged(nameof(HasSyncCaption));
            view.OnPropertyChanged(nameof(DisplayMessage));
        });

    public bool IsOffline
    {
        get => (bool)GetValue(IsOfflineProperty);
        set => SetValue(IsOfflineProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string SyncCaption
    {
        get => (string)GetValue(SyncCaptionProperty);
        set => SetValue(SyncCaptionProperty, value);
    }

    public bool HasSyncCaption => !string.IsNullOrWhiteSpace(SyncCaption);

    public string DisplayMessage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                return Message.Trim();
            }

            LocalizationService loc = LocalizationService.Current;
            return HasSyncCaption
                ? loc.Format("Common_OfflineCacheToast", SyncCaption.Trim())
                : loc.NotifOfflineNotice;
        }
    }

    public OfflineBannerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<OfflineBannerView, CultureChangedMessage>(
            this,
            static (recipient, _) => MainThread.BeginInvokeOnMainThread(() =>
                recipient.OnPropertyChanged(nameof(DisplayMessage))));

        ApplyImmediateVisibility();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        MotionHelper.AbortAll(BannerCard);
    }

    private void ApplyImmediateVisibility()
    {
        MotionHelper.AbortAll(BannerCard);
        BannerCard.IsVisible = IsOffline;
        BannerCard.Opacity = IsOffline ? 1d : 0d;
        BannerCard.TranslationY = 0d;
    }

    private async Task AnimateVisibilityAsync()
    {
        MotionHelper.AbortAll(BannerCard);

        if (MotionHelper.IsReducedMotionEnabled || !IsLoaded)
        {
            ApplyImmediateVisibility();
            return;
        }

        if (IsOffline)
        {
            BannerCard.Opacity = 0d;
            BannerCard.TranslationY = -8d;
            BannerCard.IsVisible = true;
            try
            {
                await Task.WhenAll(
                    BannerCard.FadeToAsync(1d, 220, MotorEasing.ExpoOut),
                    BannerCard.TranslateToAsync(0d, 0d, 220, MotorEasing.ExpoOut));
            }
            catch
            {
                BannerCard.Opacity = 1d;
                BannerCard.TranslationY = 0d;
            }
        }
        else if (BannerCard.IsVisible)
        {
            try
            {
                await Task.WhenAll(
                    BannerCard.FadeToAsync(0d, 160, MotorEasing.SnappyOut),
                    BannerCard.TranslateToAsync(0d, -6d, 160, MotorEasing.SnappyOut));
            }
            catch
            {
                // Ignore cancelled transition
            }

            if (!IsOffline)
            {
                BannerCard.IsVisible = false;
                BannerCard.TranslationY = 0d;
            }
        }
    }
}
