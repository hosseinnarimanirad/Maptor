using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Decides where a point snaps to among the candidates of a <see cref="SnapIndex{T}"/>.
/// <para>
/// A vertex within the tolerance wins over an edge, even over a nearer edge, so a click near a corner lands on
/// the corner. Otherwise the nearest place on an edge within the tolerance wins. With neither, there is no snap
/// and the caller keeps its own point. Tolerance and distances are in coordinate units: a map converts its pixel
/// tolerance before calling.
/// </para>
/// </summary>
public static class SnapEngine
{
    /// <param name="index">The candidates.</param>
    /// <param name="point">The point to snap, in the index's coordinates.</param>
    /// <param name="tolerance">The largest distance to snap over, in coordinate units; zero snaps only an exact hit.</param>
    /// <param name="options">Which kinds to snap to; both when null.</param>
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

        if (options.SnapToVertices && index.NearestVertex(point, tolerance, accept) is { } vertex)
            return vertex;

        if (options.SnapToEdges)
            return index.NearestEdge(point, tolerance, accept);

        return null;
    }
}
