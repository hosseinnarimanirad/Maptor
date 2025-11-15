// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic; 

namespace IRI.Maptor.Sta.Graph;

/// <summary>
/// Performs breadth-first search (BFS) traversal on a graph starting from a specified node.
/// BFS explores all nodes at the current depth level before moving to nodes at the next level.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
/// <typeparam name="TWeight">The type of edge weights. Must be comparable.</typeparam>
public class BreadthFirstSearch<TNode, TWeight>
    where TWeight : IComparable
{
    /// <summary>
    /// Gets the BFS tree as an adjacency list, representing the shortest path tree from the start node.
    /// </summary>
    public AdjacencyList<TNode, TWeight> SearchResult { get; private set; }

    SortedList<TNode, BreadthFirstSearchNode<TNode>> labels;
    Dictionary<TNode, TNode> predecessors;

    TNode startNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreadthFirstSearch{TNode, TWeight}"/> class and performs BFS on the graph.
    /// </summary>
    /// <param name="graph">The graph to search.</param>
    /// <param name="startNode">The node to start the BFS from.</param>
    /// <exception cref="ArgumentNullException">Thrown when graph or startNode is null.</exception>
    /// <exception cref="ArgumentException">Thrown when startNode does not exist in the graph.</exception>
    public BreadthFirstSearch(AdjacencyList<TNode, TWeight> graph, TNode startNode)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        if (startNode == null)
            throw new ArgumentNullException(nameof(startNode));
        if (!graph.HasTheNode(startNode))
        {
            throw new ArgumentException($"Start node '{startNode}' does not exist in the graph.", nameof(startNode));
        }

        this.startNode = startNode;

        SearchResult = new AdjacencyList<TNode, TWeight>();

        this.labels = new SortedList<TNode, BreadthFirstSearchNode<TNode>>(graph.NumberOfNodes);
        this.predecessors = new Dictionary<TNode, TNode>(graph.NumberOfNodes);

        foreach (TNode node in graph)
        {
            this.SearchResult.AddNode(node);

            labels.Add(node, new BreadthFirstSearchNode<TNode>(NodeStatus.White, double.PositiveInfinity));
        }

        labels[startNode].Status = NodeStatus.Gray;

        labels[startNode].Value = 0;

        Queue<TNode> nodes = new Queue<TNode>();

        nodes.Enqueue(startNode);

        while (nodes.Count > 0)
        {
            TNode currentNode = nodes.Dequeue();

            labels[currentNode].Status = NodeStatus.Black;

            foreach (Connection<TNode, TWeight> item in graph.GetConnections(currentNode))
            {
                if (labels[item.Node].Status == NodeStatus.White)
                {
                    nodes.Enqueue(item.Node);

                    labels[item.Node].Status = NodeStatus.Gray;

                    labels[item.Node].Predecessor = labels[currentNode];

                    labels[item.Node].Value = labels[currentNode].Value + 1;

                    predecessors[item.Node] = currentNode;

                    SearchResult.AddDirectedEdge(currentNode, item.Node, item.Weight);
                }
            }
        }
    }

    /// <summary>
    /// Gets the level (distance) of the specified node from the start node.
    /// </summary>
    /// <param name="node">The node to get the level for.</param>
    /// <returns>The level (distance) from the start node. Returns positive infinity if the node was not visited.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the node was not visited during BFS.</exception>
    public double GetLevel(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (!this.labels.Keys.Contains(node))
        {
            throw new KeyNotFoundException($"Node '{node}' was not visited during the breadth-first search.");
        }

        return labels[node].Value;
    }

    /// <summary>
    /// Gets the shortest path from the start node to the specified node.
    /// </summary>
    /// <param name="node">The target node.</param>
    /// <returns>A list of nodes representing the path from start to target, or null if no path exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the node was not visited during BFS.</exception>
    public List<TNode>? GetPathTo(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (!this.labels.Keys.Contains(node))
        {
            throw new KeyNotFoundException($"Node '{node}' was not visited during the breadth-first search.");
        }

        List<TNode> result = new List<TNode>();

        TNode currentNode = node;

        result.Add(currentNode);

        while (!currentNode.Equals(this.startNode))
        {
            if (!predecessors.TryGetValue(currentNode, out TNode? predecessor) || predecessor == null)
            {
                return null;
            }

            currentNode = predecessor;
            result.Add(currentNode);
        }

        result.Reverse();

        return result;
    }

}
