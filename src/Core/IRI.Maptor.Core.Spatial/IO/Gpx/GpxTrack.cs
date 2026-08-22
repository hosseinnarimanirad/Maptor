namespace IRI.Maptor.Core.Spatial.IO.Gpx;

/// <summary>
/// Represents a GPX track (trk) - an ordered list of points describing a path.
/// </summary>
[Serializable]
public class GpxTrack
{
    public string? Name { get; set; }

    public string? Comment { get; set; }

    public string? Description { get; set; }

    public string? Source { get; set; }

    public List<GpxLink>? Links { get; set; }

    public int? Number { get; set; }

    public string? Type { get; set; }

    public List<GpxTrackSegment> Segments { get; set; } = [];

    public GpxTrack() { }

    public GpxTrack(string? name, List<GpxTrackSegment> segments)
    {
        Name = name;
        Segments = segments ?? [];
    }
}
