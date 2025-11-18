using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

public static class WktWriter
{
    public static string AsWkt<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        bool hasZ = geometry.HasZ();
        bool hasM = geometry.HasM();

        string dimensionSuffix = GetDimensionSuffix(hasZ, hasM);

        string suffixWithSpace = string.IsNullOrEmpty(dimensionSuffix) ? "" : $" {dimensionSuffix}";

        switch (geometry.Type)
        {
            case GeometryType.Point:
                return FormattableString.Invariant($"POINT{suffixWithSpace}{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.LineString:
                return FormattableString.Invariant($"LINESTRING{suffixWithSpace}{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.Polygon:
                return FormattableString.Invariant($"POLYGON{suffixWithSpace}{WktHelpers.ToWktPointArrayString(geometry, isRingBase: true)}");

            case GeometryType.MultiPoint:
                return FormattableString.Invariant($"MULTIPOINT{suffixWithSpace}{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.MultiLineString:
                return FormattableString.Invariant($"MULTILINESTRING{suffixWithSpace}{WktHelpers.ToWktPointArrayString(geometry, isRingBase: false)}");

            case GeometryType.MultiPolygon:
                return FormattableString.Invariant($"MULTIPOLYGON{suffixWithSpace}{WktHelpers.ToWktPointArrayString(geometry, isRingBase: true)}");

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
}

