namespace IRI.Maptor.Jab.Core.Models.Project;

/// <summary>
/// Root model of a map project file (*.mtproj): the imported data files with their
/// import options and layer states, the drawing items, the last map view and base map,
/// and per-app server layer overrides.
/// </summary>
public class MapProject
{
    /// <summary>
    /// Version of the project file format. Loaders must reject files
    /// with a higher version than they understand.
    /// </summary>
    public int FormatVersion { get; set; } = 1;

    public string? Name { get; set; }

    public DateTime SavedOnUtc { get; set; }

    /// <summary>
    /// Last map view extent (Web Mercator).
    /// </summary>
    public MapProjectExtent? Extent { get; set; }

    /// <summary>
    /// Full name of the selected base map provider (e.g. "Google-Hybrid").
    /// </summary>
    public string? BaseMapFullName { get; set; }

    public double BaseMapOpacity { get; set; } = 1;

    public List<ImportedLayerEntry> ImportedLayers { get; set; } = [];

    public List<DrawingItemEntry> DrawingItems { get; set; } = [];

    public List<ServerLayerOverride> ServerLayerOverrides { get; set; } = [];
}
