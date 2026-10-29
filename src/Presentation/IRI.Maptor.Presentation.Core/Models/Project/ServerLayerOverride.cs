namespace IRI.Maptor.Presentation.Core.Models.Project;

/// <summary>
/// Project-level override for a layer that is loaded from a server by the host
/// application (not re-created from the project file). Matched by the stable
/// server layer id (AuxilaryId) and applied after the server layers are loaded.
/// </summary>
public class ServerLayerOverride
{
    public int AuxilaryId { get; set; }

    public bool IsVisible { get; set; } = true;

    public double Opacity { get; set; } = 1;

    /// <summary>
    /// Optional symbology override as SLD XML; null keeps the server-provided symbology.
    /// </summary>
    public string? SldXml { get; set; }

    /// <summary>
    /// The SLD style the layer was drawn with (one of the named styles of its SLD); null for the default.
    /// Restored when the SLD still has it, else the default style is drawn.
    /// </summary>
    public string? ActiveStyleName { get; set; }
}
