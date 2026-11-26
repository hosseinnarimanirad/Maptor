using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>A place where two lines meet: where it is on each of them.</summary>
public sealed class LineCrossing<T> where T : IPoint, new()
{
    public LineCrossing(int lineIndex, int partIndex, double measure, int otherLineIndex, int otherPartIndex, double otherMeasure, T location)
    {
        LineIndex = lineIndex;
        PartIndex = partIndex;
        Measure = measure;
        OtherLineIndex = otherLineIndex;
        OtherPartIndex = otherPartIndex;
        OtherMeasure = otherMeasure;
        Location = location;
    }

    public int LineIndex { get; }

    /// <summary>The <see cref="MeasuredPart{T}.Index"/> on the first line.</summary>
    public int PartIndex { get; }

    /// <summary>Distance along the first line's part.</summary>
    public double Measure { get; }

    public int OtherLineIndex { get; }

    public int OtherPartIndex { get; }

    public double OtherMeasure { get; }

    public T Location { get; }

    public override string ToString() => $"{LineIndex}/{PartIndex} at {Measure:0.###} × {OtherLineIndex}/{OtherPartIndex} at {OtherMeasure:0.###}";
}

/// <summary>
/// Every place where the segments of two sets of lines cross or touch, with its measure along both lines.
/// Segments are straight in the data's coordinates, as they are drawn. Segments that overlap along a stretch
/// (lying on top of each other) are not crossings and are left out. Where a crossing falls on a shared vertex it
/// is found from two segments; such repeats closer than <c>mergeDistance</c> along both lines are reported once.
/// </summary>
public static class SegmentCrossings
{
    /// <param name="lines">The first set.</param>
    /// <param name="others">The second set; null to cross the first set with itself (each pair once, a line never with itself).</param>
    /// <param name="mergeDistance">Crossings of one pair of parts closer than this along both are one crossing.</param>
    public static IReadOnlyList<LineCrossing<T>> Find<T>(IReadOnlyList<MeasuredLine<T>> lines, IReadOnlyList<MeasuredLine<T>>? others = null, double mergeDistance = 1e-6)
        where T : IPoint, new()
    {
        if (lines is null)
            throw new ArgumentNullException(nameof(lines));

        var sameSet = others is null;
        var second = others ?? lines;

        var grid = SegmentIndex.Build(second);

        var found = new List<LineCrossing<T>>();

        for (int l = 0; l < lines.Count; l++)
        {
            for (int p = 0; p < lines[l].Parts.Count; p++)
            {
                var part = lines[l].Parts[p];

                for (int s = 0; s < part.SegmentCount; s++)
                {
                    var a0 = part.Vertices[s];
                    var a1 = part.Vertices[s + 1];

                    foreach (var reference in grid.Query(Math.Min(a0.X, a1.X), Math.Min(a0.Y, a1.Y), Math.Max(a0.X, a1.X), Math.Max(a0.Y, a1.Y)))
                    {
                        if (sameSet && reference.Line <= l)
                            continue;

                        var otherPart = second[reference.Line].Parts[reference.Part];
                        var b0 = otherPart.Vertices[reference.Segment];
                        var b1 = otherPart.Vertices[reference.Segment + 1];

                        if (!Intersect(a0, a1, b0, b1, out var t, out var u))
                            continue;

                        var measure = part.Cumulative[s] + t * (part.Cumulative[s + 1] - part.Cumulative[s]);
                        var otherMeasure = otherPart.Cumulative[reference.Segment] + u * (otherPart.Cumulative[reference.Segment + 1] - otherPart.Cumulative[reference.Segment]);

                        var location = new T { X = a0.X + t * (a1.X - a0.X), Y = a0.Y + t * (a1.Y - a0.Y) };

                        found.Add(new LineCrossing<T>(l, part.Index, measure, reference.Line, otherPart.Index, otherMeasure, location));
                    }
                }
            }
        }

        return Deduplicate(found, mergeDistance);
    }

    /// <summary>
    /// True when segments a0→a1 and b0→b1 meet at a single point, at fraction <paramref name="t"/> along a and
    /// <paramref name="u"/> along b (both in [0, 1]). Collinear segments are never reported.
    /// </summary>
    public static bool Intersect<T>(T a0, T a1, T b0, T b1, out double t, out double u) where T : IPoint
    {
        t = u = 0;

        // Work relative to a0 to keep the products small for geodetic coordinates.
        var rx = a1.X - a0.X;
        var ry = a1.Y - a0.Y;
        var sx = b1.X - b0.X;
        var sy = b1.Y - b0.Y;
        var qx = b0.X - a0.X;
        var qy = b0.Y - a0.Y;

        var denominator = rx * sy - ry * sx;

        var scale = Math.Max(Math.Sqrt(rx * rx + ry * ry), Math.Sqrt(sx * sx + sy * sy));

        if (scale <= 0 || Math.Abs(denominator) <= 1e-15 * scale * scale)
            return false;

        t = (qx * sy - qy * sx) / denominator;
        u = (qx * ry - qy * rx) / denominator;

        const double slack = 1e-12;

        if (t < -slack || t > 1 + slack || u < -slack || u > 1 + slack)
            return false;

        t = Math.Max(0, Math.Min(1, t));
        u = Math.Max(0, Math.Min(1, u));

        return true;
    }

    private static IReadOnlyList<LineCrossing<T>> Deduplicate<T>(List<LineCrossing<T>> found, double mergeDistance) where T : IPoint, new()
    {
        var result = new List<LineCrossing<T>>();

        foreach (var group in found.GroupBy(c => (c.LineIndex, c.PartIndex, c.OtherLineIndex, c.OtherPartIndex)))
        {
            LineCrossing<T>? last = null;

            foreach (var crossing in group.OrderBy(c => c.Measure).ThenBy(c => c.OtherMeasure))
            {
                if (last is not null
                    && crossing.Measure - last.Measure <= mergeDistance
                    && Math.Abs(crossing.OtherMeasure - last.OtherMeasure) <= mergeDistance)
                    continue;

                result.Add(crossing);
                last = crossing;
            }
        }

        return result
            .OrderBy(c => c.LineIndex).ThenBy(c => c.PartIndex).ThenBy(c => c.Measure)
            .ThenBy(c => c.OtherLineIndex).ThenBy(c => c.OtherPartIndex)
            .ToList();
    }
}
