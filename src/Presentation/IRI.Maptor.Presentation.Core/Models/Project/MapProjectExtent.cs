namespace IRI.Maptor.Presentation.Core.Models.Project;

/// <summary>
/// A map extent in Web Mercator. Kept as its own DTO so the project file format
/// does not depend on the serialization shape of runtime geometry types.
/// </summary>
public class MapProjectExtent
{
    public double XMin { get; set; }

    public double YMin { get; set; }

    public double XMax { get; set; }

    public double YMax { get; set; }
}
