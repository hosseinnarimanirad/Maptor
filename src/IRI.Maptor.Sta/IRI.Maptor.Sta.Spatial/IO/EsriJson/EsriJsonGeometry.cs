using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using IRI.Maptor.Sta.Common.Helpers;
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

    //polyline
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

    public IGeometry Parse(int srid)
    {
        if (HasM == true)
        {
            return Geometry<IRI.Maptor.Sta.Common.Primitives.PointZM>.FromWkt(this.AsWkt(), srid);
        }
        else if (HasZ == true)
        {
            return Geometry<IRI.Maptor.Sta.Common.Primitives.PointZ>.FromWkt(this.AsWkt(), srid);
        }
        else
        {
            return Geometry<IRI.Maptor.Sta.Common.Primitives.Point>.FromWkt(this.AsWkt(), srid);
        }
    }

    public EsriJsonFeature AsFeature()
    {
        return new EsriJsonFeature()
        {
            Attributes = new Dictionary<string, object>() { { "Type", Type.ToString() } },
            Geometry = this,
        };
    }
}
