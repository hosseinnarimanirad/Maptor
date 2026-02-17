using System;
using System.Windows.Data;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class SolidColorBrushToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return (value as SolidColorBrush)?.Color ?? Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is null)
            return new SolidColorBrush(Colors.Transparent);

        return new SolidColorBrush((Color)value);
    }
}