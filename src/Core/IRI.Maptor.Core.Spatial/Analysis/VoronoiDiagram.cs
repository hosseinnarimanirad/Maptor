// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Voronoi diagram of a set of 2D sites, built as the dual of the Delaunay triangulation.
/// <see cref="Vertices"/>[i] is the circumcenter of <see cref="DelaunayTriangulation.Triangles"/>[i].
/// Cells of sites on the convex hull are unbounded (<see cref="Cell.IsClosed"/> is false); their
/// two infinite rays appear in <see cref="Edges"/> with <see cref="Edge.VertexB"/> == -1.
/// </summary>
public class VoronoiDiagram
{
    /// <summary>
    /// One Voronoi edge: the perpendicular bisector segment (or ray) separating the cells of
    /// <see cref="SiteA"/> and <see cref="SiteB"/>. When <see cref="VertexB"/> is -1 the edge is an
    /// infinite ray starting at <see cref="VertexA"/> with unit direction (<see cref="DirectionX"/>, <see cref="DirectionY"/>).
    /// </summary>
    public readonly struct Edge
    {
        public int SiteA { get; }
        public int SiteB { get; }

        public int VertexA { get; }
        public int VertexB { get; }

        public double DirectionX { get; }
        public double DirectionY { get; }

        public bool IsRay => VertexB == -1;

        public Edge(int siteA, int siteB, int vertexA, int vertexB, double directionX = 0, double directionY = 0)
        {
            SiteA = siteA; SiteB = siteB;

            VertexA = vertexA; VertexB = vertexB;

            DirectionX = directionX; DirectionY = directionY;
        }
    }

    /// <summary>
    /// The Voronoi cell of one site: its vertex indices in counter-clockwise order.
    /// For hull sites the cell is unbounded: <see cref="IsClosed"/> is false and
    /// <see cref="VertexIndices"/> is the finite chain between the two infinite rays.
    /// Sites never referenced by the triangulation (duplicates, collinear input) get an empty cell.
    /// </summary>
    public sealed class Cell
    {
        public int SiteIndex { get; }

        public IReadOnlyList<int> VertexIndices { get; }

        public bool IsClosed { get; }

        internal Cell(int siteIndex, IReadOnlyList<int> vertexIndices, bool isClosed)
        {
            SiteIndex = siteIndex;

            VertexIndices = vertexIndices;

            IsClosed = isClosed;
        }
    }

    public DelaunayTriangulation Triangulation { get; }

    /// <summary>The input sites; same list (and indices) as <see cref="DelaunayTriangulation.Points"/>.</summary>
    public IReadOnlyList<Point> Sites => Triangulation.Points;

    /// <summary>Voronoi vertices: the circumcenter of each Delaunay triangle, index-aligned with <see cref="DelaunayTriangulation.Triangles"/>.</summary>
    public IReadOnlyList<Point> Vertices { get; }

    public IReadOnlyList<Edge> Edges { get; }

    /// <summary>One cell per site, index-aligned with <see cref="Sites"/>.</summary>
    public IReadOnlyList<Cell> Cells { get; }

    private VoronoiDiagram(DelaunayTriangulation triangulation, List<Point> vertices, List<Edge> edges, List<Cell> cells)
    {
        Triangulation = triangulation;

        Vertices = vertices;

        Edges = edges;

        Cells = cells;
    }

    public static VoronoiDiagram Create(IReadOnlyList<Point> sites)
    {
        return Create(DelaunayTriangulation.Create(sites));
    }

    public static VoronoiDiagram Create(DelaunayTriangulation triangulation)
    {
        if (triangulation is null)
            throw new ArgumentNullException(nameof(triangulation));

        var triangles = triangulation.Triangles;

        var points = triangulation.Points;

        var vertices = new List<Point>(triangles.Count);

        foreach (var t in triangles)
        {
            var center = TopologyUtility.CalculateCircumcenterCenterPoint(points[t.A], points[t.B], points[t.C]);

            vertices.Add(new Point(center.X, center.Y));
        }

        return new VoronoiDiagram(triangulation, vertices, BuildEdges(triangulation), BuildCells(triangulation));
    }

    private static List<Edge> BuildEdges(DelaunayTriangulation triangulation)
    {
        var triangles = triangulation.Triangles;

        var points = triangulation.Points;

        var edges = new List<Edge>();

        for (int i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];

            AddEdge(edges, points, i, t.A, t.B, t.NeighbourAB);
            AddEdge(edges, points, i, t.B, t.C, t.NeighbourBC);
            AddEdge(edges, points, i, t.C, t.A, t.NeighbourCA);
        }

        return edges;
    }

    private static void AddEdge(List<Edge> edges, IReadOnlyList<Point> points, int triangleIndex, int u, int v, int neighbour)
    {
        if (neighbour == -1)
        {
            // hull edge: infinite ray along the perpendicular bisector of (u, v), pointing outward.
            // (u, v) is CCW in the triangle, so the interior lies to its left and outward is to its right.
            double dx = points[v].X - points[u].X;

            double dy = points[v].Y - points[u].Y;

            double length = Math.Sqrt(dx * dx + dy * dy);

            edges.Add(new Edge(u, v, triangleIndex, -1, dy / length, -dx / length));
        }
        else if (neighbour > triangleIndex)
        {
            // interior Delaunay edge, emitted once (by the lower-indexed triangle)
            edges.Add(new Edge(u, v, triangleIndex, neighbour));
        }
    }

    private static List<Cell> BuildCells(DelaunayTriangulation triangulation)
    {
        var triangles = triangulation.Triangles;

        int siteCount = triangulation.Points.Count;

        // one incident triangle per site
        var incident = new int[siteCount];

        for (int i = 0; i < siteCount; i++)
            incident[i] = -1;

        for (int i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];

            incident[t.A] = i; incident[t.B] = i; incident[t.C] = i;
        }

        var cells = new List<Cell>(siteCount);

        for (int site = 0; site < siteCount; site++)
        {
            if (incident[site] == -1)
            {
                cells.Add(new Cell(site, Array.Empty<int>(), isClosed: false));

                continue;
            }

            // for hull sites, rewind clockwise to the first triangle of the fan
            int start = incident[site];

            int current = start;

            for (int step = 0; step <= triangles.Count; step++)
            {
                int previous = PreviousClockwise(triangles[current], site);

                if (previous == -1 || previous == start)
                    break;

                current = previous;
            }

            // collect the fan counter-clockwise; circumcenters come out in CCW order around the site
            var fan = new List<int>();

            int walker = current;

            bool isClosed = false;

            for (int step = 0; step <= triangles.Count; step++)
            {
                fan.Add(walker);

                walker = NextCounterClockwise(triangles[walker], site);

                if (walker == current)
                {
                    isClosed = true;

                    break;
                }

                if (walker == -1)
                    break;
            }

            cells.Add(new Cell(site, fan, isClosed));
        }

        return cells;
    }

    // in a CCW triangle the sector at vertex s spans from its outgoing to its incoming edge;
    // the CCW-next triangle around s is across the incoming edge, the CW-previous across the outgoing one
    private static int NextCounterClockwise(DelaunayTriangulation.TriangleIndices t, int site)
        => site == t.A ? t.NeighbourCA : site == t.B ? t.NeighbourAB : t.NeighbourBC;

    private static int PreviousClockwise(DelaunayTriangulation.TriangleIndices t, int site)
        => site == t.A ? t.NeighbourAB : site == t.B ? t.NeighbourBC : t.NeighbourCA;
}
