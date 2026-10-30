namespace IRI.Maptor.Presentation.Core.Models.Project;

/// <summary>
/// One drawing item: a geometry drawing, or a text label (a point whose SLD holds only a
/// TextSymbolizer, with the text in its Label attribute).
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
