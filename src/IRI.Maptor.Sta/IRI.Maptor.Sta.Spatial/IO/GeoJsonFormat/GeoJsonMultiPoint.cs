
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using System.Text.Json.Serialization;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Extensions;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

//[JsonConverter(typeof(GeoJsonGeometryConverter))]
public class GeoJsonMultiPoint : GeoJsonBase
{
    private static readonly GeoJsonMultiPoint _empty = new GeoJsonMultiPoint() { Coordinates = [] };

    public static GeoJsonMultiPoint Empty => _empty;

    [JsonIgnore]
    //[JsonPropertyName("type")]
    public override string Type { get; set; }

    [JsonPropertyName("coordinates")]
    public double[][] Coordinates { get; set; }

    [JsonIgnore]
    public override GeometryType GeometryType { get => GeometryType.MultiPoint; }

    public GeoJsonMultiPoint()
    {
        Type = GeoJson.MultiPoint;
    }

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
        // 1400.02.03
        // number of parts equals number of points
        return NumberOfGeometries();
    }


    public override Geometry<Point> Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        if (this.Coordinates.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.MultiPoint, srid);

        return new Geometry<Point>(Coordinates!.Select(c => Geometry<Point>.ParsePointToGeometry(c, isLongitudeFirst)).ToList(), this.GeometryType, srid);
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
