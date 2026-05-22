using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonMultiPoint : TopoJsonGeometry
{
    //[JsonIgnore]
    //[JsonPropertyName("type")]
    [JsonIgnore]
    public override string Type { get; set; } //=> "MultiPoint";

    /// <summary>
    /// Array of coordinates [[x1, y1], [x2, y2], ...]
    /// </summary>
    [JsonPropertyName("coordinates")]
    public List<double[]> Coordinates { get; set; } = new();

    public TopoJsonMultiPoint()
    {
        Type = "MultiPoint";
    }
}

