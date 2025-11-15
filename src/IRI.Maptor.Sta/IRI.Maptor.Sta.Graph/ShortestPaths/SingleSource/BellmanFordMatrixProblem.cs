// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.DataStructures.CustomStructures;

namespace IRI.Maptor.Sta.Graph;

/// <summary>
/// Implements the Bellman-Ford algorithm for finding single-source shortest paths using an adjacency matrix.
/// Unlike Dijkstra's algorithm, Bellman-Ford can handle graphs with negative edge weights and can detect negative cycles.
/// Time complexity: O(VE) where V is the number of vertices and E is the number of edges.
/// This class works with Matrix input, similar to DijkstraProblem.
/// </summary>
public class BellmanFordMatrixProblem
{
    private Matrix m_Adjacency;
    private List<IndexValue<double>> distances;
    private List<int> predecessors;
    private int sourceNode;
    private bool hasNegativeCycle;

    /// <summary>
    /// Gets a value indicating whether the graph contains a negative cycle reachable from the source.
    /// </summary>
    public bool HasNegativeCycle
    {
        get { return this.hasNegativeCycle; }
    }

    /// <summary>
    /// Gets the adjacency matrix.
    /// </summary>
    public Matrix Adjacency
    {
        get { return this.m_Adjacency; }
    }

    /// <summary>
    /// Gets the number of nodes in the graph.
    /// </summary>
    public int NumberOfNodes
    {
        get { return this.Adjacency.NumberOfColumns; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BellmanFordMatrixProblem"/> class.
    /// </summary>
    /// <param name="adjacencyMatrix">The adjacency matrix representing the graph edges.</param>
    /// <param name="sourceNodeIndex">The index of the source node (0-based).</param>
    /// <exception cref="ArgumentNullException">Thrown when adjacencyMatrix is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the matrix is not square.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when sourceNodeIndex is out of range.</exception>
    public BellmanFordMatrixProblem(Matrix adjacencyMatrix, int sourceNodeIndex)
    {
        if (adjacencyMatrix == null)
            throw new ArgumentNullException(nameof(adjacencyMatrix));
        if (!adjacencyMatrix.IsSquare())
            throw new ArgumentException("Adjacency matrix must be square.", nameof(adjacencyMatrix));

        int numberOfNodes = adjacencyMatrix.NumberOfColumns;
        if (sourceNodeIndex < 0 || sourceNodeIndex >= numberOfNodes)
            throw new ArgumentOutOfRangeException(nameof(sourceNodeIndex), $"Source node index must be between 0 and {numberOfNodes - 1}.");

        this.m_Adjacency = adjacencyMatrix;
        this.sourceNode = sourceNodeIndex;
        this.distances = new List<IndexValue<double>>(numberOfNodes);
        this.predecessors = new List<int>(numberOfNodes);
        this.hasNegativeCycle = false;

        Initialize();
        ComputeShortestPaths();
    }

    /// <summary>
    /// Gets the shortest distance from the source node to the specified target node.
    /// </summary>
    /// <param name="targetNodeIndex">The index of the target node.</param>
    /// <returns>The shortest distance, or double.PositiveInfinity if no path exists or a negative cycle is detected.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when targetNodeIndex is out of range.</exception>
    public double GetDistance(int targetNodeIndex)
    {
        if (targetNodeIndex < 0 || targetNodeIndex >= this.NumberOfNodes)
            throw new ArgumentOutOfRangeException(nameof(targetNodeIndex), $"Target node index must be between 0 and {this.NumberOfNodes - 1}.");

        if (this.hasNegativeCycle)
            return double.PositiveInfinity;

        return this.distances[targetNodeIndex].Value;
    }

    /// <summary>
    /// Gets the shortest path from the source node to the specified target node.
    /// </summary>
    /// <param name="targetNodeIndex">The index of the target node.</param>
    /// <returns>A list of node indices representing the shortest path, or null if no path exists or a negative cycle is detected.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when targetNodeIndex is out of range.</exception>
    public List<int>? GetShortestPath(int targetNodeIndex)
    {
        if (targetNodeIndex < 0 || targetNodeIndex >= this.NumberOfNodes)
            throw new ArgumentOutOfRangeException(nameof(targetNodeIndex), $"Target node index must be between 0 and {this.NumberOfNodes - 1}.");

        if (this.hasNegativeCycle)
            return null;

        if (double.IsInfinity(this.distances[targetNodeIndex].Value))
            return null; // No path exists

        List<int> path = new List<int>();
        int currentNode = targetNodeIndex;

        // Reconstruct path by following predecessors
        while (currentNode != this.sourceNode)
        {
            path.Add(currentNode);
            
            // Check if predecessor is valid (not -1)
            if (this.predecessors[currentNode] == -1)
            {
                return null; // No path exists (shouldn't happen if distance is finite, but safety check)
            }
            
            currentNode = this.predecessors[currentNode];
        }

        path.Add(this.sourceNode);
        path.Reverse();

        return path;
    }

    /// <summary>
    /// Initializes distances: source node = 0, all others = infinity.
    /// Following CLRS: INITIALIZE-SINGLE-SOURCE(G, s)
    /// </summary>
    private void Initialize()
    {
        for (int i = 0; i < this.NumberOfNodes; i++)
        {
            this.distances.Add(new IndexValue<double>(this.sourceNode, double.PositiveInfinity));
            this.predecessors.Add(-1);
        }

        // Distance from source to itself is 0
        this.distances[this.sourceNode] = new IndexValue<double>(this.sourceNode, 0.0);
    }

    /// <summary>
    /// Computes shortest paths using the Bellman-Ford algorithm.
    /// Following CLRS: BELLMAN-FORD(G, w, s)
    /// Relaxes edges V-1 times, then checks for negative cycles.
    /// </summary>
    private void ComputeShortestPaths()
    {
        int numberOfNodes = this.NumberOfNodes;

        // Relax edges V-1 times (CLRS: for i = 1 to |V[G]| - 1)
        for (int i = 0; i < numberOfNodes - 1; i++)
        {
            bool relaxed = false;

            // Relax all edges
            for (int u = 0; u < numberOfNodes; u++)
            {
                if (double.IsInfinity(this.distances[u].Value))
                    continue; // Skip if u is unreachable

                for (int v = 0; v < numberOfNodes; v++)
                {
                    double weight = this.Adjacency[u, v];
                    
                    // Skip if no edge exists (infinity weight) or self-loop
                    if (double.IsInfinity(weight) || u == v)
                        continue;

                    // Relax edge (u, v): if d[u] + w(u,v) < d[v], then d[v] = d[u] + w(u,v) and π[v] = u
                    double newDistance = this.distances[u].Value + weight;
                    if (newDistance < this.distances[v].Value)
                    {
                        this.distances[v] = new IndexValue<double>(u, newDistance);
                        this.predecessors[v] = u;
                        relaxed = true;
                    }
                }
            }

            // Early termination: if no relaxation occurred, we can stop early
            if (!relaxed)
                break;
        }

        // Check for negative cycles (CLRS: for each edge (u, v) ∈ E[G])
        // If we can still relax after V-1 iterations, there's a negative cycle
        for (int u = 0; u < numberOfNodes; u++)
        {
            if (double.IsInfinity(this.distances[u].Value))
                continue;

            for (int v = 0; v < numberOfNodes; v++)
            {
                double weight = this.Adjacency[u, v];
                
                if (double.IsInfinity(weight) || u == v)
                    continue;

                // If d[u] + w(u,v) < d[v], then there's a negative cycle
                if (this.distances[u].Value + weight < this.distances[v].Value)
                {
                    this.hasNegativeCycle = true;
                    return;
                }
            }
        }
    }
}

