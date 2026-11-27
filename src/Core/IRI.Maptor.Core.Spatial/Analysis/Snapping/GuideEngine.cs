using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>What a guide gave for a point.</summary>
public enum GuideHit
{
    /// <summary>The nearest point on the guide.</summary>
    OnGuide = 1,

    /// <summary>Where the guide meets an edge of an existing geometry.</summary>
    Crossing = 2,

    /// <summary>
    /// For a perpendicular built from a reference segment: the point on that segment where the perpendicular
    /// meets it, so a segment drawn to it stands at 90° on the reference.
    /// </summary>
    PerpendicularFoot = 3,
}

/// <summary>Where a guide puts a point.</summary>
public sealed class GuideResult<T> where T : IPoint, new()
{
    public GuideResult(T location, GuideLine<T> guide, GuideHit hit, double distance, int geometryIndex = -1)
    {
        Location = location;
        Guide = guide;
        Hit = hit;
        Distance = distance;
        GeometryIndex = geometryIndex;
    }

    public T Location { get; }

    public GuideLine<T> Guide { get; }

    public GuideHit Hit { get; }

    /// <summary>From the input point to <see cref="Location"/>, in coordinate units.</summary>
    public double Distance { get; }

    /// <summary>For a crossing, the position of the crossed geometry in the <see cref="SnapIndex{T}"/>'s list; else -1.</summary>
    public int GeometryIndex { get; }
}

/// <summary>
/// Finds what a set of guides offers for a point. The results are tried in a fixed order and the first kind with a
/// candidate within the tolerance wins: the perpendicular's foot on its reference segment, then a guide's crossing
/// with an existing edge, then the nearest point on a guide. Within a kind the nearest wins. A ray offers nothing
/// behind its origin. Tolerance and distances are in coordinate units.
/// <para>
/// It does not build guides (<see cref="GuideBuilder"/>) and does not weigh guides against snap targets: the
/// caller does, with <see cref="SnapEngine"/>.
/// </para>
/// </summary>
public static class GuideEngine
{
    private const double Parallel = 1e-12;

    /// <param name="guides">The guides in force.</param>
    /// <param name="point">The point to place, in the guides' coordinates.</param>
    /// <param name="tolerance">The largest distance from <paramref name="point"/> to a result.</param>
    /// <param name="index">The existing geometries a guide may cross; none when null.</param>
    /// <param name="accept">Which of the index's geometries count; all when null.</param>
    public static GuideResult<T>? Find<T>(IReadOnlyList<GuideLine<T>> guides, T point, double tolerance, SnapIndex<T>? index = null, Func<int, bool>? accept = null)
        where T : IPoint, new()
    {
        if (guides is null)
            throw new ArgumentNullException(nameof(guides));

        if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be a finite number, zero or more.");

        if (guides.Count == 0 || point is null || double.IsNaN(point.X) || double.IsNaN(point.Y))
            return null;

        return Foot(guides, point, tolerance)
            ?? Crossing(guides, point, tolerance, index, accept)
            ?? Nearest(guides, point, tolerance);
    }

    /// <summary>
    /// The nearest point on the nearest of <paramref name="guides"/>, whatever the distance: for a direction that is
    /// locked (the angle steps while a key is held), where the point must lie on a guide. Null only without guides.
    /// </summary>
    public static GuideResult<T>? Nearest<T>(IReadOnlyList<GuideLine<T>> guides, T point) where T : IPoint, new()
        => Nearest(guides, point, double.PositiveInfinity);

    private static GuideResult<T>? Nearest<T>(IReadOnlyList<GuideLine<T>> guides, T point, double tolerance) where T : IPoint, new()
    {
        GuideResult<T>? best = null;

        foreach (var guide in guides)
        {
            var along = guide.Along(point);

            // a ray has no back side; when the tolerance is unbounded its origin stands in for it
            if (guide.IsRay && along < 0)
            {
                if (!double.IsPositiveInfinity(tolerance))
                    continue;

                along = 0;
            }

            var location = guide.At(along);
            var distance = Distance(location, point);

            if (distance > tolerance || (best is not null && distance >= best.Distance))
                continue;

            best = new GuideResult<T>(location, guide, GuideHit.OnGuide, distance);
        }

        return best;
    }

    private static GuideResult<T>? Foot<T>(IReadOnlyList<GuideLine<T>> guides, T point, double tolerance) where T : IPoint, new()
    {
        GuideResult<T>? best = null;

        foreach (var guide in guides)
        {
            if (guide.Kind != GuideKind.Perpendicular || !guide.HasReference)
                continue;

            if (!Meet(guide, guide.ReferenceStart!, guide.ReferenceEnd!, out var along))
                continue;

            var location = guide.At(along);
            var distance = Distance(location, point);

            if (distance > tolerance || (best is not null && distance >= best.Distance))
                continue;

            best = new GuideResult<T>(location, guide, GuideHit.PerpendicularFoot, distance);
        }

        return best;
    }

    private static GuideResult<T>? Crossing<T>(IReadOnlyList<GuideLine<T>> guides, T point, double tolerance, SnapIndex<T>? index, Func<int, bool>? accept)
        where T : IPoint, new()
    {
        if (index is null)
            return null;

        GuideResult<T>? best = null;

        List<(T A, T B, int GeometryIndex)>? edges = null;

        foreach (var guide in guides)
        {
            // a crossing within the tolerance lies on a guide that is within the tolerance
            var along = guide.Along(point);

            if (Distance(guide.At(along), point) > tolerance)
                continue;

            edges ??= index.EdgesNear(point, tolerance, accept).ToList();

            foreach (var edge in edges)
            {
                if (!Meet(guide, edge.A, edge.B, out var at))
                    continue;

                var location = guide.At(at);
                var distance = Distance(location, point);

                if (distance > tolerance || (best is not null && distance >= best.Distance))
                    continue;

                best = new GuideResult<T>(location, guide, GuideHit.Crossing, distance, edge.GeometryIndex);
            }
        }

        return best;
    }

    // Where the guide meets the segment a→b, as a distance along the guide; false when they are parallel, when
    // the meeting point is off the segment, or behind a ray's origin.
    private static bool Meet<T>(GuideLine<T> guide, T a, T b, out double along) where T : IPoint, new()
    {
        along = 0;

        var sx = b.X - a.X;
        var sy = b.Y - a.Y;

        var length = Math.Sqrt(sx * sx + sy * sy);

        if (length <= 0)
            return false;

        var denominator = guide.DirectionX * sy - guide.DirectionY * sx;

        if (Math.Abs(denominator) <= Parallel * length)
            return false;

        var qx = a.X - guide.Origin.X;
        var qy = a.Y - guide.Origin.Y;

        along = (qx * sy - qy * sx) / denominator;

        var onSegment = (qx * guide.DirectionY - qy * guide.DirectionX) / denominator;

        const double slack = 1e-12;

        if (onSegment < -slack || onSegment > 1 + slack)
            return false;

        return !guide.IsRay || along >= 0;
    }

    private static double Distance<T>(T a, T b) where T : IPoint
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}
