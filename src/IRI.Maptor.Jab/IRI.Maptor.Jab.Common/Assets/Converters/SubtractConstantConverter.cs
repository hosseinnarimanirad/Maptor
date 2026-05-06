using System;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Converters;

public class SubtractConstantConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        double originalValue = (double)value;

        double subtractValue = System.Convert.ToDouble(parameter);

        return originalValue - subtractValue;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return null;
    }
}
