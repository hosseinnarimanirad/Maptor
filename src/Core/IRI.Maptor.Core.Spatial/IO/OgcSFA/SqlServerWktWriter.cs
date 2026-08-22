using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.IO.OgcSFA;

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
    public static string AsWkt<T>(Geometry<T> geometry, int? coordinateDecimalPlaces = null) where T : IPoint, new()
    {
        if (coordinateDecimalPlaces is < 0)
            throw new ArgumentOutOfRangeException(nameof(coordinateDecimalPlaces), coordinateDecimalPlaces, "Coordinate decimal places must be non-negative.");

        switch (geometry.Type)
        {
            case GeometryType.Point:
                return FormattableString.Invariant($"POINT {WktHelpers.ToWktPointArrayString(geometry, isRingBase: false, coordinateDecimalPlaces)}");

            case GeometryType.LineString:
                return FormattableString.Invariant($"LINESTRING {WktHelpers.ToWktPointArrayString(geometry, isRingBase: false, coordinateDecimalPlaces)}");

            case GeometryType.Polygon:
                return FormattableString.Invariant($"POLYGON {WktHelpers.ToWktPointArrayString(geometry, isRingBase: true, coordinateDecimalPlaces)}");

            case GeometryType.MultiPoint:
                return FormattableString.Invariant($"MULTIPOINT {WktHelpers.ToWktPointArrayString(geometry, isRingBase: false, coordinateDecimalPlaces)}");

            case GeometryType.MultiLineString:
                return FormattableString.Invariant($"MULTILINESTRING {WktHelpers.ToWktPointArrayString(geometry, isRingBase: false, coordinateDecimalPlaces)}");

            case GeometryType.MultiPolygon:
                return FormattableString.Invariant($"MULTIPOLYGON {WktHelpers.ToWktPointArrayString(geometry, isRingBase: true, coordinateDecimalPlaces)}");

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"SqlServerWktWriter > AsWkt: Geometry type '{geometry.Type}' is not implemented");
        }
    }
}

