using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class SrsTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CoordinateDisplayMode srsType)
        {
            // Map icon for map projections (UTM, WebMercator)
            // Earth icon for geodetic coordinates
            return srsType switch
            {
                CoordinateDisplayMode.UTM => "Map",
                CoordinateDisplayMode.WebMercator => "Map",
                CoordinateDisplayMode.GeodeticDecimal => "Earth",
                CoordinateDisplayMode.GeodeticDms => "Earth",
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




