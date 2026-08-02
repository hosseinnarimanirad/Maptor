namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// One layer's block in the printed legend column: a header (the layer name) followed by its
/// rule rows. Groups are listed topmost layer first, matching the drawing order on the map.
/// </summary>
public class PdfLegendGroup
{
    /// <summary>Layer name as filled vector glyph outlines (crisp, RTL-safe), one-column flow.</summary>
    public PdfVectorLogo? HeaderVector { get; set; }

    /// <summary>
    /// Same header re-wrapped for the narrower two-column flow. Falls back to
    /// <see cref="HeaderVector"/> when not supplied.
    /// </summary>
    public PdfVectorLogo? HeaderVectorNarrow { get; set; }

    public List<PdfLegendEntry> Entries { get; set; } = new();
}

/// <summary>
/// Shared legend-column layout numbers. Public because the WPF-side legend builder needs them
/// to wrap/ellipsize titles at render time: label vectors are drawn at the fixed
/// <see cref="PxToPoint"/> scale (uniform font size — never shrunk to fit), so the text must
/// already be wrapped to the width it will get on paper.
/// </summary>
public static class PdfLegendMetrics
{
    /// <summary>Width of the legend column (PDF points).</summary>
    public const double ColumnWidth = 220;

    /// <summary>Inner padding of the legend column body.</summary>
    public const double ColumnPadding = 5;

    /// <summary>Rule swatch cell width (same in one- and two-column flow).</summary>
    public const double SwatchWidth = 20;

    /// <summary>Gap between the swatch and its title.</summary>
    public const double SwatchTextGap = 4;

    /// <summary>Gap between the two sub-columns when the legend flows two-up.</summary>
    public const double SubColumnGap = 6;

    /// <summary>Screen px (1/96") → PDF points (1/72"): text prints at its rendered pixel size.</summary>
    public const double PxToPoint = 72.0 / 96.0;

    /// <summary>Usable body width inside the column padding.</summary>
    public const double BodyWidthPt = ColumnWidth - 2 * ColumnPadding;

    /// <summary>Width of one sub-column when flowing two-up.</summary>
    public const double SubColumnWidthPt = (BodyWidthPt - SubColumnGap) / 2;

    /// <summary>Max paper width of a layer-name header line (no swatch), one-column flow.</summary>
    public const double HeaderMaxWidthPt = BodyWidthPt;

    /// <summary>Max paper width of a swatch row's title, one-column flow.</summary>
    public const double LabelMaxWidthPt = BodyWidthPt - SwatchWidth - SwatchTextGap;

    /// <summary>Max paper width of a header line in two-column flow.</summary>
    public const double NarrowHeaderMaxWidthPt = SubColumnWidthPt;

    /// <summary>Max paper width of a swatch row's title in two-column flow.</summary>
    public const double NarrowLabelMaxWidthPt = SubColumnWidthPt - SwatchWidth - SwatchTextGap;

    // Same widths in render pixels, for FormattedText.MaxTextWidth. Titles are wrapped to the
    // width they will actually get on paper, so the PDF never has to shrink them to fit —
    // which is what kept every layer's text at the same font size.
    public const double HeaderMaxWidthPx = HeaderMaxWidthPt / PxToPoint;

    public const double LabelMaxWidthPx = LabelMaxWidthPt / PxToPoint;

    public const double NarrowHeaderMaxWidthPx = NarrowHeaderMaxWidthPt / PxToPoint;

    public const double NarrowLabelMaxWidthPx = NarrowLabelMaxWidthPt / PxToPoint;
}

/// <summary>One legend rule row: a symbol swatch plus its title.</summary>
public class PdfLegendEntry
{
    /// <summary>
    /// Swatch as vector art — preferred, because it stays crisp at any zoom. Wins over
    /// <see cref="SwatchPngBytes"/> when valid.
    /// </summary>
    public PdfLegendSwatch? SwatchVector { get; set; }

    /// <summary>
    /// Raster fallback for symbology the vector path can't express: an opaque (alpha-free) PNG —
    /// PdfSharpCore mis-renders images carrying an SMask, so transparency must be flattened
    /// upstream. Null for a text-only row.
    /// </summary>
    public byte[]? SwatchPngBytes { get; set; }

    /// <summary>True when the row has a symbol to draw, in either form.</summary>
    public bool HasSwatch => SwatchVector is { IsValid: true } || SwatchPngBytes is { Length: > 0 };

    /// <summary>Rule title as filled vector glyph outlines (crisp, RTL-safe), one-column flow.</summary>
    public PdfVectorLogo? LabelVector { get; set; }

    /// <summary>
    /// Same title re-wrapped for the narrower two-column flow. Falls back to
    /// <see cref="LabelVector"/> when not supplied.
    /// </summary>
    public PdfVectorLogo? LabelVectorNarrow { get; set; }
}
