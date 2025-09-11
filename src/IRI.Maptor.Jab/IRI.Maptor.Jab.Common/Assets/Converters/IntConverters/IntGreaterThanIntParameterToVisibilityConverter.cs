using System;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class IntGreaterThanIntParameterToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        int intValue = (int)value;

        int minValue = int.Parse(parameter.ToString());

        return intValue > minValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
