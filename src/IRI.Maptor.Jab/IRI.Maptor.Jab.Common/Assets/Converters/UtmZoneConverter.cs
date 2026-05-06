using System;
using System.Globalization;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Converters;

public class UtmZoneConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int zone && zone > 0)
        {
            return $" | Zone: {zone}";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}




