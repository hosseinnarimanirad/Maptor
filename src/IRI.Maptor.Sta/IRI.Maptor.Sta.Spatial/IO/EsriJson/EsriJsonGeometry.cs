using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.EsriJson;

public class EsriJsonGeometry
{
    private EsriJsonGeometryType? _geometryType;

    [JsonIgnore]
    public EsriJsonGeometryType Type
    {
        get => _geometryType ?? GetGeometryType();
        set => _geometryType = value;
    }

    public double? X { get; set; }

    public double? Y { get; set; }

    public double? Z { get; set; }

    public double? M { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasM { get; set; } = false;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasZ { get; set; } = false;

    //multipoint
    public double?[][] Points { get; set; }

    //polygon
    // exterior ring is CW, interior rings are CCW
    // last point is repeated
    public double?[][][] Rings { get; set; }

    //polyline or multipolyline
    public double?[][][] Paths { get; set; }

    public EsriJsonSpatialReference SpatialReference { get; set; }

    public EsriJsonGeometryType GetGeometryType()
    {
        if (X.HasValue)
        {
            return EsriJsonGeometryType.esriGeometryPoint;
        }
        else if (Points != null)
        {
            return EsriJsonGeometryType.esriGeometryMultipoint;
        }
        else if (Rings != null)
        {
            return EsriJsonGeometryType.esriGeometryPolygon;
        }
        else if (Paths != null)
        {
            return EsriJsonGeometryType.esriGeometryPolyline;
        }
        else
        {
            throw new NotImplementedException("EsriJsonGeometry > GetGeometryType");
        }

    }

    public static EsriJsonGeometry? Parse(string esriGeometryJsonString)
    {
        try
        {
            var result = JsonHelper.Deserialize<EsriJsonGeometry>(esriGeometryJsonString);

            return result;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public override string ToString()
    {
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        return JsonHelper.Serialize(this, options);
    }

    public string AsWkt()
    {
        switch (Type)
        {
            case EsriJsonGeometryType.esriGeometryPoint:
                return EsriJsonHelper.PointToWkt(this);

            case EsriJsonGeometryType.esriGeometryMultipoint:
                return EsriJsonHelper.MultiPointToWkt(this);

            case EsriJsonGeometryType.esriGeometryPolyline:
                return EsriJsonHelper.PolylineToWkt(this);

            case EsriJsonGeometryType.esriGeometryPolygon:
                return EsriJsonHelper.PolygonToWkt(this);

            default:
                throw new NotImplementedException();
        }
    }

    //private string PointToWkt()
    //{
    //    var xValue = X.ToStringOrNull(false);

    //    var yValue = Y.ToStringOrNull(false);

    //    if (string.IsNullOrEmpty(xValue) || string.IsNullOrEmpty(yValue))
    //    {
    //        return "POINT EMPTY";
    //    }

    //    var mValue = M.ToStringOrNull(false);
    //    var zValue = Z.ToStringOrNull(mValue.Length > 0);

    //    if (string.IsNullOrEmpty(zValue) && string.IsNullOrEmpty(mValue))
    //    {
    //        return FormattableString.Invariant($"POINT({xValue.ToString(CultureInfo.InvariantCulture)} {yValue})");
    //    }
    //    else
    //    {
    //        return FormattableString.Invariant($"POINT({xValue} {yValue} {zValue} {mValue})");
    //    }

    //}

    //private string MultiPointToWkt()
    //{
    //    if (!(Points?.Length > 0))
    //    {
    //        return "MULTIPOINT EMPTY";
    //    }

    //    return FormattableString.Invariant($"MULTIPOINT{EsriJsonHelper.PointArrayToString(Points)}");
    //}

    //private string PolylineToWkt()
    //{
    //    if (!(Paths?.Length > 0))
    //    {
    //        return "LINESTRING EMPTY";
    //    }
    //    else if (Paths.Length == 1)
    //    {
    //        return FormattableString.Invariant($"LINESTRING{EsriJsonHelper.PointArrayToString(Paths[0])}");
    //    }
    //    else
    //    {
    //        return FormattableString.Invariant($"MULTILINESTRING({string.Join(", ", Paths.Select(i => EsriJsonHelper.PointArrayToString(i)))})");
    //    }

    //}

    //private string PolygonToWkt()
    //{
    //    if (!(Rings?.Length > 0))
    //    {
    //        return "POLYGON EMPTY";
    //    }
    //    else if (Rings.Length == 1)
    //    {
    //        return FormattableString.Invariant($"POLYGON({EsriJsonHelper.PointArrayToString(Rings[0])})");
    //    }
    //    else
    //    {
    //        return FormattableString.Invariant($"MULTIPOLYGON({string.Join(", ", Rings.Select(i => $"({EsriJsonHelper.PointArrayToString(i)})"))})");
    //    }

    //}

    public static EsriJsonGeometry CreateEmpty(EsriJsonGeometryType type) => new EsriJsonGeometry() { Type = type, };


    #region Convert to Geometry<T>

    public IGeometry Parse(int srid)
    {
        var geomType = GetGeometryType();
        if (HasM)
            return ParseInternal<PointZM>(srid, new PointZMFactory());
        if (HasZ)
            return ParseInternal<PointZ>(srid, new PointZFactory());
        return ParseInternal<Point>(srid, new PointFactory());
    }

    private IGeometry ParseInternal<T>(int srid, IPointFactory<T> factory) where T : IPoint, new()
    {
        return GetGeometryType() switch
        {
            EsriJsonGeometryType.esriGeometryPoint => ConvertToPoint<T>(srid, factory),
            EsriJsonGeometryType.esriGeometryMultipoint => ConvertToMultiPoint<T>(srid, factory),
            EsriJsonGeometryType.esriGeometryPolyline => ConvertToPolyline<T>(srid, factory),
            EsriJsonGeometryType.esriGeometryPolygon => ConvertToPolygon<T>(srid, factory),
            _ => throw new NotSupportedException($"Unsupported geometry type: {GetGeometryType()}")
        };
    }

    #region Point and MultiPoint

    private Geometry<T> ConvertToPoint<T>(int srid, IPointFactory<T> factory) where T : IPoint, new()
    {
        if (!X.HasValue || !Y.HasValue)
            return Geometry<T>.CreateEmpty(GeometryType.Point, srid);

        double[] coords = ExtractCoordinates(X.Value, Y.Value, Z, M);

        var point = Point.Parse<T>(coords, isLongitudeFirst: true); // Esri uses lon-lat order

        return Geometry<T>.Create(new List<T>() { point }, GeometryType.Point, srid);
    }

    private Geometry<T> ConvertToMultiPoint<T>(int srid, IPointFactory<T> factory) where T : IPoint, new()
    {
        if (Points == null || Points.Length == 0)
            return Geometry<T>.CreateEmpty(GeometryType.MultiPoint, srid);
        var points = new List<T>();
        foreach (var coord in Points)
        {
            if (coord != null && coord.Length >= 2 && coord[0].HasValue && coord[1].HasValue)
            {
                double[] coords = ExtractCoordinates(coord[0].Value, coord[1].Value,
                    coord.Length > 2 ? coord[2] : null,
                    coord.Length > 3 ? coord[3] : null);
                points.Add(Point.Parse<T>(coords, isLongitudeFirst: true));
            }
        }
        return Geometry<T>.Create(points, GeometryType.MultiPoint, srid);
    }

    #endregion

    #region Polyline

    private Geometry<T> ConvertToPolyline<T>(int srid, IPointFactory<T> factory) where T : IPoint, new()
    {
        if (Paths == null || Paths.Length == 0)
            return Geometry<T>.CreateEmpty(GeometryType.LineString, srid);

        var lineStrings = new List<Geometry<T>>();
        foreach (var path in Paths)
        {
            var points = ExtractPointsFromPath<T>(path);
            if (points.Count >= 2)
                lineStrings.Add(Geometry<T>.Create(points, GeometryType.LineString, srid));
        }

        if (lineStrings.Count == 0)
            return Geometry<T>.CreateEmpty(GeometryType.LineString, srid);
        if (lineStrings.Count == 1)
            return lineStrings[0];
        return Geometry<T>.Create(lineStrings, GeometryType.MultiLineString, srid);
    }

    #endregion

    #region Polygon (using Geometry<T>.CreatePolygonOrMultiPolygon)

    private Geometry<T> ConvertToPolygon<T>(int srid, IPointFactory<T> factory) where T : IPoint, new()
    {
        if (Rings == null || Rings.Length == 0)
            return Geometry<T>.CreateEmpty(GeometryType.Polygon, srid);

        // Convert each Esri ring to a Geometry<T> (LineString) using CreatePolygonRing,
        // which removes the duplicate closing point and prepares a closed ring.
        var rings = new List<Geometry<T>>();
        foreach (var ringCoords in Rings)
        {
            var points = ExtractPointsFromPath<T>(ringCoords);
            if (points.Count < 3) continue;
            // CreatePolygonRing returns a LineString that represents a closed ring (first != last)
            var ring = Geometry<T>.CreatePolygonRing(points, srid);
            rings.Add(ring);
        }

        if (rings.Count == 0)
            return Geometry<T>.CreateEmpty(GeometryType.Polygon, srid);

        // Use the existing method that groups rings by area and ensures correct orientation
        return Geometry<T>.CreatePolygonOrMultiPolygon(rings, srid);
    }

    #endregion

    #region Helpers

    private static double[] ExtractCoordinates(double x, double y, double? z, double? m)
    {
        int len = 2;
        if (z.HasValue) len = 3;
        if (m.HasValue) len = 4;
        var result = new double[len];
        result[0] = x;
        result[1] = y;
        if (len >= 3) result[2] = z.Value;
        if (len >= 4) result[3] = m.Value;
        return result;
    }

    private List<T> ExtractPointsFromPath<T>(double?[][] path) where T : IPoint, new()
    {
        var points = new List<T>();
        if (path == null) return points;
        foreach (var coord in path)
        {
            if (coord != null && coord.Length >= 2 && coord[0].HasValue && coord[1].HasValue)
            {
                double[] coords = ExtractCoordinates(coord[0].Value, coord[1].Value,
                    coord.Length > 2 ? coord[2] : null,
                    coord.Length > 3 ? coord[3] : null);
                points.Add(Point.Parse<T>(coords, isLongitudeFirst: true));
            }
        }
        return points;
    }

    #endregion

    #endregion

    //public IGeometry Parse(int srid)
    //{
    //    if (HasM == true)
    //    {
    //        return Geometry<IRI.Maptor.Sta.Common.Primitives.PointZM>.FromWkt(this.AsWkt(), srid);
    //    }
    //    else if (HasZ == true)
    //    {
    //        return Geometry<IRI.Maptor.Sta.Common.Primitives.PointZ>.FromWkt(this.AsWkt(), srid);
    //    }
    //    else
    //    {
    //        return Geometry<IRI.Maptor.Sta.Common.Primitives.Point>.FromWkt(this.AsWkt(), srid);
    //    }
    //}

}
