namespace IRI.Maptor.Presentation.Core.Models.Project;

/// <summary>
/// One geometry drawing item. Text drawing items are not persisted.
/// </summary>
public class DrawingItemEntry
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The drawing item serialized as a GeoJSON feature with its SLD embedded
    /// as an attribute (the same format as *.mtd drawing files).
    /// </summary>
    public string SerializedFeature { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public double Opacity { get; set; } = 1;
}
