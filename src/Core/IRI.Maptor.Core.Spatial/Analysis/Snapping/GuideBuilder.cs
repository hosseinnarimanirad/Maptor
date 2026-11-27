using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Makes the guides for a drawing situation: from the segment just drawn, from an existing segment the user picked
/// as a reference, or the fixed angle steps. It does not choose among them (<see cref="GuideEngine"/>).
/// A segment of no length gives no guides.
/// </summary>
public static class GuideBuilder
{
    /// <summary>
    /// From the segment just drawn, <paramref name="previous"/>→<paramref name="last"/>: its extension beyond
    /// <paramref name="last"/> (a ray) and the perpendicular through <paramref name="last"/> (a line).
    /// </summary>
    public static IReadOnlyList<GuideLine<T>> FromPreviousSegment<T>(T previous, T last) where T : IPoint, new()
    {
        if (!TryDirection(previous, last, out var dx, out var dy))
            return Array.Empty<GuideLine<T>>();

        return new[]
        {
            new GuideLine<T>(GuideKind.Extension, last, dx, dy, isRay: true),
            new GuideLine<T>(GuideKind.Perpendicular, last, -dy, dx, isRay: false),
        };
    }

    /// <summary>
    /// From an existing segment <paramref name="start"/>→<paramref name="end"/> picked as the reference: its
    /// extension beyond each end (two rays), and, when <paramref name="last"/> (the point being drawn from) is given,
    /// the perpendicular and the parallel through <paramref name="last"/> (two lines). The perpendicular carries the
    /// reference segment, so its foot on that segment can be found.
    /// </summary>
    public static IReadOnlyList<GuideLine<T>> FromReferenceSegment<T>(T start, T end, T? last = default) where T : IPoint, new()
    {
        if (!TryDirection(start, end, out var dx, out var dy))
            return Array.Empty<GuideLine<T>>();

        var guides = new List<GuideLine<T>>
        {
            new GuideLine<T>(GuideKind.Extension, end, dx, dy, isRay: true, start, end),
            new GuideLine<T>(GuideKind.Extension, start, -dx, -dy, isRay: true, start, end),
        };

        if (last is not null)
        {
            guides.Add(new GuideLine<T>(GuideKind.Perpendicular, last, -dy, dx, isRay: false, start, end));
            guides.Add(new GuideLine<T>(GuideKind.Parallel, last, dx, dy, isRay: false, start, end));
        }

        return guides;
    }

    /// <summary>
    /// Rays from <paramref name="last"/> every <paramref name="stepDegrees"/>, counted from the direction of
    /// <paramref name="previous"/>→<paramref name="last"/>, or from north when there is no previous point (the first
    /// segment of a shape). The step must divide 360 into at least two parts.
    /// </summary>
    public static IReadOnlyList<GuideLine<T>> AngleSteps<T>(T? previous, T last, double stepDegrees) where T : IPoint, new()
    {
        if (!(stepDegrees > 0) || stepDegrees > 180)
            throw new ArgumentOutOfRangeException(nameof(stepDegrees), stepDegrees, "The step must be above 0 and at most 180 degrees.");

        // north is straight up in a projected map
        double baseBearing = previous is not null && TryDirection(previous, last, out var dx, out var dy)
            ? WebMercatorGround.BearingOf(dx, dy)
            : 0;

        var count = (int)Math.Round(360.0 / stepDegrees);

        var guides = new List<GuideLine<T>>(count);

        for (int i = 0; i < count; i++)
        {
            var (x, y) = WebMercatorGround.DirectionOf(baseBearing + i * stepDegrees);

            guides.Add(new GuideLine<T>(GuideKind.AngleStep, last, x, y, isRay: true));
        }

        return guides;
    }

    private static bool TryDirection<T>(T from, T to, out double dx, out double dy) where T : IPoint
    {
        dx = to.X - from.X;
        dy = to.Y - from.Y;

        return (dx != 0 || dy != 0) && !double.IsNaN(dx) && !double.IsNaN(dy) && !double.IsInfinity(dx) && !double.IsInfinity(dy);
    }
}
