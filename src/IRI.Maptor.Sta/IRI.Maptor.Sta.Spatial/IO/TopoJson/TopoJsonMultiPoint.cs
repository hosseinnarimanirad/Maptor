using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonMultiPoint : TopoJsonGeometry
{
    [JsonPropertyName("type")]
    public override string Type => "MultiPoint";

    /// <summary>
    /// Array of coordinates [[x1, y1], [x2, y2], ...]
    /// </summary>
    [JsonPropertyName("coordinates")]
    public List<double[]> Coordinates { get; set; } = new();
}

