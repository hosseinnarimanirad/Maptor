using IRI.Maptor.Core.Spatial.IO.Dxf;

namespace IRI.Maptor.Core.Spatial.IO.Eps;

/// <summary>
/// Configuration options for EPS read/write operations
/// </summary>
public class EpsOptions
{
    /// <summary>
    /// Padding around bounding box (as percentage of bounding box size)
    /// </summary>
    public double BoundingBoxPadding { get; set; } = 0.05; // 5% padding

    /// <summary>
    /// Coordinate precision for writing coordinates (number of decimal places)
    /// </summary>
    public int CoordinatePrecision { get; set; } = 6;

    /// <summary>
    /// Stroke (outline) color as RGB
    /// </summary>
    public RgbColor? StrokeColor { get; set; }

    /// <summary>
    /// Fill color as RGB (for polygons)
    /// </summary>
    public RgbColor? FillColor { get; set; }

    /// <summary>
    /// Stroke width/thickness
    /// </summary>
    public double StrokeWidth { get; set; } = 1.0;

    /// <summary>
    /// Whether to include TIFF preview (default: false)
    /// </summary>
    public bool IncludePreview { get; set; } = false;

    /// <summary>
    /// Application name for Creator field
    /// </summary>
    public string Creator { get; set; } = "IRI.Maptor.Core.Spatial";

    /// <summary>
    /// Document title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Whether to preserve Feature attributes as EPS comments
    /// </summary>
    public bool PreserveFeatureAttributes { get; set; } = true;

    /// <summary>
    /// Default constructor with default values
    /// </summary>
    public EpsOptions()
    {
    }

    /// <summary>
    /// Constructor with styling options
    /// </summary>
    public EpsOptions(RgbColor? strokeColor, RgbColor? fillColor = null, double strokeWidth = 1.0)
    {
        StrokeColor = strokeColor;
        FillColor = fillColor;
        StrokeWidth = strokeWidth;
    }

    /// <summary>
    /// Converts RGB color to PostScript color format (0.0 to 1.0 range)
    /// </summary>
    public string ToPostScriptColor(RgbColor color)
    {
        return $"{color.R / 255.0:F6} {color.G / 255.0:F6} {color.B / 255.0:F6}";
    }

    /// <summary>
    /// Gets stroke color as PostScript color string
    /// </summary>
    public string? GetStrokeColorString()
    {
        if (StrokeColor.HasValue)
        {
            return ToPostScriptColor(StrokeColor.Value);
        }
        return null;
    }

    /// <summary>
    /// Gets fill color as PostScript color string
    /// </summary>
    public string? GetFillColorString()
    {
        if (FillColor.HasValue)
        {
            return ToPostScriptColor(FillColor.Value);
        }
        return null;
    }
}
