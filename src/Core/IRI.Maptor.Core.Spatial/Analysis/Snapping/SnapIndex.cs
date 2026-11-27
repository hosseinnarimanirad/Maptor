using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.AdvancedStructures;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// The vertices and edges of a fixed list of geometries, indexed for "what is near this point". It only finds
/// candidates; <see cref="SnapEngine"/> decides which one a point snaps to.
/// <para>
/// Coordinates are taken as flat (a projected SRS such as Web Mercator); distances are in coordinate units.
/// Vertices come from every geometry; edges from lines and polygon boundaries (each ring with its closing
/// segment). The first and last vertex of each line part are marked as line ends. Midpoints and intersections are
/// worked out from the segments near the asked point, so they cost nothing to index. Empty geometries, null entries
/// and non-finite coordinates are skipped. Build it once per set of geometries: it does not follow later changes
/// to them.
/// </para>
/// </summary>
public sealed class SnapIndex<T> where T : IPoint, new()
{
    // Intersections are tried between the segments nearest the point only: a dense place at a small scale can
    // put hundreds of segments within the tolerance, and every pair of them would be tested on each mouse move.
    private const int MaxSegmentsForIntersections = 64;

    private const double EndFraction = 1e-9;

    private readonly List<T> _vertices = new();
    private readonly List<int> _vertexGeometry = new();
    private readonly List<bool> _vertexIsEnd = new();
    private readonly BoxGrid<int> _vertexGrid;

    private readonly List<int> _lineGeometry = new();
    private readonly LineProximity<T> _edges;

    public SnapIndex(IReadOnlyList<Geometry<T>?> geometries)
    {
        if (geometries is null)
            throw new ArgumentNullException(nameof(geometries));

        var lines = new List<MeasuredLine<T>>();

        for (int g = 0; g < geometries.Count; g++)
        {
            var geometry = geometries[g];

            if (geometry is null || geometry.IsNullOrEmpty())
                continue;

            AddVertices(geometry, g, insidePolygon: false);

            var line = geometry.Type.GetCategory() switch
            {
                GeometryCategory.Polyline => MeasuredLine<T>.FromLine(geometry, isGeodetic: false),
                GeometryCategory.Polygon => MeasuredLine<T>.FromRings(geometry, isGeodetic: false),
                _ => null,
            };

            if (line is { IsEmpty: false })
            {
                lines.Add(line);
                _lineGeometry.Add(g);
            }
        }

        _vertexGrid = new BoxGrid<int>(VertexCellSize(_vertices));

        for (int v = 0; v < _vertices.Count; v++)
            _vertexGrid.Add(v, _vertices[v].X, _vertices[v].Y, _vertices[v].X, _vertices[v].Y);

        _edges = new LineProximity<T>(lines);

        GeometryCount = geometries.Count;
    }

    /// <summary>The length of the list the index was built from.</summary>
    public int GeometryCount { get; }

    public int VertexCount => _vertices.Count;

    /// <summary>
    /// The nearest vertex within <paramref name="tolerance"/> of a geometry that <paramref name="accept"/> allows
    /// (all when null), or null. Its kind is <see cref="SnapKind.End"/> for a line end, else
    /// <see cref="SnapKind.Vertex"/>. Of vertices at the same distance the first indexed wins. With
    /// <paramref name="endsOnly"/> only line ends are looked at.
    /// </summary>
    public SnapResult<T>? NearestVertex(T point, double tolerance, Func<int, bool>? accept = null, bool endsOnly = false)
    {
        SnapResult<T>? best = null;

        foreach (var v in _vertexGrid.Query(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance))
        {
            if (endsOnly && !_vertexIsEnd[v])
                continue;

            var geometry = _vertexGeometry[v];

            if (accept is not null && !accept(geometry))
                continue;

            var vertex = _vertices[v];

            var distance = Distance(vertex, point);

            if (distance > tolerance || (best is not null && distance >= best.Distance))
                continue;

            best = new SnapResult<T>(vertex, _vertexIsEnd[v] ? SnapKind.End : SnapKind.Vertex, distance, geometry);
        }

        return best;
    }

    /// <summary>
    /// The nearest place on an edge within <paramref name="tolerance"/> of a geometry that
    /// <paramref name="accept"/> allows (all when null), or null.
    /// </summary>
    public SnapResult<T>? NearestEdge(T point, double tolerance, Func<int, bool>? accept = null)
    {
        var nearest = _edges.Nearest(point, tolerance, AcceptLine(accept));

        return nearest is null
            ? null
            : new SnapResult<T>(nearest.Location, SnapKind.Edge, nearest.Distance, _lineGeometry[nearest.LineIndex]);
    }

    /// <summary>
    /// The nearest segment midpoint within <paramref name="tolerance"/> of a geometry that
    /// <paramref name="accept"/> allows (all when null), or null. Segments of no length have none.
    /// </summary>
    public SnapResult<T>? NearestMidpoint(T point, double tolerance, Func<int, bool>? accept = null)
    {
        SnapResult<T>? best = null;

        // a midpoint within the tolerance lies on a segment within the tolerance
        foreach (var segment in _edges.SegmentsNear(point, tolerance, AcceptLine(accept)))
        {
            if (segment.A.X == segment.B.X && segment.A.Y == segment.B.Y)
                continue;

            var midpoint = segment.At(0.5);

            var distance = Distance(midpoint, point);

            if (distance > tolerance || (best is not null && distance >= best.Distance))
                continue;

            best = new SnapResult<T>(midpoint, SnapKind.Midpoint, distance, _lineGeometry[segment.Line]);
        }

        return best;
    }

    /// <summary>
    /// The nearest intersection of two edges within <paramref name="tolerance"/>, both of geometries that
    /// <paramref name="accept"/> allows (all when null), or null. Two segments that only share an end vertex do not
    /// intersect here (that place is a vertex), and neither do segments lying along each other.
    /// </summary>
    public SnapResult<T>? NearestIntersection(T point, double tolerance, Func<int, bool>? accept = null)
    {
        // an intersection within the tolerance is on two segments that are both within the tolerance
        var segments = _edges.SegmentsNear(point, tolerance, AcceptLine(accept)).ToList();

        if (segments.Count < 2)
            return null;

        if (segments.Count > MaxSegmentsForIntersections)
            segments = segments.OrderBy(s => s.Distance).Take(MaxSegmentsForIntersections).ToList();

        SnapResult<T>? best = null;

        for (int i = 0; i < segments.Count - 1; i++)
        {
            for (int j = i + 1; j < segments.Count; j++)
            {
                var a = segments[i];
                var b = segments[j];

                if (!SegmentCrossings.Intersect(a.A, a.B, b.A, b.B, out var t, out var u))
                    continue;

                if (IsAtEnd(t) && IsAtEnd(u))
                    continue;

                var location = a.At(t);

                var distance = Distance(location, point);

                if (distance > tolerance || (best is not null && distance >= best.Distance))
                    continue;

                best = new SnapResult<T>(location, SnapKind.Intersection, distance, Math.Min(_lineGeometry[a.Line], _lineGeometry[b.Line]));
            }
        }

        return best;
    }

    /// <summary>
    /// The edges within <paramref name="tolerance"/> of <paramref name="point"/>, each with the position of its
    /// geometry in the index's list; for <see cref="GuideEngine"/>, which looks for where a guide meets them.
    /// </summary>
    internal IEnumerable<(T A, T B, int GeometryIndex)> EdgesNear(T point, double tolerance, Func<int, bool>? accept = null)
    {
        foreach (var segment in _edges.SegmentsNear(point, tolerance, AcceptLine(accept)))
            yield return (segment.A, segment.B, _lineGeometry[segment.Line]);
    }

    private Func<int, bool>? AcceptLine(Func<int, bool>? accept) => accept is null ? null : line => accept(_lineGeometry[line]);

    private static bool IsAtEnd(double fraction) => fraction <= EndFraction || fraction >= 1 - EndFraction;

    private static double Distance(T a, T b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Every vertex of the geometry; the first and last vertex of a line part (not of a polygon ring) are line ends.
    private void AddVertices(Geometry<T> geometry, int geometryIndex, bool insidePolygon)
    {
        if (geometry.Geometries is { Count: > 0 })
        {
            foreach (var child in geometry.Geometries)
            {
                if (child is not null)
                    AddVertices(child, geometryIndex, insidePolygon || geometry.Type == GeometryType.Polygon);
            }

            return;
        }

        var points = geometry.Points;

        if (points is null || points.Count == 0)
            return;

        var isLine = geometry.Type == GeometryType.LineString && !insidePolygon && points.Count > 1;

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];

            if (point is null || !IsFinite(point.X) || !IsFinite(point.Y))
                continue;

            _vertices.Add(point);
            _vertexGeometry.Add(geometryIndex);
            _vertexIsEnd.Add(isLine && (i == 0 || i == points.Count - 1));
        }
    }

    // About one vertex per cell on average over the vertices' extent.
    private static double VertexCellSize(List<T> vertices)
    {
        if (vertices.Count < 2)
            return 1;

        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;

        foreach (var v in vertices)
        {
            minX = Math.Min(minX, v.X);
            minY = Math.Min(minY, v.Y);
            maxX = Math.Max(maxX, v.X);
            maxY = Math.Max(maxY, v.Y);
        }

        var size = Math.Max(maxX - minX, maxY - minY) / Math.Ceiling(Math.Sqrt(vertices.Count));

        return size > 0 && IsFinite(size) ? size : 1;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
