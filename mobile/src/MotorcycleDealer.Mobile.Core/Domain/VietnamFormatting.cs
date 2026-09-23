using System.Globalization;
using Humanizer;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.Core.Domain;

/// <summary>
/// Vietnam presentation formatting for currency (vi-VN integer VND with dot thousands separators),
/// bilingual Humanizer.Core + Humanizer.Core.vi status/pluralization/relative-time helpers,
/// and Asia/Ho_Chi_Minh (UTC+07:00) display timestamps converted strictly at the presentation boundary.
/// </summary>
public static class VietnamFormatting
{
    public static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private static readonly NumberFormatInfo VndNumberFormat = CreateVndNumberFormat();
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    private static readonly Dictionary<string, (string ResourceKey, string EnglishSingular)> QuantityAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["model"] = ("Qty_Model", "model"),
            ["Qty_Model"] = ("Qty_Model", "model"),
            ["branch"] = ("Qty_Branch", "branch"),
            ["Qty_Branch"] = ("Qty_Branch", "branch"),
            ["bay"] = ("Qty_Bay", "bay"),
            ["Qty_Bay"] = ("Qty_Bay", "bay"),
            ["unit"] = ("Qty_Unit", "unit"),
            ["Qty_Unit"] = ("Qty_Unit", "unit"),
            ["motorcycle"] = ("Qty_Motorcycle", "motorcycle"),
            ["Qty_Motorcycle"] = ("Qty_Motorcycle", "motorcycle"),
            ["vin"] = ("Qty_Vin", "VIN"),
            ["Qty_Vin"] = ("Qty_Vin", "VIN"),
            ["slot"] = ("Qty_Slot", "slot"),
            ["Qty_Slot"] = ("Qty_Slot", "slot"),
            ["alert"] = ("Qty_Alert", "alert"),
            ["Qty_Alert"] = ("Qty_Alert", "alert"),
            ["event"] = ("Qty_Event", "event"),
            ["Qty_Event"] = ("Qty_Event", "event"),
            ["day"] = ("Qty_Day", "day"),
            ["Qty_Day"] = ("Qty_Day", "day"),
            ["month"] = ("Qty_Month", "month"),
            ["Qty_Month"] = ("Qty_Month", "month")
        };

    public static string FormatVnd(decimal amount)
    {
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        return string.Format(VndNumberFormat, "{0:N0} ₫", rounded);
    }

    public static string FormatPoints(int points)
    {
        var prefix = points > 0 ? "+" : string.Empty;
        return string.Format(VndNumberFormat, "{0}{1:N0} pts", prefix, points);
    }

    public static string FormatInteger(int value) =>
        string.Format(VndNumberFormat, "{0:N0}", value);

    public static string FormatPointsBalance(int points) =>
        string.Format(VndNumberFormat, "{0:N0} pts", points);

    public static string FormatPointsLocalized(int points, bool signed = false, CultureInfo? culture = null)
    {
        var effectiveCulture = ResolveCulture(culture);
        var unit = AppResources.GetString("Common_PointsShort", effectiveCulture);
        var prefix = signed && points > 0 ? "+" : string.Empty;
        return $"{prefix}{FormatInteger(points)} {unit}";
    }

    public static string FormatMileageKm(int mileageKm) =>
        string.Format(VndNumberFormat, "{0:N0} km", mileageKm);

    public static DateTimeOffset ToVietnamTime(DateTimeOffset utcTimestamp) =>
        utcTimestamp.ToOffset(VietnamUtcOffset);

    public static string FormatVietnamDateTime(DateTimeOffset utcTimestamp) =>
        ToVietnamTime(utcTimestamp).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string FormatVietnamDate(DateTimeOffset utcTimestamp) =>
        ToVietnamTime(utcTimestamp).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string FormatSlotDate(DateOnly slotDate) =>
        slotDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string FormatTimeRange(string startTime, string endTime)
    {
        static string Shorten(string? raw) =>
            string.IsNullOrEmpty(raw) ? string.Empty : (raw.Length >= 5 ? raw[..5] : raw);

        return $"{Shorten(startTime)} - {Shorten(endTime)}";
    }

    public static string HumanizeStatus(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return code.Trim().Humanize(LetterCasing.Title);
    }

    public static string FormatServiceType(string? serviceType, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            return string.Empty;
        }

        var trimmed = serviceType.Trim();
        var effectiveCulture = ResolveCulture(culture);
        if (AppResources.TryGetString($"Enum_ServiceType_{trimmed}", out var localized, effectiveCulture))
        {
            return localized;
        }

        return string.Equals(trimmed, "OilAndTire", StringComparison.Ordinal)
            ? "Oil & Tire"
            : HumanizeStatus(trimmed);
    }

    /// <summary>
    /// Formats a relative timestamp using Humanizer.Core + Humanizer.Core.vi for the active UI culture
    /// (e.g., "Vừa xong" / "5 phút trước" in vi-VN; "Just now" / "5 minutes ago" in en-US).
    /// </summary>
    public static string FormatRelativeTime(
        DateTimeOffset timestampUtc,
        DateTimeOffset? nowUtc = null,
        CultureInfo? culture = null)
    {
        var effectiveCulture = ResolveCulture(culture);
        var reference = nowUtc ?? DateTimeOffset.UtcNow;
        var elapsed = reference - timestampUtc;

        if (Math.Abs(elapsed.TotalSeconds) < 45)
        {
            return AppResources.GetString("Common_JustNow", effectiveCulture);
        }

        var humanizerCulture = IsVietnamese(effectiveCulture) ? VietnameseCulture : EnglishCulture;
        return timestampUtc.Humanize(dateToCompareAgainst: reference, culture: humanizerCulture);
    }

    /// <summary>
    /// Formats a localized sync-age banner using Humanizer.Core / Humanizer.Core.vi.
    /// </summary>
    public static string FormatSyncAge(
        DateTimeOffset? syncedAtUtc,
        DateTimeOffset? nowUtc = null,
        CultureInfo? culture = null)
    {
        var effectiveCulture = ResolveCulture(culture);
        if (syncedAtUtc is null)
        {
            return AppResources.GetString("Common_NeverSynced", effectiveCulture);
        }

        var reference = nowUtc ?? DateTimeOffset.UtcNow;
        var elapsed = reference - syncedAtUtc.Value;

        if (elapsed.TotalSeconds < 45)
        {
            return AppResources.GetString("Common_SyncedJustNow", effectiveCulture);
        }

        var humanizerCulture = IsVietnamese(effectiveCulture) ? VietnameseCulture : EnglishCulture;
        var relative = syncedAtUtc.Value.Humanize(dateToCompareAgainst: reference, culture: humanizerCulture);
        return AppResources.Format(effectiveCulture, "Common_SyncedAgoFormat", relative);
    }

    /// <summary>
    /// Compact English sync-age helper preserved verbatim for deterministic formatting contracts.
    /// </summary>
    public static string FormatRelativeSyncAge(DateTimeOffset? lastSyncedUtc, DateTimeOffset? nowUtc = null)
    {
        if (lastSyncedUtc is null)
        {
            return "Never synced";
        }

        var reference = nowUtc ?? DateTimeOffset.UtcNow;
        var elapsed = reference - lastSyncedUtc.Value;

        if (elapsed.TotalSeconds < 45)
        {
            return "Synced just now";
        }

        if (elapsed.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes));
            return $"Synced {minutes}m ago";
        }

        if (elapsed.TotalHours < 24)
        {
            var hours = (int)Math.Floor(elapsed.TotalHours);
            return $"Synced {hours}h ago";
        }

        var days = (int)Math.Floor(elapsed.TotalDays);
        return $"Synced {days}d ago";
    }

    /// <summary>
    /// Culture-aware compact relative sync-age helper ("Đồng bộ 12 phút trước" / "Synced 12m ago").
    /// </summary>
    public static string FormatRelativeSyncAge(
        DateTimeOffset? lastSyncedUtc,
        DateTimeOffset? nowUtc,
        CultureInfo culture)
    {
        var effectiveCulture = ResolveCulture(culture);
        if (lastSyncedUtc is null)
        {
            return AppResources.GetString("Common_NeverSynced", effectiveCulture);
        }

        var reference = nowUtc ?? DateTimeOffset.UtcNow;
        var elapsed = reference - lastSyncedUtc.Value;

        if (elapsed.TotalSeconds < 45)
        {
            return AppResources.GetString("Common_SyncedJustNow", effectiveCulture);
        }

        if (elapsed.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes));
            return AppResources.Format(effectiveCulture, "Common_SyncedMinutesAgo", minutes);
        }

        if (elapsed.TotalHours < 24)
        {
            var hours = (int)Math.Floor(elapsed.TotalHours);
            return AppResources.Format(effectiveCulture, "Common_SyncedHoursAgo", hours);
        }

        var days = (int)Math.Floor(elapsed.TotalDays);
        return AppResources.Format(effectiveCulture, "Common_SyncedDaysAgo", days);
    }

    /// <summary>
    /// Formats a localized quantity string: uses Humanizer .ToQuantity() for English pluralization
    /// and idiomatic Vietnamese noun phrasing when vi-VN is active.
    /// </summary>
    public static string FormatQuantity(string key, int count, CultureInfo? culture = null)
    {
        var effectiveCulture = ResolveCulture(culture);
        var trimmedKey = (key ?? string.Empty).Trim();

        if (IsVietnamese(effectiveCulture))
        {
            var noun = QuantityAliases.TryGetValue(trimmedKey, out var entry)
                ? AppResources.GetString(entry.ResourceKey, effectiveCulture)
                : (AppResources.TryGetString(trimmedKey, out var directVi, effectiveCulture) ? directVi : trimmedKey);
            return $"{FormatInteger(count)} {noun}";
        }

        var englishSingular = QuantityAliases.TryGetValue(trimmedKey, out var enEntry)
            ? enEntry.EnglishSingular
            : (AppResources.TryGetString(trimmedKey, out var directEn, EnglishCulture) ? directEn : trimmedKey);

        return englishSingular.ToQuantity(count);
    }

    private static CultureInfo ResolveCulture(CultureInfo? culture) =>
        culture ?? AppResources.Culture ?? CultureInfo.CurrentUICulture;

    private static bool IsVietnamese(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName.Equals("vi", StringComparison.OrdinalIgnoreCase);

    private static NumberFormatInfo CreateVndNumberFormat()
    {
        var info = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        info.NumberGroupSeparator = ".";
        info.NumberDecimalSeparator = ",";
        info.NumberDecimalDigits = 0;
        return info;
    }
}
