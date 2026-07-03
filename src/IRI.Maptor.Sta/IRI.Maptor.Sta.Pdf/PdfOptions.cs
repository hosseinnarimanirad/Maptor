using IRI.Maptor.Sta.Spatial.IO.Dxf;
using PdfSharpCore.Drawing;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// Configuration options for PDF write operations
/// </summary>
public class PdfOptions
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
    /// Coordinate precision for writing coordinates (number of decimal places)
    /// </summary>
    public int CoordinatePrecision { get; set; } = 6;

    /// <summary>
    /// Page size preset
    /// </summary>
    public PdfPageSize PageSize { get; set; } = PdfPageSize.Auto;

    /// <summary>
    /// Custom page width in points (1/72 inch). Used when PageSize is Custom
    /// </summary>
    public double? CustomPageWidth { get; set; }

    /// <summary>
    /// Custom page height in points (1/72 inch). Used when PageSize is Custom
    /// </summary>
    public double? CustomPageHeight { get; set; }

    /// <summary>
    /// Page orientation
    /// </summary>
    public PdfPageOrientation PageOrientation { get; set; } = PdfPageOrientation.Portrait;

    /// <summary>
    /// Padding around bounding box (as percentage of bounding box size)
    /// </summary>
    public double BoundingBoxPadding { get; set; } = 0.05; // 5% padding

    /// <summary>
    /// Whether to preserve Feature attributes as PDF metadata
    /// </summary>
    public bool PreserveFeatureAttributes { get; set; } = true;

    /// <summary>
    /// Document title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Document author
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Application name for Creator field
    /// </summary>
    public string Creator { get; set; } = "IRI.Maptor.Sta.Pdf";

    /// <summary>
    /// Document subject
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Document keywords
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// Radius for circle elements representing points
    /// </summary>
    public double PointCircleRadius { get; set; } = 3.0;

    /// <summary>
    /// Default constructor with default values
    /// </summary>
    public PdfOptions()
    {
    }

    /// <summary>
    /// Constructor with styling options
    /// </summary>
    public PdfOptions(RgbColor? strokeColor, RgbColor? fillColor = null, double strokeWidth = 1.0, double opacity = 1.0)
    {
        StrokeColor = strokeColor;
        FillColor = fillColor;
        StrokeWidth = strokeWidth;
        Opacity = opacity;
    }

    /// <summary>
    /// Converts RGB color to PdfSharp XColor
    /// </summary>
    public XColor ToPdfColor(RgbColor color)
    {
        var alpha = (byte)Math.Round(color.A * Math.Clamp(Opacity, 0.0, 1.0));
        return XColor.FromArgb(alpha, color.R, color.G, color.B);
    }

    /// <summary>
    /// Gets stroke color as PdfSharp XColor
    /// </summary>
    public XColor? GetStrokeColor()
    {
        if (StrokeColor.HasValue)
        {
            return ToPdfColor(StrokeColor.Value);
        }
        return null;
    }

    /// <summary>
    /// Gets fill color as PdfSharp XColor
    /// </summary>
    public XColor? GetFillColor()
    {
        if (FillColor.HasValue)
        {
            return ToPdfColor(FillColor.Value);
        }
        return null;
    }
}

/// <summary>
/// PDF page size presets
/// </summary>
public enum PdfPageSize
{
    /// <summary>
    /// Automatically calculate page size from geometry bounding box
    /// </summary>
    Auto,
    /// <summary>
    /// A4 size (595 x 842 points)
    /// </summary>
    A4,
    /// <summary>
    /// A3 size (842 x 1191 points)
    /// </summary>
    A3,
    /// <summary>
    /// Letter size (612 x 792 points)
    /// </summary>
    Letter,
    /// <summary>
    /// Custom size (use CustomPageWidth and CustomPageHeight)
    /// </summary>
    Custom
}

/// <summary>
/// PDF page orientation
/// </summary>
public enum PdfPageOrientation
{
    /// <summary>
    /// Portrait orientation
    /// </summary>
    Portrait,
    /// <summary>
    /// Landscape orientation
    /// </summary>
    Landscape
}