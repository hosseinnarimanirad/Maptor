using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using IRI.Maptor.Jab.Common.Assets.ColorBrushes;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DataSourceKindToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DataSourceKind kind)
            return Brushes.Transparent;

        return kind switch
        {
            DataSourceKind.Shapefile => ModernUIColors.GreenBrush,

            DataSourceKind.Kmz => ModernUIColors.LimeGreenBrush,
            DataSourceKind.Kml => ModernUIColors.LimeBrush,
            DataSourceKind.Gpx => ModernUIColors.LimeBrush,

            DataSourceKind.Dxf => ModernUIColors.TealBrush,

            DataSourceKind.WebApi => ModernUIColors.CyanBrush,
            DataSourceKind.GRPC => ModernUIColors.BlueBrush,

            DataSourceKind.GeoJson => ModernUIColors.IndigoBrush,
            DataSourceKind.TopoJson => ModernUIColors.IndigoBrush,

            DataSourceKind.Csv => ModernUIColors.VioletBrush,
            DataSourceKind.Tsv => ModernUIColors.PinkBrush,

            DataSourceKind.Worldfile => ModernUIColors.CrimsonBrush,
            DataSourceKind.GeoTiff => ModernUIColors.RedBrush,
            DataSourceKind.ZippedImagePyramid => ModernUIColors.OrangeBrush,

            DataSourceKind.GML => ModernUIColors.AmberBrush,
            DataSourceKind.Other => ModernUIColors.SteelBrush,
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
