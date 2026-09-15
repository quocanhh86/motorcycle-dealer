using System.Globalization;
using Humanizer;

namespace MotorcycleDealer.Mobile.Core.Domain;

/// <summary>
/// Vietnam presentation formatting for currency (vi-VN integer VND with dot thousands separators),
/// Humanizer status/pluralization/relative-sync helpers, and Asia/Ho_Chi_Minh (UTC+07:00) display timestamps
/// converted strictly at the presentation boundary.
/// </summary>
public static class VietnamFormatting
{
    public static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private static readonly NumberFormatInfo VndNumberFormat = CreateVndNumberFormat();
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

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

    public static string FormatServiceType(string? serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            return string.Empty;
        }

        var trimmed = serviceType.Trim();
        return string.Equals(trimmed, "OilAndTire", StringComparison.Ordinal)
            ? "Oil & Tire"
            : HumanizeStatus(trimmed);
    }

    public static string FormatSyncAge(DateTimeOffset? syncedAtUtc, DateTimeOffset? nowUtc = null)
    {
        if (syncedAtUtc is null)
        {
            return "Never synced";
        }

        var reference = nowUtc ?? DateTimeOffset.UtcNow;
        var elapsed = reference - syncedAtUtc.Value;

        if (elapsed.TotalSeconds < 45)
        {
            return "Synced just now";
        }

        return $"Synced {syncedAtUtc.Value.Humanize(dateToCompareAgainst: reference, culture: EnglishCulture)}";
    }

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

    private static NumberFormatInfo CreateVndNumberFormat()
    {
        var info = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        info.NumberGroupSeparator = ".";
        info.NumberDecimalSeparator = ",";
        info.NumberDecimalDigits = 0;
        return info;
    }
}
