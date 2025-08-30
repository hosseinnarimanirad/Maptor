// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic; 

namespace IRI.Maptor.Sta.Graph;

public class BreadthFirstSearch<TNode, TWeight>
    where TWeight : IComparable
{
    public AdjacencyList<TNode, TWeight> searchResult;

    SortedList<TNode, BreadthFirstSearchNode<TNode>> labels;

    TNode startNode;

    public BreadthFirstSearch(AdjacencyList<TNode, TWeight> graph, TNode startNode)
    {
        if (!graph.HasTheNode(startNode))
        {
            throw new NotImplementedException();
        }

        this.startNode = startNode;

        searchResult = new AdjacencyList<TNode, TWeight>();

        this.labels = new SortedList<TNode, BreadthFirstSearchNode<TNode>>(graph.NumberOfNodes);

        foreach (TNode node in graph)
        {
            this.searchResult.AddNode(node);

            labels.Add(node, new BreadthFirstSearchNode<TNode>(NodeStatus.White, double.PositiveInfinity));
        }

        labels[startNode].Status = NodeStatus.Gray;

        labels[startNode].Value = 0;
        //.Status = NodeStatus.Gray; labels[startNode].Value = 0;

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

                    searchResult.AddDirectedEdge(currentNode, item.Node, item.Weight);
                }
            }
        }
    }

    public double GetLevel(TNode node)
    {
        if (!this.labels.Keys.Contains(node))
        {
            throw new NotImplementedException();
        }

        return labels[node].Value;
    }

    public List<TNode> GetPathTo(TNode node)
    {
        if (!this.labels.Keys.Contains(node))
        {
            throw new NotImplementedException();
        }

        List<TNode> result = new List<TNode>();

        TNode currentNode = node;

        result.Add(currentNode);

        while (!currentNode.Equals(this.startNode))
        {
            BreadthFirstSearchNode<TNode> temp = this.labels[currentNode].Predecessor;

            if (object.Equals(temp, null))
            {
                return null;
            }
            else
            {
                currentNode = this.labels.Keys[this.labels.Values.IndexOf(temp)];

                result.Add(currentNode);
            }
        }

        result.Reverse();

        return result;
    }

}
