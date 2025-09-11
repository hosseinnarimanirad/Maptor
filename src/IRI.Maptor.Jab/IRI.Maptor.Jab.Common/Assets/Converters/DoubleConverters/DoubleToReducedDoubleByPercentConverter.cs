using System;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DoubleToReducedDoubleByPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double doubleValue;

        if (!double.TryParse(value?.ToString(), out doubleValue))
        {
            doubleValue = 0;
        }

        double percent;

        if (!double.TryParse(parameter?.ToString(), out percent))
        {
            percent = 0;
        }

        return percent / 100.0 * doubleValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
