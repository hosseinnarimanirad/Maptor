using IRI.Maptor.Core.Graph;
using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis.Network;

/// <summary>
/// An edge–node topology built from a set of line features. Nodes are the shared junction
/// coordinates; edges are the original line features. Use this to answer "which lines are
/// adjacent", to find connected sub-networks, or to feed the existing graph algorithms via
/// <see cref="ToAdjacencyList"/>.
/// <para>
/// Adjacency includes T-junctions (an endpoint of one line landing on an interior vertex of
/// another). Such a node lists the passing-through edge in <see cref="LineNetworkNode{T}.IncidentEdgeIds"/>,
/// but the passing edge is NOT split there — so <see cref="ToAdjacencyList"/> (used for weighted
/// routing) connects each edge only between its two endpoint nodes.
/// </para>
/// </summary>
public class LineNetwork<T> where T : IPoint, new()
{
    private readonly List<LineNetworkNode<T>> _nodes;
    private readonly List<LineNetworkEdge<T>> _edges;

    public IReadOnlyList<LineNetworkNode<T>> Nodes => _nodes;

    public IReadOnlyList<LineNetworkEdge<T>> Edges => _edges;

    internal LineNetwork(List<LineNetworkNode<T>> nodes, List<LineNetworkEdge<T>> edges)
    {
        _nodes = nodes;
        _edges = edges;
    }

    /// <summary>Ids of every edge incident to the given node (endpoints + shared vertices).</summary>
    public IReadOnlyList<int> GetEdgesAtNode(int nodeId) => _nodes[nodeId].IncidentEdgeIds;

    /// <summary>The edges that share at least one node with the given edge (its neighbours in the network).</summary>
    public IEnumerable<LineNetworkEdge<T>> GetAdjacentEdges(int edgeId)
    {
        var edge = _edges[edgeId];

        var seen = new HashSet<int> { edgeId };

        foreach (var nodeId in edge.IncidentNodeIds)
        {
            foreach (var other in _nodes[nodeId].IncidentEdgeIds)
            {
                if (seen.Add(other))
                    yield return _edges[other];
            }
        }
    }

    /// <summary>
    /// Groups edge ids into connected components (disconnected sub-networks). Two edges are in the
    /// same component when they can be reached from one another through shared nodes.
    /// </summary>
    public List<List<int>> GetConnectedComponents()
    {
        // Union-find over edges. Every edge incident to the same node is unioned together,
        // which naturally folds in T-junctions (the passing edge is in the node's incident list).
        var parent = new int[_edges.Count];

        for (int i = 0; i < parent.Length; i++)
            parent[i] = i;

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        void Union(int a, int b)
        {
            int ra = Find(a), rb = Find(b);
            if (ra != rb) parent[ra] = rb;
        }

        foreach (var node in _nodes)
        {
            var incident = node.IncidentEdgeIds;
            for (int i = 1; i < incident.Count; i++)
                Union(incident[0], incident[i]);
        }

        var groups = new Dictionary<int, List<int>>();

        for (int edgeId = 0; edgeId < _edges.Count; edgeId++)
        {
            int root = Find(edgeId);
            if (!groups.TryGetValue(root, out var list))
            {
                list = new List<int>();
                groups[root] = list;
            }
            list.Add(edgeId);
        }

        return groups.Values.ToList();
    }

    /// <summary>
    /// Exports the junction graph for use with the <c>IRI.Maptor.Core.Graph</c> algorithms
    /// (Dijkstra, Bellman-Ford, BFS, …). Graph nodes are junction node ids; each edge becomes an
    /// undirected connection between its two endpoint nodes, weighted by <see cref="LineNetworkEdge{T}.Length"/>.
    /// </summary>
    public AdjacencyList<int, double> ToAdjacencyList()
    {
        var graph = new AdjacencyList<int, double>(_nodes.Count);

        foreach (var node in _nodes)
            graph.AddNode(node.Id);

        foreach (var edge in _edges)
            graph.AddUndirectedEdge(edge.StartNodeId, edge.EndNodeId, edge.Length);

        return graph;
    }
}
