using System;
using System.Collections.Generic; 

namespace IRI.Maptor.Sta.Graph;

public class PrimAlgorithm<TNode, TWeight> where TWeight : IComparable
{
    AdjacencyList<TNode, TWeight> network;

    List<TNode> visitedNodes = new List<TNode>();

    List<TNode> newNodes;

    List<Edge<TNode, TWeight>> connections = new List<Edge<TNode, TWeight>>();

    Dictionary<TNode, LinkedListNode<TNode>> nodeList = new Dictionary<TNode, LinkedListNode<TNode>>();


    public PrimAlgorithm(AdjacencyList<TNode, TWeight> network)
    {
        this.network = network;
    }

    public AdjacencyList<TNode, TWeight> GetMinimumSpanningTree()
    {
        AdjacencyList<TNode, TWeight> result = new AdjacencyList<TNode, TWeight>(this.network.NumberOfNodes);

        TNode currentNode = this.network[0];
        UpdateNewEdges(currentNode);
        this.newNodes = new List<TNode>();
        this.newNodes.Add(this.network[0]);

        Initialize();

        while (result.NumberOfNodes != network.NumberOfNodes)
        {
            Edge<TNode, TWeight> safeEdge = GetMinimumConnection(connections);

            connections.Remove(safeEdge);

            //this.newNodes = ProceedNode(this.newNodes);

            LinkedListNode<TNode> first = nodeList[safeEdge.Connection.Node];

            LinkedListNode<TNode> second = nodeList[safeEdge.Node];

            if (first.List != second.List)
            {
                result.AddUndirectedEdge(first.Value, second.Value, safeEdge.Connection.Weight);

                LinkedList<TNode> tempList = second.List;

                do
                {
                    LinkedListNode<TNode> tempNode = tempList.First;

                    tempList.RemoveFirst();

                    first.List.AddLast(tempNode);

                } while (tempList.Count > 0);

                UpdateNewEdges(first.Value);

                UpdateNewEdges(second.Value);
            }

        }

        return result;
    }

    private void Initialize()
    {
        foreach (TNode item in this.network)
        {
            LinkedList<TNode> temp = new LinkedList<TNode>();

            temp.AddFirst(item);

            nodeList.Add(item, temp.First);
        }
    }

    private static Edge<TNode, TWeight> GetMinimumConnection(List<Edge<TNode, TWeight>> list)
    {
        Edge<TNode, TWeight> result = list[0];

        for (int i = 0; i < list.Count; i++)
        {
            if (result.Connection.Weight.CompareTo(list[i].Connection.Weight) > 0)
            {
                result = list[i];
            }
        }

        return result;
    }

    private void UpdateNewEdges(TNode node)
    {
        if (!this.visitedNodes.Contains(node))
        {
            LinkedListNode<Connection<TNode, TWeight>> neighbour = network.GetConnections(node).First;

            while (neighbour != null)
            {
                //if (!this.visitedNodes.Contains(neighbour.Value.Node))
                //{
                connections.Add(new Edge<TNode, TWeight>(node, neighbour.Value));
                //}

                neighbour = neighbour.Next;
            }

            this.visitedNodes.Add(node);
        }


    }
}
