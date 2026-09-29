using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Standardized RFC 7807 / network error recovery banner with Phosphor WarningCircle glyph,
/// adaptive WCAG AA danger palette, auto-visibility when Message is empty, and localized Retry action.
/// </summary>
public partial class ErrorRetryBannerView : ContentView
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message),
        typeof(string),
        typeof(ErrorRetryBannerView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((ErrorRetryBannerView)b).OnPropertyChanged(nameof(HasMessage)));

    public static readonly BindableProperty RetryTextProperty = BindableProperty.Create(
        nameof(RetryText),
        typeof(string),
        typeof(ErrorRetryBannerView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((ErrorRetryBannerView)b).OnPropertyChanged(nameof(EffectiveRetryText)));

    public static readonly BindableProperty RetryCommandProperty = BindableProperty.Create(
        nameof(RetryCommand),
        typeof(ICommand),
        typeof(ErrorRetryBannerView),
        defaultValue: null,
        propertyChanged: (b, _, _) => ((ErrorRetryBannerView)b).OnPropertyChanged(nameof(ShowRetry)));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string RetryText
    {
        get => (string)GetValue(RetryTextProperty);
        set => SetValue(RetryTextProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool ShowRetry => RetryCommand is not null;

    public string EffectiveRetryText => !string.IsNullOrWhiteSpace(RetryText)
        ? RetryText.Trim()
        : LocalizationService.Current.Common_Retry;

    public ErrorRetryBannerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ErrorRetryBannerView, CultureChangedMessage>(
            this,
            static (recipient, _) => MainThread.BeginInvokeOnMainThread(() =>
                recipient.OnPropertyChanged(nameof(EffectiveRetryText))));
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CultureChangedMessage>(this);
    }
}
