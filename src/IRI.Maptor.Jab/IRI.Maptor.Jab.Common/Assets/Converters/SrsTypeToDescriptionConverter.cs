using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class SrsTypeToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CoordinateEditorSrsType srsType)
        {
            return srsType switch
            {
                CoordinateEditorSrsType.UTM => "UTM",
                CoordinateEditorSrsType.WebMercator => "Web Mercator",
                CoordinateEditorSrsType.GeodeticDecimal => "Geodetic (Decimal Degrees)",
                CoordinateEditorSrsType.GeodeticDms => "Geodetic (DMS)",
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

