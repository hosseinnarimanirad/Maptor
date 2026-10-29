using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using MahApps.Metro.IconPacks;
using WellKnownMark = IRI.Maptor.Core.Ogc.SLD.WellKnownMark;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>An opacity (0–1) as a whole percentage, "80%".</summary>
public class FractionToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is double fraction ? $"{Math.Round(fraction * 100):0}%" : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>The outline of an SLD well-known mark in a 16×16 box, for the shape picker.</summary>
public class WellKnownMarkToGeometryConverter : IValueConverter
{
    private static readonly Geometry Circle = Freeze(Geometry.Parse("M8,1 A7,7 0 1 1 7.99,1 Z"));
    private static readonly Geometry Square = Freeze(Geometry.Parse("M2,2 H14 V14 H2 Z"));
    private static readonly Geometry Triangle = Freeze(Geometry.Parse("M8,1.5 L15,14 L1,14 Z"));
    private static readonly Geometry Star = Freeze(Geometry.Parse("M8,0.8 L10.1,5.6 L15.2,5.9 L11.3,9.2 L12.6,14.3 L8,11.5 L3.4,14.3 L4.7,9.2 L0.8,5.9 L5.9,5.6 Z"));
    private static readonly Geometry Cross = Freeze(Geometry.Parse("M6,1 H10 V6 H15 V10 H10 V15 H6 V10 H1 V6 H6 Z"));
    private static readonly Geometry X = Freeze(Geometry.Parse("M3,1 L8,6 L13,1 L15,3 L10,8 L15,13 L13,15 L8,10 L3,15 L1,13 L6,8 L1,3 Z"));

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            WellKnownMark.circle => Circle,
            WellKnownMark.square => Square,
            WellKnownMark.triangle => Triangle,
            WellKnownMark.star => Star,
            WellKnownMark.cross => Cross,
            WellKnownMark.x => X,
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Geometry Freeze(Geometry geometry)
    {
        geometry.Freeze();
        return geometry;
    }
}

/// <summary>The icon of a symbolizer kind (<c>SymbolizerViewModelBase.SymbolizerType</c>).</summary>
public class SymbolizerTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value as string switch
        {
            "Point" => PackIconMaterialKind.MapMarkerOutline,
            "Line" => PackIconMaterialKind.VectorPolyline,
            "Polygon" => PackIconMaterialKind.FormatColorFill,
            "Text" => PackIconMaterialKind.FormatText,
            "Raster" => PackIconMaterialKind.ImageOutline,
            _ => PackIconMaterialKind.ShapeOutline
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
