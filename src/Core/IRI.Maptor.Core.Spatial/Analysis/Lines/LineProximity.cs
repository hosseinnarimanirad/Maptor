using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>The nearest place on a line to a point.</summary>
public sealed class NearestOnLine<T> where T : IPoint, new()
{
    public NearestOnLine(int lineIndex, int partIndex, double measure, double distance, T location)
    {
        LineIndex = lineIndex;
        PartIndex = partIndex;
        Measure = measure;
        Distance = distance;
        Location = location;
    }

    public int LineIndex { get; }

    /// <summary>The <see cref="MeasuredPart{T}.Index"/>.</summary>
    public int PartIndex { get; }

    public double Measure { get; }

    /// <summary>Metres when geodetic, coordinate units otherwise.</summary>
    public double Distance { get; }

    public T Location { get; }
}

/// <summary>Finds the nearest line of a fixed set to a point, through a segment index built once.</summary>
public sealed class LineProximity<T> where T : IPoint, new()
{
    private readonly IReadOnlyList<MeasuredLine<T>> _lines;
    private readonly BoxGrid<SegmentRef> _grid;

    public LineProximity(IReadOnlyList<MeasuredLine<T>> lines)
    {
        _lines = lines ?? throw new ArgumentNullException(nameof(lines));
        _grid = SegmentIndex.Build(lines);
    }

    /// <summary>
    /// The nearest place within <paramref name="maxDistance"/> on a line that <paramref name="accept"/> allows
    /// (all lines when null); null when there is none.
    /// </summary>
    public NearestOnLine<T>? Nearest(T point, double maxDistance, Func<int, bool>? accept = null)
    {
        NearestOnLine<T>? best = null;

        foreach (var near in SegmentsNear(point, maxDistance, accept))
        {
            if (best is not null && near.Distance >= best.Distance)
                continue;

            var part = _lines[near.Line].Parts[near.Part];

            var measure = part.Cumulative[near.Segment] + near.Fraction * (part.Cumulative[near.Segment + 1] - part.Cumulative[near.Segment]);

            best = new NearestOnLine<T>(near.Line, part.Index, measure, near.Distance, near.Nearest());
        }

        return best;
    }

    /// <summary>
    /// Every segment within <paramref name="maxDistance"/> of <paramref name="point"/>, of the lines that
    /// <paramref name="accept"/> allows (all when null), in the index's own order.
    /// </summary>
    internal IEnumerable<NearSegment> SegmentsNear(T point, double maxDistance, Func<int, bool>? accept = null)
    {
        if (_lines.Count == 0)
            yield break;

        var isGeodetic = _lines[0].IsGeodetic;

        var frame = LocalMetricFrame.At(point, isGeodetic);
        var (dx, dy) = frame.SearchHalfSize(maxDistance);

        foreach (var reference in _grid.Query(point.X - dx, point.Y - dy, point.X + dx, point.Y + dy))
        {
            if (accept is not null && !accept(reference.Line))
                continue;

            var part = _lines[reference.Line].Parts[reference.Part];
            var a = part.Vertices[reference.Segment];
            var b = part.Vertices[reference.Segment + 1];

            var (ax, ay) = frame.ToMetres(a);
            var (bx, by) = frame.ToMetres(b);

            var sx = bx - ax;
            var sy = by - ay;
            var lengthSquared = sx * sx + sy * sy;

            var t = lengthSquared <= 0 ? 0 : Math.Max(0, Math.Min(1, -(ax * sx + ay * sy) / lengthSquared));

            var fx = ax + t * sx;
            var fy = ay + t * sy;

            var distance = Math.Sqrt(fx * fx + fy * fy);

            if (distance > maxDistance)
                continue;

            yield return new NearSegment(reference.Line, reference.Part, reference.Segment, a, b, t, distance);
        }
    }

    /// <summary>A segment near a point: which one, its two ends, and the nearest place on it.</summary>
    internal readonly struct NearSegment
    {
        public NearSegment(int line, int part, int segment, T a, T b, double fraction, double distance)
        {
            Line = line;
            Part = part;
            Segment = segment;
            A = a;
            B = b;
            Fraction = fraction;
            Distance = distance;
        }

        public int Line { get; }

        /// <summary>Position in the line's <see cref="MeasuredLine{T}.Parts"/> list.</summary>
        public int Part { get; }

        public int Segment { get; }

        public T A { get; }

        public T B { get; }

        /// <summary>How far along the segment (0 to 1) the nearest place is.</summary>
        public double Fraction { get; }

        /// <summary>From the point to the nearest place (metres when geodetic, coordinate units otherwise).</summary>
        public double Distance { get; }

        public T Nearest() => At(Fraction);

        public T At(double fraction) => new T { X = A.X + fraction * (B.X - A.X), Y = A.Y + fraction * (B.Y - A.Y) };
    }
}
