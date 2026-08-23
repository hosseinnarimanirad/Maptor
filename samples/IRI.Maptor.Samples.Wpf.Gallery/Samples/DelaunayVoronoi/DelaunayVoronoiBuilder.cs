using System;
using System.Collections.Generic;
using System.Diagnostics;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.DelaunayVoronoi;

/// <summary>
/// Scatters random points over an extent and turns them into the two dual structures Maptor
/// computes for a point set: the Delaunay triangulation and its Voronoi diagram, both ready to be
/// drawn as map layers.
/// <para>
/// There is no WPF in this file. Everything here is <c>IRI.Maptor.Core.Spatial</c>, so the same code
/// runs in a console tool, a service or a web API.
/// </para>
/// </summary>
public static class DelaunayVoronoiBuilder
{
    /// <summary>Three points is the smallest set that has a triangle.</summary>
    public const int MinimumPointCount = 3;

    /// <summary>Everything one Generate produces, as features in the map's own reference system.</summary>
    /// <param name="Sites">The input points.</param>
    /// <param name="Triangles">One polygon per Delaunay triangle.</param>
    /// <param name="Cells">One polygon per Voronoi cell, clipped to the extent.</param>
    /// <param name="UnboundedCellCount">Cells of sites on the convex hull; these were clipped open.</param>
    /// <param name="Elapsed">How long the whole build took, computation and feature conversion.</param>
    public sealed record Result(
        IReadOnlyList<Feature<Point>> Sites,
        IReadOnlyList<Feature<Point>> Triangles,
        IReadOnlyList<Feature<Point>> Cells,
        int UnboundedCellCount,
        TimeSpan Elapsed);

    /// <summary>
    /// Generates <paramref name="pointCount"/> random points inside <paramref name="extent"/> and
    /// builds both structures from them.
    /// </summary>
    /// <param name="extent">
    /// The area to scatter points over, in the map's reference system. Both algorithms are planar,
    /// so the coordinates must already be projected; pass a Web Mercator extent, not longitude and
    /// latitude.
    /// </param>
    /// <param name="seed">A fixed seed to reproduce a point set, or null for a new one every call.</param>
    public static Result Build(int pointCount, BoundingBox extent, int? seed = null)
    {
        if (pointCount < MinimumPointCount)
            throw new ArgumentOutOfRangeException(nameof(pointCount), pointCount, $"At least {MinimumPointCount} points are required.");

        var watch = Stopwatch.StartNew();

        var sites = CreateRandomPoints(pointCount, extent, seed);

        // The whole computation is these two lines; everything below only turns the result into features.
        var triangulation = DelaunayTriangulation.Create(sites);

        var voronoi = triangulation.GetVoronoiDiagram();

        var triangleFeatures = BuildTriangleFeatures(triangulation);

        var cellFeatures = BuildCellFeatures(voronoi, extent, out int unboundedCellCount);

        var siteFeatures = BuildSiteFeatures(sites);

        watch.Stop();

        return new Result(siteFeatures, triangleFeatures, cellFeatures, unboundedCellCount, watch.Elapsed);
    }

    // ------------------------------------------------------------------ input

    /// <summary>Uniform random points, kept a little inside the extent so nothing sits on its edge.</summary>
    private static List<Point> CreateRandomPoints(int pointCount, BoundingBox extent, int? seed)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        double marginX = extent.Width * 0.02;

        double marginY = extent.Height * 0.02;

        double xMin = extent.XMin + marginX;

        double yMin = extent.YMin + marginY;

        double width = extent.Width - 2 * marginX;

        double height = extent.Height - 2 * marginY;

        var points = new List<Point>(pointCount);

        for (int i = 0; i < pointCount; i++)
            points.Add(new Point(xMin + random.NextDouble() * width, yMin + random.NextDouble() * height));

        return points;
    }

    // ------------------------------------------------------------------ Delaunay

    private static List<Feature<Point>> BuildTriangleFeatures(DelaunayTriangulation triangulation)
    {
        var points = triangulation.Points;

        var features = new List<Feature<Point>>(triangulation.Triangles.Count);

        for (int i = 0; i < triangulation.Triangles.Count; i++)
        {
            var triangle = triangulation.Triangles[i];

            var a = points[triangle.A];

            var b = points[triangle.B];

            var c = points[triangle.C];

            var geometry = Geometry<Point>.CreatePolygon([a, b, c], SridHelper.WebMercator);

            // The smallest angle is what Delaunay maximises: no other triangulation of the same
            // points has a larger minimum angle, which is why its triangles look so even.
            features.Add(new Feature<Point>(geometry, new Dictionary<string, object>
            {
                ["Id"] = i,
                ["Min angle (deg)"] = Math.Round(SmallestAngleInDegrees(a, b, c), 1),
                ["Area (km2)"] = Math.Round(AreaInSquareKilometres([a, b, c]), 1),
            }));
        }

        return features;
    }

    private static double SmallestAngleInDegrees(Point a, Point b, Point c)
    {
        double ab = Distance(a, b);

        double bc = Distance(b, c);

        double ca = Distance(c, a);

        // law of cosines, once per vertex; the shortest side faces the smallest angle
        double angleA = AngleInDegrees(ab, ca, bc);

        double angleB = AngleInDegrees(ab, bc, ca);

        double angleC = AngleInDegrees(bc, ca, ab);

        return Math.Min(angleA, Math.Min(angleB, angleC));
    }

    private static double AngleInDegrees(double adjacent1, double adjacent2, double opposite)
    {
        double denominator = 2 * adjacent1 * adjacent2;

        if (denominator == 0)
            return 0;

        double cosine = (adjacent1 * adjacent1 + adjacent2 * adjacent2 - opposite * opposite) / denominator;

        return Math.Acos(Math.Clamp(cosine, -1, 1)) * 180 / Math.PI;
    }

    // ------------------------------------------------------------------ Voronoi

    /// <summary>
    /// One polygon per cell, clipped to <paramref name="extent"/>. Cells of sites on the convex hull
    /// are unbounded: their two infinite rays are first run out to a rectangle large enough to hold
    /// every circumcentre, closed along that rectangle, and only then clipped back to the extent, so
    /// the finished diagram tiles the whole area without gaps or cut corners.
    /// </summary>
    private static List<Feature<Point>> BuildCellFeatures(VoronoiDiagram voronoi, BoundingBox extent, out int unboundedCellCount)
    {
        unboundedCellCount = 0;

        var features = new List<Feature<Point>>(voronoi.Cells.Count);

        if (voronoi.Cells.Count == 0)
            return features;

        var outer = OuterRectangle(voronoi, extent);

        var (startRays, endRays) = RayDirectionsBySite(voronoi);

        foreach (var cell in voronoi.Cells)
        {
            // sites the triangulation never referenced (duplicates, collinear input) have no cell
            if (cell.VertexIndices.Count == 0)
                continue;

            List<Point>? ring = cell.IsClosed
                ? ClosedCellRing(cell, voronoi)
                : OpenCellRing(cell, voronoi, outer, startRays, endRays);

            if (ring is null)
                continue;

            var clipped = ClipToRectangle(ring, extent);

            if (clipped.Count < 3)
                continue;

            if (!cell.IsClosed)
                unboundedCellCount++;

            var geometry = Geometry<Point>.CreatePolygon(clipped, SridHelper.WebMercator);

            features.Add(new Feature<Point>(geometry, new Dictionary<string, object>
            {
                ["Site"] = cell.SiteIndex,
                ["Vertices"] = clipped.Count,
                ["Unbounded"] = !cell.IsClosed,
                ["Area (km2)"] = Math.Round(AreaInSquareKilometres(clipped), 1),
            }));
        }

        return features;
    }

    /// <summary>The circumcentres of a closed cell, already counter-clockwise around its site.</summary>
    private static List<Point> ClosedCellRing(VoronoiDiagram.Cell cell, VoronoiDiagram voronoi)
    {
        var ring = new List<Point>(cell.VertexIndices.Count);

        foreach (var vertexIndex in cell.VertexIndices)
            ring.Add(voronoi.Vertices[vertexIndex]);

        return ring;
    }

    /// <summary>
    /// A hull site's cell, closed against <paramref name="outer"/>: in from infinity along the first
    /// ray, through the finite chain of circumcentres, out along the second ray, then back around the
    /// rectangle. Returns null when the diagram is degenerate and a ray is missing.
    /// </summary>
    private static List<Point>? OpenCellRing(
        VoronoiDiagram.Cell cell,
        VoronoiDiagram voronoi,
        BoundingBox outer,
        IReadOnlyDictionary<int, (double X, double Y)> startRays,
        IReadOnlyDictionary<int, (double X, double Y)> endRays)
    {
        if (!startRays.TryGetValue(cell.SiteIndex, out var startDirection) ||
            !endRays.TryGetValue(cell.SiteIndex, out var endDirection))
            return null;

        var fan = cell.VertexIndices;

        var first = voronoi.Vertices[fan[0]];

        var last = voronoi.Vertices[fan[fan.Count - 1]];

        var ring = new List<Point>(fan.Count + 6) { ExitPoint(first, startDirection.X, startDirection.Y, outer) };

        foreach (var vertexIndex in fan)
            ring.Add(voronoi.Vertices[vertexIndex]);

        var exit = ExitPoint(last, endDirection.X, endDirection.Y, outer);

        ring.Add(exit);

        AppendCornersBetween(ring, outer, exit, ring[0]);

        return ring;
    }

    /// <summary>
    /// The outward ray of each hull site, split by which end of its fan the ray closes.
    /// The fan is walked counter-clockwise, so the ray it starts from is the one whose Delaunay hull
    /// edge leaves the site (<c>SiteA</c>) and the ray it ends on is the one that arrives (<c>SiteB</c>).
    /// </summary>
    private static (Dictionary<int, (double X, double Y)> Start, Dictionary<int, (double X, double Y)> End)
        RayDirectionsBySite(VoronoiDiagram voronoi)
    {
        var start = new Dictionary<int, (double X, double Y)>();

        var end = new Dictionary<int, (double X, double Y)>();

        foreach (var edge in voronoi.Edges)
        {
            if (!edge.IsRay)
                continue;

            start[edge.SiteA] = (edge.DirectionX, edge.DirectionY);

            end[edge.SiteB] = (edge.DirectionX, edge.DirectionY);
        }

        return (start, end);
    }

    /// <summary>
    /// A rectangle that contains the extent and every circumcentre with room to spare, so an
    /// unbounded cell's rays always start inside it and leave through one of its sides.
    /// </summary>
    private static BoundingBox OuterRectangle(VoronoiDiagram voronoi, BoundingBox extent)
    {
        double xMin = extent.XMin, yMin = extent.YMin, xMax = extent.XMax, yMax = extent.YMax;

        foreach (var vertex in voronoi.Vertices)
        {
            if (double.IsNaN(vertex.X) || double.IsNaN(vertex.Y))
                continue;

            xMin = Math.Min(xMin, vertex.X);

            yMin = Math.Min(yMin, vertex.Y);

            xMax = Math.Max(xMax, vertex.X);

            yMax = Math.Max(yMax, vertex.Y);
        }

        double margin = 0.25 * Math.Max(xMax - xMin, yMax - yMin);

        return new BoundingBox(xMin - margin, yMin - margin, xMax + margin, yMax + margin);
    }

    /// <summary>Where a ray leaves <paramref name="rectangle"/>, starting from a point inside it.</summary>
    private static Point ExitPoint(Point origin, double directionX, double directionY, BoundingBox rectangle)
    {
        double distance = double.PositiveInfinity;

        if (directionX > 0)
            distance = Math.Min(distance, (rectangle.XMax - origin.X) / directionX);
        else if (directionX < 0)
            distance = Math.Min(distance, (rectangle.XMin - origin.X) / directionX);

        if (directionY > 0)
            distance = Math.Min(distance, (rectangle.YMax - origin.Y) / directionY);
        else if (directionY < 0)
            distance = Math.Min(distance, (rectangle.YMin - origin.Y) / directionY);

        if (double.IsInfinity(distance) || distance <= 0)
            return new Point(origin.X, origin.Y);

        return new Point(origin.X + directionX * distance, origin.Y + directionY * distance);
    }

    /// <summary>
    /// Adds the corners of <paramref name="rectangle"/> that lie between two boundary points when
    /// walking counter-clockwise. Without them a cell that should wrap a corner would be cut across it.
    /// </summary>
    private static void AppendCornersBetween(List<Point> ring, BoundingBox rectangle, Point from, Point to)
    {
        double start = PerimeterPosition(from, rectangle);

        double span = Modulo4(PerimeterPosition(to, rectangle) - start);

        // corner k sits at whole position k; walk forward and take the ones we pass
        for (int step = 1; step <= 4; step++)
        {
            double corner = Math.Ceiling(start + 1e-9) + step - 1;

            if (Modulo4(corner - start) >= span)
                break;

            ring.Add(CornerAt((int)Modulo4(corner), rectangle));
        }
    }

    /// <summary>
    /// Position of a point on the rectangle's boundary, measured counter-clockwise from its
    /// bottom-left corner: 0-1 bottom, 1-2 right, 2-3 top, 3-4 left.
    /// </summary>
    private static double PerimeterPosition(Point point, BoundingBox rectangle)
    {
        double toleranceX = rectangle.Width * 1e-9;

        double toleranceY = rectangle.Height * 1e-9;

        if (point.Y <= rectangle.YMin + toleranceY)
            return SafeRatio(point.X - rectangle.XMin, rectangle.Width);

        if (point.X >= rectangle.XMax - toleranceX)
            return 1 + SafeRatio(point.Y - rectangle.YMin, rectangle.Height);

        if (point.Y >= rectangle.YMax - toleranceY)
            return 2 + SafeRatio(rectangle.XMax - point.X, rectangle.Width);

        return 3 + SafeRatio(rectangle.YMax - point.Y, rectangle.Height);
    }

    private static Point CornerAt(int position, BoundingBox rectangle) => position switch
    {
        0 => new Point(rectangle.XMin, rectangle.YMin),
        1 => new Point(rectangle.XMax, rectangle.YMin),
        2 => new Point(rectangle.XMax, rectangle.YMax),
        _ => new Point(rectangle.XMin, rectangle.YMax),
    };

    private static double Modulo4(double value) => ((value % 4) + 4) % 4;

    private static double SafeRatio(double value, double total) => total == 0 ? 0 : Math.Clamp(value / total, 0, 1);

    // ------------------------------------------------------------------ sites

    private static List<Feature<Point>> BuildSiteFeatures(IReadOnlyList<Point> sites)
    {
        var features = new List<Feature<Point>>(sites.Count);

        for (int i = 0; i < sites.Count; i++)
        {
            var geometry = Geometry<Point>.Create(sites[i].X, sites[i].Y, SridHelper.WebMercator);

            features.Add(new Feature<Point>(geometry, new Dictionary<string, object>
            {
                ["Id"] = i,
                ["X"] = Math.Round(sites[i].X),
                ["Y"] = Math.Round(sites[i].Y),
            }));
        }

        return features;
    }

    // ------------------------------------------------------------------ clipping

    /// <summary>
    /// Sutherland-Hodgman: clips a convex polygon against the four sides of a rectangle, one side at
    /// a time. Voronoi cells are always convex, which is what makes this the right algorithm here.
    /// </summary>
    private static List<Point> ClipToRectangle(List<Point> polygon, BoundingBox rectangle)
    {
        var result = ClipAgainstSide(polygon, p => p.X >= rectangle.XMin, (a, b) => InterpolateAtX(a, b, rectangle.XMin));

        result = ClipAgainstSide(result, p => p.X <= rectangle.XMax, (a, b) => InterpolateAtX(a, b, rectangle.XMax));

        result = ClipAgainstSide(result, p => p.Y >= rectangle.YMin, (a, b) => InterpolateAtY(a, b, rectangle.YMin));

        return ClipAgainstSide(result, p => p.Y <= rectangle.YMax, (a, b) => InterpolateAtY(a, b, rectangle.YMax));
    }

    private static List<Point> ClipAgainstSide(List<Point> polygon, Func<Point, bool> isInside, Func<Point, Point, Point> intersect)
    {
        var result = new List<Point>(polygon.Count + 4);

        if (polygon.Count == 0)
            return result;

        for (int i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];

            var previous = polygon[(i - 1 + polygon.Count) % polygon.Count];

            bool currentInside = isInside(current);

            bool previousInside = isInside(previous);

            if (currentInside)
            {
                if (!previousInside)
                    result.Add(intersect(previous, current));

                result.Add(current);
            }
            else if (previousInside)
            {
                result.Add(intersect(previous, current));
            }
        }

        return result;
    }

    private static Point InterpolateAtX(Point from, Point to, double x)
    {
        double ratio = to.X == from.X ? 0 : (x - from.X) / (to.X - from.X);

        return new Point(x, from.Y + ratio * (to.Y - from.Y));
    }

    private static Point InterpolateAtY(Point from, Point to, double y)
    {
        double ratio = to.Y == from.Y ? 0 : (y - from.Y) / (to.Y - from.Y);

        return new Point(from.X + ratio * (to.X - from.X), y);
    }

    // ------------------------------------------------------------------ measurements

    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    /// <summary>
    /// Shoelace area, in square kilometres of the projected plane. Web Mercator exaggerates area as
    /// latitude grows, so these numbers compare shapes with each other, not with the ground.
    /// </summary>
    private static double AreaInSquareKilometres(List<Point> ring)
    {
        double sum = 0;

        for (int i = 0; i < ring.Count; i++)
        {
            var current = ring[i];

            var next = ring[(i + 1) % ring.Count];

            sum += current.X * next.Y - next.X * current.Y;
        }

        return Math.Abs(sum) / 2 / 1_000_000;
    }
}
