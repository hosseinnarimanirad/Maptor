using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.IO.TopoJson;

/// <summary>
/// Transform for quantized coordinates
/// Actual coordinate = (quantized * scale) + translate
/// </summary>
public class TopoJsonTransform
{
    /// <summary>
    /// Scale factors [scaleX, scaleY]
    /// </summary>
    [JsonPropertyName("scale")]
    public double[] Scale { get; set; } = new[] { 1.0, 1.0 };

    /// <summary>
    /// Translation offsets [translateX, translateY]
    /// </summary>
    [JsonPropertyName("translate")]
    public double[] Translate { get; set; } = new[] { 0.0, 0.0 };

    /// <summary>
    /// Transform quantized coordinates to actual coordinates
    /// </summary>
    public double[] Apply(int[] quantized)
    {
        return new[]
        {
            quantized[0] * Scale[0] + Translate[0],
            quantized[1] * Scale[1] + Translate[1]
        };
    }

    /// <summary>
    /// Transform actual coordinates to quantized coordinates
    /// </summary>
    public int[] Inverse(double x, double y)
    {
        return new[]
        {
            (int)Math.Round((x - Translate[0]) / Scale[0]),
            (int)Math.Round((y - Translate[1]) / Scale[1])
        };
    }
}

