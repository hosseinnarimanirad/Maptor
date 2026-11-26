using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>A stretch of one part of a line that lies within the distance of another line.</summary>
public sealed class CoveredStretch
{
    public CoveredStretch(int partIndex, double from, double to, int otherLineIndex)
    {
        PartIndex = partIndex;
        From = from;
        To = to;
        OtherLineIndex = otherLineIndex;
    }

    /// <summary>The <see cref="MeasuredPart{T}.Index"/> of the covered part.</summary>
    public int PartIndex { get; }

    public double From { get; }

    public double To { get; }

    public double Length => To - From;

    /// <summary>The index of the covering line in the list of other lines.</summary>
    public int OtherLineIndex { get; }

    public override string ToString() => $"part {PartIndex} {From:0.###}–{To:0.###} by line {OtherLineIndex}";
}

/// <summary>
/// Which stretches of a line lie within a distance of other lines. For every pair of a segment of the line and a
/// nearby segment of another line, the points of the first segment within the distance of the second form one
/// interval (the distance from a point moving along a straight segment to another segment is a convex function),
/// found by a convex search and bisection in a <see cref="LocalMetricFrame"/>. Intervals are merged per part and
/// per other line.
/// <para>
/// With <c>maxAngleDegrees</c>, only segment pairs whose directions differ by at most that angle (either way
/// round) count, so that a line crossing the other is not "covered" where it crosses.
/// </para>
/// </summary>
public static class LineCoverage
{
    private const int Iterations = 60;

    /// <param name="line">The line whose stretches are wanted.</param>
    /// <param name="others">The lines that may cover it. Pass the line itself only if self-coverage is wanted.</param>
    /// <param name="distance">How close a point of <paramref name="line"/> must be to another line (metres when geodetic).</param>
    /// <param name="maxAngleDegrees">Count only segments running within this angle of each other; null for any.</param>
    /// <returns>Stretches ordered by part, then by start; touching or overlapping stretches of one other line are merged.</returns>
    public static IReadOnlyList<CoveredStretch> Compute<T>(MeasuredLine<T> line, IReadOnlyList<MeasuredLine<T>> others, double distance, double? maxAngleDegrees = null)
        where T : IPoint, new()
        => new LineCoverageIndex<T>(others ?? throw new ArgumentNullException(nameof(others))).Of(line, distance, maxAngleDegrees);

    internal static IReadOnlyList<CoveredStretch> Compute<T>(MeasuredLine<T> line, IReadOnlyList<MeasuredLine<T>> others, BoxGrid<SegmentRef> grid, double distance, double? maxAngleDegrees)
        where T : IPoint, new()
    {
        if (line is null)
            throw new ArgumentNullException(nameof(line));

        var isGeodetic = line.IsGeodetic;

        var raw = new List<CoveredStretch>();

        var maxAngle = maxAngleDegrees is null ? (double?)null : maxAngleDegrees.Value * Math.PI / 180.0;

        foreach (var part in line.Parts)
        {
            for (int s = 0; s < part.SegmentCount; s++)
            {
                var a0 = part.Vertices[s];
                var a1 = part.Vertices[s + 1];

                var (dx, dy) = Widest(a0, a1, distance, isGeodetic);

                var box = (Math.Min(a0.X, a1.X) - dx, Math.Min(a0.Y, a1.Y) - dy, Math.Max(a0.X, a1.X) + dx, Math.Max(a0.Y, a1.Y) + dy);

                foreach (var reference in grid.Query(box.Item1, box.Item2, box.Item3, box.Item4))
                {
                    var otherPart = others[reference.Line].Parts[reference.Part];
                    var b0 = otherPart.Vertices[reference.Segment];
                    var b1 = otherPart.Vertices[reference.Segment + 1];

                    var frame = LocalMetricFrame.At(a0, isGeodetic);

                    var (ax, ay) = frame.ToMetres(a1);
                    var (bx0, by0) = frame.ToMetres(b0);
                    var (bx1, by1) = frame.ToMetres(b1);

                    var planeLength = Math.Sqrt(ax * ax + ay * ay);

                    if (planeLength <= 0)
                        continue;

                    if (maxAngle is not null && !RunsAlong(ax, ay, bx1 - bx0, by1 - by0, maxAngle.Value))
                        continue;

                    if (!WithinInterval(ax, ay, planeLength, bx0, by0, bx1, by1, distance, out var t0, out var t1))
                        continue;

                    var segmentMeasure = part.Cumulative[s + 1] - part.Cumulative[s];

                    var from = part.Cumulative[s] + t0 / planeLength * segmentMeasure;
                    var to = part.Cumulative[s] + t1 / planeLength * segmentMeasure;

                    raw.Add(new CoveredStretch(part.Index, from, to, reference.Line));
                }
            }
        }

        return Merge(raw);
    }

    /// <summary>The total covered length of each part, all other lines together (stretches of different lines merged).</summary>
    public static double CoveredLength(IEnumerable<CoveredStretch> stretches)
        => MergeIntervals(stretches.Select(s => (s.PartIndex, s.From, s.To))).Sum(i => i.To - i.From);

    /// <summary>Merges intervals per part, whatever line covered them.</summary>
    public static IReadOnlyList<(int PartIndex, double From, double To)> MergeIntervals(IEnumerable<(int PartIndex, double From, double To)> intervals)
    {
        var result = new List<(int PartIndex, double From, double To)>();

        foreach (var group in intervals.GroupBy(i => i.PartIndex).OrderBy(g => g.Key))
        {
            (int PartIndex, double From, double To)? current = null;

            foreach (var interval in group.OrderBy(i => i.From))
            {
                if (current is null)
                {
                    current = interval;
                }
                else if (interval.From <= current.Value.To + 1e-9)
                {
                    current = (current.Value.PartIndex, current.Value.From, Math.Max(current.Value.To, interval.To));
                }
                else
                {
                    result.Add(current.Value);
                    current = interval;
                }
            }

            if (current is not null)
                result.Add(current.Value);
        }

        return result;
    }

    /// <summary>Takes away the second set of intervals from the first, per part.</summary>
    public static IReadOnlyList<(int PartIndex, double From, double To)> Subtract(
        IEnumerable<(int PartIndex, double From, double To)> intervals,
        IEnumerable<(int PartIndex, double From, double To)> remove)
    {
        var removeByPart = MergeIntervals(remove).GroupBy(r => r.PartIndex).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<(int PartIndex, double From, double To)>();

        foreach (var interval in MergeIntervals(intervals))
        {
            var pieces = new List<(double From, double To)> { (interval.From, interval.To) };

            if (removeByPart.TryGetValue(interval.PartIndex, out var cuts))
            {
                foreach (var cut in cuts)
                {
                    pieces = pieces.SelectMany(p =>
                        cut.To <= p.From || cut.From >= p.To
                            ? new[] { p }
                            : new[] { (p.From, Math.Max(p.From, cut.From)), (Math.Min(p.To, cut.To), p.To) }.Where(x => x.Item2 - x.Item1 > 1e-9))
                        .ToList();
                }
            }

            result.AddRange(pieces.Select(p => (interval.PartIndex, p.From, p.To)));
        }

        return result;
    }

    private static IReadOnlyList<CoveredStretch> Merge(List<CoveredStretch> raw)
    {
        var result = new List<CoveredStretch>();

        foreach (var group in raw.GroupBy(r => (r.PartIndex, r.OtherLineIndex)))
        {
            foreach (var merged in MergeIntervals(group.Select(g => (g.PartIndex, g.From, g.To))))
                result.Add(new CoveredStretch(merged.PartIndex, merged.From, merged.To, group.Key.OtherLineIndex));
        }

        return result.OrderBy(r => r.PartIndex).ThenBy(r => r.From).ThenBy(r => r.OtherLineIndex).ToList();
    }

    /// <summary>
    /// The interval [t0, t1] of positions along segment (0,0)→(ax,ay) of length <paramref name="length"/> whose
    /// distance to segment b is at most <paramref name="distance"/>; false when there is none.
    /// </summary>
    private static bool WithinInterval(double ax, double ay, double length, double bx0, double by0, double bx1, double by1, double distance, out double t0, out double t1)
    {
        t0 = t1 = 0;

        double F(double t)
        {
            var x = ax * t / length;
            var y = ay * t / length;

            return PointSegmentDistance(x, y, bx0, by0, bx1, by1) - distance;
        }

        // Minimum of a convex function by ternary search.
        double low = 0, high = length;

        for (int i = 0; i < Iterations; i++)
        {
            var m1 = low + (high - low) / 3;
            var m2 = high - (high - low) / 3;

            if (F(m1) <= F(m2))
                high = m2;
            else
                low = m1;
        }

        var best = (low + high) / 2;

        foreach (var candidate in new[] { 0.0, length })
        {
            if (F(candidate) < F(best))
                best = candidate;
        }

        if (F(best) > 0)
            return false;

        t0 = F(0) <= 0 ? 0 : Root(F, 0, best);
        t1 = F(length) <= 0 ? length : Root(F, length, best);

        return t1 > t0 || Math.Abs(t1 - t0) < 1e-12;
    }

    /// <summary>Bisection between an outside point (F &gt; 0) and an inside point (F ≤ 0).</summary>
    private static double Root(Func<double, double> f, double outside, double inside)
    {
        for (int i = 0; i < Iterations; i++)
        {
            var mid = (outside + inside) / 2;

            if (f(mid) <= 0)
                inside = mid;
            else
                outside = mid;
        }

        return inside;
    }

    private static bool RunsAlong(double ax, double ay, double bx, double by, double maxAngle)
    {
        var la = Math.Sqrt(ax * ax + ay * ay);
        var lb = Math.Sqrt(bx * bx + by * by);

        if (la <= 0 || lb <= 0)
            return false;

        var cos = Math.Abs(ax * bx + ay * by) / (la * lb);

        return cos >= Math.Cos(maxAngle) - 1e-12;
    }

    internal static double PointSegmentDistance(double px, double py, double x0, double y0, double x1, double y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;

        var lengthSquared = dx * dx + dy * dy;

        var t = lengthSquared <= 0 ? 0 : Math.Max(0, Math.Min(1, ((px - x0) * dx + (py - y0) * dy) / lengthSquared));

        var fx = x0 + t * dx - px;
        var fy = y0 + t * dy - py;

        return Math.Sqrt(fx * fx + fy * fy);
    }

    /// <summary>The larger search half-size at the two ends of a segment.</summary>
    private static (double Dx, double Dy) Widest<T>(T a, T b, double metres, bool isGeodetic) where T : IPoint
    {
        var first = LocalMetricFrame.At(a, isGeodetic).SearchHalfSize(metres);
        var second = LocalMetricFrame.At(b, isGeodetic).SearchHalfSize(metres);

        return (Math.Max(first.HalfWidth, second.HalfWidth), Math.Max(first.HalfHeight, second.HalfHeight));
    }
}

/// <summary>
/// <see cref="LineCoverage"/> against one fixed set of lines, with their segment index built once: use it when many
/// lines are checked against the same set.
/// </summary>
public sealed class LineCoverageIndex<T> where T : IPoint, new()
{
    private readonly IReadOnlyList<MeasuredLine<T>> _others;
    private readonly BoxGrid<SegmentRef> _grid;

    public LineCoverageIndex(IReadOnlyList<MeasuredLine<T>> others)
    {
        _others = others ?? throw new ArgumentNullException(nameof(others));
        _grid = SegmentIndex.Build(others);
    }

    public IReadOnlyList<MeasuredLine<T>> Lines => _others;

    /// <summary>See <see cref="LineCoverage.Compute{T}(MeasuredLine{T}, IReadOnlyList{MeasuredLine{T}}, double, double?)"/>.</summary>
    public IReadOnlyList<CoveredStretch> Of(MeasuredLine<T> line, double distance, double? maxAngleDegrees = null)
        => LineCoverage.Compute(line, _others, _grid, distance, maxAngleDegrees);
}
/// <summary>A reference to one segment of one part of one line in a list of lines.</summary>
internal readonly struct SegmentRef : IEquatable<SegmentRef>
{
    public SegmentRef(int line, int part, int segment)
    {
        Line = line;
        Part = part;
        Segment = segment;
    }

    public int Line { get; }

    /// <summary>Position in the line's <see cref="MeasuredLine{T}.Parts"/> list.</summary>
    public int Part { get; }

    public int Segment { get; }

    public bool Equals(SegmentRef other) => Line == other.Line && Part == other.Part && Segment == other.Segment;

    public override bool Equals(object? obj) => obj is SegmentRef other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Line, Part, Segment);
}

/// <summary>A <see cref="BoxGrid{TItem}"/> over the segments of a list of lines.</summary>
internal static class SegmentIndex
{
    public static BoxGrid<SegmentRef> Build<T>(IReadOnlyList<MeasuredLine<T>> lines) where T : IPoint, new()
    {
        var extents = new List<double>();

        foreach (var line in lines)
        {
            foreach (var part in line.Parts)
            {
                for (int s = 0; s < part.SegmentCount; s++)
                    extents.Add(Math.Max(Math.Abs(part.Vertices[s + 1].X - part.Vertices[s].X), Math.Abs(part.Vertices[s + 1].Y - part.Vertices[s].Y)));
            }
        }

        // The median segment extent, kept away from zero.
        extents.Sort();

        var cell = extents.Count == 0 ? 1 : Math.Max(extents[extents.Count / 2], extents[extents.Count - 1] * 1e-6);

        var grid = new BoxGrid<SegmentRef>(cell > 0 ? cell : 1);

        for (int l = 0; l < lines.Count; l++)
        {
            for (int p = 0; p < lines[l].Parts.Count; p++)
            {
                var part = lines[l].Parts[p];

                for (int s = 0; s < part.SegmentCount; s++)
                {
                    var a = part.Vertices[s];
                    var b = part.Vertices[s + 1];

                    grid.Add(new SegmentRef(l, p, s), Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
                }
            }
        }

        return grid;
    }
}
