using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>Where a boundary piece of one polygon lies relative to another polygon.</summary>
public enum PieceLocation
{
    Outside = 0,

    Inside = 1,

    /// <summary>On the other polygon's boundary, running the same way (both polygons on the same side).</summary>
    OnSameDirection = 2,

    /// <summary>On the other polygon's boundary, running the other way (the polygons on either side).</summary>
    OnOppositeDirection = 3,
}

/// <summary>
/// A polygon or multipolygon flattened into a local plane in metres, its rings oriented the house way: outer
/// rings counterclockwise, holes clockwise. Rings are point lists without the closing repeat.
/// </summary>
public sealed class PlanarPolygon
{
    private PlanarPolygon(IReadOnlyList<(double X, double Y)[]> rings)
    {
        Rings = rings;

        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;

        foreach (var ring in rings)
        {
            foreach (var (x, y) in ring)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        Box = (minX, minY, maxX, maxY);
    }

    public IReadOnlyList<(double X, double Y)[]> Rings { get; }

    public (double MinX, double MinY, double MaxX, double MaxY) Box { get; }

    public double Area => Rings.Sum(SignedArea);

    public static PlanarPolygon From<T>(Geometry<T> geometry, LocalMetricFrame frame) where T : IPoint, new()
    {
        var rings = new List<(double X, double Y)[]>();

        foreach (var polygon in PolygonMeasure.PolygonsOf(geometry))
        {
            var polygonRings = PolygonMeasure.RingsOf(polygon);

            for (int r = 0; r < polygonRings.Count; r++)
            {
                var ring = polygonRings[r].Select(p => frame.ToMetres(p)).Select(m => (m.East, m.North)).ToArray();

                var area = SignedArea(ring);

                if (area == 0)
                    continue;

                // Outer counterclockwise (positive), holes clockwise (negative).
                if ((r == 0) != (area > 0))
                    Array.Reverse(ring);

                rings.Add(ring);
            }
        }

        return new PlanarPolygon(rings);
    }

    public static double SignedArea((double X, double Y)[] ring)
    {
        var sum = 0.0;

        for (int i = 0; i < ring.Length; i++)
        {
            var (x1, y1) = ring[i];
            var (x2, y2) = ring[(i + 1) % ring.Length];

            sum += x1 * y2 - x2 * y1;
        }

        return sum / 2;
    }

    /// <summary>Even–odd test over every ring.</summary>
    public bool Contains(double x, double y)
    {
        var inside = false;

        foreach (var ring in Rings)
        {
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                var (xi, yi) = ring[i];
                var (xj, yj) = ring[j];

                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                    inside = !inside;
            }
        }

        return inside;
    }

    public IEnumerable<((double X, double Y) Start, (double X, double Y) End)> Edges()
    {
        foreach (var ring in Rings)
        {
            for (int i = 0; i < ring.Length; i++)
                yield return (ring[i], ring[(i + 1) % ring.Length]);
        }
    }

    public bool BoxesTouch(PlanarPolygon other, double margin)
        => Box.MinX - margin <= other.Box.MaxX && other.Box.MinX - margin <= Box.MaxX
        && Box.MinY - margin <= other.Box.MaxY && other.Box.MinY - margin <= Box.MaxY;
}

/// <summary>One straight piece of a polygon's boundary, in the plane of a <see cref="PlanarPolygon"/>.</summary>
public readonly struct RingPiece
{
    public RingPiece((double X, double Y) start, (double X, double Y) end, int ringIndex)
    {
        Start = start;
        End = end;
        RingIndex = ringIndex;
    }

    public (double X, double Y) Start { get; }

    public (double X, double Y) End { get; }

    public int RingIndex { get; }

    public double Length => Math.Sqrt((End.X - Start.X) * (End.X - Start.X) + (End.Y - Start.Y) * (End.Y - Start.Y));

    public (double X, double Y) Middle => ((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);
}

/// <summary>
/// Splits a polygon's boundary where it meets other polygons (crossings, touching vertices, shared stretches)
/// and tells where each piece lies relative to another polygon. Shared edges, vertices on the other boundary and
/// holes are handled, which is what overlap and gap measurements need.
/// </summary>
public static class BoundaryPieces
{
    /// <summary>
    /// The boundary of <paramref name="polygon"/> cut at every place it meets any of <paramref name="others"/>.
    /// A vertex of another polygon within <paramref name="tolerance"/> metres of an edge cuts the edge too.
    /// </summary>
    public static IReadOnlyList<RingPiece> Split(PlanarPolygon polygon, IReadOnlyList<PlanarPolygon> others, double tolerance)
    {
        var otherEdges = others.SelectMany(o => o.Edges()).ToList();

        var cell = Math.Max(1e-6, otherEdges.Select(e => Math.Max(Math.Abs(e.End.X - e.Start.X), Math.Abs(e.End.Y - e.Start.Y))).DefaultIfEmpty(1).Max());

        var grid = new BoxGrid<int>(cell);

        for (int i = 0; i < otherEdges.Count; i++)
        {
            var (s, e) = otherEdges[i];
            grid.Add(i, Math.Min(s.X, e.X) - tolerance, Math.Min(s.Y, e.Y) - tolerance, Math.Max(s.X, e.X) + tolerance, Math.Max(s.Y, e.Y) + tolerance);
        }

        var pieces = new List<RingPiece>();

        for (int r = 0; r < polygon.Rings.Count; r++)
        {
            var ring = polygon.Rings[r];

            for (int i = 0; i < ring.Length; i++)
            {
                var p = ring[i];
                var q = ring[(i + 1) % ring.Length];

                var dx = q.X - p.X;
                var dy = q.Y - p.Y;
                var lengthSquared = dx * dx + dy * dy;

                if (lengthSquared <= 0)
                    continue;

                var cuts = new List<double> { 0, 1 };

                foreach (var index in grid.Query(Math.Min(p.X, q.X), Math.Min(p.Y, q.Y), Math.Max(p.X, q.X), Math.Max(p.Y, q.Y)))
                {
                    var (c, d) = otherEdges[index];

                    if (Intersect(p, q, c, d, out var t))
                        cuts.Add(t);

                    foreach (var v in new[] { c, d })
                    {
                        var tv = ((v.X - p.X) * dx + (v.Y - p.Y) * dy) / lengthSquared;

                        if (tv <= 0 || tv >= 1)
                            continue;

                        var fx = p.X + tv * dx - v.X;
                        var fy = p.Y + tv * dy - v.Y;

                        if (fx * fx + fy * fy <= tolerance * tolerance)
                            cuts.Add(tv);
                    }
                }

                cuts.Sort();

                var previous = cuts[0];

                foreach (var t in cuts.Skip(1))
                {
                    if ((t - previous) * Math.Sqrt(lengthSquared) <= 1e-9)
                        continue;

                    pieces.Add(new RingPiece((p.X + previous * dx, p.Y + previous * dy), (p.X + t * dx, p.Y + t * dy), r));

                    previous = t;
                }
            }
        }

        return pieces;
    }

    /// <summary>Where <paramref name="piece"/> lies relative to <paramref name="other"/>.</summary>
    public static PieceLocation Locate(RingPiece piece, PlanarPolygon other, double tolerance)
    {
        var (mx, my) = piece.Middle;

        var nearest = double.MaxValue;
        var nearestDot = 0.0;

        foreach (var (s, e) in other.Edges())
        {
            var distance = LineCoverage.PointSegmentDistance(mx, my, s.X, s.Y, e.X, e.Y);

            if (distance < nearest)
            {
                nearest = distance;
                nearestDot = (piece.End.X - piece.Start.X) * (e.X - s.X) + (piece.End.Y - piece.Start.Y) * (e.Y - s.Y);
            }
        }

        if (nearest <= tolerance)
            return nearestDot >= 0 ? PieceLocation.OnSameDirection : PieceLocation.OnOppositeDirection;

        return other.Contains(mx, my) ? PieceLocation.Inside : PieceLocation.Outside;
    }

    private static bool Intersect((double X, double Y) a0, (double X, double Y) a1, (double X, double Y) b0, (double X, double Y) b1, out double t)
    {
        t = 0;

        var rx = a1.X - a0.X;
        var ry = a1.Y - a0.Y;
        var sx = b1.X - b0.X;
        var sy = b1.Y - b0.Y;

        var denominator = rx * sy - ry * sx;

        if (Math.Abs(denominator) <= 1e-15 * Math.Max(rx * rx + ry * ry, sx * sx + sy * sy))
            return false;

        var qx = b0.X - a0.X;
        var qy = b0.Y - a0.Y;

        t = (qx * sy - qy * sx) / denominator;
        var u = (qx * ry - qy * rx) / denominator;

        return t > 0 && t < 1 && u >= 0 && u <= 1;
    }
}
