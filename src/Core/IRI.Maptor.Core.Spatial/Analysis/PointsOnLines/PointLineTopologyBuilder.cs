using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;

/// <summary>
/// Builds the topology between a point layer and a line layer whose points stand on the lines —
/// towers on power-line routes, valves on pipes, stations on a road — and reports what is wrong
/// with the data along the way.
/// <list type="number">
/// <item>Membership: a point belongs to <b>every</b> line it lies within
/// <see cref="PointLineTopologyOptions.Tolerance"/> of — none (an orphan), one, or several (a point
/// shared by two lines). Nearest-line guessing is deliberately avoided: a point too far from every
/// line is reported, not assigned.</item>
/// <item>Order: each line's points are ordered by where they project onto it (part, then distance
/// along the part). Optional sequence numbers never decide the order; they only choose the direction
/// (so that they rise) and are checked against the geometric order.</item>
/// </list>
/// Geodetic data (WGS84 longitude/latitude) is measured in metres: distances to the line on a local
/// plane around each point, lengths along the line with Vincenty. Any other SRS is measured in its
/// own coordinate units. Points must be in the lines' SRS.
/// </summary>
public sealed class PointLineTopologyBuilder<T> where T : IPoint, new()
{
    private const double MetresPerDegree = 111_320;

    private readonly PointLineTopologyOptions _options;

    public PointLineTopologyBuilder(PointLineTopologyOptions? options = null)
    {
        _options = options ?? PointLineTopologyOptions.Default;
    }

    public PointLineTopologyOptions Options => _options;

    /// <param name="lines">The line layer; null or unusable entries are kept (and reported) so indices stay aligned.</param>
    /// <param name="points">The point layer, in the lines' SRS.</param>
    /// <param name="sequences">Optional sequence number per point (parallel to <paramref name="points"/>).</param>
    public PointLineTopology<T> Build(
        IReadOnlyList<Geometry<T>?> lines,
        IReadOnlyList<T?> points,
        IReadOnlyList<long?>? sequences = null)
    {
        if (lines is null)
            throw new ArgumentNullException(nameof(lines));

        if (points is null)
            throw new ArgumentNullException(nameof(points));

        if (sequences is not null && sequences.Count != points.Count)
            throw new ArgumentException("There must be one sequence entry per point.", nameof(sequences));

        var issues = new List<PointLineIssue<T>>();

        var isGeodetic = _options.IsGeodetic
                         ?? lines.FirstOrDefault(l => l is not null && !l.IsNullOrEmpty())?.Srid == SridHelper.GeodeticWGS84;

        var tolerance = Math.Max(0, _options.Tolerance);

        var preparedLines = PrepareLines(lines, isGeodetic, issues);

        var grid = new SegmentGrid(_options.GridCellSize ?? (isGeodetic ? 0.01 : 1000));

        foreach (var line in preparedLines)
        {
            foreach (var part in line.Parts)
            {
                for (int s = 0; s < part.Vertices.Count - 1; s++)
                    grid.Add(new SegmentRef(line.Index, part.Index, s), part.Vertices[s], part.Vertices[s + 1]);
            }
        }

        // Membership: every line within the tolerance, the closest station on each.
        var stationsOfLines = preparedLines.ToDictionary(l => l.Index, _ => new List<LineStation<T>>());
        var linesOfPoints = new IReadOnlyList<int>[points.Count];

        for (int p = 0; p < points.Count; p++)
        {
            var point = points[p];

            if (point is null || !IsFinite(point.X) || !IsFinite(point.Y))
            {
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.InvalidPoint, pointIndex: p));
                linesOfPoints[p] = Array.Empty<int>();
                continue;
            }

            var sequence = sequences?[p];

            var best = new Dictionary<int, LineStation<T>>();

            var (dx, dy) = SearchHalfSize(point, tolerance, isGeodetic);

            foreach (var segment in grid.Query(point.X - dx, point.Y - dy, point.X + dx, point.Y + dy))
            {
                var part = preparedLines[segment.Line].Parts[segment.PartPosition];

                var (distance, t) = DistanceToSegment(point, part.Vertices[segment.Segment], part.Vertices[segment.Segment + 1], isGeodetic);

                if (distance > tolerance)
                    continue;

                var measure = part.Cumulative[segment.Segment] + t * (part.Cumulative[segment.Segment + 1] - part.Cumulative[segment.Segment]);

                if (!best.TryGetValue(segment.Line, out var current) || distance < current.Offset)
                    best[segment.Line] = new LineStation<T>(p, preparedLines[segment.Line].Index, part.Index, measure, distance, sequence, point);
            }

            var lineIndices = best.Keys.OrderBy(i => i).ToArray();

            linesOfPoints[p] = lineIndices;

            foreach (var station in best.Values)
                stationsOfLines[station.LineIndex].Add(station);

            if (lineIndices.Length == 0)
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.OrphanPoint, pointIndex: p, location: point));
            else if (lineIndices.Length > 1)
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.SharedPoint, pointIndex: p, value: lineIndices.Length, location: point));
        }

        var result = new List<PointLineTopologyLine<T>>(preparedLines.Count);

        foreach (var line in preparedLines)
            result.Add(OrderAndCheck(line, stationsOfLines[line.Index], sequences is not null, tolerance, issues));

        return new PointLineTopology<T>(result, linesOfPoints, issues, tolerance, isGeodetic);
    }

    #region Lines

    private sealed class PreparedPart
    {
        public int Index;
        public List<T> Vertices = new();
        public double[] Cumulative = Array.Empty<double>();
        public double Length => Cumulative.Length == 0 ? 0 : Cumulative[Cumulative.Length - 1];
    }

    private sealed class PreparedLine
    {
        public int Index;
        public Geometry<T>? Geometry;
        public List<PreparedPart> Parts = new();
        public double[] PartLengths = Array.Empty<double>();
    }

    private static List<PreparedLine> PrepareLines(IReadOnlyList<Geometry<T>?> lines, bool isGeodetic, List<PointLineIssue<T>> issues)
    {
        var prepared = new List<PreparedLine>(lines.Count);

        for (int i = 0; i < lines.Count; i++)
        {
            var geometry = lines[i];

            var line = new PreparedLine { Index = i, Geometry = geometry };

            var parts = PartsOf(geometry);

            line.PartLengths = new double[parts.Count];

            for (int p = 0; p < parts.Count; p++)
            {
                var vertices = parts[p].Where(v => v is not null && IsFinite(v.X) && IsFinite(v.Y)).ToList();

                if (vertices.Count < 2)
                    continue;

                var cumulative = new double[vertices.Count];

                for (int v = 1; v < vertices.Count; v++)
                    cumulative[v] = cumulative[v - 1] + Length(vertices[v - 1], vertices[v], isGeodetic);

                if (cumulative[cumulative.Length - 1] <= 0)
                    continue;

                line.Parts.Add(new PreparedPart { Index = p, Vertices = vertices, Cumulative = cumulative });
                line.PartLengths[p] = cumulative[cumulative.Length - 1];
            }

            if (line.Parts.Count == 0)
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.InvalidLine, lineIndex: i));

            prepared.Add(line);
        }

        return prepared;
    }

    /// <summary>The vertex lists of a line or multi-line; anything else yields its leaf point lists.</summary>
    private static List<List<T>> PartsOf(Geometry<T>? geometry)
    {
        var parts = new List<List<T>>();

        void Collect(Geometry<T>? g)
        {
            if (g is null)
                return;

            if (g.Geometries is { Count: > 0 })
            {
                foreach (var child in g.Geometries)
                    Collect(child);
            }
            else if (g.Points is not null)
            {
                parts.Add(g.Points);
            }
        }

        Collect(geometry);

        return parts;
    }

    #endregion

    #region Order and checks

    private PointLineTopologyLine<T> OrderAndCheck(
        PreparedLine line,
        List<LineStation<T>> stations,
        bool hasSequences,
        double tolerance,
        List<PointLineIssue<T>> issues)
    {
        var asDrawn = stations.OrderBy(s => s.PartIndex).ThenBy(s => s.Measure).ThenBy(s => s.PointIndex).ToList();

        var isReversed = false;
        var isDirectionDecided = true;

        var numbered = asDrawn.Where(s => s.Sequence.HasValue).ToList();

        if (hasSequences && _options.OrientBySequence && numbered.Count >= 2)
        {
            int rising = 0, falling = 0;

            for (int i = 1; i < numbered.Count; i++)
            {
                if (numbered[i].Sequence > numbered[i - 1].Sequence)
                    rising++;
                else if (numbered[i].Sequence < numbered[i - 1].Sequence)
                    falling++;
            }

            if (falling > rising)
                isReversed = true;
            else if (falling == rising)
                isDirectionDecided = false;
        }

        var ordered = isReversed ? Enumerable.Reverse(asDrawn).ToList() : asDrawn;

        var result = new PointLineTopologyLine<T>(line.Index, line.Geometry, line.PartLengths, ordered, isReversed, isDirectionDecided);

        if (line.Parts.Count == 0)
            return result;

        if (ordered.Count < 2)
        {
            issues.Add(new PointLineIssue<T>(PointLineIssueCode.LineWithoutPoints, lineIndex: line.Index, value: ordered.Count));
            return result;
        }

        if (!isDirectionDecided)
            issues.Add(new PointLineIssue<T>(PointLineIssueCode.DirectionUndecided, lineIndex: line.Index));

        // Two points at one place along the line.
        for (int i = 1; i < asDrawn.Count; i++)
        {
            var previous = asDrawn[i - 1];
            var current = asDrawn[i];

            if (current.PartIndex == previous.PartIndex && current.Measure - previous.Measure <= _options.CoincidentTolerance)
            {
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.CoincidentPoints, lineIndex: line.Index,
                    pointIndex: current.PointIndex, otherPointIndex: previous.PointIndex, partIndex: current.PartIndex, location: current.Location));
            }
        }

        if (hasSequences)
            CheckSequences(line.Index, ordered, isDirectionDecided, issues);

        // Every part should start and end at a point.
        foreach (var part in line.Parts)
        {
            var onPart = asDrawn.Where(s => s.PartIndex == part.Index).ToList();

            if (!onPart.Any(s => s.Measure <= tolerance))
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.OpenLineEnd, lineIndex: line.Index, partIndex: part.Index, value: 0, location: part.Vertices[0]));

            if (!onPart.Any(s => part.Length - s.Measure <= tolerance))
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.OpenLineEnd, lineIndex: line.Index, partIndex: part.Index, value: part.Length, location: part.Vertices[part.Vertices.Count - 1]));
        }

        return result;
    }

    private static void CheckSequences(int lineIndex, List<LineStation<T>> ordered, bool isDirectionDecided, List<PointLineIssue<T>> issues)
    {
        foreach (var station in ordered.Where(s => !s.Sequence.HasValue))
            issues.Add(new PointLineIssue<T>(PointLineIssueCode.MissingSequence, lineIndex: lineIndex, pointIndex: station.PointIndex, location: station.Location));

        var numbered = ordered.Where(s => s.Sequence.HasValue).ToList();

        foreach (var group in numbered.GroupBy(s => s.Sequence!.Value).Where(g => g.Count() > 1))
        {
            var first = group.First();

            foreach (var repeat in group.Skip(1))
            {
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.DuplicateSequence, lineIndex: lineIndex,
                    pointIndex: repeat.PointIndex, otherPointIndex: first.PointIndex, value: group.Key, location: repeat.Location));
            }
        }

        // With no direction there is no "rising" to check against.
        if (!isDirectionDecided || numbered.Count < 2)
            return;

        // The longest non-decreasing run is taken as right; every number off it is out of order. A
        // single wrong number (1 2 3 99 5 6) is then the one reported, not everything after it.
        var kept = LongestNonDecreasing(numbered.Select(s => s.Sequence!.Value).ToList());

        for (int i = 0; i < numbered.Count; i++)
        {
            if (!kept.Contains(i))
            {
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.SequenceOutOfOrder, lineIndex: lineIndex,
                    pointIndex: numbered[i].PointIndex, value: numbered[i].Sequence, location: numbered[i].Location));
            }
        }

        var run = kept.OrderBy(i => i).Select(i => numbered[i]).ToList();

        for (int i = 1; i < run.Count; i++)
        {
            var missing = run[i].Sequence!.Value - run[i - 1].Sequence!.Value - 1;

            if (missing > 0)
            {
                issues.Add(new PointLineIssue<T>(PointLineIssueCode.SequenceGap, lineIndex: lineIndex,
                    pointIndex: run[i].PointIndex, otherPointIndex: run[i - 1].PointIndex, value: missing, location: run[i].Location));
            }
        }
    }

    /// <summary>Indices of one longest non-decreasing subsequence (patience sorting, O(n log n)).</summary>
    private static HashSet<int> LongestNonDecreasing(IReadOnlyList<long> values)
    {
        var tails = new List<int>();          // index of the smallest tail of each run length
        var previous = new int[values.Count];

        for (int i = 0; i < values.Count; i++)
        {
            // First run whose tail is greater than values[i] (upper bound keeps equal values).
            int low = 0, high = tails.Count;

            while (low < high)
            {
                var mid = (low + high) / 2;

                if (values[tails[mid]] <= values[i])
                    low = mid + 1;
                else
                    high = mid;
            }

            previous[i] = low > 0 ? tails[low - 1] : -1;

            if (low == tails.Count)
                tails.Add(i);
            else
                tails[low] = i;
        }

        var result = new HashSet<int>();

        for (int i = tails.Count == 0 ? -1 : tails[tails.Count - 1]; i >= 0; i = previous[i])
            result.Add(i);

        return result;
    }

    #endregion

    #region Geometry

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static double Length(T first, T second, bool isGeodetic)
    {
        if (!isGeodetic)
            return SpatialUtility.GetEuclideanLength(first, second);

        var length = SpatialUtility.GetEllipsoidalLength(first, second);

        return IsFinite(length) ? length : 0;
    }

    /// <summary>Half the size of the search box around a point, in coordinate units.</summary>
    private static (double Dx, double Dy) SearchHalfSize(T point, double tolerance, bool isGeodetic)
    {
        if (!isGeodetic)
            return (tolerance, tolerance);

        // A little extra so rounding in the conversion never loses a line right at the tolerance.
        var metres = tolerance * 1.01 + 0.001;

        var cos = Math.Max(Math.Cos(point.Y * Math.PI / 180.0), 0.01);

        return (metres / (MetresPerDegree * cos), metres / MetresPerDegree);
    }

    /// <summary>
    /// Distance from the point to the segment and where its foot falls on it (0 = start, 1 = end).
    /// Geodetic data is flattened onto a local plane centred on the point, with the WGS84
    /// metres-per-degree series at its latitude: well under a millimetre off at these distances.
    /// </summary>
    private static (double Distance, double T) DistanceToSegment(T point, T start, T end, bool isGeodetic)
    {
        double sx, sy, ex, ey;

        if (isGeodetic)
        {
            var latitude = point.Y * Math.PI / 180.0;

            var perDegreeY = 111132.92 - 559.82 * Math.Cos(2 * latitude) + 1.175 * Math.Cos(4 * latitude) - 0.0023 * Math.Cos(6 * latitude);
            var perDegreeX = 111412.84 * Math.Cos(latitude) - 93.5 * Math.Cos(3 * latitude) + 0.118 * Math.Cos(5 * latitude);

            sx = (start.X - point.X) * perDegreeX;
            sy = (start.Y - point.Y) * perDegreeY;
            ex = (end.X - point.X) * perDegreeX;
            ey = (end.Y - point.Y) * perDegreeY;
        }
        else
        {
            sx = start.X - point.X;
            sy = start.Y - point.Y;
            ex = end.X - point.X;
            ey = end.Y - point.Y;
        }

        var dx = ex - sx;
        var dy = ey - sy;

        var lengthSquared = dx * dx + dy * dy;

        var t = lengthSquared <= 0 ? 0 : Math.Clamp(-(sx * dx + sy * dy) / lengthSquared, 0.0, 1.0);

        var fx = sx + t * dx;
        var fy = sy + t * dy;

        return (Math.Sqrt(fx * fx + fy * fy), t);
    }

    #endregion

    #region Segment grid

    private readonly struct SegmentRef : IEquatable<SegmentRef>
    {
        public SegmentRef(int line, int partPosition, int segment)
        {
            Line = line;
            PartPosition = partPosition;
            Segment = segment;
        }

        public int Line { get; }

        /// <summary>Position in the prepared line's part list (not the input part index).</summary>
        public int PartPosition { get; }

        public int Segment { get; }

        public bool Equals(SegmentRef other) => Line == other.Line && PartPosition == other.PartPosition && Segment == other.Segment;

        public override bool Equals(object? obj) => obj is SegmentRef other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Line, PartPosition, Segment);
    }

    /// <summary>A uniform grid over segment bounding boxes: a cheap prefilter, never the test itself.</summary>
    private sealed class SegmentGrid
    {
        private readonly double _cellSize;
        private readonly Dictionary<(long, long), List<SegmentRef>> _cells = new();

        public SegmentGrid(double cellSize)
        {
            _cellSize = cellSize > 0 ? cellSize : 1;
        }

        public void Add(SegmentRef segment, T start, T end)
        {
            var (minX, minY) = Cell(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
            var (maxX, maxY) = Cell(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));

            for (long x = minX; x <= maxX; x++)
            {
                for (long y = minY; y <= maxY; y++)
                {
                    if (!_cells.TryGetValue((x, y), out var bucket))
                    {
                        bucket = new List<SegmentRef>();
                        _cells[(x, y)] = bucket;
                    }

                    bucket.Add(segment);
                }
            }
        }

        public IEnumerable<SegmentRef> Query(double minX, double minY, double maxX, double maxY)
        {
            var (fromX, fromY) = Cell(minX, minY);
            var (toX, toY) = Cell(maxX, maxY);

            var seen = new HashSet<SegmentRef>();

            for (long x = fromX; x <= toX; x++)
            {
                for (long y = fromY; y <= toY; y++)
                {
                    if (!_cells.TryGetValue((x, y), out var bucket))
                        continue;

                    foreach (var segment in bucket)
                    {
                        if (seen.Add(segment))
                            yield return segment;
                    }
                }
            }
        }

        private (long, long) Cell(double x, double y) => ((long)Math.Floor(x / _cellSize), (long)Math.Floor(y / _cellSize));
    }

    #endregion
}
