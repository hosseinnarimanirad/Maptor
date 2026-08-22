using System;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class IsLessThanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //if (value is null)
        //    return false;

        double doubleValue;

        if (!double.TryParse(value?.ToString(), out doubleValue))
            return false;

        double parameterValue;

        if (!double.TryParse(parameter?.ToString(), out parameterValue))
            return false;

        return doubleValue < parameterValue;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
