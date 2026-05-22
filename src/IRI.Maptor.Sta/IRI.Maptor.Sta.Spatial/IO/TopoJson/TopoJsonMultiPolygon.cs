using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonMultiPolygon : TopoJsonGeometry
{
    [JsonIgnore]
    //[JsonPropertyName("type")]
    public override string Type { get; set; } //=> "MultiPolygon";

    /// <summary>
    /// Array of polygons (each polygon is an array of rings)
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<List<List<int>>> Arcs { get; set; } = new();

    public TopoJsonMultiPolygon()
    {
        Type = "MultiPolygon";
    }
}

