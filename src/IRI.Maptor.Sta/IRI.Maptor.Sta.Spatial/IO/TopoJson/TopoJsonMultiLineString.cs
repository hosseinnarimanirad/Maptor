using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonMultiLineString : TopoJsonGeometry
{
    [JsonPropertyName("type")]
    public override string Type => "MultiLineString";

    /// <summary>
    /// Array of arc sequences
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<List<int>> Arcs { get; set; } = new();
}

