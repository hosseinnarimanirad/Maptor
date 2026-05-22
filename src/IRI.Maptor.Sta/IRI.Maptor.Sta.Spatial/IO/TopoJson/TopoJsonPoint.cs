using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

public class TopoJsonPoint : TopoJsonGeometry
{
    [JsonIgnore]
    //[JsonPropertyName("type")]
    public override string Type { get; set; } //=> "Point";

    /// <summary>
    /// Coordinates [x, y] or [x, y, z]
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[] Coordinates { get; set; } = Array.Empty<double>();

    public TopoJsonPoint()
    {
        Type = "Point";
    }
}

