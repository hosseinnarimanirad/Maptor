using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

class EnumToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return DependencyProperty.UnsetValue;

        try
        {
            return ((Enum)value).GetDescription();
        }
        catch (Exception ex)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Enum.ToObject(targetType, value);
    }
}
