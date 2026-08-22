namespace IRI.Maptor.Core.Spatial.IO.Gpx;

/// <summary>
/// Represents a GPX route (rte) - an ordered list of waypoints representing a planned path.
/// </summary>
[Serializable]
public class GpxRoute
{
    public string? Name { get; set; }

    public string? Comment { get; set; }

    public string? Description { get; set; }

    public string? Source { get; set; }

    public List<GpxLink>? Links { get; set; }

    public int? Number { get; set; }

    public string? Type { get; set; }

    public List<GpxRoutePoint> RoutePoints { get; set; } = [];
}
