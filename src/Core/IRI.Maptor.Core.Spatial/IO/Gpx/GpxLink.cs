namespace IRI.Maptor.Core.Spatial.IO.Gpx;

/// <summary>
/// Represents a GPX link to an external resource.
/// </summary>
public class GpxLink
{
    public string Href { get; set; } = string.Empty;

    public string? Text { get; set; }

    public string? Type { get; set; }
}
