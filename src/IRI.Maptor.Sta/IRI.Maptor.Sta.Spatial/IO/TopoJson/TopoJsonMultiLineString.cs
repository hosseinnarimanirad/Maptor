using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonMultiLineString : TopoJsonGeometry
{
    //[JsonPropertyName("type")]
    [JsonIgnore]
    public override string Type { get; set; } //=> "MultiLineString";

    /// <summary>
    /// Array of arc sequences
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<List<int>> Arcs { get; set; } = new();

    public TopoJsonMultiLineString()
    {
        Type = "MultiLineString";
    }
}

