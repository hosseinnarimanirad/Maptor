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
    /// Pre-rendered title image (script/RTL-safe raster). Fallback if <see cref="TitleVector"/> is null.
    /// </summary>
    public byte[]? TitlePngBytes { get; set; }

    /// <summary>
    /// Title as filled vector glyph outlines (preferred — crisp, resolution-independent, RTL-safe).
    /// Wins over <see cref="TitlePngBytes"/> and <see cref="TitleText"/>.
    /// </summary>
    public PdfVectorLogo? TitleVector { get; set; }

    public bool ShowScaleBar { get; set; }

    public bool ShowGraticule { get; set; }

    /// <summary>
    /// Right-to-left sheet: mirrors the side columns (legend on the right, company on the left)
    /// and their bottom cells. The map, title box and scale bar stay centered; graticule labels
    /// stay geographic (west on the left). Set from the app's current culture.
    /// </summary>
    public bool RightToLeft { get; set; }

    /// <summary>
    /// Render numeric text (graticule coordinates, scale bar) with Persian/Farsi digits (۰–۹).
    /// The date/time and any RTL title text are localized upstream when built.
    /// </summary>
    public bool UsePersianDigits { get; set; }

    /// <summary>
    /// Graticule spacing in degrees; null to choose automatically from the extent span
    /// </summary>
    public double? GraticuleIntervalDegrees { get; set; }

    /// <summary>
    /// Producer icon (e.g. Maptor logo), drawn in the footer band
    /// </summary>
    public byte[]? PrimaryLogoPngBytes { get; set; }

    /// <summary>
    /// Producer brand mark as vector (preferred over <see cref="PrimaryLogoPngBytes"/> when set)
    /// </summary>
    public PdfVectorLogo? PrimaryVectorLogo { get; set; }

    /// <summary>
    /// Map producer's company logo, drawn at the top of the right (company) column
    /// </summary>
    public byte[]? SecondaryLogoPngBytes { get; set; }

    /// <summary>
    /// Company title drawn as PDF text (Latin only). For Persian/Arabic use
    /// <see cref="CompanyTitlePngBytes"/> instead.
    /// </summary>
    public string? CompanyTitleText { get; set; }

    /// <summary>
    /// Pre-rendered company-title raster (script/RTL-safe). Fallback if <see cref="CompanyTitleVector"/> is null.
    /// </summary>
    public byte[]? CompanyTitlePngBytes { get; set; }

    /// <summary>
    /// Company title as filled vector glyph outlines (preferred — crisp, RTL-safe).
    /// </summary>
    public PdfVectorLogo? CompanyTitleVector { get; set; }

    /// <summary>
    /// Company subtitle drawn as PDF text (Latin only); RTL-safe counterpart is
    /// <see cref="CompanySubtitlePngBytes"/>.
    /// </summary>
    public string? CompanySubtitleText { get; set; }

    /// <summary>
    /// Pre-rendered company-subtitle raster (script/RTL-safe). Fallback if <see cref="CompanySubtitleVector"/> is null.
    /// </summary>
    public byte[]? CompanySubtitlePngBytes { get; set; }

    /// <summary>
    /// Company subtitle as filled vector glyph outlines (preferred — crisp, RTL-safe).
    /// </summary>
    public PdfVectorLogo? CompanySubtitleVector { get; set; }

    /// <summary>
    /// Reserve the left (legend) column with a bordered box + header. Legend items
    /// are not drawn yet; this just carves out the standard three-column layout.
    /// </summary>
    public bool ShowLegendColumn { get; set; }

    /// <summary>
    /// Pre-rendered "Legend" header raster (RTL-safe). Fallback if <see cref="LegendHeaderVector"/> is null.
    /// </summary>
    public byte[]? LegendHeaderPngBytes { get; set; }

    /// <summary>
    /// "Legend" header as filled vector glyph outlines (preferred — crisp, RTL-safe).
    /// </summary>
    public PdfVectorLogo? LegendHeaderVector { get; set; }

    /// <summary>
    /// Export date/time text (Latin digits) in the bottom-left cell; null to omit.
    /// Drawn as vector glyphs when <see cref="DateTimeVector"/> is set, else as embedded-font PDF text.
    /// </summary>
    public string? DateTimeText { get; set; }

    /// <summary>
    /// Export date/time as filled vector glyph outlines (preferred over <see cref="DateTimeText"/>).
    /// </summary>
    public PdfVectorLogo? DateTimeVector { get; set; }

    /// <summary>
    /// TTF bytes for graticule/scale-bar labels; embedded via a font resolver.
    /// Null falls back to <see cref="LabelFontFamily"/> resolved from system fonts.
    /// </summary>
    public byte[]? LabelFontBytes { get; set; }

    public string LabelFontFamily { get; set; } = "Arial";

    public bool HasTitle =>
        TitleVector is { IsValid: true } || TitlePngBytes is { Length: > 0 } || !string.IsNullOrWhiteSpace(TitleText);

    /// <summary>
    /// The right column has content: company logo, title, or subtitle.
    /// </summary>
    public bool HasCompanyInfo =>
        SecondaryLogoPngBytes is { Length: > 0 } ||
        CompanyTitleVector is { IsValid: true } || CompanyTitlePngBytes is { Length: > 0 } || !string.IsNullOrWhiteSpace(CompanyTitleText) ||
        CompanySubtitleVector is { IsValid: true } || CompanySubtitlePngBytes is { Length: > 0 } || !string.IsNullOrWhiteSpace(CompanySubtitleText);

    /// <summary>
    /// The bottom band has content: scale bar, Maptor logo, or a date/time stamp.
    /// </summary>
    public bool HasBottomBand =>
        ShowScaleBar ||
        PrimaryLogoPngBytes is { Length: > 0 } ||
        PrimaryVectorLogo is { IsValid: true } ||
        DateTimeVector is { IsValid: true } ||
        !string.IsNullOrWhiteSpace(DateTimeText);

    public bool HasAny =>
        HasTitle ||
        ShowGraticule ||
        ShowLegendColumn ||
        HasCompanyInfo ||
        HasBottomBand;
}