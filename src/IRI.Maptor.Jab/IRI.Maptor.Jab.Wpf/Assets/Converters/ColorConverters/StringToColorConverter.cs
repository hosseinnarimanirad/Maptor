using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using IRI.Maptor.Jab.Wpf.Helpers;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ColorHelper.ToWpfColor(value?.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //return ColorHelper.ToHexString((Color)value);

        return ((Color)value).ToHexString();
    }
}
