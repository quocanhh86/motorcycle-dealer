using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Abstractions;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;

namespace MotorcycleDealer.Mobile.Core.Localization;

/// <summary>
/// Singleton reactive localization engine for MotorcycleDealer.Mobile.Core.
/// Defaults to Vietnamese ("vi-VN"), supports instant hot-switch to English ("en-US")
/// with zero app restart via OnPropertyChanged(string.Empty) + CultureChangedMessage,
/// and exposes strongly-typed properties safe for .NET 10 XAML SourceGen x:DataType bindings.
/// </summary>
public sealed partial class LocalizationService : ObservableObject
{
    public const string VietnameseCultureCode = "vi-VN";
    public const string EnglishCultureCode = "en-US";
    public const string DefaultCultureCode = VietnameseCultureCode;

    private static readonly Dictionary<string, string> EnumGroupAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BookingStatus"] = "BookingStatus",
        ["StockStatus"] = "StockStatus",
        ["VoucherStatus"] = "VoucherStatus",
        ["CustomerVoucherStatus"] = "VoucherStatus",
        ["LedgerType"] = "LedgerType",
        ["LedgerTransactionType"] = "LedgerType",
        ["LoyaltyTier"] = "LoyaltyTier",
        ["LoyaltyTierCode"] = "LoyaltyTier",
        ["NotificationEvent"] = "NotificationEvent",
        ["NotificationEventType"] = "NotificationEvent",
        ["ReminderStatus"] = "ReminderStatus",
        ["SalesOrderStatus"] = "SalesOrderStatus",
        ["SparePartCategory"] = "SparePartCategory",
        ["TestDriveStatus"] = "TestDriveStatus",
        ["UserRole"] = "UserRole",
        ["UserRoleCode"] = "UserRole",
        ["DiscountType"] = "DiscountType",
        ["VoucherDiscountType"] = "DiscountType",
        ["WarrantyStatus"] = "WarrantyStatus",
        ["WarrantyClaimStatus"] = "WarrantyStatus",
        ["WarrantyDefect"] = "WarrantyDefect",
        ["WarrantyDefectCategory"] = "WarrantyDefect",
        ["MotorcycleCategory"] = "MotorcycleCategory",
        ["ServiceType"] = "ServiceType",
        ["PaymentMethod"] = "PaymentMethod",
        ["MaintenanceStatus"] = "MaintenanceStatus",
        ["SignalRStatus"] = "SignalRStatus",
        ["ApplicableScope"] = "ApplicableScope",
        ["InvoiceItemType"] = "InvoiceItemType",
        ["RegistrationSource"] = "RegistrationSource",
        ["StatusBadge"] = "StatusBadge"
    };

    private static LocalizationService? s_current;
    private static readonly Lock s_currentLock = new();

    private readonly IAppPreferencesStore _preferencesStore;
    private readonly IMessenger _messenger;

    private CultureInfo _currentCulture;
    private string _currentCultureCode;

    public LocalizationService(
        IAppPreferencesStore? preferencesStore = null,
        IMessenger? messenger = null)
    {
        _preferencesStore = preferencesStore ?? new InMemoryAppPreferencesStore();
        _messenger = messenger ?? WeakReferenceMessenger.Default;

        var savedLocale = _preferencesStore.GetString(AppPreferenceKeys.Locale, DefaultCultureCode);
        var normalized = NormalizeCultureCode(savedLocale);

        _currentCultureCode = normalized;
        _currentCulture = CultureInfo.GetCultureInfo(normalized);
        ApplyCultureToRuntime(_currentCulture);

        lock (s_currentLock)
        {
            s_current = this;
        }
    }

    public static LocalizationService Current
    {
        get
        {
            if (s_current is not null)
            {
                return s_current;
            }

            lock (s_currentLock)
            {
                return s_current ??= new LocalizationService();
            }
        }
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public string CurrentCultureCode => _currentCultureCode;

    public bool IsVietnamese =>
        string.Equals(_currentCultureCode, VietnameseCultureCode, StringComparison.OrdinalIgnoreCase);

    public bool IsEnglish => !IsVietnamese;

    public string ActiveLanguageShortCode => IsVietnamese ? "VI" : "EN";

    public string AcceptLanguageHeaderValue => IsVietnamese
        ? "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7"
        : "en-US,en;q=0.9,vi-VN;q=0.8,vi;q=0.7";

    public string this[string key] => GetString(key);

    public string SetCulture(string cultureCode)
    {
        var normalized = NormalizeCultureCode(cultureCode);
        var culture = CultureInfo.GetCultureInfo(normalized);

        _currentCultureCode = normalized;
        _currentCulture = culture;

        ApplyCultureToRuntime(culture);
        _preferencesStore.SetString(AppPreferenceKeys.Locale, normalized);

        // Notify every compiled x:DataType binding on LocalizationService in a single pass
        OnPropertyChanged(string.Empty);

        // Broadcast to ViewModels and platform handlers so dynamic computed strings & headers refresh live
        _messenger.Send(new CultureChangedMessage(normalized));
        return normalized;
    }

    public string ToggleCulture() =>
        SetCulture(IsVietnamese ? EnglishCultureCode : VietnameseCultureCode);

    public string GetString(string key) =>
        AppResources.GetString(key, _currentCulture);

    public string Format(string key, params object?[] args) =>
        AppResources.Format(_currentCulture, key, args);

    public string GetEnumLabel(string group, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return AppResources.GetString("Common_Dash", _currentCulture);
        }

        var trimmedCode = code.Trim();
        if (string.Equals(trimmedCode, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(group, "MotorcycleCategory", StringComparison.OrdinalIgnoreCase))
            {
                return AppResources.GetString("Enum_MotorcycleCategory_All", _currentCulture);
            }

            return AppResources.GetString("Common_All", _currentCulture);
        }

        if (!string.IsNullOrWhiteSpace(group) &&
            EnumGroupAliases.TryGetValue(group.Trim(), out var canonicalGroup))
        {
            var resourceKey = $"Enum_{canonicalGroup}_{trimmedCode}";
            if (AppResources.TryGetString(resourceKey, out var localized, _currentCulture))
            {
                return localized;
            }
        }

        // Cross-group fallback for StatusBadgeView / generic enum codes
        foreach (var fallbackGroup in new[]
        {
            "BookingStatus", "TestDriveStatus", "WarrantyStatus", "VoucherStatus",
            "StockStatus", "LoyaltyTier", "SalesOrderStatus", "ReminderStatus",
            "MaintenanceStatus", "SignalRStatus", "StatusBadge"
        })
        {
            var key = $"Enum_{fallbackGroup}_{trimmedCode}";
            if (AppResources.TryGetString(key, out var found, _currentCulture))
            {
                return found;
            }
        }

        return VietnamFormatting.HumanizeStatus(trimmedCode);
    }

    public string GetErrorLabel(string? errorCode, string? fallback = null)
    {
        if (TryResolveErrorCode(errorCode, out var localized))
        {
            return localized;
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return AppResources.GetString("Err_REQUEST_FAILED", _currentCulture);
    }

    public string GetApiErrorMessage<T>(ApiResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Problem is not null)
        {
            if (TryResolveErrorCode(result.Problem.ErrorCode, out var codeMessage))
            {
                return codeMessage;
            }

            if (result.Problem.Errors is { Count: > 0 })
            {
                var firstValidation = result.Problem.Errors.Values
                    .SelectMany(v => v ?? [])
                    .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg));
                if (!string.IsNullOrWhiteSpace(firstValidation))
                {
                    return firstValidation;
                }
            }

            if (!string.IsNullOrWhiteSpace(result.Problem.Detail))
            {
                return result.Problem.Detail;
            }

            if (!string.IsNullOrWhiteSpace(result.Problem.Title))
            {
                return result.Problem.Title;
            }
        }

        if (result.IsNetworkError)
        {
            return AppResources.GetString("Err_NETWORK_OFFLINE", _currentCulture);
        }

        return AppResources.GetString("Err_REQUEST_FAILED", _currentCulture);
    }

    private bool TryResolveErrorCode(string? errorCode, out string message)
    {
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            var normalizedCode = errorCode.Trim().ToUpperInvariant().Replace('-', '_');
            switch (normalizedCode)
            {
                case "NETWORK_UNREACHABLE":
                    message = AppResources.GetString("Err_NETWORK_UNREACHABLE", _currentCulture);
                    return true;
                case "REQUEST_TIMEOUT":
                    message = AppResources.GetString("Err_REQUEST_TIMEOUT", _currentCulture);
                    return true;
            }

            if (AppResources.TryGetString($"Err_{normalizedCode}", out var localized, _currentCulture))
            {
                message = localized;
                return true;
            }
        }

        message = string.Empty;
        return false;
    }

    public string FormatQuantity(string key, int count) =>
        VietnamFormatting.FormatQuantity(key, count, _currentCulture);

    public static string NormalizeCultureCode(string? rawCultureCode)
    {
        if (string.IsNullOrWhiteSpace(rawCultureCode))
        {
            return DefaultCultureCode;
        }

        var trimmed = rawCultureCode.Trim().ToLowerInvariant();
        if (trimmed is "en" or "en-us" || trimmed.StartsWith("en-", StringComparison.Ordinal))
        {
            return EnglishCultureCode;
        }

        return VietnameseCultureCode;
    }

    private static void ApplyCultureToRuntime(CultureInfo culture)
    {
        AppResources.Culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
