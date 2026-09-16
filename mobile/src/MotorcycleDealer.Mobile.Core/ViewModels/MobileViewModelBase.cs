using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Messaging;

namespace MotorcycleDealer.Mobile.Core.ViewModels;

/// <summary>
/// Shared observable base for all 17 mobile ViewModels using C# 13 partial [ObservableProperty] syntax.
/// Exposes reactive compiled-binding LocalizationService (Loc), live VI/EN toggle, Light/Dark theme toggle,
/// and leak-safe WeakReferenceMessenger subscriptions to CultureChangedMessage and ThemeChangedMessage.
/// </summary>
public abstract partial class MobileViewModelBase : ObservableObject
{
    private readonly IAppPreferencesStore? _preferencesStore;
    private readonly IMessenger _baseMessenger;

    protected MobileViewModelBase(
        LocalizationService? localizationService = null,
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
    {
        Loc = localizationService ?? LocalizationService.Current;
        _preferencesStore = preferencesStore;
        _baseMessenger = messenger ?? WeakReferenceMessenger.Default;

        SyncAgeLabel = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        ActiveThemeMode = NormalizeThemeMode(_preferencesStore?.GetString(AppPreferenceKeys.Theme, "light"));

        _baseMessenger.Register<MobileViewModelBase, CultureChangedMessage>(
            this,
            static (recipient, message) => recipient.HandleCultureChanged(message.CultureCode));

        _baseMessenger.Register<MobileViewModelBase, ThemeChangedMessage>(
            this,
            static (recipient, message) => recipient.HandleThemeChanged(message.ThemeMode));
    }

    public LocalizationService Loc { get; }

    public bool IsVietnamese => Loc.IsVietnamese;
    public bool IsEnglish => Loc.IsEnglish;
    public string ActiveLanguageCode => Loc.CurrentCultureCode;
    public string LanguageToggleText => Loc.LangToggleLabel;
    public bool IsDarkTheme => string.Equals(ActiveThemeMode, "dark", StringComparison.OrdinalIgnoreCase);
    public string ThemeToggleText => IsDarkTheme ? Loc.ThemeLightLabel : Loc.ThemeDarkLabel;

    [ObservableProperty]
    public partial string ActiveThemeMode { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsOfflineBannerVisible { get; set; }

    [ObservableProperty]
    public partial string SyncAgeLabel { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string SuccessMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnSuccessMessageChanged(string value) => OnPropertyChanged(nameof(HasSuccess));

    partial void OnActiveThemeModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(ThemeToggleText));
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        var next = Loc.ToggleCulture();
        TrackUserAction("Locale.Toggle", "Settings", next);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var next = IsDarkTheme ? "light" : "dark";
        _preferencesStore?.SetString(AppPreferenceKeys.Theme, next);
        ActiveThemeMode = next;
        TrackUserAction("Theme.Toggle", "Settings", next);
        _baseMessenger.Send(new ThemeChangedMessage(next));
    }

    protected virtual void OnCultureChanged(string cultureCode)
    {
    }

    protected void UnregisterBaseMessages() => _baseMessenger.UnregisterAll(this);

    protected string FormatApiError<T>(ApiResult<T> result) => Loc.GetApiErrorMessage(result);

    protected void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    protected void TrackUserAction(
        string action,
        string category = "UserInteraction",
        string? target = null,
        string outcome = "Success",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        MobileUserActionLogHub.Current?.Track(
            action: action,
            category: category,
            target: target,
            outcome: outcome,
            metadata: metadata);
    }

    private void HandleCultureChanged(string cultureCode)
    {
        OnPropertyChanged(nameof(IsVietnamese));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(ActiveLanguageCode));
        OnPropertyChanged(nameof(LanguageToggleText));
        OnPropertyChanged(nameof(ThemeToggleText));
        OnCultureChanged(cultureCode);
    }

    private void HandleThemeChanged(string themeMode)
    {
        var normalized = NormalizeThemeMode(themeMode);
        if (!string.Equals(ActiveThemeMode, normalized, StringComparison.OrdinalIgnoreCase))
        {
            ActiveThemeMode = normalized;
        }
    }

    private static string NormalizeThemeMode(string? raw) =>
        string.Equals(raw?.Trim(), "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
}
