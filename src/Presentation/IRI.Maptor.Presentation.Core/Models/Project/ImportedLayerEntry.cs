using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Persistence.Abstractions;

namespace IRI.Maptor.Presentation.Core.Models.Project;

/// <summary>
/// One imported data file (or pasted raw data). An import that produced a group layer
/// (KML, DXF, GeoPackage, ...) is still a single entry; the state of each created
/// sub-layer lives in <see cref="LayerStates"/>.
/// </summary>
public class ImportedLayerEntry
{
    /// <summary>
    /// Discriminator used to pick the import routine on load.
    /// </summary>
    public DataSourceKind Kind { get; set; }

    /// <summary>
    /// Where the data came from, as captured at save time (absolute).
    /// Null for pasted raw data (see <see cref="ImportOptions.RawText"/>).
    /// </summary>
    public SourceLocation? Location { get; set; }

    /// <summary>
    /// The <see cref="Location"/> path relative to the project file directory, when
    /// derivable. Tried first on load so a project moved together with its data
    /// folder keeps working.
    /// </summary>
    public string? RelativePath { get; set; }

    public ImportOptions? Options { get; set; }

    /// <summary>
    /// State of each non-group layer created by this import, matched by layer name on load.
    /// </summary>
    public List<LayerStateEntry> LayerStates { get; set; } = [];

    /// <summary>
    /// State of each group layer created by this import (the DXF/KML parent, the
    /// GeoPackage vector and tiles groups), matched by layer name on load.
    /// <see cref="LayerStateEntry.SldXml"/> is always null for a group.
    /// </summary>
    public List<LayerStateEntry> GroupStates { get; set; } = [];
}
