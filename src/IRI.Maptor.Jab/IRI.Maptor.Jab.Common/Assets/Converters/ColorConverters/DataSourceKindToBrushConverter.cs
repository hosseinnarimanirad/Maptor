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
            DataSourceKind.Kmz => ModernUIColors.LimeGreenBrush,
            DataSourceKind.Kml => ModernUIColors.LimeBrush,

            DataSourceKind.Shapefile => ModernUIColors.GreenBrush,

            DataSourceKind.GML => ModernUIColors.TealBrush,

            DataSourceKind.WebApi => ModernUIColors.CyanBrush,
            DataSourceKind.GRPC => ModernUIColors.BlueBrush,

            DataSourceKind.GeoJson => ModernUIColors.IndigoBrush,
            DataSourceKind.TopoJson => ModernUIColors.VioletBrush,

            DataSourceKind.Tsv => ModernUIColors.PinkBrush,
            DataSourceKind.Csv => ModernUIColors.MagentaBrush,

            DataSourceKind.Gpx => ModernUIColors.CrimsonBrush,

            DataSourceKind.ZippedImagePyramid => ModernUIColors.RedBrush,
            DataSourceKind.GeoTiff => ModernUIColors.OrangeBrush,
            DataSourceKind.Dxf => ModernUIColors.AmberBrush,

            //DataSourceKind.EPS => ModernUIColors.YellowBrush,
            //DataSourceKind.SVG => ModernUIColors.BrownBrush,
            //DataSourceKind.GRD => ModernUIColors.OliveBrush,

            DataSourceKind.Other => ModernUIColors.SteelBrush,
            //?? => ModernUIColors.MauveColor,
            DataSourceKind.Worldfile => ModernUIColors.TaupeBrush,

            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
