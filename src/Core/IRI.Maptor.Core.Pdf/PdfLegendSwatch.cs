using IRI.Maptor.Core.Spatial.IO.Dxf;

namespace IRI.Maptor.Core.Pdf;

/// <summary>
/// A legend symbol drawn as true PDF vector art (stays crisp at any zoom) instead of a raster
/// thumbnail. Only the <i>style</i> travels — the sample geometry itself is canonical (a stroked
/// line, a filled rectangle, a centered point marker) and is synthesized by the composer inside
/// the swatch cell, so it is resolution-independent by construction.
/// <para>
/// A rule may stack several parts (e.g. a wide casing line under a narrow core line); they are
/// drawn in order.
/// </para>
/// </summary>
public class PdfLegendSwatch
{
    public List<PdfLegendSwatchPart> Parts { get; set; } = new();

    public bool IsValid => Parts.Count > 0;
}

/// <summary>The sample geometry a swatch part draws.</summary>
public enum PdfLegendSwatchShape
{
    /// <summary>A horizontal stroked line spanning the cell.</summary>
    Line,

    /// <summary>A filled + outlined rectangle inset in the cell.</summary>
    Polygon,

    /// <summary>A point marker (or circle) centered in the cell.</summary>
    Point,
}

/// <summary>One symbolizer's contribution to a legend swatch.</summary>
public class PdfLegendSwatchPart
{
    public PdfLegendSwatchShape Shape { get; set; }

    public RgbColor? Fill { get; set; }

    public RgbColor? Stroke { get; set; }

    /// <summary>Stroke thickness in PDF points (converted from screen px upstream).</summary>
    public double StrokeWidth { get; set; } = 1.0;

    /// <summary>Layer opacity (0..1), folded into the drawn colors' alpha.</summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>Point symbol template; when null a point falls back to a filled circle.</summary>
    public PdfPointMarker? Marker { get; set; }

    /// <summary>Radius (points) of the fallback circle for <see cref="PdfLegendSwatchShape.Point"/>.</summary>
    public double PointRadius { get; set; } = 3.0;
}
