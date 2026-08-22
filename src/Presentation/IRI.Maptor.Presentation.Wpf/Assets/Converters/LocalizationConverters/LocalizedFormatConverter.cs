using System;
using System.Globalization;
using System.Windows.Data;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>
/// Formats a value with a format string that is itself localized, which a plain
/// Binding cannot do (StringFormat is not bindable).
/// values[0] = the value, values[1] = the format string, e.g. "(also: {0})".
/// </summary>
public class LocalizedFormatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
            return string.Empty;

        var format = values[1] as string;

        if (string.IsNullOrEmpty(format))
            return values[0]?.ToString() ?? string.Empty;

        return string.Format(culture, format, values[0]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
