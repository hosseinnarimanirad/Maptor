using System;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class TileTypeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is TileType) || parameter == null)
        {
            return false;
        }

        var type = (TileType)value;

        var expectedType = (TileType)Enum.Parse(typeof(TileType), parameter.ToString(), true);

        return type == expectedType;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((bool)value && parameter != null)
        {
            return (TileType)Enum.Parse(typeof(TileType), parameter.ToString(), true);
        }
        else
        {
            return TileType.Terrain;
        }

    }
}
