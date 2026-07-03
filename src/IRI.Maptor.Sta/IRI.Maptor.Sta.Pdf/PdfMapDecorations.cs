namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// Optional map-layout decorations (title, scale bar, logos, graticule) composed
/// around the map frame by <see cref="PdfWriter.WriteLayers"/>. When none are set,
/// the writer produces the classic full-bleed export.
/// </summary>
public class PdfMapDecorations
{
    /// <summary>
    /// Title drawn as PDF text. Latin scripts only — PdfSharpCore has no bidi/RTL
    /// shaping; for Persian/Arabic titles use <see cref="TitlePngBytes"/> instead.
    /// </summary>
    public string? TitleText { get; set; }

    /// <summary>
    /// Pre-rendered title image (preferred; script/RTL-safe). Wins over <see cref="TitleText"/>.
    /// </summary>
    public byte[]? TitlePngBytes { get; set; }

    public bool ShowScaleBar { get; set; }

    public bool ShowGraticule { get; set; }

    /// <summary>
    /// Graticule spacing in degrees; null to choose automatically from the extent span
    /// </summary>
    public double? GraticuleIntervalDegrees { get; set; }

    /// <summary>
    /// Producer icon (e.g. Maptor logo), drawn in the footer band
    /// </summary>
    public byte[]? PrimaryLogoPngBytes { get; set; }

    /// <summary>
    /// Map producer's company logo, drawn in the title band
    /// </summary>
    public byte[]? SecondaryLogoPngBytes { get; set; }

    /// <summary>
    /// TTF bytes for graticule/scale-bar labels; embedded via a font resolver.
    /// Null falls back to <see cref="LabelFontFamily"/> resolved from system fonts.
    /// </summary>
    public byte[]? LabelFontBytes { get; set; }

    public string LabelFontFamily { get; set; } = "Arial";

    public bool HasTitle => TitlePngBytes is { Length: > 0 } || !string.IsNullOrWhiteSpace(TitleText);

    public bool HasAny =>
        HasTitle ||
        ShowScaleBar ||
        ShowGraticule ||
        PrimaryLogoPngBytes is { Length: > 0 } ||
        SecondaryLogoPngBytes is { Length: > 0 };
}