using Drawing = System.Drawing;

namespace IRI.Maptor.Jab.Common.Cartography.Legend;

/// <summary>
/// Layout / styling options for rendering a <see cref="SymbologyLegend"/> — both the per-rule
/// swatch bitmaps (WPF panel) and the composed PNG export.
/// </summary>
public class SldLegendOptions
{
    /// <summary>Width of a single symbol swatch cell, in pixels.</summary>
    public int SwatchWidth { get; set; } = 32;

    /// <summary>Height of a single symbol swatch cell, in pixels.</summary>
    public int SwatchHeight { get; set; } = 24;

    /// <summary>Inner padding inside a swatch cell, in pixels.</summary>
    public int SwatchPadding { get; set; } = 4;

    /// <summary>Vertical gap between rows in the composed PNG, in pixels.</summary>
    public int RowSpacing { get; set; } = 6;

    /// <summary>Horizontal gap between a swatch and its label, in pixels.</summary>
    public int SwatchTextGap { get; set; } = 8;

    /// <summary>Outer padding around the composed PNG, in pixels.</summary>
    public int Padding { get; set; } = 8;

    /// <summary>Background of the composed PNG. Use <see cref="Drawing.Color.Transparent"/> for a transparent legend.</summary>
    public Drawing.Color Background { get; set; } = Drawing.Color.White;

    public string FontFamily { get; set; } = "Segoe UI";

    public float FontSize { get; set; } = 10f;

    public Drawing.Color TextColor { get; set; } = Drawing.Color.Black;

    /// <summary>Draw a bold group header (layer / style title) above each group of rules.</summary>
    public bool ShowGroupHeaders { get; set; } = true;

    /// <summary>Append a readable rendering of the rule filter (e.g. "type = primary").</summary>
    public bool ShowFilterText { get; set; } = true;

    /// <summary>Append a readable rendering of the rule scale range (e.g. "1:1k–1:500k").</summary>
    public bool ShowScaleText { get; set; } = true;

    /// <summary>Right-to-left layout for Persian / Arabic labels.</summary>
    public bool IsRtl { get; set; } = false;

    public static SldLegendOptions Default => new();
}
