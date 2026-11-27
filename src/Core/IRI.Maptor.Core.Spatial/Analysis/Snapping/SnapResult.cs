using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>What a point snapped to.</summary>
public enum SnapKind
{
    /// <summary>A vertex of a line or polygon, or a point feature.</summary>
    Vertex = 1,

    /// <summary>The nearest place on a segment of a line or polygon boundary.</summary>
    Edge = 2,
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

    /// <summary>The snapped point: the vertex itself, or a new point on the edge.</summary>
    public T Location { get; }

    public SnapKind Kind { get; }

    /// <summary>From the input point to <see cref="Location"/>, in coordinate units.</summary>
    public double Distance { get; }

    /// <summary>The position of the snapped geometry in the list the <see cref="SnapIndex{T}"/> was built from.</summary>
    public int GeometryIndex { get; }
}
