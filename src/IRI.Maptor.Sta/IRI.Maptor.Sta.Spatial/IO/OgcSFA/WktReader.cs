using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

public static class WktReader
{
    const string Point = "POINT";
    const string MultiPoint = "MULTIPOINT";
    const string LineString = "LINESTRING";
    const string MultiLineString = "MULTILINESTRING";
    const string Polygon = "POLYGON";
    const string MultiPolygon = "MULTIPOLYGON";

    public static IGeometry Parse(string wktString, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(wktString))
            return Geometry<Point>.Empty;

        var typeChars = wktString.TakeWhile(c => c != '(').ToArray();

        if (typeChars == null || typeChars.Length == 0)
            return Geometry<Point>.Empty;

        var type = new string(typeChars).Trim().ToUpper();
        var coordinates = wktString.Substring(typeChars.Length, wktString.Length - typeChars.Length);

        // Detect coordinate dimension from type suffix
        var dimension = DetectDimension(type);
        var baseType = RemoveDimensionSuffix(type);

        switch (baseType)
        {
            case Point:
                return WktHelpers.ParsePoint(coordinates, srid, dimension, "WktReader");

            case MultiPoint:
                return ParseMultiPoint(coordinates, srid, dimension);

            case LineString:
                return WktHelpers.ParseLineString(coordinates, srid, isRing: false, dimension, "WktReader");

            case MultiLineString:
                return WktHelpers.ParseMultiLineString(coordinates, srid, dimension, "WktReader");

            case Polygon:
                return WktHelpers.ParsePolygon(coordinates, srid, dimension, "WktReader");

            case MultiPolygon:
                return WktHelpers.ParseMultiPolygon(coordinates, srid, dimension, "WktReader");

            default:
                throw new NotImplementedException($"WktReader > Parse: Unsupported geometry type '{baseType}'");
        }
    }

    private static CoordinateDimension DetectDimension(string type)
    {
        if (type.EndsWith("ZM", StringComparison.OrdinalIgnoreCase))
            return CoordinateDimension.ZM;
        if (type.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            return CoordinateDimension.Z;
        if (type.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            return CoordinateDimension.M;
        return CoordinateDimension.TwoD;
    }

    private static string RemoveDimensionSuffix(string type)
    {
        var result = type;

        if (type.EndsWith("ZM", StringComparison.OrdinalIgnoreCase))
            result = type.Substring(0, type.Length - 2);

        else if (type.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || type.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            result = type.Substring(0, type.Length - 1);

        return result.Trim().ToUpper();
    }

    private static IGeometry ParseMultiPoint(string wktString, int srid, CoordinateDimension dimension)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => ParseMultiPoint<Point>(wktString, srid),
            CoordinateDimension.Z => ParseMultiPoint<PointZ>(wktString, srid),
            CoordinateDimension.M => ParseMultiPoint<PointM>(wktString, srid),
            CoordinateDimension.ZM => ParseMultiPoint<PointZM>(wktString, srid),
            _ => throw new NotImplementedException($"WktReader > ParseMultiPoint: Unsupported dimension '{dimension}'")
        };
    }

    private static Geometry<T> ParseMultiPoint<T>(string wktString, int srid) where T : IPoint, new()
    {
        var dimension = WktHelpers.GetDimensionFromType<T>();
        var cleanedString = wktString.Replace('(', ' ').Replace(')', ' ');
        var coordinates = WktHelpers.GetCoordinates(cleanedString, isRing: false, dimension, "WktReader");

        if (coordinates.IsNullOrEmpty())
            return WktHelpers.CreateEmptyGeometry<T>(GeometryType.MultiPoint, srid);

        return WktHelpers.CreateTypedMultiPointGeometry<T>(coordinates, srid);
    }

}

