using System;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class Base64ToByteArrayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            return System.Convert.FromBase64String(value.ToString());
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
