namespace IRI.Maptor.Sta.Common.IO.Gpx;

/// <summary>
/// Represents GPX file-level metadata.
/// </summary>
public class GpxMetadata
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public GpxPerson? Author { get; set; }

    public GpxCopyright? Copyright { get; set; }

    public List<GpxLink>? Links { get; set; }

    public DateTime? Time { get; set; }

    public string? Keywords { get; set; }

    public GpxBounds? Bounds { get; set; }
}

/// <summary>
/// Represents a person or organization in GPX metadata.
/// </summary>
public class GpxPerson
{
    public string? Name { get; set; }

    public GpxEmail? Email { get; set; }

    public GpxLink? Link { get; set; }
}

/// <summary>
/// Represents an email address (id@domain) in GPX.
/// </summary>
public class GpxEmail
{
    public string Id { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;
}

/// <summary>
/// Represents copyright information.
/// </summary>
public class GpxCopyright
{
    public string Author { get; set; } = string.Empty;

    public int? Year { get; set; }

    public string? License { get; set; }
}

/// <summary>
/// Represents geographic bounds (minlat, minlon, maxlat, maxlon).
/// </summary>
public class GpxBounds
{
    public double MinLat { get; set; }

    public double MinLon { get; set; }

    public double MaxLat { get; set; }

    public double MaxLon { get; set; }
}
