using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Controls.Views.General.Converters;

public class CoordinateDimensionToMVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CoordinateDimension dimension)
        {
            return dimension == CoordinateDimension.M || dimension == CoordinateDimension.ZM
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

