using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DataSourceKindToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DataSourceKind kind || kind == DataSourceKind.None)
            return Brushes.Transparent;

        return kind switch
        {
            DataSourceKind.Shapefile => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), // green
            DataSourceKind.Kml => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),      // orange
            DataSourceKind.GeoJson => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),  // blue
            DataSourceKind.WebApi => new SolidColorBrush(Color.FromRgb(0x9C, 0x27, 0xB0)),   // purple
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
