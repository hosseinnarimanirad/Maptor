using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonMultiPolygon : TopoJsonGeometry
{
    [JsonPropertyName("type")]
    public override string Type => "MultiPolygon";

    /// <summary>
    /// Array of polygons (each polygon is an array of rings)
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<List<List<int>>> Arcs { get; set; } = new();
}

