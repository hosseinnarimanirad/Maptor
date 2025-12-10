using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class SrsTypeToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CoordinateDisplayMode srsType)
        {
            return srsType switch
            {
                CoordinateDisplayMode.UTM => "UTM",
                CoordinateDisplayMode.WebMercator => "Web Mercator",
                CoordinateDisplayMode.GeodeticDecimal => "Geodetic (Decimal Degrees)",
                CoordinateDisplayMode.GeodeticDms => "Geodetic (DMS)",
                _ => srsType.ToString()
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}