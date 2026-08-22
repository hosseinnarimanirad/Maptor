using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.IO.TopoJson;

public class TopoJsonGeometryCollection : TopoJsonGeometry
{

    [JsonIgnore]
    public override string Type { get; set; } //=> "GeometryCollection";

    /// <summary>
    /// Array of geometry objects
    /// </summary>
    [JsonPropertyName("geometries")]
    public List<TopoJsonGeometry>? Geometries { get; set; }

    public TopoJsonGeometryCollection()
    {
        Type = "GeometryCollection";
    }
}

