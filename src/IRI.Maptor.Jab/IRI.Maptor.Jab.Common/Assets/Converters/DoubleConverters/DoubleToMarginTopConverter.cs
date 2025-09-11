using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DoubleToMarginTopConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double marginTop;

        if (!double.TryParse(value?.ToString(), out marginTop))
        {
            marginTop = 0;
        }

        return new Thickness(0, marginTop, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
