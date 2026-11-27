using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Decides where a point snaps to among the candidates of a <see cref="SnapIndex{T}"/>.
/// <para>
/// The kinds are tried in a fixed order, and the first kind with a candidate within the tolerance wins, even over a
/// nearer candidate of a later kind: line end, vertex, intersection, midpoint, edge. So a stored point always beats a
/// computed one, and a click near a corner lands on the corner. Within one kind the nearest candidate wins. With
/// nothing in range there is no snap and the caller keeps its own point. Tolerance and distances are in coordinate
/// units: a map converts its pixel tolerance before calling.
/// </para>
/// <para>
/// A line end is its own kind only while <see cref="SnapOptions.SnapToEnds"/> is on; otherwise it is an ordinary
/// vertex (and snapped to as one when vertices are on).
/// </para>
/// </summary>
public static class SnapEngine
{
    /// <param name="index">The candidates.</param>
    /// <param name="point">The point to snap, in the index's coordinates.</param>
    /// <param name="tolerance">The largest distance to snap over, in coordinate units; zero snaps only an exact hit.</param>
    /// <param name="options">Which kinds to snap to; all when null.</param>
    /// <param name="accept">
    /// Which geometries (by their position in the index's list) may be snapped to; all when null. A caller that edits
    /// one of them leaves it out, so its vertices do not pull the edited vertex back.
    /// </param>
    /// <returns>The snap, or null when nothing is within <paramref name="tolerance"/>.</returns>
    public static SnapResult<T>? Snap<T>(SnapIndex<T> index, T point, double tolerance, SnapOptions? options = null, Func<int, bool>? accept = null)
        where T : IPoint, new()
    {
        if (index is null)
            throw new ArgumentNullException(nameof(index));

        if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be a finite number, zero or more.");

        if (point is null || double.IsNaN(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.X) || double.IsInfinity(point.Y))
            return null;

        options ??= SnapOptions.Default;

        if (options.SnapToEnds && index.NearestVertex(point, tolerance, accept, endsOnly: true) is { } end)
            return end;

        // With ends off, a line end found here is an ordinary vertex.
        if (options.SnapToVertices && index.NearestVertex(point, tolerance, accept) is { } vertex)
            return vertex.Kind == SnapKind.Vertex ? vertex : new SnapResult<T>(vertex.Location, SnapKind.Vertex, vertex.Distance, vertex.GeometryIndex);

        if (options.SnapToIntersections && index.NearestIntersection(point, tolerance, accept) is { } intersection)
            return intersection;

        if (options.SnapToMidpoints && index.NearestMidpoint(point, tolerance, accept) is { } midpoint)
            return midpoint;

        if (options.SnapToEdges)
            return index.NearestEdge(point, tolerance, accept);

        return null;
    }
}
