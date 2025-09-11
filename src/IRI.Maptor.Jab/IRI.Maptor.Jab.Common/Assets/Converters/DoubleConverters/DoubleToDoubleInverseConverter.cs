using System;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DoubleToDoubleInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return 1.0 / (double)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return 1.0 / (double)value;
    }
}
