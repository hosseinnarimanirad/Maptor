using System;
using System.Windows;
using System.Windows.Data;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        bool target = true;

        if (parameter != null)
            bool.TryParse(parameter.ToString(), out target);

        if (value is bool boolValue)
            return (boolValue == target) ? Visibility.Visible : Visibility.Collapsed;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if ((Visibility)value == Visibility.Visible)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
