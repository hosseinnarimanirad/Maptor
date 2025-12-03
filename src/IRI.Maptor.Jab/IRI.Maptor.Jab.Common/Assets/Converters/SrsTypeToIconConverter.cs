using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class SrsTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CoordinateEditorSrsType srsType)
        {
            // Map icon for map projections (UTM, WebMercator)
            // Earth icon for geodetic coordinates
            return srsType switch
            {
                CoordinateEditorSrsType.UTM => "Map",
                CoordinateEditorSrsType.WebMercator => "Map",
                CoordinateEditorSrsType.GeodeticDecimal => "Earth",
                CoordinateEditorSrsType.GeodeticDms => "Earth",
                _ => "Map"
            };
        }
        return "Map";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

