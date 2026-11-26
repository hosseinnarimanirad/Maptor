using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>Empty space fully surrounded by two or more polygons.</summary>
public sealed class EnclosedGap<T> where T : IPoint, new()
{
    public EnclosedGap(IReadOnlyList<int> polygonIndices, double area, Geometry<T> outline)
    {
        PolygonIndices = polygonIndices;
        Area = area;
        Outline = outline;
    }

    /// <summary>The polygons around the gap, as indices into the input list, ascending.</summary>
    public IReadOnlyList<int> PolygonIndices { get; }

    /// <summary>Square metres when geodetic, square coordinate units otherwise.</summary>
    public double Area { get; }

    /// <summary>The gap as a polygon, in the data's coordinates.</summary>
    public Geometry<T> Outline { get; }
}

/// <summary>
/// Finds the holes in a set of polygons taken together: the pieces of their boundaries that touch no other polygon
/// form closed loops; the outside of a block runs counterclockwise and a hole in it clockwise. A clockwise loop
/// made from the boundaries of two or more polygons is an enclosed gap. (A hole of a single polygon, such as a
/// courtyard, is the polygon's own and is not reported.) Groups of polygons whose boxes touch are measured each in
/// its own <see cref="LocalMetricFrame"/>.
/// </summary>
public static class EnclosedGaps
{
    /// <param name="polygons">Polygons or multipolygons; null entries are skipped.</param>
    /// <param name="isGeodetic">True for WGS84 longitude/latitude.</param>
    /// <param name="tolerance">Boundaries closer than this (metres) count as shared, and loop ends this close join.</param>
    public static IReadOnlyList<EnclosedGap<T>> Find<T>(IReadOnlyList<Geometry<T>?> polygons, bool isGeodetic, double tolerance) where T : IPoint, new()
    {
        if (polygons is null)
            throw new ArgumentNullException(nameof(polygons));

        tolerance = Math.Max(tolerance, 1e-9);

        var valid = Enumerable.Range(0, polygons.Count).Where(i => polygons[i] is not null && PolygonMeasure.PolygonsOf(polygons[i]!).Count > 0).ToList();

        var gaps = new List<EnclosedGap<T>>();

        foreach (var group in Groups(polygons, valid, tolerance, isGeodetic))
        {
            if (group.Count < 2)
                continue;

            var box = group.Select(i => polygons[i]!.GetBoundingBox()).Aggregate((x, y) => x.Add(y));
            var frame = LocalMetricFrame.At(box.Center, isGeodetic);

            var planar = group.ToDictionary(i => i, i => PlanarPolygon.From(polygons[i]!, frame));

            var free = new List<(RingPiece Piece, int Polygon)>();

            foreach (var i in group)
            {
                var neighbours = group.Where(j => j != i && planar[i].BoxesTouch(planar[j], tolerance)).Select(j => planar[j]).ToList();

                foreach (var piece in BoundaryPieces.Split(planar[i], neighbours, tolerance))
                {
                    if (neighbours.All(n => BoundaryPieces.Locate(piece, n, tolerance) == PieceLocation.Outside))
                        free.Add((piece, i));
                }
            }

            foreach (var loop in Loops(free, tolerance))
            {
                var contributors = loop.Select(l => l.Polygon).Distinct().OrderBy(i => i).ToList();

                if (contributors.Count < 2)
                    continue;

                var ring = loop.Select(l => l.Piece.Start).ToArray();

                var signed = PlanarPolygon.SignedArea(ring);

                if (signed >= 0)
                    continue;

                var points = ring.Reverse().Select(p => frame.FromMetres(p.X, p.Y)).Select(c => new T { X = c.X, Y = c.Y }).ToList();

                var outline = Geometry<T>.CreatePolygon(points, polygons[group[0]]!.Srid);

                gaps.Add(new EnclosedGap<T>(contributors, -signed, outline));
            }
        }

        return gaps.OrderBy(g => g.PolygonIndices[0]).ThenBy(g => g.Area).ToList();
    }

    /// <summary>Groups of polygons whose boxes touch, directly or through others.</summary>
    private static IEnumerable<List<int>> Groups<T>(IReadOnlyList<Geometry<T>?> polygons, List<int> valid, double tolerance, bool isGeodetic) where T : IPoint, new()
    {
        var boxes = valid.ToDictionary(i => i, i => polygons[i]!.GetBoundingBox());

        var parent = valid.ToDictionary(i => i, i => i);

        int Find(int i)
        {
            while (parent[i] != i)
                i = parent[i] = parent[parent[i]];

            return i;
        }

        if (valid.Count == 0)
            return Enumerable.Empty<List<int>>();

        var (dx, dy) = LocalMetricFrame.At(boxes[valid[0]].Center, isGeodetic).SearchHalfSize(tolerance);

        var sizes = valid.Select(i => Math.Max(boxes[i].Width, boxes[i].Height)).OrderBy(s => s).ToList();
        var grid = new BoxGrid<int>(Math.Max(sizes[sizes.Count / 2], 1e-9));

        foreach (var i in valid)
            grid.Add(i, boxes[i].XMin - dx, boxes[i].YMin - dy, boxes[i].XMax + dx, boxes[i].YMax + dy);

        foreach (var a in valid)
        {
            var x = boxes[a];

            foreach (var b in grid.Query(x.XMin - dx, x.YMin - dy, x.XMax + dx, x.YMax + dy))
            {
                if (b <= a)
                    continue;

                var y = boxes[b];

                if (x.XMin - dx <= y.XMax && y.XMin - dx <= x.XMax && x.YMin - dy <= y.YMax && y.YMin - dy <= x.YMax)
                {
                    var ra = Find(a);
                    var rb = Find(b);

                    if (ra != rb)
                        parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
                }
            }
        }

        return valid.GroupBy(Find).Select(g => g.OrderBy(i => i).ToList());
    }

    /// <summary>Chains pieces end to start into closed loops; pieces that close no loop are dropped.</summary>
    private static IEnumerable<List<(RingPiece Piece, int Polygon)>> Loops(List<(RingPiece Piece, int Polygon)> pieces, double tolerance)
    {
        var used = new bool[pieces.Count];

        double Gap((double X, double Y) p, (double X, double Y) q) => Math.Sqrt((p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y));

        for (int first = 0; first < pieces.Count; first++)
        {
            if (used[first])
                continue;

            var loop = new List<int> { first };
            used[first] = true;

            var closed = false;

            while (true)
            {
                var end = pieces[loop[loop.Count - 1]].Piece.End;

                if (loop.Count > 2 && Gap(end, pieces[first].Piece.Start) <= tolerance)
                {
                    closed = true;
                    break;
                }

                var next = -1;
                var best = double.MaxValue;

                for (int k = 0; k < pieces.Count; k++)
                {
                    if (used[k])
                        continue;

                    var gap = Gap(end, pieces[k].Piece.Start);

                    if (gap <= tolerance && gap < best)
                    {
                        best = gap;
                        next = k;
                    }
                }

                if (next < 0)
                    break;

                used[next] = true;
                loop.Add(next);
            }

            if (closed)
                yield return loop.Select(k => pieces[k]).ToList();
        }
    }
}
