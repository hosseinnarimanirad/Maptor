using IRI.Maptor.Sta.Common.Common.JsonConverters;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Text.Json.Serialization;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

public class GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Feature";

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("geometry")]
    public IGeoJsonGeometry Geometry { get; set; }

    [JsonPropertyName("geometry_name")]
    public string Geometry_name { get; set; }

    [JsonPropertyName("properties")]
    [JsonConverter(typeof(DictionaryStringObjectConverter))]
    public Dictionary<string, object> Properties { get; set; }

    public static GeoJsonFeature Create(IGeoJsonGeometry geometry, Dictionary<string, object> attributes = null)
    {
        return new GeoJsonFeature()
        {
            Geometry = geometry,
            Geometry_name = string.Empty,
            Id = "0",
            Properties = attributes ?? new Dictionary<string, object>(),
        };
    }

    public Feature<Point> AsFeature(bool isLongitudeFirst, SrsBase? targetSrs = null)
    {
        targetSrs = targetSrs ?? SrsBases.GeodeticWgs84;

        return new Feature<Point>()
        {
            Attributes = this.Properties/*.ToDictionary(f => f.Key, f => (object)f.Value)*/,
            //Id = feature.id,
            //TheGeometry = feature.Geometry.AsSqlGeography(isLongitudeFirst, SridHelper.GeodeticWGS84)
            //                                    .Project(targetSrs.FromWgs84Geodetic<Point>, SridHelper.WebMercator).AsGeometry()
            TheGeometry = this.Geometry.Parse(isLongitudeFirst, SridHelper.GeodeticWGS84).Project(targetSrs)
        };
    }
}
