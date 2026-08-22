namespace IRI.Maptor.Core.Spatial.IO.Dxf;

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
/// AutoCAD Color Index (ACI) → RGB conversion for the standard 255-color palette.
/// Indexes 1-9 and 250-255 are fixed colors; 10-249 follow the palette's structure:
/// 24 hues 15° apart × 5 brightness levels × full/muted saturation.
/// </summary>
public static class DxfAciColor
{
    private static readonly double[] _brightnessLevels = { 255, 189, 129, 104, 79 };

    private static readonly byte[] _grayLevels = { 51, 91, 132, 173, 214, 255 }; // ACI 250-255

    public static string ToHex(int aci)
    {
        var (r, g, b) = ToRgb(aci);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public static (byte R, byte G, byte B) ToRgb(int aci)
    {
        switch (aci)
        {
            case 1: return (255, 0, 0);       // red
            case 2: return (255, 255, 0);     // yellow
            case 3: return (0, 255, 0);       // green
            case 4: return (0, 255, 255);     // cyan
            case 5: return (0, 0, 255);       // blue
            case 6: return (255, 0, 255);     // magenta
            case 7: return (255, 255, 255);   // white/black (viewport dependent)
            case 8: return (128, 128, 128);
            case 9: return (192, 192, 192);
        }

        if (aci >= 250 && aci <= 255)
        {
            var gray = _grayLevels[aci - 250];
            return (gray, gray, gray);
        }

        if (aci >= 10 && aci <= 249)
        {
            int hueIndex = (aci - 10) / 10;
            int brightnessRow = (aci - 10) % 10 / 2;
            bool mutedSaturation = (aci - 10) % 2 == 1;

            double max = _brightnessLevels[brightnessRow];
            double min = mutedSaturation ? Math.Round(max * 2.0 / 3.0) : 0;

            return HueToRgb(hueIndex * 15.0, max, min);
        }

        return (255, 255, 255); // 0 (ByBlock), 256 (ByLayer) or out of range
    }

    private static (byte R, byte G, byte B) HueToRgb(double hueDegrees, double max, double min)
    {
        double chroma = max - min;
        double h = hueDegrees / 60.0;
        double mid = chroma * (1 - Math.Abs(h % 2 - 1)) + min;

        var (r, g, b) = (int)h switch
        {
            0 => (max, mid, min),
            1 => (mid, max, min),
            2 => (min, max, mid),
            3 => (min, mid, max),
            4 => (mid, min, max),
            _ => (max, min, mid),
        };

        return ((byte)Math.Round(r), (byte)Math.Round(g), (byte)Math.Round(b));
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

