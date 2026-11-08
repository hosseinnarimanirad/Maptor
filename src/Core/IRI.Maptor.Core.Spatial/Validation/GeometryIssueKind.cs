namespace IRI.Maptor.Core.Spatial.Validation;

/// <summary>
/// A problem found in a feature's geometry by <see cref="GeometryValidator"/>.
/// </summary>
public enum GeometryIssueKind
{
    /// <summary>The feature has no geometry, or an empty one. Cannot be repaired.</summary>
    NoGeometry,

    /// <summary>A polygon ring repeats its first point at the end. Repair: remove the repeated point(s).</summary>
    ClosedRing,

    /// <summary>The same point appears twice or more in a row. Repair: keep one copy.</summary>
    RepeatedPoint,

    /// <summary>
    /// A ring has fewer than 3 distinct points, a line fewer than 2, or a point none.
    /// Repair: drop that part; the feature is removed when nothing is left.
    /// </summary>
    TooFewPoints,

    /// <summary>A ring whose points all lie on one line (zero area). Repair: drop that ring.</summary>
    ZeroAreaRing,

    /// <summary>A coordinate is NaN or infinite. Cannot be repaired.</summary>
    InvalidCoordinate,

    /// <summary>A longitude/latitude outside ±180/±90 in a geodetic (WGS 84) geometry. Cannot be repaired.</summary>
    CoordinateOutOfRange,

    /// <summary>
    /// A ring wound the wrong way (outer rings counterclockwise, holes clockwise).
    /// Always fixed automatically; reported for information only.
    /// </summary>
    WrongRingOrientation,
}

public static class GeometryIssueKindExtensions
{
    /// <summary>True when <see cref="GeometryRepair"/> can repair the problem (possibly by dropping a part).</summary>
    public static bool CanFix(this GeometryIssueKind kind) => kind switch
    {
        GeometryIssueKind.NoGeometry => false,
        GeometryIssueKind.InvalidCoordinate => false,
        GeometryIssueKind.CoordinateOutOfRange => false,
        _ => true,
    };

    /// <summary>True for problems that are always fixed without asking the user.</summary>
    public static bool IsFixedAutomatically(this GeometryIssueKind kind) => kind == GeometryIssueKind.WrongRingOrientation;
}
