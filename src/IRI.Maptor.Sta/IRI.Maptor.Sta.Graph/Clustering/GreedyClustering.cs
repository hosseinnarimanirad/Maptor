using System;
using System.Collections.Generic;
using System.Linq; 

namespace IRI.Maptor.Sta.Graph;

public class GreedyClustering<TNode, TWeight>
{
    AdjacencyList<TNode, TWeight> graph;

    List<LinkedList<TNode>> clusters;

    Func<Edge<TNode, TWeight>, Edge<TNode, TWeight>, int> compareFunction;

    IRI.Maptor.Sta.DataStructures.BinaryHeap<Edge<TNode, TWeight>> mostDesiredPairs;

    public TWeight ClusterSpacing
    {
        get { return GetMaximum(); }
    }

    public GreedyClustering(AdjacencyList<TNode, TWeight> graph, Func<Edge<TNode, TWeight>, Edge<TNode, TWeight>, int> compareFunction)
    {
        this.graph = graph;
        this.compareFunction = compareFunction;
    }

    private void Initialize()
    {
        this.clusters = new List<LinkedList<TNode>>();

        foreach (TNode item in graph)
        {
            LinkedList<TNode> temp = new LinkedList<TNode>();

            temp.AddFirst(item);

            this.clusters.Add(temp);
        }
        
        this.mostDesiredPairs = new IRI.Maptor.Sta.DataStructures.BinaryHeap<Edge<TNode, TWeight>>(graph.GetEdges().ToArray(), compareFunction);
    }

    public List<LinkedList<TNode>> Cluster(int numberOfClusters)
    {
        Initialize();

        if (numberOfClusters < 1)
            throw new ArgumentException("Number of clusters must be at least 1.", nameof(numberOfClusters));
        if (this.clusters.Count < numberOfClusters)
        {
            throw new ArgumentException($"Cannot create {numberOfClusters} clusters from {this.clusters.Count} nodes.", nameof(numberOfClusters));
        }

        while (this.clusters.Count != numberOfClusters)
        {
            Edge<TNode, TWeight> edge = mostDesiredPairs.ReleaseValue();

            LinkedList<TNode> first = FindCluster(edge.Node);

            LinkedList<TNode> second = FindCluster(edge.Connection.Node);

            if (first != second)
            {
                MergeClusters(first, second);
            }
        }

        return this.clusters;
    }

    public List<LinkedList<TNode>> Cluster(TWeight threshold, Func<TWeight, TWeight, bool> criteriaFunc, Func<TNode, TNode, TWeight> wightFunc)
    {
        Initialize();

        Edge<TNode, TWeight> edge;

        bool condition;

        do
        {
            edge = mostDesiredPairs.ReleaseValue();

            LinkedList<TNode> first = FindCluster(edge.Node);

            LinkedList<TNode> second = FindCluster(edge.Connection.Node);

            condition = criteriaFunc(edge.Connection.Weight, threshold);

            if (first != second && condition)
            {
                MergeClusters(first, second);
            }

        } while (condition);

        return this.clusters;
    }


    private LinkedList<TNode> FindCluster(TNode node)
    {
        foreach (LinkedList<TNode> item in this.clusters)
        {
            if (item.Contains(node))
            {
                return item;
            }
        }

        throw new InvalidOperationException($"Node '{node}' was not found in any cluster.");
    }

    private void MergeClusters(LinkedList<TNode> first, LinkedList<TNode> second)
    {
        while (second.Count > 0)
        {
            TNode temp = second.First.Value;

            second.RemoveFirst();

            first.AddLast(temp);
        }

        this.clusters.Remove(second);
    }

    private TWeight GetMaximum()
    {
        LinkedList<TNode> first;

        LinkedList<TNode> second;

        Edge<TNode, TWeight> edge;

        do
        {
            edge = mostDesiredPairs.ReleaseValue();

            first = FindCluster(edge.Node);

            second = FindCluster(edge.Connection.Node);

        } while (first == second);

        return edge.Connection.Weight;
    }
}

