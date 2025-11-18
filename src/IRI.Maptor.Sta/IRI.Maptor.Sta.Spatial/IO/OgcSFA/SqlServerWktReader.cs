using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

/// <summary>
/// Reads WKT strings in SQL Server format.
/// Key differences from OGC WKT:
/// - No dimension suffix (POINT(1 1 1) instead of POINT Z(1 1 1))
/// - Dimension inferred from coordinate count (2=2D, 3=3D/Z, 4=4D/ZM)
/// - MULTIPOINT supports both SQL Server format MULTIPOINT((1 2), (3 4)) and OGC format
/// </summary>
public static class SqlServerWktReader
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

        // Detect coordinate dimension from first coordinate set (not from type suffix)
        var dimension = DetectDimensionFromCoordinates(coordinates);
        var baseType = type; // No suffix to remove in SQL Server format

        switch (baseType)
        {
            case Point:
                return WktHelpers.ParsePoint(coordinates, srid, dimension, "SqlServerWktReader");

            case MultiPoint:
                return ParseMultiPoint(coordinates, srid, dimension);

            case LineString:
                return WktHelpers.ParseLineString(coordinates, srid, isRing: false, dimension, "SqlServerWktReader");

            case MultiLineString:
                return WktHelpers.ParseMultiLineString(coordinates, srid, dimension, "SqlServerWktReader");

            case Polygon:
                return WktHelpers.ParsePolygon(coordinates, srid, dimension, "SqlServerWktReader");

            case MultiPolygon:
                return WktHelpers.ParseMultiPolygon(coordinates, srid, dimension, "SqlServerWktReader");

            default:
                throw new NotImplementedException($"SqlServerWktReader > Parse: Unsupported geometry type '{baseType}'");
        }
    }

    /// <summary>
    /// Detects coordinate dimension by examining the first coordinate set.
    /// SQL Server format: 2 coords = 2D, 3 coords = 3D (assumed Z), 4 coords = 4D (ZM)
    /// </summary>
    private static CoordinateDimension DetectDimensionFromCoordinates(string coordinates)
    {
        string firstPoint = new string(coordinates.SkipWhile(c => c == '(')?.TakeWhile(c => c != ',')?.ToArray() ?? []);

        if (string.IsNullOrWhiteSpace(firstPoint))
            return CoordinateDimension.TwoD;
         
        var coordParts = firstPoint.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        return coordParts.Length switch
        {
            2 => CoordinateDimension.TwoD,
            3 => CoordinateDimension.Z, // SQL Server typically uses Z for 3D
            4 => CoordinateDimension.ZM,
            _ => CoordinateDimension.TwoD
        };
    }

    private static IGeometry ParseMultiPoint(string wktString, int srid, CoordinateDimension dimension)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => ParseMultiPoint<Point>(wktString, srid),
            CoordinateDimension.Z => ParseMultiPoint<PointZ>(wktString, srid),
            CoordinateDimension.M => ParseMultiPoint<PointM>(wktString, srid),
            CoordinateDimension.ZM => ParseMultiPoint<PointZM>(wktString, srid),
            _ => throw new NotImplementedException($"SqlServerWktReader > ParseMultiPoint: Unsupported dimension '{dimension}'")
        };
    }

    private static Geometry<T> ParseMultiPoint<T>(string wktString, int srid) where T : IPoint, new()
    {
        // SQL Server allows MULTIPOINT((1 2), (3 4)) without outer parentheses
        // OGC format MULTIPOINT(((1 2)), ((3 4))) is also supported
        var dimension = WktHelpers.GetDimensionFromType<T>();
        var items = WktHelpers.Process(wktString);

        List<List<double>> coordinates = new List<List<double>>();

        if (items.Count == 0)
        {
            // Try SQL Server format: MULTIPOINT((1 2), (3 4))
            // Clean parentheses and parse directly
            var cleanedString = wktString.Replace('(', ' ').Replace(')', ' ');
            coordinates = WktHelpers.GetCoordinates(cleanedString, isRing: false, dimension, "SqlServerWktReader");
        }
        else
        {
            // OGC format: MULTIPOINT(((1 2)), ((3 4)))
            // Find level 2 or 3 items (depending on format)
            var pointItems = items.Where(i => i.level >= 2).ToList();

            int expectedCoordCount = dimension switch
            {
                CoordinateDimension.TwoD => 2,
                CoordinateDimension.Z => 3,
                CoordinateDimension.M => 3,
                CoordinateDimension.ZM => 4,
                _ => 2
            };

            foreach (var item in pointItems)
            {
                var subString = wktString.Substring(item.start, item.end - item.start);
                var pointCoords = WktHelpers.ParseCoordinates(subString);
                if (pointCoords.Count > 0)
                {
                    if (pointCoords[0].Count != expectedCoordCount)
                    {
                        throw new ArgumentException($"SqlServerWktReader > ParseMultiPoint: Expected {expectedCoordCount} coordinate values for dimension {dimension}, but found {pointCoords[0].Count} values.");
                    }
                    coordinates.Add(pointCoords[0]);
                }
            }
        }

        if (coordinates.IsNullOrEmpty())
            return WktHelpers.CreateEmptyGeometry<T>(GeometryType.MultiPoint, srid);

        return WktHelpers.CreateTypedMultiPointGeometry<T>(coordinates, srid);
    }

}

