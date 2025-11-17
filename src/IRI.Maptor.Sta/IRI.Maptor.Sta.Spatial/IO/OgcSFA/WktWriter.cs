using System.Text;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

public static class WktWriter
{
    public static string AsWkt<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        // Detect coordinate dimension from actual point instances
        // Check first point if available, otherwise check type
        bool hasZ = false;
        bool hasM = false;
        
        if (geometry.Points != null && geometry.Points.Count > 0)
        {
            var firstPoint = geometry.Points[0];
            hasZ = firstPoint is IHasZ;
            hasM = firstPoint is IHasM;
        }
        else if (geometry.Geometries != null && geometry.Geometries.Count > 0)
        {
            // For multi-geometries, check first geometry's first point
            var firstGeometry = geometry.Geometries[0];
            if (firstGeometry.Points != null && firstGeometry.Points.Count > 0)
            {
                var firstPoint = firstGeometry.Points[0];
                hasZ = firstPoint is IHasZ;
                hasM = firstPoint is IHasM;
            }
        }
        else
        {
            // Fallback to type check
            hasZ = typeof(IHasZ).IsAssignableFrom(typeof(T));
            hasM = typeof(IHasM).IsAssignableFrom(typeof(T));
        }
        
        string dimensionSuffix = GetDimensionSuffix(hasZ, hasM);

        switch (geometry.Type)
        {
            case GeometryType.Point:
                return FormattableString.Invariant($"POINT{dimensionSuffix}{ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.LineString:
                return FormattableString.Invariant($"LINESTRING{dimensionSuffix}{ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.Polygon:
                return FormattableString.Invariant($"POLYGON{dimensionSuffix}{ToWktPointArrayString(geometry, isRingBase: true)}");

            case GeometryType.MultiPoint:
                return FormattableString.Invariant($"MULTIPOINT{dimensionSuffix}{ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.MultiLineString:
                return FormattableString.Invariant($"MULTILINESTRING{dimensionSuffix}{ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.MultiPolygon:
                return FormattableString.Invariant($"MULTIPOLYGON{dimensionSuffix}{ToWktPointArrayString(geometry, isRingBase: true)}");

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"WktWriter > AsWkt: Geometry type '{geometry.Type}' is not implemented");
        }
    }

    private static string GetDimensionSuffix(bool hasZ, bool hasM)
    {
        if (hasZ && hasM)
            return "ZM";
        if (hasZ)
            return "Z";
        if (hasM)
            return "M";
        return "";
    }

    private static string ToWktPointArrayString<T>(Geometry<T> geometry, bool isRingBase) where T : IPoint, new()
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
                return GetWktPoint(geometry.Points[0]);

            case GeometryType.LineString:
                return GetWktLineString(geometry.Points, isRingBase);

            case GeometryType.Polygon:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return GetWktLineStringForGeometry(geometry, isRingBase);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException($"WktWriter > ToWktPointArrayString: Geometry type '{geometry.Type}' is not implemented");
        }
    }

    private static string GetWktPoint<T>(T point) where T : IPoint
    {
        bool hasZ = point is IHasZ;
        bool hasM = point is IHasM;

        if (hasZ && hasM)
        {
            var z = ((IHasZ)point).Z;
            var m = ((IHasM)point).M;
            return FormattableString.Invariant($"({point.X.ToInvariantString()} {point.Y.ToInvariantString()} {z.ToInvariantString()} {m.ToInvariantString()})");
        }
        else if (hasZ)
        {
            var z = ((IHasZ)point).Z;
            return FormattableString.Invariant($"({point.X.ToInvariantString()} {point.Y.ToInvariantString()} {z.ToInvariantString()})");
        }
        else if (hasM)
        {
            var m = ((IHasM)point).M;
            return FormattableString.Invariant($"({point.X.ToInvariantString()} {point.Y.ToInvariantString()} {m.ToInvariantString()})");
        }
        else
        {
            return FormattableString.Invariant($"({point.X.ToInvariantString()} {point.Y.ToInvariantString()})");
        }
    }

    // polygon, multi point, multi linestring, multipolygon
    private static string GetWktLineStringForGeometry<T>(Geometry<T> geometry, bool isRingBase) where T : IPoint, new()
    {
        var items = geometry.Geometries.Select(g => ToWktPointArrayString(g, isRingBase));

        StringBuilder result = new StringBuilder("(");

        foreach (var ring in items)
        {
            result.Append(FormattableString.Invariant($"{ring},"));
        }

        result.Remove(result.Length - 1, 1);

        result.Append(")");

        return result.ToString();
    }

    private static string GetWktLineString<T>(List<T> points, bool isRingBase) where T : IPoint, new()
    {
        if (points == null || points.Count == 0)
            return "()";

        StringBuilder builder = new StringBuilder("(");

        foreach (var point in points)
        {
            builder.Append(GetWktPointString(point));
            builder.Append(",");
        }

        if (isRingBase && points.Count > 0)
        {
            // Close the ring by adding the first point again
            builder.Append(GetWktPointString(points[0]));
            builder.Append(",");
        }

        if (builder.Length > 0 && builder[builder.Length - 1] == ',')
        {
            builder.Remove(builder.Length - 1, 1);
        }

        builder.Append(")");

        return builder.ToString();
    }

    private static string GetWktPointString<T>(T point) where T : IPoint
    {
        bool hasZ = point is IHasZ;
        bool hasM = point is IHasM;

        if (hasZ && hasM)
        {
            var z = ((IHasZ)point).Z;
            var m = ((IHasM)point).M;
            return FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()} {z.ToInvariantString()} {m.ToInvariantString()}");
        }
        else if (hasZ)
        {
            var z = ((IHasZ)point).Z;
            return FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()} {z.ToInvariantString()}");
        }
        else if (hasM)
        {
            var m = ((IHasM)point).M;
            return FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()} {m.ToInvariantString()}");
        }
        else
        {
            return FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()}");
        }
    }
}

