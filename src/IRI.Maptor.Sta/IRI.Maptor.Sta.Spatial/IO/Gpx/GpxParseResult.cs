namespace IRI.Maptor.Sta.Common.IO.Gpx;

/// <summary>
/// Result of parsing a GPX file, containing waypoints, routes, tracks, and metadata.
/// </summary>
public sealed class GpxParseResult
{
    public List<GpxWaypoint> Waypoints { get; }
    public List<GpxRoute> Routes { get; }
    public List<GpxTrack> Tracks { get; }
    public GpxMetadata? Metadata { get; }

    public GpxParseResult(List<GpxWaypoint> waypoints, List<GpxRoute> routes, List<GpxTrack> tracks, GpxMetadata? metadata)
    {
        Waypoints = waypoints ?? new List<GpxWaypoint>();
        Routes = routes ?? new List<GpxRoute>();
        Tracks = tracks ?? new List<GpxTrack>();
        Metadata = metadata;
    }
}
