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

        if (!double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
            return false;

        double parameterValue;

        if (!double.TryParse(System.Convert.ToString(parameter, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out parameterValue))
            return false;

        return doubleValue < parameterValue;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
