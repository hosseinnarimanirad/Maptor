// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Delaunay triangulation of a set of 2D points (Bowyer–Watson incremental insertion).
/// Vertices are referenced by their index into <see cref="Points"/>, which preserves the
/// order of the input list. Exact duplicate points are skipped during insertion (triangles
/// always reference the first occurrence). If all points are collinear, <see cref="Triangles"/>
/// is empty.
/// </summary>
public class DelaunayTriangulation
{
    /// <summary>
    /// One triangle of the result: CCW vertex indices into <see cref="Points"/> and, per edge,
    /// the index of the adjacent triangle in <see cref="Triangles"/> (-1 on the convex hull).
    /// </summary>
    public readonly struct TriangleIndices
    {
        public int A { get; }
        public int B { get; }
        public int C { get; }

        public int NeighbourAB { get; }
        public int NeighbourBC { get; }
        public int NeighbourCA { get; }

        public TriangleIndices(int a, int b, int c, int neighbourAB, int neighbourBC, int neighbourCA)
        {
            A = a; B = b; C = c;

            NeighbourAB = neighbourAB; NeighbourBC = neighbourBC; NeighbourCA = neighbourCA;
        }

        public bool HasVertex(int pointIndex) => A == pointIndex || B == pointIndex || C == pointIndex;

        public override string ToString() => $"{A} {B} {C}";
    }

    private readonly List<Point> _points;

    private readonly List<TriangleIndices> _triangles;

    /// <summary>The input points, in their original order (duplicates included, but never referenced by triangles).</summary>
    public IReadOnlyList<Point> Points => _points;

    /// <summary>The Delaunay triangles; all vertex triples are counter-clockwise.</summary>
    public IReadOnlyList<TriangleIndices> Triangles => _triangles;

    private DelaunayTriangulation(List<Point> points, List<TriangleIndices> triangles)
    {
        _points = points;

        _triangles = triangles;
    }

    public static DelaunayTriangulation Create(IReadOnlyList<Point> points)
    {
        if (points is null)
            throw new ArgumentNullException(nameof(points));

        if (points.Count < 3)
            throw new ArgumentException("At least three points are required.", nameof(points));

        var copy = new List<Point>(points.Count);

        foreach (var point in points)
        {
            if (point is null || point.IsNaN())
                throw new ArgumentException("Points must not be null or NaN.", nameof(points));

            copy.Add(new Point(point.X, point.Y));
        }

        return new DelaunayTriangulation(copy, Build(copy));
    }

    public Triangle GetTriangle(int triangleIndex)
    {
        var t = _triangles[triangleIndex];

        return new Triangle(_points[t.A], _points[t.B], _points[t.C]);
    }

    /// <summary>
    /// Walks from <paramref name="startTriangleIndex"/> to the triangle containing
    /// <paramref name="point"/> (interior, edge, or vertex). Returns -1 when the point
    /// lies outside the convex hull or the triangulation is empty.
    /// </summary>
    public int FindContainingTriangle(Point point, int startTriangleIndex = 0)
    {
        if (_triangles.Count == 0)
            return -1;

        int current = (startTriangleIndex >= 0 && startTriangleIndex < _triangles.Count) ? startTriangleIndex : 0;

        int maxSteps = 4 * _triangles.Count + 16;

        for (int step = 0; step < maxSteps; step++)
        {
            var t = _triangles[current];

            Point a = _points[t.A], b = _points[t.B], c = _points[t.C];

            double oab = Orient(a, b, point, out double eab);
            double obc = Orient(b, c, point, out double ebc);
            double oca = Orient(c, a, point, out double eca);

            if (oab >= -eab && obc >= -ebc && oca >= -eca)
                return current;

            int next = -1;

            double worst = 0;

            // cross the most-violated edge that has a neighbour
            if (oab < -eab && t.NeighbourAB != -1 && oab < worst) { worst = oab; next = t.NeighbourAB; }
            if (obc < -ebc && t.NeighbourBC != -1 && obc < worst) { worst = obc; next = t.NeighbourBC; }
            if (oca < -eca && t.NeighbourCA != -1 && oca < worst) { worst = oca; next = t.NeighbourCA; }

            if (next == -1)
                return -1; // every violated edge is a hull edge; the domain is convex, so the point is outside

            current = next;
        }

        // walk did not converge (numerical cycling); fall back to a linear scan
        for (int i = 0; i < _triangles.Count; i++)
        {
            var t = _triangles[i];

            Point a = _points[t.A], b = _points[t.B], c = _points[t.C];

            if (Orient(a, b, point, out double eab) >= -eab &&
                Orient(b, c, point, out double ebc) >= -ebc &&
                Orient(c, a, point, out double eca) >= -eca)
                return i;
        }

        return -1;
    }

    #region Construction (Bowyer–Watson)

    private sealed class WorkTri
    {
        public int A, B, C;                 // CCW vertex indices

        public int NAB = -1, NBC = -1, NCA = -1; // neighbour triangle ids, -1 = none

        public bool Alive = true;
    }

    private static List<TriangleIndices> Build(List<Point> inputPoints)
    {
        int n = inputPoints.Count;

        var pts = new List<Point>(n + 3);

        pts.AddRange(inputPoints);

        AddSuperTriangleVertices(pts);

        var tris = new List<WorkTri> { new WorkTri { A = n, B = n + 1, C = n + 2 } };

        var seen = new Dictionary<(double, double), int>(n);

        int hint = 0;

        var cavity = new List<int>();

        var inCavity = new HashSet<int>();

        var pending = new Stack<int>();

        var boundary = new List<(int U, int V, int Outer, int Dead)>();

        var newTriByFirst = new Dictionary<int, int>();

        var newTriBySecond = new Dictionary<int, int>();

        for (int i = 0; i < n; i++)
        {
            Point p = pts[i];

            if (!seen.TryAdd((p.X, p.Y), i))
                continue; // exact duplicate; triangles keep referencing the first occurrence

            int seed = Locate(tris, pts, p, hint);

            if (seed == -1)
                throw new InvalidOperationException("Delaunay point location failed; the input may be numerically degenerate.");

            // grow the cavity: all triangles whose circumcircle contains p, connected to the seed
            cavity.Clear(); inCavity.Clear();

            pending.Push(seed); inCavity.Add(seed);

            while (pending.Count > 0)
            {
                int id = pending.Pop();

                cavity.Add(id);

                var t = tris[id];

                void VisitNeighbour(int nb)
                {
                    if (nb != -1 && !inCavity.Contains(nb) && InCircumcircle(tris[nb], pts, p))
                    {
                        inCavity.Add(nb);

                        pending.Push(nb);
                    }
                }

                VisitNeighbour(t.NAB); VisitNeighbour(t.NBC); VisitNeighbour(t.NCA);
            }

            // collect the boundary of the cavity (directed edges keep the cavity, and p, on their left)
            boundary.Clear();

            foreach (int id in cavity)
            {
                var t = tris[id];

                if (t.NAB == -1 || !inCavity.Contains(t.NAB)) boundary.Add((t.A, t.B, t.NAB, id));
                if (t.NBC == -1 || !inCavity.Contains(t.NBC)) boundary.Add((t.B, t.C, t.NBC, id));
                if (t.NCA == -1 || !inCavity.Contains(t.NCA)) boundary.Add((t.C, t.A, t.NCA, id));
            }

            foreach (int id in cavity)
                tris[id].Alive = false;

            // fan p to every boundary edge and stitch the fan together
            newTriByFirst.Clear(); newTriBySecond.Clear();

            foreach (var (u, v, outer, dead) in boundary)
            {
                int id = tris.Count;

                tris.Add(new WorkTri { A = u, B = v, C = i, NAB = outer });

                if (outer != -1)
                    ReplaceNeighbour(tris[outer], dead, id);

                newTriByFirst[u] = id;

                newTriBySecond[v] = id;
            }

            foreach (var (u, v, _, _) in boundary)
            {
                int id = newTriByFirst[u];

                tris[id].NBC = newTriByFirst.TryGetValue(v, out int next) ? next : -1;

                tris[id].NCA = newTriBySecond.TryGetValue(u, out int previous) ? previous : -1;
            }

            hint = tris.Count - 1;
        }

        return Compact(tris, n);
    }

    private static void AddSuperTriangleVertices(List<Point> pts)
    {
        double minX = pts[0].X, maxX = pts[0].X, minY = pts[0].Y, maxY = pts[0].Y;

        foreach (var p in pts)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        double cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;

        double size = 100 * Math.Max(Math.Max(maxX - minX, maxY - minY), 1e-6);

        // large CCW triangle comfortably containing the bounding box
        pts.Add(new Point(cx - 3 * size, cy - size));
        pts.Add(new Point(cx + 3 * size, cy - size));
        pts.Add(new Point(cx, cy + 3 * size));
    }

    private static int Locate(List<WorkTri> tris, List<Point> pts, Point p, int hint)
    {
        int current = (hint >= 0 && hint < tris.Count && tris[hint].Alive) ? hint : FindAnyAlive(tris);

        int maxSteps = 4 * tris.Count + 16;

        for (int step = 0; current != -1 && step < maxSteps; step++)
        {
            var t = tris[current];

            Point a = pts[t.A], b = pts[t.B], c = pts[t.C];

            double oab = Orient(a, b, p, out double eab);
            double obc = Orient(b, c, p, out double ebc);
            double oca = Orient(c, a, p, out double eca);

            if (oab >= -eab && obc >= -ebc && oca >= -eca)
                return current;

            int next = -1;

            double worst = 0;

            if (oab < -eab && t.NAB != -1 && oab < worst) { worst = oab; next = t.NAB; }
            if (obc < -ebc && t.NBC != -1 && obc < worst) { worst = obc; next = t.NBC; }
            if (oca < -eca && t.NCA != -1 && oca < worst) { worst = oca; next = t.NCA; }

            if (next == -1)
                break;

            current = next;
        }

        // fallback: linear scan over the alive triangles
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];

            if (!t.Alive)
                continue;

            Point a = pts[t.A], b = pts[t.B], c = pts[t.C];

            if (Orient(a, b, p, out double eab) >= -eab &&
                Orient(b, c, p, out double ebc) >= -ebc &&
                Orient(c, a, p, out double eca) >= -eca)
                return i;
        }

        return -1;
    }

    private static int FindAnyAlive(List<WorkTri> tris)
    {
        for (int i = tris.Count - 1; i >= 0; i--)
        {
            if (tris[i].Alive)
                return i;
        }

        return -1;
    }

    private static void ReplaceNeighbour(WorkTri triangle, int oldNeighbour, int newNeighbour)
    {
        if (triangle.NAB == oldNeighbour) triangle.NAB = newNeighbour;
        else if (triangle.NBC == oldNeighbour) triangle.NBC = newNeighbour;
        else if (triangle.NCA == oldNeighbour) triangle.NCA = newNeighbour;
        else throw new InvalidOperationException("Inconsistent triangle adjacency.");
    }

    private static List<TriangleIndices> Compact(List<WorkTri> tris, int pointCount)
    {
        var map = new int[tris.Count];

        var keptIds = new List<int>();

        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];

            bool keep = t.Alive && t.A < pointCount && t.B < pointCount && t.C < pointCount;

            map[i] = keep ? keptIds.Count : -1;

            if (keep)
                keptIds.Add(i);
        }

        var result = new List<TriangleIndices>(keptIds.Count);

        foreach (int id in keptIds)
        {
            var t = tris[id];

            result.Add(new TriangleIndices(
                t.A, t.B, t.C,
                t.NAB == -1 ? -1 : map[t.NAB],
                t.NBC == -1 ? -1 : map[t.NBC],
                t.NCA == -1 ? -1 : map[t.NCA]));
        }

        return result;
    }

    #endregion

    #region Predicates

    /// <summary>
    /// Twice the signed area of (a, b, p): positive when p lies left of the directed line a→b.
    /// <paramref name="errorBound"/> is the magnitude below which the sign is unreliable.
    /// </summary>
    private static double Orient(Point a, Point b, Point p, out double errorBound)
    {
        double l = (b.X - a.X) * (p.Y - a.Y);

        double r = (b.Y - a.Y) * (p.X - a.X);

        errorBound = 1e-12 * (Math.Abs(l) + Math.Abs(r));

        return l - r;
    }

    private static bool InCircumcircle(WorkTri t, List<Point> pts, Point p)
    {
        Point a = pts[t.A], b = pts[t.B], c = pts[t.C];

        double adx = a.X - p.X, ady = a.Y - p.Y;
        double bdx = b.X - p.X, bdy = b.Y - p.Y;
        double cdx = c.X - p.X, cdy = c.Y - p.Y;

        double aLift = adx * adx + ady * ady;
        double bLift = bdx * bdx + bdy * bdy;
        double cLift = cdx * cdx + cdy * cdy;

        double t1 = aLift * (bdx * cdy - cdx * bdy);
        double t2 = bLift * (adx * cdy - cdx * ady);
        double t3 = cLift * (adx * bdy - bdx * ady);

        double det = t1 - t2 + t3;

        double errorBound = 1e-12 * (Math.Abs(t1) + Math.Abs(t2) + Math.Abs(t3));

        // det > 0 <=> p strictly inside the circumcircle of the CCW triangle (a, b, c)
        return det > errorBound;
    }

    #endregion

    #region Voronoi

    /// <summary>The Voronoi diagram dual to this triangulation. See <see cref="VoronoiDiagram"/>.</summary>
    public VoronoiDiagram GetVoronoiDiagram()
    {
        return VoronoiDiagram.Create(this);
    }

    #endregion
}
