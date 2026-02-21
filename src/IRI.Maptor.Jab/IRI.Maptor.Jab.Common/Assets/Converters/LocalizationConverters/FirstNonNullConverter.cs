using System;
using System.Globalization;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

/// <summary>
/// Returns the first non-null, non-empty value from the MultiBinding values.
/// Used for fallback display (e.g. BannerText ?? LocalizedString).
/// </summary>
public class FirstNonNullConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return s;
            if (value != null)
                return value;
        }
        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
