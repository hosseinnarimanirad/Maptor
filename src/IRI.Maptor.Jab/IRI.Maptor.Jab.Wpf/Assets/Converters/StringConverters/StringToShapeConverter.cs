using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class StringToShapeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(value?.ToString()))
            return null;
        try
        {
            return System.Windows.Media.Geometry.Parse(value?.ToString());
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
