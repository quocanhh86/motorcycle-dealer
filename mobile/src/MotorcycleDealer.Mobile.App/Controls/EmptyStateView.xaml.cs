using System.Windows.Input;
using MotorcycleDealer.Mobile.App.Icons;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Motorsport empty-state panel combining optional self-hosted Lottie animation (with reduced-motion
/// Phosphor icon fallback), localized title/subtitle, and an optional primary recovery CTA.
/// </summary>
public partial class EmptyStateView : ContentView
{
    public static readonly BindableProperty IconGlyphProperty = BindableProperty.Create(
        nameof(IconGlyph),
        typeof(string),
        typeof(EmptyStateView),
        defaultValue: PhosphorIcons.Motorcycle);

    public static readonly BindableProperty LottieFileProperty = BindableProperty.Create(
        nameof(LottieFile),
        typeof(string),
        typeof(EmptyStateView),
        defaultValue: string.Empty);

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(EmptyStateView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((EmptyStateView)b).OnPropertyChanged(nameof(HasTitle)));

    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(
        nameof(Subtitle),
        typeof(string),
        typeof(EmptyStateView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((EmptyStateView)b).OnPropertyChanged(nameof(HasSubtitle)));

    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText),
        typeof(string),
        typeof(EmptyStateView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((EmptyStateView)b).OnPropertyChanged(nameof(ShowAction)));

    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
        nameof(ActionCommand),
        typeof(ICommand),
        typeof(EmptyStateView),
        defaultValue: null,
        propertyChanged: (b, _, _) => ((EmptyStateView)b).OnPropertyChanged(nameof(ShowAction)));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string LottieFile
    {
        get => (string)GetValue(LottieFileProperty);
        set => SetValue(LottieFileProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public bool ShowAction => !string.IsNullOrWhiteSpace(ActionText) && ActionCommand is not null;

    public EmptyStateView()
    {
        InitializeComponent();
    }
}
