using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Analysis.Network;

/// <summary>
/// Builds a <see cref="LineNetwork{T}"/> from a collection of line features by snapping coincident
/// vertices into shared junction nodes.
/// <para>
/// Noding rule: endpoints plus shared vertices. Two features connect where their endpoints coincide
/// (within <see cref="Tolerance"/>), and also where an endpoint of one feature lands on an interior
/// vertex of another (a T-junction). Mid-segment crossings are NOT split — that would be full noding,
/// which this builder does not perform.
/// </para>
/// <para>
/// The tolerance is expressed in coordinate units (degrees for WGS84, the projection's linear unit
/// otherwise) because snapping compares raw coordinates. Edge lengths, by contrast, use the
/// SRID-appropriate metric (ellipsoidal for WGS84, Euclidean otherwise).
/// </para>
/// </summary>
public class LineNetworkBuilder<T> where T : IPoint, new()
{
    private readonly double _tolerance;
    private readonly double _cellSize;

    private readonly List<LineNetworkNode<T>> _nodes = new List<LineNetworkNode<T>>();
    private readonly Dictionary<(long, long), List<int>> _grid = new Dictionary<(long, long), List<int>>();

    public double Tolerance => _tolerance;

    public LineNetworkBuilder(double tolerance)
    {
        _tolerance = tolerance < 0 ? 0 : tolerance;
        _cellSize = _tolerance <= 0 ? 1e-9 : _tolerance;
    }

    /// <summary>Sensible default snapping tolerance for a given SRID (coordinate units).</summary>
    public static double GetDefaultTolerance(int srid)
        => srid == SridHelper.GeodeticWGS84 ? 1e-7 : 1e-6;

    /// <summary>Builds the network. Null / empty features and features with fewer than two points are skipped.</summary>
    public LineNetwork<T> Build(IEnumerable<Geometry<T>> lines)
    {
        if (lines == null)
            throw new ArgumentNullException(nameof(lines));

        var edges = new List<LineNetworkEdge<T>>();

        int sourceIndex = -1;

        // Pass 1 — endpoints become nodes; each feature becomes an edge.
        foreach (var feature in lines)
        {
            sourceIndex++;

            if (feature == null || feature.IsNullOrEmpty())
                continue;

            var points = feature.GetAllPoints();

            if (points.Count < 2)
                continue;

            int startNode = GetOrCreateNode(points[0]);
            int endNode = GetOrCreateNode(points[points.Count - 1]);

            double length = feature.Srid == SridHelper.GeodeticWGS84
                ? feature.GetEllipsoidalLength()
                : feature.GetEuclideanLength();

            int edgeId = edges.Count;

            edges.Add(new LineNetworkEdge<T>(edgeId, sourceIndex, feature, startNode, endNode, length));

            _nodes[startNode].IncidentEdgeIds.Add(edgeId);

            if (endNode != startNode)
                _nodes[endNode].IncidentEdgeIds.Add(edgeId);
        }

        // Pass 2 — shared-vertex (T-junction) detection: an interior vertex that lands on an
        // existing junction node makes this edge incident to that node. Nodes are not created here.
        foreach (var edge in edges)
        {
            var points = edge.Feature.GetAllPoints();

            for (int i = 1; i < points.Count - 1; i++)
            {
                int nodeId = FindNode(points[i]);

                if (nodeId < 0)
                    continue;

                var incident = _nodes[nodeId].IncidentEdgeIds;

                if (!incident.Contains(edge.Id))
                    incident.Add(edge.Id);

                if (!edge.IncidentNodeIds.Contains(nodeId))
                    edge.IncidentNodeIds.Add(nodeId);
            }
        }

        return new LineNetwork<T>(_nodes, edges);
    }

    private int GetOrCreateNode(T point)
    {
        int existing = FindNode(point);

        if (existing >= 0)
            return existing;

        int newId = _nodes.Count;

        _nodes.Add(new LineNetworkNode<T>(newId, point));

        var cell = GetCell(point);

        if (!_grid.TryGetValue(cell, out var bucket))
        {
            bucket = new List<int>();
            _grid[cell] = bucket;
        }

        bucket.Add(newId);

        return newId;
    }

    /// <summary>Returns the id of an existing node within tolerance of the point, or -1 if none.</summary>
    private int FindNode(T point)
    {
        var (cx, cy) = GetCell(point);

        for (long ix = cx - 1; ix <= cx + 1; ix++)
        {
            for (long iy = cy - 1; iy <= cy + 1; iy++)
            {
                if (!_grid.TryGetValue((ix, iy), out var ids))
                    continue;

                foreach (var id in ids)
                {
                    if (SpatialUtility.GetEuclideanLength(_nodes[id].Location, point) <= _tolerance)
                        return id;
                }
            }
        }

        return -1;
    }

    private (long, long) GetCell(T point)
        => ((long)Math.Floor(point.X / _cellSize), (long)Math.Floor(point.Y / _cellSize));
}
