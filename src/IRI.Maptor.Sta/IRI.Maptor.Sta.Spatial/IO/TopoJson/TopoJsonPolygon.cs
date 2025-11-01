using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonPolygon : TopoJsonGeometry
{
    [JsonPropertyName("type")]
    public override string Type => "Polygon";

    /// <summary>
    /// Array of arc sequences (first is exterior ring, rest are holes)
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<List<int>> Arcs { get; set; } = new();
}

