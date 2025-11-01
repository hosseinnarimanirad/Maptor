using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonGeometryCollection : TopoJsonGeometry
{
    [JsonPropertyName("type")]
    public override string Type => "GeometryCollection";

    /// <summary>
    /// Array of geometry objects
    /// Note: Using JsonElement to avoid polymorphic deserialization issues.
    /// We skip GeometryCollections anyway, so we don't need to parse the nested geometries.
    /// </summary>
    [JsonPropertyName("geometries")]
    public List<JsonElement>? Geometries { get; set; }
}

