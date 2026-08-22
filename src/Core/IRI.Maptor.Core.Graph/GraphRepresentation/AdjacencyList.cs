// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Graph;

public class AdjacencyList<TNode, TWeight> : IEnumerable<TNode>
{
    #region Fields & Properties

    private SortedList<TNode, LinkedList<Connection<TNode, TWeight>>> list;
    private int edgeCount;

    public int NumberOfNodes
    {
        get { return this.list.Count; }
    }

    public int NumberOfEdges
    {
        get { return this.edgeCount; }
    }

    public TNode this[int index]
    {
        get
        {
            if (index < 0 || index >= this.list.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be between 0 and {this.list.Count - 1}.");

            return this.list.Keys[index];
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyList{TNode, TWeight}"/> class.
    /// </summary>
    public AdjacencyList()
        : this(1)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyList{TNode, TWeight}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity for the adjacency list.</param>
    public AdjacencyList(int capacity)
    {
        this.list = new SortedList<TNode, LinkedList<Connection<TNode, TWeight>>>(capacity);
        this.edgeCount = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyList{TNode, TWeight}"/> class with the specified nodes.
    /// </summary>
    /// <param name="nodes">The list of nodes to add to the graph.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    public AdjacencyList(List<TNode> nodes)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        this.list = new SortedList<TNode, LinkedList<Connection<TNode, TWeight>>>(nodes.Count);
        this.edgeCount = 0;

        foreach (TNode item in nodes)
        {
            this.AddNode(item);
        }

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyList{TNode, TWeight}"/> class from an adjacency matrix.
    /// </summary>
    /// <param name="nodes">The list of nodes corresponding to the matrix rows/columns.</param>
    /// <param name="adjacencyMatrix">The adjacency matrix representing the graph edges.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or adjacencyMatrix is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the matrix dimensions do not match the number of nodes.</exception>
    public AdjacencyList(List<TNode> nodes, TWeight[,] adjacencyMatrix)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (adjacencyMatrix == null)
            throw new ArgumentNullException(nameof(adjacencyMatrix));

        int numberOfNodes = nodes.Count;

        if (adjacencyMatrix.GetLength(0) != numberOfNodes || adjacencyMatrix.GetLength(1) != numberOfNodes)
        {
            throw new ArgumentException("Adjacency matrix dimensions must match the number of nodes.", nameof(adjacencyMatrix));
        }

        this.list = new SortedList<TNode, LinkedList<Connection<TNode, TWeight>>>(nodes.Count);
        this.edgeCount = 0;

        foreach (TNode item in nodes)
        {
            this.AddNode(item);
        }

        for (int i = 0; i < numberOfNodes; i++)
        {
            for (int j = 0; j < numberOfNodes; j++)
            {
                this.AddDirectedEdgeUnsafely(nodes[i], nodes[j], adjacencyMatrix[i, j]);
            }
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the connections (edges) for the node at the specified index.
    /// </summary>
    /// <param name="nodeIndex">The zero-based index of the node.</param>
    /// <returns>A linked list of connections from the node.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public LinkedList<Connection<TNode, TWeight>> GetConnectionsByNodeIndex(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= this.list.Count)
            throw new ArgumentOutOfRangeException(nameof(nodeIndex), $"Index must be between 0 and {this.list.Count - 1}.");

        return this.list.Values[nodeIndex];
    }

    /// <summary>
    /// Gets the connections (edges) for the specified node.
    /// </summary>
    /// <param name="node">The node to get connections for.</param>
    /// <returns>A linked list of connections from the node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the node does not exist in the graph.</exception>
    public LinkedList<Connection<TNode, TWeight>> GetConnections(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (!this.list.ContainsKey(node))
            throw new KeyNotFoundException($"Node '{node}' does not exist in the graph.");

        return this.list[node];
    }

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the node already exists in the graph.</exception>
    public void AddNode(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (this.list.Keys.Contains(node))
        {
            throw new ArgumentException($"Node '{node}' already exists in the graph.", nameof(node));
        }

        this.list.Add(node, new LinkedList<Connection<TNode, TWeight>>());
    }

    /// <summary>
    /// Adds a directed edge from the first node to the second node with the specified weight.
    /// If either node does not exist, it will be automatically added to the graph.
    /// </summary>
    /// <param name="firstNode">The source node of the edge.</param>
    /// <param name="secondNode">The target node of the edge.</param>
    /// <param name="weight">The weight of the edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when firstNode or secondNode is null.</exception>
    public void AddDirectedEdge(TNode firstNode, TNode secondNode, TWeight weight)
    {
        if (firstNode == null)
            throw new ArgumentNullException(nameof(firstNode));
        if (secondNode == null)
            throw new ArgumentNullException(nameof(secondNode));

        if (!this.list.Keys.Contains(firstNode))
        {
            this.AddNode(firstNode);
        }

        if (!this.list.Keys.Contains(secondNode))
        {
            this.AddNode(secondNode);
        }

        this.list[firstNode].AddLast(new Connection<TNode, TWeight>(secondNode, weight));
        this.edgeCount++;
    }

    /// <summary>
    /// Adds an undirected edge between the two nodes with the specified weight.
    /// If either node does not exist, it will be automatically added to the graph.
    /// </summary>
    /// <param name="firstNode">The first node of the edge.</param>
    /// <param name="secondNode">The second node of the edge.</param>
    /// <param name="weight">The weight of the edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when firstNode or secondNode is null.</exception>
    public void AddUndirectedEdge(TNode firstNode, TNode secondNode, TWeight weight)
    {
        if (firstNode == null)
            throw new ArgumentNullException(nameof(firstNode));
        if (secondNode == null)
            throw new ArgumentNullException(nameof(secondNode));

        if (!this.list.Keys.Contains(firstNode))
        {
            this.AddNode(firstNode);
        }

        if (!this.list.Keys.Contains(secondNode))
        {
            this.AddNode(secondNode);
        }

        this.list[firstNode].AddLast(new Connection<TNode, TWeight>(secondNode, weight));
        this.edgeCount++;

        this.list[secondNode].AddLast(new Connection<TNode, TWeight>(firstNode, weight));
        this.edgeCount++;
    }

    /// <summary>
    /// Adds a directed edge without checking if nodes exist. This method assumes both nodes already exist in the graph.
    /// Use this method for performance-critical scenarios where node existence is guaranteed.
    /// </summary>
    /// <param name="firstNode">The source node of the edge.</param>
    /// <param name="secondNode">The target node of the edge.</param>
    /// <param name="weight">The weight of the edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when firstNode or secondNode is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when firstNode does not exist in the graph.</exception>
    public void AddDirectedEdgeUnsafely(TNode firstNode, TNode secondNode, TWeight weight)
    {
        if (firstNode == null)
            throw new ArgumentNullException(nameof(firstNode));
        if (secondNode == null)
            throw new ArgumentNullException(nameof(secondNode));
        if (!this.list.ContainsKey(firstNode))
            throw new KeyNotFoundException($"Node '{firstNode}' does not exist in the graph.");

        this.list[firstNode].AddLast(new Connection<TNode, TWeight>(secondNode, weight));
        this.edgeCount++;
    }

    /// <summary>
    /// Determines whether the graph contains the specified node.
    /// </summary>
    /// <param name="node">The node to locate in the graph.</param>
    /// <returns>true if the graph contains the node; otherwise, false.</returns>
    public bool HasTheNode(TNode node)
    {
        return this.list.Keys.Contains(node);
    }

    /// <summary>
    /// Creates the transpose (reverse) of the graph by reversing all edge directions.
    /// </summary>
    /// <returns>A new adjacency list representing the transposed graph.</returns>
    public AdjacencyList<TNode, TWeight> Transpose()
    {
        AdjacencyList<TNode, TWeight> result = new AdjacencyList<TNode, TWeight>();

        foreach (TNode item in this)
        {
            result.AddNode(item);
        }

        foreach (TNode tempNode in this)
        {
            LinkedList<Connection<TNode, TWeight>> connections = this.GetConnections(tempNode);

            foreach (Connection<TNode, TWeight> tempConnection in connections)
            {
                result.AddDirectedEdge(tempConnection.Node, tempNode, tempConnection.Weight);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns a string representation of the graph.
    /// </summary>
    /// <returns>A string showing all nodes and their connections.</returns>
    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();

        foreach (TNode item in this)
        {
            LinkedList<Connection<TNode, TWeight>> connections = this.GetConnections(item);

            builder.Append("(" + item.ToString() + ": ");
            
            builder.Append(string.Join(" , ", connections));

            builder.AppendLine(")-");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Aggregates all edge weights using the specified aggregation function.
    /// </summary>
    /// <param name="initialValue">The initial value for the aggregation.</param>
    /// <param name="aggregateFunc">The function to aggregate weights.</param>
    /// <returns>The aggregated weight value.</returns>
    public TWeight AggregateWeights(TWeight initialValue, Func<TWeight, TWeight, TWeight> aggregateFunc)
    {
        TWeight result = initialValue;

        for (int i = 0; i < this.NumberOfNodes; i++)
        {
            LinkedList<Connection<TNode, TWeight>> temp = GetConnections(this[i]);

            LinkedListNode<Connection<TNode, TWeight>> currentItem = temp.First;

            while (currentItem != null)
            {
                result = aggregateFunc(result, currentItem.Value.Weight);

                currentItem = currentItem.Next;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all edges in the graph.
    /// </summary>
    /// <returns>A list of all edges in the graph.</returns>
    public List<Edge<TNode, TWeight>> GetEdges()
    {
        List<Edge<TNode, TWeight>> edges = new List<Edge<TNode, TWeight>>();

        foreach (TNode item in this)
        {
            LinkedList<Connection<TNode, TWeight>> connections = this.GetConnections(item);

            foreach (Connection<TNode, TWeight> con in connections)
            {
                edges.Add(new Edge<TNode, TWeight>(item, con));
            }
        }

        return edges;
    }

    #endregion

    #region IEnumerable<TNode> Members

    public IEnumerator<TNode> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfNodes; i++)
        {
            yield return this.list.Keys[i];
        }
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion
}