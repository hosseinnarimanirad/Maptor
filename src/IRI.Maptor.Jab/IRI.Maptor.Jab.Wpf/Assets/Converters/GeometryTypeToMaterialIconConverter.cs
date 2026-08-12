using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Sta.Common.Enums;
using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class GeometryTypeToMaterialIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GeometryType geometryType)
        {
            return geometryType switch
            {
                GeometryType.Point => PackIconMaterialKind.VectorPoint,
                GeometryType.MultiPoint => PackIconMaterialKind.VectorPoint,

                GeometryType.LineString => PackIconMaterialKind.VectorPolyline,
                GeometryType.MultiLineString => PackIconMaterialKind.VectorPolyline,

                GeometryType.Polygon => PackIconMaterialKind.VectorSquare,
                GeometryType.MultiPolygon => PackIconMaterialKind.VectorSquare,

                _ => PackIconMaterialKind.Null// Default fallback
            };
        }
        return PackIconMaterialKind.Null; // Default fallback
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}