using System.Globalization;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.App.Converters;

/// <summary>
/// Localizes canonical domain status / enum codes via LocalizationService.Current.GetEnumLabel.
/// Optionally accepts an enum group name via ConverterParameter (e.g. "BookingStatus", "WarrantyStatus").
/// </summary>
public sealed class HumanizeStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string raw = (value?.ToString() ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return LocalizationService.Current.Common_Dash;
        }

        string group = parameter as string ?? string.Empty;
        return LocalizationService.Current.GetEnumLabel(group, raw);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
