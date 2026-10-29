using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Topological defects of a simplified geometry that the raster measures cannot see: rings that collapsed (fewer than
/// three distinct vertices, i.e. no area) and proper crossings between non-adjacent edges of the same ring or line
/// (self-intersections). Used by E12 on polygons with thin parts.
/// </summary>
public static class TopologyCounts
{
    /// <summary>Rings of polygons with fewer than three distinct vertices (a ring reduced to a point or a segment).</summary>
    public static int CollapsedRings(Geometry<Point> geometry)
    {
        return Rings(geometry).Count(ring => ring.Select(p => (p.X, p.Y)).Distinct().Count() < 3);
    }

    /// <summary>Proper crossings between non-adjacent edges of the same ring or line string, summed over the geometry.</summary>
    public static int SelfIntersections(Geometry<Point> geometry)
    {
        var count = 0;

        foreach (var (points, isRing) in Chains(geometry))
        {
            var n = points.Count;

            // edges i = (points[i], points[i+1]); a closed ring repeats its first point at the end
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 2; j < n - 1; j++)
                {
                    // the first and the last edge of a closed ring share its first point
                    if (isRing && i == 0 && j == n - 2)
                        continue;

                    if (ProperlyCross(points[i], points[i + 1], points[j], points[j + 1]))
                        count++;
                }
            }
        }

        return count;
    }

    private static IEnumerable<List<Point>> Rings(Geometry<Point> geometry)
        => Chains(geometry).Where(c => c.IsRing).Select(c => c.Points);

    private static IEnumerable<(List<Point> Points, bool IsRing)> Chains(Geometry<Point> geometry)
    {
        if (geometry is null || geometry.IsNullOrEmpty())
            yield break;

        switch (geometry.Type)
        {
            case GeometryType.LineString:
                yield return (geometry.Points, false);
                break;

            case GeometryType.Polygon:
                foreach (var ring in geometry.Geometries ?? new())
                    yield return (Closed(ring.Points), true);
                break;

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                foreach (var part in geometry.Geometries ?? new())
                    foreach (var chain in Chains(part))
                        yield return chain;
                break;
        }
    }

    private static List<Point> Closed(List<Point> ring)
    {
        if (ring.Count > 0 && (ring[0].X != ring[^1].X || ring[0].Y != ring[^1].Y))
            return ring.Append(ring[0]).ToList();

        return ring;
    }

    // strict crossing: each segment's end points lie on opposite sides of the other (touching or collinear overlap not counted)
    private static bool ProperlyCross(Point a, Point b, Point c, Point d)
    {
        double Orient(Point p, Point q, Point r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);

        var d1 = Orient(c, d, a);
        var d2 = Orient(c, d, b);
        var d3 = Orient(a, b, c);
        var d4 = Orient(a, b, d);

        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }
}
