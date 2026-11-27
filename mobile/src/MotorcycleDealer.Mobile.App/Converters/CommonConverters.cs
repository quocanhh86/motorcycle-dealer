using System.Globalization;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.App.Converters;

public sealed class IsGreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            int i => i > 0,
            long l => l > 0,
            decimal d => d > 0m,
            double dbl => dbl > 0d,
            _ => false
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class EqualsStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class VndCurrencyConverter : IValueConverter
{
    private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        decimal amount = value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double dbl => (decimal)dbl,
            _ => 0m
        };

        return $"{Math.Round(amount, 0, MidpointRounding.AwayFromZero).ToString("N0", ViVn)} ₫";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class VietnamDateTimeConverter : IValueConverter
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string format = parameter as string ?? "dd/MM/yyyy HH:mm";
        return value switch
        {
            DateTimeOffset dto => dto.ToOffset(VietnamOffset).ToString(format, CultureInfo.InvariantCulture) + " ICT",
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)).ToOffset(VietnamOffset).ToString(format, CultureInfo.InvariantCulture) + " ICT",
            DateOnly d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm", CultureInfo.InvariantCulture),
            _ => "—"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class PointsDisplayConverter : IValueConverter
{
    private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int pts = value switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            _ => 0
        };

        bool signed = string.Equals(parameter as string, "signed", StringComparison.OrdinalIgnoreCase);
        string unit = LocalizationService.Current.Common_PointsShort;
        if (signed && pts > 0)
        {
            return $"+{pts.ToString("N0", ViVn)} {unit}";
        }

        return $"{pts.ToString("N0", ViVn)} {unit}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToChipBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool active = value is bool b && b;
        if (active)
        {
            return Color.FromArgb("#DC2626");
        }

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return isDark ? Color.FromArgb("#1E293B") : Color.FromArgb("#F1F5F9");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToChipTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool active = value is bool b && b;
        if (active)
        {
            return Colors.White;
        }

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return isDark ? Color.FromArgb("#CBD5E1") : Color.FromArgb("#334155");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StringEqualsMultiConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
        {
            return false;
        }

        string? left = values[0]?.ToString();
        string? right = values[1]?.ToString();
        return !string.IsNullOrWhiteSpace(left)
            && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
