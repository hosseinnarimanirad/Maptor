using System;
using System.Windows;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        bool target = true;

        if (parameter != null)
        {
            target = bool.Parse(parameter.ToString());
        }

        if (!(value is bool))
        {
            return Visibility.Collapsed;
        }

        if ((bool)value == target)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Collapsed;
        }
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
