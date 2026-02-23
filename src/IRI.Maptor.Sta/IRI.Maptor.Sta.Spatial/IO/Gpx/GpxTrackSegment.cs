namespace IRI.Maptor.Sta.Common.IO.Gpx;

/// <summary>
/// Represents a GPX track segment (trkseg) - a continuous sequence of track points.
/// </summary>
public class GpxTrackSegment
{
    public List<GpxTrackPoint> TrackPoints { get; set; } = [];

    public GpxTrackSegment() { }

    public GpxTrackSegment(List<GpxTrackPoint> trackPoints)
    {
        TrackPoints = trackPoints ?? [];
    }
}
