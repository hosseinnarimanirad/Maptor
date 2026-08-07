namespace IRI.Maptor.Jab.Core.Models.Project;

/// <summary>
/// Persisted state of one layer created by an import. Matched by
/// <see cref="LayerName"/> on load, since layer ids are regenerated every session.
/// </summary>
public class LayerStateEntry
{
    public string LayerName { get; set; } = string.Empty;

    /// <summary>
    /// Position among siblings in the table of contents; higher is nearer the top.
    /// Restored verbatim on load. Zero in project files written before ordering was
    /// persisted, which the loader detects and ignores.
    /// </summary>
    public int TocOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public double Opacity { get; set; } = 1;

    /// <summary>
    /// Symbology as SLD XML, when the layer supports it; null for raster layers.
    /// </summary>
    public string? SldXml { get; set; }
}
