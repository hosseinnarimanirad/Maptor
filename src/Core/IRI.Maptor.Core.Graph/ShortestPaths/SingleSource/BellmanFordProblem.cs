// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.Graph;

/// <summary>
/// Implements the Bellman-Ford algorithm for finding single-source shortest paths in a weighted directed graph.
/// Unlike Dijkstra's algorithm, Bellman-Ford can handle graphs with negative edge weights and can detect negative cycles.
/// Time complexity: O(VE) where V is the number of vertices and E is the number of edges.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph. Must be comparable.</typeparam>
/// <typeparam name="TWeight">The type of edge weights. Must be comparable and support arithmetic operations.</typeparam>
public class BellmanFordProblem<TNode, TWeight>
    where TWeight : IComparable
{
    private AdjacencyList<TNode, TWeight> graph;
    private Dictionary<TNode, TWeight> distances;
    private Dictionary<TNode, TNode> predecessors;
    private TNode sourceNode;
    private bool hasNegativeCycle;

    /// <summary>
    /// Gets a value indicating whether the graph contains a negative cycle reachable from the source.
    /// </summary>
    public bool HasNegativeCycle
    {
        get { return this.hasNegativeCycle; }
    }

    /// <summary>
    /// Gets the shortest distances from the source node to all other nodes.
    /// </summary>
    public Dictionary<TNode, TWeight> Distances
    {
        get { return new Dictionary<TNode, TWeight>(this.distances); }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BellmanFordProblem{TNode, TWeight}"/> class.
    /// </summary>
    /// <param name="graph">The graph to analyze.</param>
    /// <param name="sourceNode">The source node for shortest path calculations.</param>
    /// <exception cref="ArgumentNullException">Thrown when graph or sourceNode is null.</exception>
    /// <exception cref="ArgumentException">Thrown when sourceNode does not exist in the graph.</exception>
    public BellmanFordProblem(AdjacencyList<TNode, TWeight> graph, TNode sourceNode)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        if (sourceNode == null)
            throw new ArgumentNullException(nameof(sourceNode));
        if (!graph.HasTheNode(sourceNode))
            throw new ArgumentException($"Source node '{sourceNode}' does not exist in the graph.", nameof(sourceNode));

        this.graph = graph;
        this.sourceNode = sourceNode;
        this.distances = new Dictionary<TNode, TWeight>(graph.NumberOfNodes);
        this.predecessors = new Dictionary<TNode, TNode>(graph.NumberOfNodes);
        this.hasNegativeCycle = false;

        Initialize();
        ComputeShortestPaths();
    }


    /// <summary>
    /// Gets the shortest distance from the source node to the specified target node.
    /// </summary>
    /// <param name="targetNode">The target node.</param>
    /// <param name="distance">When this method returns, contains the shortest distance if a path exists; otherwise, contains the infinity value.</param>
    /// <returns>true if a path exists and no negative cycle was detected; false if no path exists or a negative cycle is detected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when targetNode is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when targetNode does not exist in the graph.</exception>
    public bool TryGetDistance(TNode targetNode, out TWeight distance)
    {
        if (targetNode == null)
            throw new ArgumentNullException(nameof(targetNode));
        if (!graph.HasTheNode(targetNode))
            throw new KeyNotFoundException($"Target node '{targetNode}' does not exist in the graph.");

        if (this.hasNegativeCycle)
        {
            distance = GetInfinity();
            return false;
        }

        // Since we initialized all nodes, the distance should always exist
        distance = this.distances[targetNode];
        
        // Return false if distance is infinity (no path exists)
        if (IsInfinity(distance))
            return false;
            
        return true;
    }

    /// <summary>
    /// Gets the shortest distance from the source node to the specified target node.
    /// Returns infinity if no path exists or a negative cycle is detected.
    /// </summary>
    /// <param name="targetNode">The target node.</param>
    /// <returns>The shortest distance, or infinity if no path exists or a negative cycle is detected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when targetNode is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when targetNode does not exist in the graph.</exception>
    public TWeight GetDistance(TNode targetNode)
    {
        if (targetNode == null)
            throw new ArgumentNullException(nameof(targetNode));
        if (!graph.HasTheNode(targetNode))
            throw new KeyNotFoundException($"Target node '{targetNode}' does not exist in the graph.");

        if (this.hasNegativeCycle)
            return GetInfinity();

        // Since we initialized all nodes, the distance should always exist
        TWeight distance = this.distances[targetNode];
        return distance;
    }

    /// <summary>
    /// Gets the shortest path from the source node to the specified target node.
    /// </summary>
    /// <param name="targetNode">The target node.</param>
    /// <returns>A list of nodes representing the shortest path, or null if no path exists or a negative cycle is detected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when targetNode is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when targetNode does not exist in the graph.</exception>
    public List<TNode>? GetShortestPath(TNode targetNode)
    {
        if (targetNode == null)
            throw new ArgumentNullException(nameof(targetNode));
        if (!graph.HasTheNode(targetNode))
            throw new KeyNotFoundException($"Target node '{targetNode}' does not exist in the graph.");

        if (this.hasNegativeCycle)
            return null;

        if (!this.predecessors.ContainsKey(targetNode) && !targetNode.Equals(this.sourceNode))
        {
            return null; // No path exists
        }

        List<TNode> path = new List<TNode>();
        TNode currentNode = targetNode;
        HashSet<TNode> visited = new HashSet<TNode>();

        // Reconstruct path by following predecessors
        while (!visited.Contains(currentNode))
        {
            path.Add(currentNode);
            visited.Add(currentNode);

            if (currentNode.Equals(this.sourceNode))
                break;

            if (!this.predecessors.TryGetValue(currentNode, out TNode? predecessor))
            {
                return null; // No path exists
            }

            currentNode = predecessor;
        }

        // Check for cycles (shouldn't happen if no negative cycle, but safety check)
        if (!currentNode.Equals(this.sourceNode))
        {
            return null; // Cycle detected in path reconstruction
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Initializes distances: source node = 0, all others = infinity (maximum value).
    /// </summary>
    private void Initialize()
    {
        // Initialize all distances to infinity (represented as maximum value for the type)
        foreach (TNode node in this.graph)
        {
            this.distances[node] = GetInfinity();
        }

        // Distance from source to itself is 0
        this.distances[this.sourceNode] = GetZero();
    }

    /// <summary>
    /// Computes shortest paths using the Bellman-Ford algorithm.
    /// Relaxes edges V-1 times, then checks for negative cycles.
    /// </summary>
    private void ComputeShortestPaths()
    {
        int numberOfNodes = this.graph.NumberOfNodes;
        List<Edge<TNode, TWeight>> edges = this.graph.GetEdges();

        // Relax edges V-1 times
        for (int i = 0; i < numberOfNodes - 1; i++)
        {
            bool relaxed = false;

            foreach (Edge<TNode, TWeight> edge in edges)
            {
                if (RelaxEdge(edge))
                {
                    relaxed = true;
                }
            }

            // Early termination: if no relaxation occurred, we can stop early
            if (!relaxed)
                break;
        }

        // Check for negative cycles by relaxing one more time
        foreach (Edge<TNode, TWeight> edge in edges)
        {
            if (CanRelaxEdge(edge))
            {
                this.hasNegativeCycle = true;
                break;
            }
        }
    }

    /// <summary>
    /// Relaxes an edge if it can improve the shortest distance.
    /// Following CLRS: if d[u] + w(u,v) &lt; d[v], then set d[v] = d[u] + w(u,v) and π[v] = u.
    /// </summary>
    /// <param name="edge">The edge to relax.</param>
    /// <returns>true if relaxation occurred; otherwise, false.</returns>
    private bool RelaxEdge(Edge<TNode, TWeight> edge)
    {
        TNode u = edge.Node;
        TNode v = edge.Connection.Node;
        TWeight weight = edge.Connection.Weight;

        // u should always exist since we initialized all nodes
        if (!this.distances.ContainsKey(u))
            return false;

        TWeight distanceU = this.distances[u];
        
        // If distanceU is infinity, we cannot relax (infinity + weight = infinity)
        if (IsInfinity(distanceU))
            return false;

        TWeight distanceV = this.distances.ContainsKey(v) ? this.distances[v] : GetInfinity();

        // Relax if d[u] + w(u,v) < d[v]
        TWeight newDistance = Add(distanceU, weight);
        if (Compare(newDistance, distanceV) < 0)
        {
            this.distances[v] = newDistance;
            this.predecessors[v] = u;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an edge can be relaxed (used for negative cycle detection).
    /// After V-1 iterations, if we can still relax an edge, it indicates a negative cycle.
    /// </summary>
    /// <param name="edge">The edge to check.</param>
    /// <returns>true if the edge can be relaxed; otherwise, false.</returns>
    private bool CanRelaxEdge(Edge<TNode, TWeight> edge)
    {
        TNode u = edge.Node;
        TNode v = edge.Connection.Node;
        TWeight weight = edge.Connection.Weight;

        if (!this.distances.ContainsKey(u))
            return false;

        TWeight distanceU = this.distances[u];
        
        // If distanceU is infinity, we cannot relax
        if (IsInfinity(distanceU))
            return false;

        TWeight distanceV = this.distances.ContainsKey(v) ? this.distances[v] : GetInfinity();

        // If we can still relax after V-1 iterations, there's a negative cycle
        TWeight newDistance = Add(distanceU, weight);
        return Compare(newDistance, distanceV) < 0;
    }

    /// <summary>
    /// Checks if a weight value represents infinity.
    /// </summary>
    private bool IsInfinity(TWeight value)
    {
        if (typeof(TWeight) == typeof(double))
        {
            return double.IsInfinity((double)(object)value!);
        }
        if (typeof(TWeight) == typeof(float))
        {
            return float.IsInfinity((float)(object)value!);
        }
        if (typeof(TWeight) == typeof(int))
        {
            return ((int)(object)value!) == int.MaxValue;
        }
        if (typeof(TWeight) == typeof(long))
        {
            return ((long)(object)value!) == long.MaxValue;
        }
        if (typeof(TWeight) == typeof(decimal))
        {
            return ((decimal)(object)value!) == decimal.MaxValue;
        }

        // For other types, assume they don't have infinity
        return false;
    }

    /// <summary>
    /// Gets the infinity value for the weight type (used for numeric types).
    /// For numeric types, returns the maximum value; for other types, throws an exception.
    /// </summary>
    private TWeight GetInfinity()
    {
        if (typeof(TWeight) == typeof(double))
        {
            return (TWeight)(object)double.PositiveInfinity;
        }
        if (typeof(TWeight) == typeof(float))
        {
            return (TWeight)(object)float.PositiveInfinity;
        }
        if (typeof(TWeight) == typeof(int))
        {
            return (TWeight)(object)int.MaxValue;
        }
        if (typeof(TWeight) == typeof(long))
        {
            return (TWeight)(object)long.MaxValue;
        }
        if (typeof(TWeight) == typeof(decimal))
        {
            return (TWeight)(object)decimal.MaxValue;
        }

        // For other types, try to use the maximum value if it's a numeric type
        throw new NotSupportedException($"Type {typeof(TWeight).Name} is not supported. Bellman-Ford requires a numeric weight type.");
    }

    /// <summary>
    /// Gets the zero value for the weight type.
    /// </summary>
    private TWeight GetZero()
    {
        if (typeof(TWeight) == typeof(double))
        {
            return (TWeight)(object)0.0;
        }
        if (typeof(TWeight) == typeof(float))
        {
            return (TWeight)(object)0.0f;
        }
        if (typeof(TWeight) == typeof(int))
        {
            return (TWeight)(object)0;
        }
        if (typeof(TWeight) == typeof(long))
        {
            return (TWeight)(object)0L;
        }
        if (typeof(TWeight) == typeof(decimal))
        {
            return (TWeight)(object)0m;
        }

        // Fallback: try Activator for other types
        try
        {
            return (TWeight)Activator.CreateInstance(typeof(TWeight))!;
        }
        catch
        {
            throw new NotSupportedException($"Type {typeof(TWeight).Name} is not supported. Bellman-Ford requires a numeric weight type.");
        }
    }

    /// <summary>
    /// Adds two weight values.
    /// </summary>
    private TWeight Add(TWeight a, TWeight b)
    {
        if (typeof(TWeight) == typeof(double))
        {
            return (TWeight)(object)((double)(object)a! + (double)(object)b!);
        }
        if (typeof(TWeight) == typeof(float))
        {
            return (TWeight)(object)((float)(object)a! + (float)(object)b!);
        }
        if (typeof(TWeight) == typeof(int))
        {
            return (TWeight)(object)((int)(object)a! + (int)(object)b!);
        }
        if (typeof(TWeight) == typeof(long))
        {
            return (TWeight)(object)((long)(object)a! + (long)(object)b!);
        }
        if (typeof(TWeight) == typeof(decimal))
        {
            return (TWeight)(object)((decimal)(object)a! + (decimal)(object)b!);
        }

        throw new NotSupportedException($"Addition is not supported for type {typeof(TWeight).Name}.");
    }

    /// <summary>
    /// Compares two weight values.
    /// </summary>
    private int Compare(TWeight a, TWeight b)
    {
        return a.CompareTo(b);
    }
}

