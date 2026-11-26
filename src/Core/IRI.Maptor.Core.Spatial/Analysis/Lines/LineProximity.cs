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
        if (_lines.Count == 0)
            return null;

        var isGeodetic = _lines[0].IsGeodetic;

        var frame = LocalMetricFrame.At(point, isGeodetic);
        var (dx, dy) = frame.SearchHalfSize(maxDistance);

        NearestOnLine<T>? best = null;

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

            if (distance > maxDistance || (best is not null && distance >= best.Distance))
                continue;

            var measure = part.Cumulative[reference.Segment] + t * (part.Cumulative[reference.Segment + 1] - part.Cumulative[reference.Segment]);

            var location = new T { X = a.X + t * (b.X - a.X), Y = a.Y + t * (b.Y - a.Y) };

            best = new NearestOnLine<T>(reference.Line, part.Index, measure, distance, location);
        }

        return best;
    }
}
