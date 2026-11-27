using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>What a point snapped to.</summary>
public enum SnapKind
{
    /// <summary>A vertex of a line or polygon, or a point feature.</summary>
    Vertex = 1,

    /// <summary>The nearest place on a segment of a line or polygon boundary.</summary>
    Edge = 2,

    /// <summary>The first or last vertex of a line (of each part of a multi-part line). Polygons have none.</summary>
    End = 3,

    /// <summary>The middle of a segment of a line or polygon boundary.</summary>
    Midpoint = 4,

    /// <summary>
    /// Where two segments meet in a single point with no shared vertex there: a crossing, or one segment's end on the
    /// inside of the other. Two segments that only share an end vertex do not count (that is a vertex).
    /// </summary>
    Intersection = 5,
}

/// <summary>Where a point snapped to, and what it snapped to.</summary>
public sealed class SnapResult<T> where T : IPoint, new()
{
    public SnapResult(T location, SnapKind kind, double distance, int geometryIndex)
    {
        Location = location;
        Kind = kind;
        Distance = distance;
        GeometryIndex = geometryIndex;
    }

    /// <summary>The snapped point: the vertex itself, or a new point on an edge (edge, midpoint, intersection).</summary>
    public T Location { get; }

    public SnapKind Kind { get; }

    /// <summary>From the input point to <see cref="Location"/>, in coordinate units.</summary>
    public double Distance { get; }

    /// <summary>
    /// The position of the snapped geometry in the list the <see cref="SnapIndex{T}"/> was built from; for an
    /// intersection, that of the first of the two geometries.
    /// </summary>
    public int GeometryIndex { get; }
}
