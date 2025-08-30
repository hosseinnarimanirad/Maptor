// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic; 

namespace IRI.Maptor.Sta.Graph;

public class FastDepthFirstSearch<TNode, TWeight> where TWeight : new()
{
    List<TNode> nodeOrderBasedOnFinishTime = new List<TNode>();

    SortedList<TNode, FastDepthFirstSearchNode<TNode>> labels;

    public List<LinkedList<TNode>> parts;

    public List<TNode> SortedNodesBasedOnFinishTime
    {
        get { return this.nodeOrderBasedOnFinishTime; }
    }

    private int NumberOfNodes
    {
        get { return this.labels.Count; }
    }

    public FastDepthFirstSearch(AdjacencyList<TNode, TWeight> graph) : this(graph, null) { }

    public FastDepthFirstSearch(AdjacencyList<TNode, TWeight> graph, List<TNode> nodeOrder)
    {
        Initialize(graph);

        if (nodeOrder == null)
        {
            nodeOrder = new List<TNode>();

            for (int i = 0; i < graph.NumberOfNodes; i++)
            {
                nodeOrder.Add(graph[i]);
            }
        }

        DoTheDepthFirstSearch(graph, nodeOrder);
    }

    private void Initialize(AdjacencyList<TNode, TWeight> graph)
    {
        this.parts = new List<LinkedList<TNode>>();

        this.labels = new SortedList<TNode, FastDepthFirstSearchNode<TNode>>(graph.NumberOfNodes);

        foreach (TNode item in graph)
        {
            this.labels.Add(item, new FastDepthFirstSearchNode<TNode>(item));
        }
    }

    private void DoTheDepthFirstSearch(AdjacencyList<TNode, TWeight> graph, List<TNode> nodeOrder)
    {
        if (graph.NumberOfNodes != nodeOrder.Count)
        {
            throw new NotImplementedException();
        }

        int time = 0;

        this.parts = new List<LinkedList<TNode>>();

        Stack<TNode> stack = new Stack<TNode>(graph.NumberOfNodes);

        for (int n = 0; n < graph.NumberOfNodes; n++)
        {
            if (!this.labels[nodeOrder[n]].IsWhite())
            {
                continue;
            }

            this.parts.Add(new LinkedList<TNode>());

            stack.Push(nodeOrder[n]);

            while (stack.Count > 0)
            {
                TNode currentNode = stack.Pop();

                if (this.labels[currentNode].IsWhite())
                {
                    //
                    this.parts[this.parts.Count - 1].AddLast(currentNode);

                    this.labels[currentNode].DiscoverTime = ++time;

                    stack.Push(currentNode);

                    LinkedList<Connection<TNode, TWeight>> edges = graph.GetConnections(currentNode);

                    LinkedListNode<Connection<TNode, TWeight>> tempConnection = edges.Last;

                    while (tempConnection != null)
                    {
                        if (this.labels[tempConnection.Value.Node].IsWhite())
                        {
                            this.labels[tempConnection.Value.Node].Predecessor = this.labels[currentNode];

                            stack.Push(tempConnection.Value.Node);
                        }

                        tempConnection = tempConnection.Previous;
                    }

                }
                else if (this.labels[currentNode].IsGray())
                {
                    this.labels[currentNode].FinishTime = ++time;

                    this.nodeOrderBasedOnFinishTime.Add(currentNode);
                }
            }

        }
    }


    public AdjacencyList<TNode, TWeight> GetDepthFirstSearchGraph()
    {
        AdjacencyList<TNode, TWeight> result = new AdjacencyList<TNode, TWeight>(this.NumberOfNodes);

        foreach (TNode item in this.labels.Keys)
        {
            result.AddNode(item);
        }

        for (int i = 0; i < this.NumberOfNodes; i++)
        {
            if (this.labels.Values[i].Predecessor != null)
            {
                result.AddDirectedEdge(this.labels.Values[i].Predecessor.Value, this.labels.Values[i].Value, new TWeight());
            }
        }

        return result;
    }

    public static List<LinkedList<TNode>> GetStronglyConnectedComponents(AdjacencyList<TNode, TWeight> graph)
    {
        DateTime t0 = DateTime.Now;

        FastDepthFirstSearch<TNode, TWeight> dfs = new FastDepthFirstSearch<TNode, TWeight>(graph);

        TimeSpan dt = DateTime.Now - t0;

        AdjacencyList<TNode, TWeight> dfsGraph = dfs.GetDepthFirstSearchGraph();

        List<TNode> order = dfs.SortedNodesBasedOnFinishTime;

        order.Reverse();

        AdjacencyList<TNode, TWeight> transpose = graph.Transpose();// Graph.Transpose(graph);

        FastDepthFirstSearch<TNode, TWeight> secondDfs = new FastDepthFirstSearch<TNode, TWeight>(transpose, order);

        return secondDfs.parts;
    }

}
