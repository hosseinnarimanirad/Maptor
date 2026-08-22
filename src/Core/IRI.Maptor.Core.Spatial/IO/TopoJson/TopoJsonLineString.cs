using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.IO.TopoJson;

public class TopoJsonLineString : TopoJsonGeometry
{

    //[JsonPropertyName("type")]
    [JsonIgnore]
    public override string Type { get; set; } //=> "LineString";

    /// <summary>
    /// Arc indices that form this line string
    /// Negative index means reversed arc
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<int> Arcs { get; set; } = new();

    public TopoJsonLineString()
    {
        Type = "LineString";
    }
}

