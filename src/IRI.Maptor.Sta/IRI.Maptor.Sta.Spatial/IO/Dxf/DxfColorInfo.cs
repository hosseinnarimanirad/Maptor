namespace IRI.Maptor.Sta.Spatial.IO.Dxf;

/// <summary>
/// Color and styling information for DXF export
/// </summary>
public class DxfColorInfo
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
    /// Line thickness for strokes
    /// </summary>
    public double StrokeThickness { get; set; } = 1.0;

    /// <summary>
    /// Opacity (0.0 to 1.0). Converted to transparency in DXF
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    public DxfColorInfo()
    {
    }

    public DxfColorInfo(RgbColor? strokeColor, RgbColor? fillColor = null, double strokeThickness = 1.0, double opacity = 1.0)
    {
        StrokeColor = strokeColor;
        FillColor = fillColor;
        StrokeThickness = strokeThickness;
        Opacity = opacity;
    }
}

/// <summary>
/// RGB color representation
/// </summary>
public struct RgbColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }

    public RgbColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Converts to DXF true color format (24-bit RGB integer)
    /// </summary>
    public int ToDxfTrueColor()
    {
        return (R << 16) | (G << 8) | B;
    }

    /// <summary>
    /// Gets the alpha value adjusted by opacity
    /// </summary>
    public byte GetAlpha(double opacity)
    {
        return (byte)Math.Round(A * Math.Clamp(opacity, 0.0, 1.0));
    }
}

