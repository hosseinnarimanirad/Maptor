using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
            return new SolidColorBrush(color);
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value as SolidColorBrush)?.Color ?? Colors.Transparent;
    }
}
