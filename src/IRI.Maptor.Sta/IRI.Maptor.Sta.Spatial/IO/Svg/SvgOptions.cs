using IRI.Maptor.Sta.Spatial.IO.Dxf;

namespace IRI.Maptor.Sta.Spatial.IO.Svg;

/// <summary>
/// Configuration options for SVG read/write operations
/// </summary>
public class SvgOptions
{
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
    /// Opacity (0.0 to 1.0)
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// Whether to include viewBox attribute (calculated from bounding box)
    /// </summary>
    public bool IncludeViewBox { get; set; } = true;

    /// <summary>
    /// Padding around viewBox (as percentage of bounding box size)
    /// </summary>
    public double ViewBoxPadding { get; set; } = 0.05; // 5% padding

    /// <summary>
    /// Coordinate precision for writing coordinates (number of decimal places)
    /// </summary>
    public int CoordinatePrecision { get; set; } = 6;

    /// <summary>
    /// Whether to preserve Feature attributes as SVG attributes (id, class, data-*, etc.)
    /// </summary>
    public bool PreserveFeatureAttributes { get; set; } = true;

    /// <summary>
    /// Radius for circle elements representing points
    /// </summary>
    public double PointCircleRadius { get; set; } = 3.0;

    /// <summary>
    /// Default constructor with default values
    /// </summary>
    public SvgOptions()
    {
    }

    /// <summary>
    /// Constructor with styling options
    /// </summary>
    public SvgOptions(RgbColor? strokeColor, RgbColor? fillColor = null, double strokeWidth = 1.0, double opacity = 1.0)
    {
        StrokeColor = strokeColor;
        FillColor = fillColor;
        StrokeWidth = strokeWidth;
        Opacity = opacity;
    }

    /// <summary>
    /// Converts RGB color to SVG hex color string (e.g., "#FF0000" or "#FF0000FF" with alpha)
    /// </summary>
    public string ToSvgColor(RgbColor color, bool includeAlpha = false)
    {
        if (includeAlpha && color.A < 255)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
        }
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Gets stroke color as SVG color string
    /// </summary>
    public string? GetStrokeColorString()
    {
        if (StrokeColor.HasValue)
        {
            var color = StrokeColor.Value;
            var alpha = (byte)Math.Round(color.A * Opacity);
            if (alpha < 255)
            {
                return ToSvgColor(new RgbColor(color.R, color.G, color.B, alpha), true);
            }
            return ToSvgColor(color);
        }
        return null;
    }

    /// <summary>
    /// Gets fill color as SVG color string
    /// </summary>
    public string? GetFillColorString()
    {
        if (FillColor.HasValue)
        {
            var color = FillColor.Value;
            var alpha = (byte)Math.Round(color.A * Opacity);
            if (alpha < 255)
            {
                return ToSvgColor(new RgbColor(color.R, color.G, color.B, alpha), true);
            }
            return ToSvgColor(color);
        }
        return null;
    }
}



