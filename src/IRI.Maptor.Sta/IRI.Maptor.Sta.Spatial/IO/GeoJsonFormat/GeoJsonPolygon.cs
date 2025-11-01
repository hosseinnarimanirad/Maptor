using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using System.Text.Json.Serialization;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;


//[JsonConverter(typeof(GeoJsonGeometryConverter))]
public class GeoJsonPolygon : GeoJsonBase
{
    [JsonIgnore]
    //[JsonPropertyName("type")]
    public override string Type { get; set; }

    [JsonPropertyName("coordinates")]
    public double[][][] Coordinates { get; set; }

    [JsonIgnore]
    public override GeometryType GeometryType { get => GeometryType.Polygon; }

    public override bool IsNullOrEmpty()
    {
        return Coordinates == null || Coordinates.Length < 1;
    }

    public override int NumberOfGeometries()
    {
        return Coordinates == null ? 0 : Coordinates.Length;
    }

    public override int NumberOfPoints()
    {
        return Coordinates == null ? 0 : Coordinates.Sum(ring => ring == null ? 0 : ring.Length);
    }

    public override Geometry<Point> Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        return Geometry<Point>.ParsePolygonToGeometry(Coordinates, this.GeometryType, isLongitudeFirst, srid);
    }

    //public string Serialize(bool indented, bool removeSpaces = false)
    //{
    //    return GeoJson.Serialize(this, indented, removeSpaces);
    //}

    //public Geometry<Point> TransformToWeMercator(bool isLongitudeFirst = true)
    //{
    //    return this.Parse(isLongitudeFirst, SridHelper.GeodeticWGS84)
    //                .Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
    //}

    //public GeoJsonFeature AsFeature() => GeoJson.AsFeature(this);

    //public GeoJsonFeatureSet AsFeatureSet() => GeoJson.AsFeatureSet(this);
}
