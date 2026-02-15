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
            DataSourceKind.Kml => ModernUIColors.OrangeBrush,
            DataSourceKind.Kmz => ModernUIColors.AmberBrush,
            DataSourceKind.GeoJson => ModernUIColors.BlueBrush,
            DataSourceKind.WebApi => ModernUIColors.VioletBrush,
            DataSourceKind.GRPC => ModernUIColors.IndigoBrush,
            DataSourceKind.GML => ModernUIColors.TealBrush,
            DataSourceKind.Dxf => ModernUIColors.CrimsonBrush,
            DataSourceKind.Worldfile => ModernUIColors.BrownBrush,
            DataSourceKind.GeoTiff => ModernUIColors.TaupeBrush,
            DataSourceKind.ZippedImagePyramid => ModernUIColors.CyanBrush,
            DataSourceKind.Csv => ModernUIColors.LimeGreenBrush,
            DataSourceKind.Tsv => ModernUIColors.LimeBrush,
            DataSourceKind.Other => ModernUIColors.SteelBrush,
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
