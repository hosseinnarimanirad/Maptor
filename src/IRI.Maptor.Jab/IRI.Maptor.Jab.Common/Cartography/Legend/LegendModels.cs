using System.Collections.Generic;
using System.Windows.Media.Imaging;

using IRI.Maptor.Sta.Ogc.SLD;

namespace IRI.Maptor.Jab.Common.Cartography.Legend;

/// <summary>
/// A rendered, fully-managed description of a layer's SLD symbology, suitable for binding into a
/// WPF legend panel. Built from a <see cref="StyledLayerDescriptor"/> by <c>SldLegendBuilder</c>.
/// Holds no unmanaged (GDI+) resources — swatches are frozen <see cref="BitmapImage"/>s.
/// </summary>
public class SymbologyLegend
{
    public List<LegendStyleGroup> Groups { get; set; } = new();

    /// <summary>True when there is nothing to show (no rules across all groups).</summary>
    public bool IsEmpty
    {
        get
        {
            foreach (var group in Groups)
            {
                if (group.Rows.Count > 0)
                    return false;
            }

            return true;
        }
    }
}

/// <summary>One FeatureTypeStyle (per layer / style) — a header plus its rule rows.</summary>
public class LegendStyleGroup
{
    /// <summary>Layer or style title used as the group header (may be null when unnamed).</summary>
    public string? Header { get; set; }

    public List<LegendRuleRow> Rows { get; set; } = new();
}

/// <summary>One SLD <see cref="Rule"/> — a symbol swatch plus its label, filter and scale text.</summary>
public class LegendRuleRow
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Readable rule filter (e.g. "type = primary"); null when the rule has no filter.</summary>
    public string? FilterText { get; set; }

    /// <summary>Readable rule scale range (e.g. "1:1k–1:500k"); null when unbounded.</summary>
    public string? ScaleText { get; set; }

    /// <summary>Rendered symbol swatch for the WPF panel (frozen, may be null on render failure).</summary>
    public BitmapImage? SwatchImage { get; set; }

    /// <summary>
    /// The source SLD rule this row was built from. Kept (managed, cheap) so the PNG exporter can
    /// re-render the swatch as a <see cref="System.Drawing.Bitmap"/> without re-traversing the tree.
    /// </summary>
    public Rule? Rule { get; set; }
}
