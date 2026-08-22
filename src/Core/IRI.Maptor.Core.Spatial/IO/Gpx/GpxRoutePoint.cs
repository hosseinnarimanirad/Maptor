namespace IRI.Maptor.Core.Spatial.IO.Gpx;

/// <summary>
/// Represents a GPX route point (rtept) - a waypoint in a route.
/// </summary>
[Serializable]
public class GpxRoutePoint
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double? Elevation { get; set; }

    public DateTime? Time { get; set; }

    public decimal? MagVar { get; set; }

    public decimal? GeoidHeight { get; set; }

    public string? Name { get; set; }

    public string? Comment { get; set; }

    public string? Description { get; set; }

    public string? Source { get; set; }

    public string? Symbol { get; set; }

    public string? Type { get; set; }

    public string? Fix { get; set; }

    public int? Sat { get; set; }

    public decimal? Hdop { get; set; }

    public decimal? Vdop { get; set; }

    public decimal? Pdop { get; set; }

    public int? AgeOfDgpsData { get; set; }

    public int? DgpsId { get; set; }

    public List<GpxLink>? Links { get; set; }
}
