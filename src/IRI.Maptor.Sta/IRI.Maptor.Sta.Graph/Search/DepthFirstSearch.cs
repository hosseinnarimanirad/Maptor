// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;
using System.Linq; 

namespace IRI.Maptor.Sta.Graph;

/// <summary>
/// Performs depth-first search (DFS) traversal on a graph.
/// DFS explores as far as possible along each branch before backtracking.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
/// <typeparam name="TWeight">The type of edge weights. Must be comparable.</typeparam>
public class DepthFirstSearch<TNode, TWeight>
    where TWeight : IComparable
{
    /// <summary>
    /// Represents the type of edge in a DFS tree.
    /// </summary>
    public enum EdgeType
    {
        /// <summary>Tree edge - an edge in the DFS tree.</summary>
        Tree,
        /// <summary>Back edge - an edge to an ancestor in the DFS tree (indicates a cycle).</summary>
        Back,
        /// <summary>Forward edge - an edge to a descendant in the DFS tree.</summary>
        Forward,
        /// <summary>Cross edge - an edge between nodes in different subtrees.</summary>
        Cross
    }

    /// <summary>
    /// Gets the DFS tree as an adjacency list.
    /// </summary>
    public AdjacencyList<TNode, TWeight> SearchResult { get; private set; }

    private AdjacencyList<TNode, TWeight> graph;

    List<Edge<TNode, TWeight>> backEdges;

    List<Edge<TNode, TWeight>> crossEdges;

    List<Edge<TNode, TWeight>> forwardEdges;

    public bool IsOriginalGraphCyclic
    {
        get { return backEdges.Count > 0; }
    }
     
    SortedList<TNode, FastDepthFirstSearchNode<TNode>> labels;
    Dictionary<TNode, TNode> predecessors;

    TNode startNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="DepthFirstSearch{TNode, TWeight}"/> class and performs DFS on the graph.
    /// </summary>
    /// <param name="graph">The graph to search.</param>
    /// <param name="startNode">The node to start the DFS from.</param>
    /// <exception cref="ArgumentNullException">Thrown when graph or startNode is null.</exception>
    /// <exception cref="ArgumentException">Thrown when startNode does not exist in the graph.</exception>
    public DepthFirstSearch(AdjacencyList<TNode, TWeight> graph, TNode startNode)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        if (startNode == null)
            throw new ArgumentNullException(nameof(startNode));
        if (!graph.HasTheNode(startNode))
        {
            throw new ArgumentException($"Start node '{startNode}' does not exist in the graph.", nameof(startNode));
        }

        this.InitializeMembers(graph, startNode);

        foreach (TNode node in graph)
        {
            this.SearchResult.AddNode(node);

            labels.Add(node, new FastDepthFirstSearchNode<TNode>(node, null, null));
        }

        int time = 0;

        Visit(startNode, ref time);

        foreach (TNode node in graph)
        {
            if (labels[node].IsWhite())
            {
                Visit(node, ref time);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DepthFirstSearch{TNode, TWeight}"/> class with a specific node order.
    /// Designed to compute Strongly Connected Components (SCC).
    /// </summary>
    /// <param name="graph">The graph to search.</param>
    /// <param name="nodeOrder">The order in which nodes should be visited.</param>
    /// <exception cref="ArgumentNullException">Thrown when graph or nodeOrder is null.</exception>
    /// <exception cref="ArgumentException">Thrown when nodeOrder is empty or contains invalid nodes.</exception>
    public DepthFirstSearch(AdjacencyList<TNode, TWeight> graph, List<TNode> nodeOrder)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        if (nodeOrder == null)
            throw new ArgumentNullException(nameof(nodeOrder));
        if (nodeOrder.Count == 0)
            throw new ArgumentException("Node order list cannot be empty.", nameof(nodeOrder));
        if (!graph.HasTheNode(nodeOrder[0]))
        {
            throw new ArgumentException($"Start node '{nodeOrder[0]}' does not exist in the graph.", nameof(nodeOrder));
        }

        this.InitializeMembers(graph, nodeOrder[0]);

        foreach (TNode node in graph)
        {
            this.SearchResult.AddNode(node);

            labels.Add(node, new FastDepthFirstSearchNode<TNode>(node, null, null));
        }

        int time = 0;

        foreach (TNode node in nodeOrder)
        {
            if (labels[node].IsWhite())
            {
                Visit(node, ref time);
            }
        }
    }

    private void InitializeMembers(AdjacencyList<TNode, TWeight> graph, TNode startNode)
    {
        this.startNode = startNode;

        this.graph = graph;

        this.backEdges = new List<Edge<TNode, TWeight>>();

        this.crossEdges = new List<Edge<TNode, TWeight>>();

        this.forwardEdges = new List<Edge<TNode, TWeight>>();

        this.SearchResult = new AdjacencyList<TNode, TWeight>();

        this.labels = new SortedList<TNode, FastDepthFirstSearchNode<TNode>>(graph.NumberOfNodes);
        this.predecessors = new Dictionary<TNode, TNode>(graph.NumberOfNodes);
    }

    private void Visit(TNode currentNode, ref int time)
    {
        this.labels[currentNode].DiscoverTime = ++time;

        LinkedList<Connection<TNode, TWeight>> connections = graph.GetConnections(currentNode);

        foreach (Connection<TNode, TWeight> node in connections)
        {
            if (this.labels[node.Node].IsWhite())
            {
                SearchResult.AddDirectedEdge(currentNode, node.Node, node.Weight);

                labels[node.Node].Predecessor = labels[currentNode];
                predecessors[node.Node] = currentNode;

                Visit(node.Node, ref time);
            }
            else if (this.labels[node.Node].IsGray())
            {
                this.backEdges.Add(new Edge<TNode, TWeight>(currentNode, new Connection<TNode, TWeight>(node.Node, node.Weight)));
            }
            else if (this.labels[node.Node].IsBlack())
            {
                List<TNode> temp = GetPathToSource(node.Node);

                if (temp.Contains(currentNode))
                {
                    this.forwardEdges.Add(new Edge<TNode, TWeight>(currentNode, new Connection<TNode, TWeight>(node.Node, node.Weight)));
                }
                else
                {
                    this.crossEdges.Add(new Edge<TNode, TWeight>(currentNode, new Connection<TNode, TWeight>(node.Node, node.Weight)));
                }
            }
            else
            {
                throw new InvalidOperationException($"Unexpected node status for node '{node.Node}'.");
            }
        }

        this.labels[currentNode].FinishTime = ++time;
    }

    public List<TNode> GetPathToSource(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (!this.labels.Keys.Contains(node))
        {
            throw new KeyNotFoundException($"Node '{node}' was not visited during the depth-first search.");
        }

        List<TNode> result = new List<TNode>();

        TNode currentNode = node;

        result.Add(currentNode);

        while (predecessors.TryGetValue(currentNode, out TNode? predecessor))
        {
            currentNode = predecessor;
            result.Add(currentNode);
        }

        result.Reverse();

        return result;

    }

    /// <summary>
    /// Calculates a topological sort of the graph nodes based on finish times.
    /// The graph must be a Directed Acyclic Graph (DAG).
    /// </summary>
    /// <returns>A list of nodes in topological order.</returns>
    public List<TNode> CalculateTopologicalSort()
    {
        List<TNode> result = new List<TNode>();

        foreach (TNode item in this.SearchResult)
        {
            int index = 0;

            foreach (TNode temp in result)
            {
                if (this.labels[item].FinishTime > this.labels[temp].FinishTime)
                {
                    index++;
                }
            }

            result.Insert(index, item);
        }

        result.Reverse();

        return result;
    }

    public List<TNode> GetSortedNodes(SortType sortType)
    {
        if (sortType == SortType.BasedOnDiscoverTime)
        {
            return GetSortedNodesBasedOnDiscoverTime();
        }
        else if (sortType == SortType.BasedOnFinishTime)
        {
            return GetSortedNodesBasedOnFinishTime();
        }
        else
        {
            throw new ArgumentException($"Unknown sort type: {sortType}.", nameof(sortType));
        }
    }

    private List<TNode> GetSortedNodesBasedOnFinishTime()
    {
        return SearchResult.OrderBy(node => this.labels[node].FinishTime ?? int.MaxValue).ToList();
    }

    private List<TNode> GetSortedNodesBasedOnDiscoverTime()
    {
        return SearchResult.OrderBy(node => this.labels[node].DiscoverTime ?? int.MaxValue).ToList();
    }

    public override string ToString()
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();

        for (int i = 1; i <= 2 * SearchResult.NumberOfNodes; i++)
        {
            for (int j = 0; j < SearchResult.NumberOfNodes; j++)
            {
                if (this.labels[SearchResult[j]].DiscoverTime.Equals(i))
                {
                    result.Append(string.Format("({0} ", SearchResult[j].ToString()));
                }
                else if (this.labels[SearchResult[j]].FinishTime.Equals(i))
                {
                    result.Append(string.Format(" {0})", SearchResult[j].ToString()));
                }
            }
        }

        return result.ToString();
    }

    public List<List<TNode>> GetComponents()
    {
        int tempIndex = 0;

        int tempGraphNumber = -1;

        List<List<TNode>> result = new List<List<TNode>>();

        for (int i = 1; i < 2 * SearchResult.NumberOfNodes * 2; i++)
        {
            for (int j = 0; j < SearchResult.NumberOfNodes; j++)
            {
                FastDepthFirstSearchNode<TNode> tempInfo = this.labels[this.SearchResult[j]];

                if (tempInfo.DiscoverTime == i)
                {
                    if (tempIndex == 0)
                    {
                        result.Add(new List<TNode>());

                        tempGraphNumber++;
                    }

                    tempIndex++;
                }
                else if (tempInfo.FinishTime == i)
                {
                    tempIndex--;

                    result[tempGraphNumber].Add(this.SearchResult[j]);
                }
            }
        }

        return result;
    }
}
