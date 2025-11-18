using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

/// <summary>
/// Writes WKT strings in SQL Server format.
/// SQL Server format differences from OGC:
/// - No dimension suffix (POINT(1 2 3) instead of POINT Z(1 2 3))
/// - Dimension is inferred from coordinate count
/// </summary>
public static class SqlServerWktWriter
{
    /// <summary>
    /// Converts a Geometry to WKT string format in SQL Server format.
    /// SQL Server format does not use dimension suffixes (Z, M, ZM).
    /// </summary>
    public static string AsWkt<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
                return FormattableString.Invariant($"POINT{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.LineString:
                return FormattableString.Invariant($"LINESTRING{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.Polygon:
                return FormattableString.Invariant($"POLYGON{WktHelpers.ToWktPointArrayString(geometry, isRingBase: true)}");

            case GeometryType.MultiPoint:
                return FormattableString.Invariant($"MULTIPOINT{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.MultiLineString:
                return FormattableString.Invariant($"MULTILINESTRING{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.MultiPolygon:
                return FormattableString.Invariant($"MULTIPOLYGON{WktHelpers.ToWktPointArrayString(geometry, isRingBase: true)}");

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"SqlServerWktWriter > AsWkt: Geometry type '{geometry.Type}' is not implemented");
        }
    }
}

