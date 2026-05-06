using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.Converters;

public class CoordinateDimensionToZVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CoordinateDimension dimension)
        {
            return dimension == CoordinateDimension.Z || dimension == CoordinateDimension.ZM
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}