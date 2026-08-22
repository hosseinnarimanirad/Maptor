using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.IO.TopoJson;

public class TopoJsonPolygon : TopoJsonGeometry
{
    [JsonIgnore]
    //[JsonPropertyName("type")]
    public override string Type { get; set; } //=> "Polygon";

    /// <summary>
    /// Array of arc sequences (first is exterior ring, rest are holes)
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<List<int>> Arcs { get; set; } = new();

    public TopoJsonPolygon()
    {
        Type = "Polygon";
    }
}

