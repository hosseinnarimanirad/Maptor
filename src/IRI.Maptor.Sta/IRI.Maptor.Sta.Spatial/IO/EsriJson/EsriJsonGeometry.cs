using System.Globalization;
using System.Text.Json.Serialization;

using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Sta.Spatial.Primitives.Esri;

//[DataContract]

//[JsonObject]
public class EsriJsonGeometry
{
    const string pointType = "POINT";
    const string multiPointType = "MULTIPOINT";
    const string polylineType = "POLYLINE";
    const string polygonType = "POLYGON";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EsriJsonGeometryType Type { get; set; }

    public double? X { get; set; }

    public double? Y { get; set; }

    public double? Z { get; set; }

    public double? M { get; set; }

    public bool? HasM { get; set; }

    public bool? HasZ { get; set; }

    //multipoint
    public double?[][] Points { get; set; }

    //polygon
    public double?[][][] Rings { get; set; }

    //polyline
    public double?[][][] Paths { get; set; }

    public EsriJsonSpatialreference SpatialReference { get; set; }

    public static EsriJsonGeometry Parse(string esriGeometryJsonString, EsriJsonGeometryType type)
    {
        try
        {
            var result = JsonHelper.Deserialize<EsriJsonGeometry>(esriGeometryJsonString);

            result.Type = type;

            return result;
        }
        catch (Exception ex)
        {
            return null;
        }
    }


    public string AsWkt()
    {
        switch (Type)
        {
            case EsriJsonGeometryType.point:
                return PointToWkt();

            case EsriJsonGeometryType.multipoint:
                return MultiPointToWkt();

            case EsriJsonGeometryType.polyline:
                return PolylineToWkt();

            case EsriJsonGeometryType.polygon:
                return PolygonToWkt();

            default:
                throw new NotImplementedException();
        }
    }

    private string PointToWkt()
    {
        var xValue = X.ToStringOrNull(false);

        var yValue = Y.ToStringOrNull(false);

        if (string.IsNullOrEmpty(xValue) || string.IsNullOrEmpty(yValue))
        {
            return "POINT EMPTY";
        }

        var mValue = M.ToStringOrNull(false);
        var zValue = Z.ToStringOrNull(mValue.Length > 0);

        if (string.IsNullOrEmpty(zValue) && string.IsNullOrEmpty(mValue))
        {
            return FormattableString.Invariant($"POINT({xValue.ToString(CultureInfo.InvariantCulture)} {yValue})");
        }
        else
        {
            return FormattableString.Invariant($"POINT({xValue} {yValue} {zValue} {mValue})");
        }

    }

    private string MultiPointToWkt()
    {
        if (!(Points?.Length > 0))
        {
            return "MULTIPOINT EMPTY";
        }

        return FormattableString.Invariant($"MULTIPOINT{EsriJsonHelper.PointArrayToString(Points)}");
    }

    private string PolylineToWkt()
    {
        if (!(Paths?.Length > 0))
        {
            return "LINESTRING EMPTY";
        }
        else if (Paths.Length == 1)
        {
            return FormattableString.Invariant($"LINESTRING{EsriJsonHelper.PointArrayToString(Paths[0])}");
        }
        else
        {
            return FormattableString.Invariant($"MULTILINESTRING({string.Join(", ", Paths.Select(i => EsriJsonHelper.PointArrayToString(i)))})");
        }

    }

    private string PolygonToWkt()
    {
        if (!(Rings?.Length > 0))
        {
            return "POLYGON EMPTY";
        }
        else if (Rings.Length == 1)
        {
            return FormattableString.Invariant($"POLYGON({EsriJsonHelper.PointArrayToString(Rings[0])})");
        }
        else
        {
            return FormattableString.Invariant($"MULTIPOLYGON({string.Join(", ", Rings.Select(i => $"({EsriJsonHelper.PointArrayToString(i)})"))})");
        }

    }

}
