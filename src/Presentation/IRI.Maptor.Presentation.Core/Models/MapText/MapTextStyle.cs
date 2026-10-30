namespace IRI.Maptor.Presentation.Core.Models.MapText;

/// <summary>
/// How a text-on-map label is framed.
/// </summary>
public enum MapTextFrame
{
    /// <summary>Coloured frame around a white text field (the original look).</summary>
    Box,

    /// <summary>Only the text, no frame or field.</summary>
    Plain,

    /// <summary>Only the text, with a contrasting outline so it reads on any background.</summary>
    Halo,
}

/// <summary>
/// Text alignment relative to the text's flow direction; mirrors the names of
/// System.Windows.TextAlignment, so under right-to-left Left renders on the right edge.
/// </summary>
public enum MapTextAlignment
{
    Left,
    Right,
    Center,
    Justify,
}

/// <summary>
/// The formatting of a text-on-map label as a plain DTO (no WPF types), so it can be
/// serialized as is. Colours are #AARRGGBB strings.
/// </summary>
public class MapTextStyle
{
    public MapTextFrame Frame { get; set; } = MapTextFrame.Box;

    public string FontFamilyName { get; set; } = "IRANSans";

    public double FontSize { get; set; } = 14;

    public bool IsBold { get; set; }

    public bool IsItalic { get; set; }

    public bool IsUnderline { get; set; }

    public MapTextAlignment Alignment { get; set; } = MapTextAlignment.Left;

    public bool IsRightToLeft { get; set; } = true;

    public string TextColor { get; set; } = "#FF000000";

    /// <summary>
    /// Halo outline colour; null means automatic (contrasting with <see cref="TextColor"/>).
    /// </summary>
    public string? HaloColor { get; set; }

    /// <summary>
    /// Box frame fill; null means the theme highlight brush.
    /// </summary>
    public string? BackgroundColor { get; set; }
}
