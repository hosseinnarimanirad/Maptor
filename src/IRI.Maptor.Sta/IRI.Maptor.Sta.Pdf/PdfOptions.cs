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
    /// Map-export path only: when true, the page is sized to a custom size that holds the map
    /// at its current on-screen scale (no rescaling to fit a preset). Requires
    /// <see cref="PreservedWebMercatorScale"/>; <see cref="PageSize"/>/<see cref="PageOrientation"/>
    /// are ignored while this is on.
    /// </summary>
    public bool PreserveMapScale { get; set; }

    /// <summary>
    /// Web-mercator scale (page-physical / mercator ratio) used to size the page when
    /// <see cref="PreserveMapScale"/> is on. Typically the map viewer's current MapScale.
    /// </summary>
    public double? PreservedWebMercatorScale { get; set; }

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
    /// Optional marker template stamped at each point feature (reproduces the on-screen symbol).
    /// When null, points fall back to a circle of <see cref="PointCircleRadius"/>.
    /// </summary>
    public PdfPointMarker? PointMarker { get; set; }

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
    /// <summary>ISO A0 (2384 x 3370 points)</summary>
    A0,
    /// <summary>ISO A1 (1684 x 2384 points)</summary>
    A1,
    /// <summary>ISO A2 (1191 x 1684 points)</summary>
    A2,
    /// <summary>ISO A3 (842 x 1191 points)</summary>
    A3,
    /// <summary>ISO A4 (595 x 842 points)</summary>
    A4,
    /// <summary>ISO A5 (420 x 595 points)</summary>
    A5,
    /// <summary>ISO B2 (1417 x 2004 points)</summary>
    B2,
    /// <summary>ISO B3 (1001 x 1417 points)</summary>
    B3,
    /// <summary>ISO B4 (709 x 1001 points)</summary>
    B4,
    /// <summary>US Letter (612 x 792 points)</summary>
    Letter,
    /// <summary>US Legal (612 x 1008 points)</summary>
    Legal,
    /// <summary>US Tabloid / Ledger (792 x 1224 points)</summary>
    Tabloid,
    /// <summary>
    /// Custom size (use CustomPageWidth and CustomPageHeight)
    /// </summary>
    Custom
}

/// <summary>
/// Physical dimensions (in PDF points, 1/72 inch) of the standard <see cref="PdfPageSize"/>
/// presets. A single source of truth so the writer and any UI stay in sync.
/// </summary>
public static class PdfPageDimensions
{
    // Portrait (width &lt; height) dimensions in points. 1 mm = 72/25.4 pt; 1 in = 72 pt.
    private static readonly Dictionary<PdfPageSize, (double Width, double Height)> _portrait = new()
    {
        [PdfPageSize.A0] = (2384, 3370),
        [PdfPageSize.A1] = (1684, 2384),
        [PdfPageSize.A2] = (1191, 1684),
        [PdfPageSize.A3] = (842, 1191),
        [PdfPageSize.A4] = (595, 842),
        [PdfPageSize.A5] = (420, 595),
        [PdfPageSize.B2] = (1417, 2004),
        [PdfPageSize.B3] = (1001, 1417),
        [PdfPageSize.B4] = (709, 1001),
        [PdfPageSize.Letter] = (612, 792),
        [PdfPageSize.Legal] = (612, 1008),
        [PdfPageSize.Tabloid] = (792, 1224),
    };

    /// <summary>
    /// True for a preset that has fixed physical dimensions (everything except
    /// <see cref="PdfPageSize.Auto"/> and <see cref="PdfPageSize.Custom"/>).
    /// </summary>
    public static bool IsFixed(PdfPageSize size) => _portrait.ContainsKey(size);

    /// <summary>
    /// Returns the preset's dimensions in points for the given orientation.
    /// Falls back to A4 for non-fixed presets (Auto/Custom).
    /// </summary>
    public static (double Width, double Height) Get(PdfPageSize size, PdfPageOrientation orientation)
    {
        if (!_portrait.TryGetValue(size, out var dims))
            dims = _portrait[PdfPageSize.A4];

        return orientation == PdfPageOrientation.Landscape ? (dims.Height, dims.Width) : dims;
    }
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