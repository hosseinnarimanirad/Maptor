using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

/// <summary>
/// TopoJSON Topology - the root object
/// </summary>
public class TopoJsonTopology
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Topology";

    /// <summary>
    /// Named geometry objects
    /// </summary>
    [JsonPropertyName("objects")]
    public Dictionary<string, TopoJsonGeometry> Objects { get; set; } = new();

    /// <summary>
    /// Array of arcs (line segments)
    /// </summary>
    [JsonPropertyName("arcs")]
    public List<List<int[]>> Arcs { get; set; } = new();

    /// <summary>
    /// Optional transform for quantized coordinates
    /// </summary>
    [JsonPropertyName("transform")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TopoJsonTransform? Transform { get; set; }

    /// <summary>
    /// Optional bounding box [minX, minY, maxX, maxY]
    /// </summary>
    [JsonPropertyName("bbox")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double[]? BBox { get; set; }
}

