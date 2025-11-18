using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

/// <summary>
/// Shared helper methods for WKT parsing and writing.
/// Used by both OGC and SQL Server WKT implementations.
/// </summary>
internal static class WktHelpers
{
    #region Common Utilities

    /// <summary>
    /// Parses parentheses structure in WKT string to identify nested geometry levels.
    /// Returns list of (level, startIndex, endIndex) tuples.
    /// </summary>
    public static List<(int level, int start, int end)> Process(string wktString)
    {
        int currentLevel = 0;
        List<(int level, int start, int end)> result = new List<(int level, int start, int end)>();
        Stack<int> startIndex = new Stack<int>();

        for (int i = 0; i < wktString.Length; i++)
        {
            if (wktString[i] == '(')
            {
                startIndex.Push(i);
                currentLevel++;
            }

            if (wktString[i] == ')')
            {
                result.Add((currentLevel, startIndex.Pop(), i));
                currentLevel--;
            }
        }

        return result;
    }

    /// <summary>
    /// Parses coordinate string into a list of coordinate arrays.
    /// </summary>
    public static List<List<double>> ParseCoordinates(string pointArray)
    {
        var cleanedPointArray = pointArray?.Trim(' ', ')', '(');

        if (string.IsNullOrEmpty(cleanedPointArray))
            return new List<List<double>>();

        return cleanedPointArray
            .Split(',')
            .Select(p => p.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(i => double.Parse(i))
                            .ToList())
            .ToList();
    }

    /// <summary>
    /// Parses coordinates from a string and validates them against the expected dimension.
    /// </summary>
    public static List<List<double>> GetCoordinates(string pointArray, bool isRing, CoordinateDimension dimension, string callerName)
    {
        var coordinates = ParseCoordinates(pointArray);

        // Validate coordinate count matches expected dimension
        int expectedCoordCount = dimension switch
        {
            CoordinateDimension.TwoD => 2,
            CoordinateDimension.Z => 3,
            CoordinateDimension.M => 3,
            CoordinateDimension.ZM => 4,
            _ => 2
        };
         
        // The last point is repeated for close rings
        return isRing ? coordinates.Take(coordinates.Count - 1).ToList() : coordinates;
    }

    #endregion

    #region Point Creation

    /// <summary>
    /// Creates a List<Point> directly from coordinate arrays.
    /// </summary>
    private static List<Point> CreatePointList(List<List<double>> coordinates)
    {
        return coordinates.Select(c => new Point(c[0], c[1])).ToList();
    }

    /// <summary>
    /// Creates a List<PointZ> directly from coordinate arrays.
    /// </summary>
    private static List<PointZ> CreatePointZList(List<List<double>> coordinates)
    {
        return coordinates.Select(c => new PointZ { X = c[0], Y = c[1], Z = c[2] }).ToList();
    }

    /// <summary>
    /// Creates a List<PointM> directly from coordinate arrays.
    /// </summary>
    private static List<PointM> CreatePointMList(List<List<double>> coordinates)
    {
        return coordinates.Select(c => new PointM { X = c[0], Y = c[1], M = c[2] }).ToList();
    }

    /// <summary>
    /// Creates a List<PointZM> directly from coordinate arrays.
    /// </summary>
    private static List<PointZM> CreatePointZMList(List<List<double>> coordinates)
    {
        return coordinates.Select(c => new PointZM { X = c[0], Y = c[1], Z = c[2], M = c[3] }).ToList();
    }

    /// <summary>
    /// Gets the coordinate dimension from a point type.
    /// </summary>
    public static CoordinateDimension GetDimensionFromType<T>() where T : IPoint
    {
        if (typeof(T) == typeof(PointZM))
            return CoordinateDimension.ZM;
        if (typeof(T) == typeof(PointZ))
            return CoordinateDimension.Z;
        if (typeof(T) == typeof(PointM))
            return CoordinateDimension.M;
        return CoordinateDimension.TwoD;
    }

    /// <summary>
    /// Creates a typed list of points from coordinate arrays.
    /// </summary>
    private static List<T> CreatePointList<T>(List<List<double>> coordinates) where T : IPoint, new()
    {
        if (typeof(T) == typeof(PointZM))
            return CreatePointZMList(coordinates).Cast<T>().ToList();
        if (typeof(T) == typeof(PointZ))
            return CreatePointZList(coordinates).Cast<T>().ToList();
        if (typeof(T) == typeof(PointM))
            return CreatePointMList(coordinates).Cast<T>().ToList();
        return CreatePointList(coordinates).Cast<T>().ToList();
    }

    /// <summary>
    /// Creates a point instance from a coordinate array based on point type.
    /// </summary>
    private static T CreatePointFromCoordinates<T>(List<double> c) where T : IPoint, new()
    {
        if (typeof(T) == typeof(PointZM))
        {
            var pointZM = new PointZM { X = c[0], Y = c[1], Z = c[2], M = c[3] };
            return (T)(IPoint)pointZM;
        }
        if (typeof(T) == typeof(PointZ))
        {
            var pointZ = new PointZ { X = c[0], Y = c[1], Z = c[2] };
            return (T)(IPoint)pointZ;
        }
        if (typeof(T) == typeof(PointM))
        {
            var pointM = new PointM { X = c[0], Y = c[1], M = c[2] };
            return (T)(IPoint)pointM;
        }
        var point = new Point(c[0], c[1]);
        return (T)(IPoint)point;
    }

    #endregion

    #region Geometry Creation

    /// <summary>
    /// Creates a typed geometry from coordinate arrays.
    /// </summary>
    private static Geometry<T> CreateTypedGeometry<T>(List<List<double>> coordinates, GeometryType geometryType, int srid) where T : IPoint, new()
    {
        var points = CreatePointList<T>(coordinates);
        return Geometry<T>.CreatePointOrLineString(points, srid);
    }

    /// <summary>
    /// Creates a MultiPoint geometry from coordinate arrays.
    /// </summary>
    public static Geometry<T> CreateTypedMultiPointGeometry<T>(List<List<double>> coordinates, int srid) where T : IPoint, new()
    {
        var pointGeometries = coordinates.Select(c =>
        {
            var point = CreatePointFromCoordinates<T>(c);
            return new Geometry<T>(new List<T> { point }, GeometryType.Point, srid);
        }).ToList();

        return new Geometry<T>(pointGeometries, GeometryType.MultiPoint, srid);
    }

    /// <summary>
    /// Creates a multi-geometry (Polygon, MultiLineString, MultiPolygon) from a list of geometries.
    /// </summary>
    private static Geometry<T> CreateTypedMultiGeometry<T>(List<Geometry<T>> geometries, GeometryType geometryType, int srid) where T : IPoint, new()
    {
        if (geometries.IsNullOrEmpty())
            return CreateEmptyGeometry<T>(geometryType, srid);

        return new Geometry<T>(geometries, geometryType, srid);
    }

    /// <summary>
    /// Creates an empty geometry of the specified type.
    /// </summary>
    public static Geometry<T> CreateEmptyGeometry<T>(GeometryType geometryType, int srid) where T : IPoint, new()
    {
        return Geometry<T>.CreateEmpty(geometryType, srid);
    }

    #endregion

    #region Point Parsing

    /// <summary>
    /// Parses a Point geometry from a WKT string.
    /// </summary>
    private static Geometry<T> ParsePoint<T>(string wktString, int srid, CoordinateDimension dimension, string callerName) where T : IPoint, new()
    {
        var coordinates = ParseCoordinates(wktString);
        
        // Validate coordinate count matches expected dimension
        int expectedCoordCount = dimension switch
        {
            CoordinateDimension.TwoD => 2,
            CoordinateDimension.Z => 3,
            CoordinateDimension.M => 3,
            CoordinateDimension.ZM => 4,
            _ => 2
        };

        // Validate all coordinates have the correct number of values
        foreach (var coord in coordinates)
        {
            if (coord.Count != expectedCoordCount)
            {
                throw new ArgumentException($"{callerName} > ParsePoint: Expected {expectedCoordCount} coordinate values for dimension {dimension}, but found {coord.Count} values.");
            }
        }

        return CreateTypedGeometry<T>(coordinates, GeometryType.Point, srid);
    }

    /// <summary>
    /// Parses a Point geometry from a WKT string, switching on dimension.
    /// </summary>
    public static IGeometry ParsePoint(string wktString, int srid, CoordinateDimension dimension, string callerName)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => ParsePoint<Point>(wktString, srid, dimension, callerName),
            CoordinateDimension.Z => ParsePoint<PointZ>(wktString, srid, dimension, callerName),
            CoordinateDimension.M => ParsePoint<PointM>(wktString, srid, dimension, callerName),
            CoordinateDimension.ZM => ParsePoint<PointZM>(wktString, srid, dimension, callerName),
            _ => throw new NotImplementedException($"{callerName} > ParsePoint: Unsupported dimension '{dimension}'")
        };
    }

    #endregion

    #region LineString Parsing

    /// <summary>
    /// Parses a LineString geometry from a WKT string.
    /// </summary>
    private static Geometry<T> ParseLineString<T>(string wktString, int srid, bool isRing, string callerName) where T : IPoint, new()
    {
        var dimension = GetDimensionFromType<T>();
        var coordinates = GetCoordinates(wktString, isRing, dimension, callerName);
        return CreateTypedGeometry<T>(coordinates, GeometryType.LineString, srid);
    }

    /// <summary>
    /// Parses a LineString geometry from a WKT string, switching on dimension.
    /// </summary>
    public static IGeometry ParseLineString(string wktString, int srid, bool isRing, CoordinateDimension dimension, string callerName)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => ParseLineString<Point>(wktString, srid, isRing, callerName),
            CoordinateDimension.Z => ParseLineString<PointZ>(wktString, srid, isRing, callerName),
            CoordinateDimension.M => ParseLineString<PointM>(wktString, srid, isRing, callerName),
            CoordinateDimension.ZM => ParseLineString<PointZM>(wktString, srid, isRing, callerName),
            _ => throw new NotImplementedException($"{callerName} > ParseLineString: Unsupported dimension '{dimension}'")
        };
    }

    #endregion

    #region MultiLineString Parsing

    /// <summary>
    /// Parses a MultiLineString geometry from a WKT string.
    /// </summary>
    private static Geometry<T> ParseMultiLineString<T>(string wktString, int srid, string callerName) where T : IPoint, new()
    {
        var items = Process(wktString);
        var lineStrings = new List<Geometry<T>>();

        foreach (var item in items.Where(i => i.level == 2))
        {
            var subString = wktString.Substring(item.start, item.end - item.start);
            lineStrings.Add(ParseLineString<T>(subString, srid, isRing: false, callerName));
        }

        return CreateTypedMultiGeometry(lineStrings, GeometryType.MultiLineString, srid);
    }

    /// <summary>
    /// Parses a MultiLineString geometry from a WKT string, switching on dimension.
    /// </summary>
    public static IGeometry ParseMultiLineString(string wktString, int srid, CoordinateDimension dimension, string callerName)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => ParseMultiLineString<Point>(wktString, srid, callerName),
            CoordinateDimension.Z => ParseMultiLineString<PointZ>(wktString, srid, callerName),
            CoordinateDimension.M => ParseMultiLineString<PointM>(wktString, srid, callerName),
            CoordinateDimension.ZM => ParseMultiLineString<PointZM>(wktString, srid, callerName),
            _ => throw new NotImplementedException($"{callerName} > ParseMultiLineString: Unsupported dimension '{dimension}'")
        };
    }

    #endregion

    #region Polygon Parsing

    /// <summary>
    /// Parses a Polygon geometry from a WKT string.
    /// </summary>
    private static Geometry<T> ParsePolygon<T>(string wktString, int srid, string callerName) where T : IPoint, new()
    {
        var items = Process(wktString);
        var rings = new List<Geometry<T>>();

        foreach (var item in items.Where(i => i.level == 2))
        {
            var subString = wktString.Substring(item.start, item.end - item.start);
            rings.Add(ParseLineString<T>(subString, srid, isRing: true, callerName));
        }

        return CreateTypedMultiGeometry(rings, GeometryType.Polygon, srid);
    }

    /// <summary>
    /// Parses a Polygon geometry from a WKT string, switching on dimension.
    /// </summary>
    public static IGeometry ParsePolygon(string wktString, int srid, CoordinateDimension dimension, string callerName)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => ParsePolygon<Point>(wktString, srid, callerName),
            CoordinateDimension.Z => ParsePolygon<PointZ>(wktString, srid, callerName),
            CoordinateDimension.M => ParsePolygon<PointM>(wktString, srid, callerName),
            CoordinateDimension.ZM => ParsePolygon<PointZM>(wktString, srid, callerName),
            _ => throw new NotImplementedException($"{callerName} > ParsePolygon: Unsupported dimension '{dimension}'")
        };
    }

    #endregion

    #region MultiPolygon Parsing

    /// <summary>
    /// Parses a MultiPolygon geometry from a WKT string.
    /// </summary>
    private static Geometry<T> ParseMultiPolygon<T>(string wktString, int srid, string callerName) where T : IPoint, new()
    {
        var items = Process(wktString);
        var polygons = new List<Geometry<T>>();

        foreach (var item in items.Where(i => i.level == 2))
        {
            var rings = new List<Geometry<T>>();

            foreach (var ring in items.Where(i => i.level == 3 && i.end < item.end && i.start > item.start))
            {
                var subString = wktString.Substring(ring.start, ring.end - ring.start);
                rings.Add(ParseLineString<T>(subString, srid, isRing: true, callerName));
            }

            polygons.Add(CreateTypedMultiGeometry(rings, GeometryType.Polygon, srid));
        }

        return CreateTypedMultiGeometry(polygons, GeometryType.MultiPolygon, srid);
    }

    /// <summary>
    /// Parses a MultiPolygon geometry from a WKT string, switching on dimension.
    /// </summary>
    public static IGeometry ParseMultiPolygon(string wktString, int srid, CoordinateDimension dimension, string callerName)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => ParseMultiPolygon<Point>(wktString, srid, callerName),
            CoordinateDimension.Z => ParseMultiPolygon<PointZ>(wktString, srid, callerName),
            CoordinateDimension.M => ParseMultiPolygon<PointM>(wktString, srid, callerName),
            CoordinateDimension.ZM => ParseMultiPolygon<PointZM>(wktString, srid, callerName),
            _ => throw new NotImplementedException($"{callerName} > ParseMultiPolygon: Unsupported dimension '{dimension}'")
        };
    }

    #endregion

    #region WKT Writing Helpers

    /// <summary>
    /// Formats a point as a WKT coordinate string.
    /// </summary>
    internal static string FormatWktPoint<T>(T point, bool includeParentheses) where T : IPoint
    {
        string coordinates;
        
        // Use pattern matching to efficiently check and cast in one operation
        if (point is IHasZ hasZPoint && point is IHasM hasMPoint)
        {
            // Point has both Z and M
            coordinates = FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()} {hasZPoint.Z.ToInvariantString()} {hasMPoint.M.ToInvariantString()}");
        }
        else if (point is IHasZ hasZPointOnly)
        {
            // Point has only Z
            coordinates = FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()} {hasZPointOnly.Z.ToInvariantString()}");
        }
        else if (point is IHasM hasMPointOnly)
        {
            // Point has only M
            coordinates = FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()} {hasMPointOnly.M.ToInvariantString()}");
        }
        else
        {
            // Point has neither Z nor M
            coordinates = FormattableString.Invariant($"{point.X.ToInvariantString()} {point.Y.ToInvariantString()}");
        }

        return includeParentheses ? $"({coordinates})" : coordinates;
    }

    /// <summary>
    /// Formats a list of points as a WKT linestring.
    /// </summary>
    internal static string GetWktLineString<T>(List<T> points, bool isRingBase) where T : IPoint, new()
    {
        if (points == null || points.Count == 0)
            return "()";

        StringBuilder builder = new StringBuilder("(");

        foreach (var point in points)
        {
            builder.Append(FormatWktPoint(point, includeParentheses: false));
            builder.Append(", ");
        }

        if (isRingBase && points.Count > 0)
        {
            // Close the ring by adding the first point again
            builder.Append(FormatWktPoint(points[0], includeParentheses: false));
            builder.Append(", ");
        }

        if (builder.Length > 0 && builder.Length >= 2 && builder[builder.Length - 2] == ',' && builder[builder.Length - 1] == ' ')
        {
            builder.Remove(builder.Length - 2, 2);
        }

        builder.Append(")");

        return builder.ToString();
    }

    /// <summary>
    /// Formats a geometry's point array as a WKT string (for polygon, multi point, multi linestring, multipolygon).
    /// </summary>
    internal static string GetWktLineStringForGeometry<T>(Geometry<T> geometry, bool isRingBase) where T : IPoint, new()
    {
        var items = geometry.Geometries.Select(g => ToWktPointArrayString(g, isRingBase));

        StringBuilder result = new StringBuilder("(");

        foreach (var ring in items)
        {
            result.Append(FormattableString.Invariant($"{ring}, "));
        }

        if (result.Length > 0 && result.Length >= 2 && result[result.Length - 2] == ',' && result[result.Length - 1] == ' ')
        {
            result.Remove(result.Length - 2, 2);
        }

        result.Append(")");

        return result.ToString();
    }

    /// <summary>
    /// Converts a geometry to a WKT point array string.
    /// </summary>
    internal static string ToWktPointArrayString<T>(Geometry<T> geometry, bool isRingBase) where T : IPoint, new()
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
                return FormatWktPoint(geometry.Points[0], includeParentheses: true);

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
                throw new NotImplementedException($"WktHelpers > ToWktPointArrayString: Geometry type '{geometry.Type}' is not implemented");
        }
    }

    #endregion
}
