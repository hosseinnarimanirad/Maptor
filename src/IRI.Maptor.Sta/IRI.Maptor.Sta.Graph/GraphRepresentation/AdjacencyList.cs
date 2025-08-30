// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Graph;

public class AdjacencyList<TNode, TWeight> : IEnumerable<TNode>
{
    #region Fields & Properties

    private SortedList<TNode, LinkedList<Connection<TNode, TWeight>>> list;

    public int NumberOfNodes
    {
        get { return this.list.Count; }
    }

    public int NumberOfEdges
    {
        get { return CalculateNumberOfEdges(); }
    }

    public TNode this[int index]
    {
        get
        {
            return this.list.Keys[index];
        }
    }

    #endregion

    #region Constructors

    public AdjacencyList()
        : this(1)
    {
    }

    public AdjacencyList(int capacity)
    {
        this.list = new SortedList<TNode, LinkedList<Connection<TNode, TWeight>>>(capacity);
    }

    public AdjacencyList(List<TNode> nodes)
    {
        this.list = new SortedList<TNode, LinkedList<Connection<TNode, TWeight>>>(nodes.Count);

        foreach (TNode item in nodes)
        {
            this.AddNode(item);
        }

    }

    public AdjacencyList(List<TNode> nodes, TWeight[,] adjacencyMatrix)
    {
        int numberOfNodes = nodes.Count;

        if (adjacencyMatrix.GetLength(0) != numberOfNodes || adjacencyMatrix.GetLength(1) != numberOfNodes)
        {
            throw new NotImplementedException();
        }

        this.list = new SortedList<TNode, LinkedList<Connection<TNode, TWeight>>>(nodes.Count);

        foreach (TNode item in nodes)
        {
            this.AddNode(item);
        }

        for (int i = 0; i < numberOfNodes; i++)
        {
            for (int j = 0; j < numberOfNodes; j++)
            {
                this.AddDirectedEdgeUnsafly(nodes[i], nodes[j], adjacencyMatrix[i, j]);
            }
        }
    }

    #endregion

    #region Methods

    private int CalculateNumberOfEdges()
    {
        int result = 0;

        for (int i = 0; i < this.list.Count; i++)
        {
            result += this.list.Values[i].Count;
        }

        return result;
    }

    public LinkedList<Connection<TNode, TWeight>> GetConnectionsByNodeIndex(int nodeIndex)
    {
        return this.list.Values[nodeIndex];
    }

    public LinkedList<Connection<TNode, TWeight>> GetConnections(TNode node)
    {
        return this.list[node];
    }

    public void AddNode(TNode node)
    {
        if (this.list.Keys.Contains(node))
        {
            throw new NotImplementedException();
        }

        this.list.Add(node, new LinkedList<Connection<TNode, TWeight>>());
    }

    public void AddDirectedEdge(TNode firstNode, TNode secondNode, TWeight weight)
    {
        if (!this.list.Keys.Contains(firstNode))//|| !this.list.Keys.Contains(secondNode))
        {
            this.AddNode(firstNode);
            //throw new NotImplementedException();
        }

        if (!this.list.Keys.Contains(secondNode))
        {
            this.AddNode(secondNode);
        }

        this.list[firstNode].AddLast(new Connection<TNode, TWeight>(secondNode, weight));
    }

    public void AddUndirectedEdge(TNode firstNode, TNode secondNode, TWeight weight)
    {
        if (!this.list.Keys.Contains(firstNode))
        {
            this.AddNode(firstNode);
        }

        if (!this.list.Keys.Contains(secondNode))
        {
            this.AddNode(secondNode);
        }

        this.list[firstNode].AddLast(new Connection<TNode, TWeight>(secondNode, weight));

        this.list[secondNode].AddLast(new Connection<TNode, TWeight>(firstNode, weight));
    }

    public void AddDirectedEdgeUnsafly(TNode firstNode, TNode secondNode, TWeight weight)
    {
        this.list[firstNode].AddLast(new Connection<TNode, TWeight>(secondNode, weight));
    }

    //public TWeight[,] ToAdjacencyMatrix()
    //{
    //    int numberOfNodes = this.NumberOfNodes;

    //    if (numberOfNodes == 0)
    //    {
    //        return null;
    //    }

    //    TWeight[,] result = new TWeight[numberOfNodes, numberOfNodes];

    //    for (int i = 0; i < numberOfNodes; i++)
    //    {
    //        int tempLength = this.values[i].Count;

    //        for (int j = 0; j < tempLength; j++)
    //        {
    //            //result[i, j] = this.list.Values[i].[j].Weight;
    //        }
    //    }

    //    return result;
    //}

    public bool HasTheNode(TNode node)
    {
        return this.list.Keys.Contains(node);
    }

    //public bool HasTheEdge(Edge<TNode, TWeight> edge)
    //{
    //    if (!this.HasTheNode(edge.Node))
    //    {
    //        throw new NotImplementedException();
    //    }

    //    LinkedList<Connection<TNode, TWeight>> tempResult = GetConnections(edge.Node);

    //    return tempResult.Contains(edge.Connection);
    //}

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

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();

        foreach (TNode item in this)
        {
            LinkedList<Connection<TNode, TWeight>> connections = this.GetConnections(item);

            builder.Append("(" + item.ToString() + ": ");
            
            builder.Append(string.Join(" , ", connections));
            //foreach (Connection<TNode, TWeight> item2 in connections)
            //{
            //    builder.Append(item2.ToString() + ", ");
            //}

            builder.AppendLine(")-");
        }

        return builder.ToString();
    }

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

//Emkane remove kardane yek "node".