using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Common.Converters;

/// <summary>
/// Converts UTC DateTime to local time string for display (e.g. tooltip).
/// Uses current culture for formatting.
/// </summary>
public class UtcDateTimeToLocalTimeConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
            return string.Empty;

        var localTime = dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime;

        var cultureInfo = LocalizationManager.Instance.CurrentCulture;

        var formatted = localTime.ToString("g", cultureInfo);

        if (LocalizationManager.Instance.IsPersian)
            formatted = formatted.LatinNumbersToFarsiNumbers();

        return formatted;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
