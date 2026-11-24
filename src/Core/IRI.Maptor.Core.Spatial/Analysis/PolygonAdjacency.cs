using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Finds where polygons share their boundaries (e.g. land parcels and their neighbours).
/// </summary>
/// <remarks>
/// Every edge of every ring is split into <see cref="BoundaryPiece"/>s: a piece is either shared with
/// exactly one other polygon or open (no polygon within the tolerance). Two edges share the part where
/// they run within <c>tolerance</c> of each other, so vertices do not have to match and slightly skewed
/// digitising still counts. Polygons that only touch at a corner are not neighbours. Rings are expected
/// without the repeated closing vertex (the library convention); a repeated one is ignored.
/// </remarks>
public static class PolygonAdjacency
{
    /// <summary>
    /// Analyses <paramref name="polygons"/> (Polygon or MultiPolygon, all in the same projected SRS).
    /// The result has one <see cref="PolygonBoundary"/> per input polygon, in input order.
    /// </summary>
    /// <param name="tolerance">Largest gap, in map units, at which two boundaries still count as shared. Must be positive.</param>
    public static List<PolygonBoundary> Analyze(IReadOnlyList<Geometry<Point>> polygons, double tolerance)
    {
        if (polygons is null)
            throw new ArgumentNullException(nameof(polygons));

        if (!(tolerance > 0))
            throw new ArgumentOutOfRangeException(nameof(tolerance), "The tolerance must be positive.");

        var rings = polygons.Select(GetRings).ToList();

        var extents = rings.Select(r => GetExtent(r, tolerance)).ToList();

        var candidates = FindCandidatePairs(extents);

        var result = new List<PolygonBoundary>(polygons.Count);

        for (int i = 0; i < polygons.Count; i++)
        {
            var pieces = new List<BoundaryPiece>();

            foreach (var ring in rings[i])
            {
                for (int e = 0; e < ring.Points.Count; e++)
                {
                    var start = ring.Points[e];
                    var end = ring.Points[(e + 1) % ring.Points.Count];

                    var intervals = FindSharedIntervals(start, end, candidates[i], rings, tolerance);

                    pieces.AddRange(SplitEdge(ring.PartIndex, ring.RingIndex, e, start, end, intervals, tolerance));
                }
            }

            result.Add(new PolygonBoundary(i, pieces));
        }

        return result;
    }

    private sealed class Ring
    {
        public Ring(int partIndex, int ringIndex, List<Point> points)
        {
            PartIndex = partIndex;
            RingIndex = ringIndex;
            Points = points;
        }

        public int PartIndex { get; }

        public int RingIndex { get; }

        public List<Point> Points { get; }
    }

    private readonly struct SharedInterval
    {
        public SharedInterval(double from, double to, int neighbour)
        {
            From = from;
            To = to;
            Neighbour = neighbour;
        }

        public double From { get; }

        public double To { get; }

        public int Neighbour { get; }
    }

    private static List<Ring> GetRings(Geometry<Point> geometry)
    {
        var result = new List<Ring>();

        if (geometry is null)
            return result;

        void AddPolygon(Geometry<Point> polygon, int partIndex)
        {
            if (polygon.Geometries is null)
                return;

            for (int r = 0; r < polygon.Geometries.Count; r++)
            {
                var points = polygon.Geometries[r].Points;

                if (points is null || points.Count < 2)
                    continue;

                var copy = points.ToList();

                // tolerate a repeated closing vertex
                if (copy.Count > 2 && copy[0].X == copy[^1].X && copy[0].Y == copy[^1].Y)
                    copy.RemoveAt(copy.Count - 1);

                result.Add(new Ring(partIndex, r, copy));
            }
        }

        switch (geometry.Type)
        {
            case GeometryType.Polygon:
                AddPolygon(geometry, 0);
                break;

            case GeometryType.MultiPolygon:
                for (int p = 0; p < (geometry.Geometries?.Count ?? 0); p++)
                    AddPolygon(geometry.Geometries![p], p);
                break;
        }

        return result;
    }

    private static BoundingBox GetExtent(List<Ring> rings, double tolerance)
    {
        var points = rings.SelectMany(r => r.Points).ToList();

        if (points.Count == 0)
            return BoundingBox.NaN;

        return new BoundingBox(
            points.Min(p => p.X) - tolerance,
            points.Min(p => p.Y) - tolerance,
            points.Max(p => p.X) + tolerance,
            points.Max(p => p.Y) + tolerance);
    }

    /// <summary>Sweep along X: for each polygon, the other polygons whose (tolerance-grown) extents overlap its own.</summary>
    private static List<List<int>> FindCandidatePairs(List<BoundingBox> extents)
    {
        var candidates = extents.Select(_ => new List<int>()).ToList();

        var order = Enumerable.Range(0, extents.Count)
            .Where(i => !extents[i].IsNaN())
            .OrderBy(i => extents[i].XMin)
            .ToList();

        for (int a = 0; a < order.Count; a++)
        {
            var first = extents[order[a]];

            for (int b = a + 1; b < order.Count; b++)
            {
                var second = extents[order[b]];

                if (second.XMin > first.XMax)
                    break;

                if (second.YMin <= first.YMax && second.YMax >= first.YMin)
                {
                    candidates[order[a]].Add(order[b]);
                    candidates[order[b]].Add(order[a]);
                }
            }
        }

        return candidates;
    }

    /// <summary>The parts of edge start→end (as distances from start) that other polygons' edges run along.</summary>
    private static List<SharedInterval> FindSharedIntervals(Point start, Point end, List<int> candidates, List<List<Ring>> rings, double tolerance)
    {
        var result = new List<SharedInterval>();

        double dx = end.X - start.X, dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length <= 0)
            return result;

        double ux = dx / length, uy = dy / length;

        double minX = Math.Min(start.X, end.X) - tolerance, maxX = Math.Max(start.X, end.X) + tolerance;
        double minY = Math.Min(start.Y, end.Y) - tolerance, maxY = Math.Max(start.Y, end.Y) + tolerance;

        foreach (var neighbour in candidates)
        {
            foreach (var ring in rings[neighbour])
            {
                for (int e = 0; e < ring.Points.Count; e++)
                {
                    var r = ring.Points[e];
                    var s = ring.Points[(e + 1) % ring.Points.Count];

                    if (Math.Max(r.X, s.X) < minX || Math.Min(r.X, s.X) > maxX || Math.Max(r.Y, s.Y) < minY || Math.Min(r.Y, s.Y) > maxY)
                        continue;

                    // signed distances of r and s from the edge's line
                    double dr = ux * (r.Y - start.Y) - uy * (r.X - start.X);
                    double ds = ux * (s.Y - start.Y) - uy * (s.X - start.X);

                    // the part of r→s (as fractions 0..1) that stays within the tolerance band
                    double s0, s1;

                    if (Math.Abs(dr) <= tolerance && Math.Abs(ds) <= tolerance)
                    {
                        s0 = 0;
                        s1 = 1;
                    }
                    else if (dr == ds)
                    {
                        continue;
                    }
                    else
                    {
                        double a = (-tolerance - dr) / (ds - dr);
                        double b = (tolerance - dr) / (ds - dr);

                        s0 = Math.Max(0, Math.Min(a, b));
                        s1 = Math.Min(1, Math.Max(a, b));

                        if (s1 <= s0)
                            continue;
                    }

                    double t0 = Project(r, s, s0, start, ux, uy);
                    double t1 = Project(r, s, s1, start, ux, uy);

                    double from = Math.Max(0, Math.Min(t0, t1));
                    double to = Math.Min(length, Math.Max(t0, t1));

                    // a corner contact or a crossing edge covers (almost) nothing
                    if (to - from > tolerance)
                        result.Add(new SharedInterval(from, to, neighbour));
                }
            }
        }

        return result;
    }

    private static double Project(Point r, Point s, double fraction, Point origin, double ux, double uy)
    {
        double x = r.X + fraction * (s.X - r.X);
        double y = r.Y + fraction * (s.Y - r.Y);

        return (x - origin.X) * ux + (y - origin.Y) * uy;
    }

    private static IEnumerable<BoundaryPiece> SplitEdge(int partIndex, int ringIndex, int edgeIndex, Point start, Point end, List<SharedInterval> intervals, double tolerance)
    {
        double length = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));

        if (length <= 0)
            yield break;

        // elementary pieces between all interval borders, each given the neighbour that covers it best
        var breaks = intervals.SelectMany(i => new[] { i.From, i.To })
            .Append(0).Append(length)
            .OrderBy(t => t)
            .ToList();

        var spans = new List<(double From, double To, int? Neighbour)>();

        for (int i = 0; i < breaks.Count - 1; i++)
        {
            double from = breaks[i], to = breaks[i + 1];

            if (to - from <= 1e-12)
                continue;

            double middle = (from + to) / 2;

            int? neighbour = intervals
                .Where(v => v.From <= middle && v.To >= middle)
                .OrderByDescending(v => v.To - v.From)
                .ThenBy(v => v.Neighbour)
                .Select(v => (int?)v.Neighbour)
                .FirstOrDefault();

            AddOrMerge(spans, from, to, neighbour);
        }

        // slivers shorter than the tolerance are noise: give them to the previous span (or the next one)
        for (int i = 0; spans.Count > 1 && i < spans.Count; i++)
        {
            if (spans[i].To - spans[i].From > tolerance)
                continue;

            if (i > 0)
                spans[i - 1] = (spans[i - 1].From, spans[i].To, spans[i - 1].Neighbour);
            else
                spans[i + 1] = (spans[i].From, spans[i + 1].To, spans[i + 1].Neighbour);

            spans.RemoveAt(i);

            // neighbours of the removed sliver may now be the same: merge them
            for (int j = spans.Count - 1; j > 0; j--)
            {
                if (spans[j].Neighbour == spans[j - 1].Neighbour)
                {
                    spans[j - 1] = (spans[j - 1].From, spans[j].To, spans[j - 1].Neighbour);
                    spans.RemoveAt(j);
                }
            }

            i = -1;
        }

        double ux = (end.X - start.X) / length, uy = (end.Y - start.Y) / length;

        foreach (var span in spans)
        {
            var pieceStart = span.From <= 0 ? start : new Point(start.X + ux * span.From, start.Y + uy * span.From);
            var pieceEnd = span.To >= length ? end : new Point(start.X + ux * span.To, start.Y + uy * span.To);

            yield return new BoundaryPiece(partIndex, ringIndex, edgeIndex, pieceStart, pieceEnd, span.To - span.From, span.Neighbour);
        }
    }

    private static void AddOrMerge(List<(double From, double To, int? Neighbour)> spans, double from, double to, int? neighbour)
    {
        if (spans.Count > 0 && spans[^1].Neighbour == neighbour)
            spans[^1] = (spans[^1].From, to, neighbour);
        else
            spans.Add((from, to, neighbour));
    }
}

/// <summary>The boundary of one polygon, split into shared and open pieces (see <see cref="PolygonAdjacency"/>).</summary>
public sealed class PolygonBoundary
{
    public PolygonBoundary(int polygonIndex, IReadOnlyList<BoundaryPiece> pieces)
    {
        PolygonIndex = polygonIndex;
        Pieces = pieces;
    }

    /// <summary>Index of the polygon in the analysed list.</summary>
    public int PolygonIndex { get; }

    /// <summary>All pieces, ring by ring in vertex order.</summary>
    public IReadOnlyList<BoundaryPiece> Pieces { get; }

    /// <summary>Indices of the polygons that share at least one piece of this boundary.</summary>
    public IReadOnlyList<int> Neighbours => Pieces.Where(p => p.NeighbourIndex.HasValue).Select(p => p.NeighbourIndex!.Value).Distinct().ToList();

    /// <summary>Total length shared with <paramref name="neighbourIndex"/>.</summary>
    public double GetSharedLength(int neighbourIndex) => Pieces.Where(p => p.NeighbourIndex == neighbourIndex).Sum(p => p.Length);
}

/// <summary>A straight piece of a polygon's boundary: part of one edge, shared with one neighbour or open.</summary>
public sealed class BoundaryPiece
{
    public BoundaryPiece(int partIndex, int ringIndex, int edgeIndex, Point start, Point end, double length, int? neighbourIndex)
    {
        PartIndex = partIndex;
        RingIndex = ringIndex;
        EdgeIndex = edgeIndex;
        Start = start;
        End = end;
        Length = length;
        NeighbourIndex = neighbourIndex;
    }

    /// <summary>Polygon part (0 for a Polygon; the part of a MultiPolygon).</summary>
    public int PartIndex { get; }

    /// <summary>Ring in the part (0 = exterior).</summary>
    public int RingIndex { get; }

    /// <summary>Edge in the ring: from vertex <c>EdgeIndex</c> to the next one.</summary>
    public int EdgeIndex { get; }

    public Point Start { get; }

    public Point End { get; }

    public double Length { get; }

    /// <summary>The polygon sharing this piece, or null when the piece is open.</summary>
    public int? NeighbourIndex { get; }

    public bool IsShared => NeighbourIndex.HasValue;
}
