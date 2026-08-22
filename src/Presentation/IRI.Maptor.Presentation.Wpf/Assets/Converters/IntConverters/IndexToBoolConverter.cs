using System;
using System.Globalization;
using System.Windows.Data;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class IndexToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter is null) return false;

        return (int)value == int.Parse(parameter.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? int.Parse(parameter.ToString()) : -1;
    }
}
