using System;
using System.Windows.Data;
using System.Globalization;
using IRI.Maptor.Jab.Common.TileServices;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class IsMapProviderSelected : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TileMapProvider tileMapProvider ? tileMapProvider.Is(parameter?.ToString()) : false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
