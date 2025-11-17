using System.Text;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
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

    private enum CoordinateDimension
    {
        TwoD,  // X Y
        Z,     // X Y Z
        M,     // X Y M
        ZM     // X Y Z M
    }

    public static Geometry<Point> Parse(string wktString, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(wktString))
            return Geometry<Point>.Empty;

        var typeChars = wktString.TakeWhile(c => c != '(')?.ToArray();

        if (typeChars.IsNullOrEmpty())
            return Geometry<Point>.Empty;

        var type = new string(typeChars).Trim().ToUpper();
        var coordinates = wktString.Substring(typeChars.Length, wktString.Length - typeChars.Length);

        // Detect coordinate dimension from type suffix
        var dimension = DetectDimension(type);
        var baseType = RemoveDimensionSuffix(type);

        switch (baseType)
        {
            case Point:
                return ParsePoint(coordinates, srid, isRing: false, dimension);

            case MultiPoint:
                return ParseMultiPoint(coordinates, srid, isRing: false, dimension);

            case LineString:
                return ParseLineString(coordinates, srid, isRing: false, dimension);

            case MultiLineString:
                return ParseMultiLineString(coordinates, srid, dimension);

            case Polygon:
                return ParsePolygon(coordinates, srid, dimension);

            case MultiPolygon:
                return ParseMultiPolygon(coordinates, srid, dimension);

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
        if (type.EndsWith("ZM", StringComparison.OrdinalIgnoreCase))
            return type.Substring(0, type.Length - 2);
        if (type.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || type.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            return type.Substring(0, type.Length - 1);
        return type;
    }

    private static Geometry<Point> ParsePoint(string wktString, int srid, bool isRing, CoordinateDimension dimension)
    {
        var points = GetPoints(wktString, isRing, dimension);
        return Geometry<Point>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<Point> ParseMultiPoint(string wktString, int srid, bool isRing, CoordinateDimension dimension)
    {
        var cleanedString = wktString.Replace('(', ' ').Replace(')', ' ');
        var points = GetPoints(cleanedString, isRing, dimension);

        if (points.IsNullOrEmpty())
            return Geometry<Point>.Empty;

        return new Geometry<Point>(points.Select(p => Geometry<Point>.Create(p.X, p.Y, srid)).ToList(), GeometryType.MultiPoint, srid);
    }

    private static Geometry<Point> ParseLineString(string wktString, int srid, bool isRing, CoordinateDimension dimension)
    {
        var points = GetPoints(wktString, isRing, dimension);
        return Geometry<Point>.CreatePointOrLineString(points, srid);
    }

    private static Geometry<Point> ParseMultiLineString(string wktString, int srid, CoordinateDimension dimension)
    {
        var items = Process(wktString);
        List<Geometry<Point>> lineStrings = new List<Geometry<Point>>();

        foreach (var item in items.Where(i => i.level == 2))
        {
            var subString = wktString.Substring(item.start, item.end - item.start);
            lineStrings.Add(ParseLineString(subString, srid, isRing: false, dimension));
        }

        return new Geometry<Point>(lineStrings, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> ParsePolygon(string wktString, int srid, CoordinateDimension dimension)
    {
        var items = Process(wktString);
        List<Geometry<Point>> rings = new List<Geometry<Point>>();

        foreach (var item in items.Where(i => i.level == 2))
        {
            var subString = wktString.Substring(item.start, item.end - item.start);
            rings.Add(ParseLineString(subString, srid, isRing: true, dimension));
        }

        return new Geometry<Point>(rings, GeometryType.Polygon, srid);
    }

    private static Geometry<Point> ParseMultiPolygon(string wktString, int srid, CoordinateDimension dimension)
    {
        var items = Process(wktString);
        List<Geometry<Point>> polygons = new List<Geometry<Point>>();

        foreach (var item in items.Where(i => i.level == 2))
        {
            List<Geometry<Point>> rings = new List<Geometry<Point>>();

            foreach (var ring in items.Where(i => i.level == 3 && i.end < item.end && i.start > item.start))
            {
                var subString = wktString.Substring(ring.start, ring.end - ring.start);
                rings.Add(ParseLineString(subString, srid, isRing: true, dimension));
            }

            polygons.Add(new Geometry<Point>(rings, GeometryType.Polygon, srid));
        }

        return new Geometry<Point>(polygons, GeometryType.MultiPolygon, srid);
    }

    private static List<Point> GetPoints(string pointArray, bool isRing, CoordinateDimension dimension)
    {
        var cleanedPointArray = pointArray?.Trim(' ', ')', '(');

        if (string.IsNullOrEmpty(cleanedPointArray))
            return new List<Point>();

        var points = cleanedPointArray
            .Split(',')
            .Select(p => p.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(i => double.Parse(i))
                            .ToList());

        // The last point is repeated for close rings
        var pointList = isRing ? points.Take(points.Count() - 1).ToList() : points.ToList();

        return pointList.Select(p => CreatePoint(p, dimension)).ToList();
    }

    private static Point CreatePoint(List<double> coordinates, CoordinateDimension dimension)
    {
        return dimension switch
        {
            CoordinateDimension.TwoD => new Point(coordinates[0], coordinates[1]),
            CoordinateDimension.Z => new PointZ { X = coordinates[0], Y = coordinates[1], Z = coordinates[2] },
            CoordinateDimension.M => new PointM { X = coordinates[0], Y = coordinates[1], M = coordinates[2] },
            // Note: PointZM doesn't extend Point, so we use PointZ and preserve Z (M is lost)
            // For full ZM support, consider using Geometry<PointZM> overloads
            CoordinateDimension.ZM => new PointZ { X = coordinates[0], Y = coordinates[1], Z = coordinates[2] },
            _ => throw new NotImplementedException($"WktReader > CreatePoint: Unsupported dimension '{dimension}'")
        };
    }

    // 1400.03.21
    private static List<(int level, int start, int end)> Process(string wktString)
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
}

