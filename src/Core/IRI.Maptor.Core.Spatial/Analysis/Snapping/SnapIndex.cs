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
/// segment). Empty geometries, null entries and non-finite coordinates are skipped. Build it once per set of
/// geometries: it does not follow later changes to them.
/// </para>
/// </summary>
public sealed class SnapIndex<T> where T : IPoint, new()
{
    private readonly List<T> _vertices = new();
    private readonly List<int> _vertexGeometry = new();
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

            foreach (var point in geometry.GetAllPoints() ?? new List<T>())
            {
                if (point is null || !IsFinite(point.X) || !IsFinite(point.Y))
                    continue;

                _vertices.Add(point);
                _vertexGeometry.Add(g);
            }

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
    /// (all when null), or null. Of vertices at the same distance the first indexed wins.
    /// </summary>
    public SnapResult<T>? NearestVertex(T point, double tolerance, Func<int, bool>? accept = null)
    {
        SnapResult<T>? best = null;

        foreach (var v in _vertexGrid.Query(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance))
        {
            var geometry = _vertexGeometry[v];

            if (accept is not null && !accept(geometry))
                continue;

            var vertex = _vertices[v];

            var dx = vertex.X - point.X;
            var dy = vertex.Y - point.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > tolerance || (best is not null && distance >= best.Distance))
                continue;

            best = new SnapResult<T>(vertex, SnapKind.Vertex, distance, geometry);
        }

        return best;
    }

    /// <summary>
    /// The nearest place on an edge within <paramref name="tolerance"/> of a geometry that
    /// <paramref name="accept"/> allows (all when null), or null.
    /// </summary>
    public SnapResult<T>? NearestEdge(T point, double tolerance, Func<int, bool>? accept = null)
    {
        var nearest = _edges.Nearest(point, tolerance, accept is null ? null : line => accept(_lineGeometry[line]));

        return nearest is null
            ? null
            : new SnapResult<T>(nearest.Location, SnapKind.Edge, nearest.Distance, _lineGeometry[nearest.LineIndex]);
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
