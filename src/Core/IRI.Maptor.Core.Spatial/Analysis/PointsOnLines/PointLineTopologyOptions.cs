namespace IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;

/// <summary>Settings for <see cref="PointLineTopologyBuilder{T}"/>.</summary>
public sealed class PointLineTopologyOptions
{
    /// <summary>
    /// A point belongs to every line it lies within this distance of. Metres when the data is
    /// geodetic (WGS84 longitude/latitude), coordinate units otherwise.
    /// </summary>
    public double Tolerance { get; set; } = 0.5;

    /// <summary>
    /// Two points of one line closer than this along the line are reported as coincident
    /// (metres when geodetic, coordinate units otherwise).
    /// </summary>
    public double CoincidentTolerance { get; set; } = 0.01;

    /// <summary>
    /// When sequence numbers are given, order each line so that they rise; otherwise the points
    /// keep the direction the line was drawn in.
    /// </summary>
    public bool OrientBySequence { get; set; } = true;

    /// <summary>
    /// Whether coordinates are geodetic WGS84 (longitude, latitude in degrees). Null = decided from
    /// the SRID of the first usable line.
    /// </summary>
    public bool? IsGeodetic { get; set; }

    /// <summary>
    /// Cell size of the spatial grid that finds the line segments near a point, in coordinate units.
    /// Null = 0.01° for geodetic data, 1000 units otherwise. Affects speed only, never the result.
    /// </summary>
    public double? GridCellSize { get; set; }

    public static PointLineTopologyOptions Default => new PointLineTopologyOptions();
}
