using System;
using System.Windows.Data;
using System.Globalization;
using IRI.Maptor.Jab.Core;

namespace IRI.Maptor.Jab.Common.Converters;

public class TileTypeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is BaseMapType) || parameter == null)
        {
            return false;
        }

        var type = (BaseMapType)value;

        var expectedType = (BaseMapType)Enum.Parse(typeof(BaseMapType), parameter.ToString(), true);

        return type == expectedType;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((bool)value && parameter != null)
        {
            return (BaseMapType)Enum.Parse(typeof(BaseMapType), parameter.ToString(), true);
        }
        else
        {
            return BaseMapType.Google_Terrain;
        }

    }
}
