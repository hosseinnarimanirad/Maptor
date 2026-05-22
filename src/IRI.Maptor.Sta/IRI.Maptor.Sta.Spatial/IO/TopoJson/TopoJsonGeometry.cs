using IRI.Maptor.Sta.Common.Common.JsonConverters;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

/// <summary>
/// Base class for TopoJSON geometry objects
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TopoJsonPoint), "Point")]
[JsonDerivedType(typeof(TopoJsonMultiPoint), "MultiPoint")]
[JsonDerivedType(typeof(TopoJsonLineString), "LineString")]
[JsonDerivedType(typeof(TopoJsonMultiLineString), "MultiLineString")]
[JsonDerivedType(typeof(TopoJsonPolygon), "Polygon")]
[JsonDerivedType(typeof(TopoJsonMultiPolygon), "MultiPolygon")]
[JsonDerivedType(typeof(TopoJsonGeometryCollection), "GeometryCollection")]
public abstract class TopoJsonGeometry
{
    [JsonPropertyName("type")]
    ////[JsonIgnore]
    public abstract string Type { get; set; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(DictionaryStringObjectConverter))]
    public Dictionary<string, object>? Properties { get; set; }
}

