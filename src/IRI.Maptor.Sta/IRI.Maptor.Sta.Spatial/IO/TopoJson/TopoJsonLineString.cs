using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonLineString : TopoJsonGeometry
{
    [JsonPropertyName("type")]
    public override string Type => "LineString";

    /// <summary>
    /// Arc indices that form this line string
    /// Negative index means reversed arc
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<int> Arcs { get; set; } = new();
}

