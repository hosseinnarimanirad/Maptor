using System;
using System.Windows;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Converters;

public class VisibilityToBoolConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var visibility = (Visibility)value;

        return visibility switch
        {
            Visibility.Visible => true,
            Visibility.Collapsed => false,
            Visibility.Hidden => null,
            _ => false,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is null)
            return Visibility.Hidden;

        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }
}
