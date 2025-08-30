// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic; 

namespace IRI.Maptor.Sta.Graph;

public class DepthFirstSearch<TNode, TWeight>
    where TWeight : IComparable
{
    public enum EdgeType
    {
        Tree,
        Back,
        Forward,
        Cross
    }

    public AdjacencyList<TNode, TWeight> searchResult;

    private AdjacencyList<TNode, TWeight> graph;

    List<Edge<TNode, TWeight>> backEdges;

    List<Edge<TNode, TWeight>> crossEdges;

    List<Edge<TNode, TWeight>> forwardEdges;

    public bool IsOriginalGraphCyclic
    {
        get { return backEdges.Count > 0; }
    }
     
    SortedList<TNode, FastDepthFirstSearchNode<TNode>> labels;

    TNode startNode;

    public DepthFirstSearch(AdjacencyList<TNode, TWeight> graph, TNode startNode)
    {
        if (!graph.HasTheNode(startNode))
        {
            throw new NotImplementedException();
        }

        this.InitializeMembers(graph, startNode);

        foreach (TNode node in graph)
        {
            this.searchResult.AddNode(node);

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
    /// Designed to compute the Strongly Connected Components
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="startNode"></param>
    /// <param name="nodeOrder"></param>
    public DepthFirstSearch(AdjacencyList<TNode, TWeight> graph, List<TNode> nodeOrder)
    {
        if (!graph.HasTheNode(startNode))
        {
            throw new NotImplementedException();
        }

        this.InitializeMembers(graph, nodeOrder[0]);

        foreach (TNode node in graph)
        {
            this.searchResult.AddNode(node);

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

        this.searchResult = new AdjacencyList<TNode, TWeight>();

        //this.labels = new  UniquePairs<TNode, NodeInfo<TNode, TimeStamp>>();
        this.labels = new SortedList<TNode, FastDepthFirstSearchNode<TNode>>(graph.NumberOfNodes);
    }

    private void Visit(TNode currentNode, ref int time)
    {
        //this.labels[currentNode].Status = NodeStatus.Gray;

        //time += 1;

        //this.labels[currentNode].Value = new TimeStamp(time, null);
        this.labels[currentNode].DiscoverTime = ++time;

        LinkedList<Connection<TNode, TWeight>> connections = graph.GetConnections(currentNode);

        foreach (Connection<TNode, TWeight> node in connections)
        {
            if (this.labels[node.Node].IsWhite())
            {
                searchResult.AddDirectedEdge(currentNode, node.Node, node.Weight);

                labels[node.Node].Predecessor = labels[currentNode];

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
                throw new NotImplementedException();
            }
        }

        //this.labels[currentNode].Status = NodeStatus.Black;

        //this.labels[currentNode].Value = new TimeStamp(this.labels[currentNode].Value.DiscoverTime, ++time);
        this.labels[currentNode].FinishTime = ++time;
    }

    public List<TNode> GetPathToSource(TNode node)
    {
        //if (!this.labels.FirstValuesContains(node))
        if (!this.labels.Keys.Contains(node))
        {
            throw new NotImplementedException();
        }

        List<TNode> result = new List<TNode>();

        TNode currentNode = node;

        result.Add(currentNode);

        //while (!object.Equals(this.labels.GetSecondValue(currentNode).Predecessor, null))
        while (!object.Equals(this.labels[currentNode].Predecessor, null))
        {
            //currentNode = labels.GetFirstValue(labels.GetSecondValue(currentNode).Predecessor);
            currentNode = labels.Keys[this.labels.Values.IndexOf(labels[currentNode].Predecessor)];

            result.Add(currentNode);
        }

        result.Reverse();

        return result;

    }

    /// <summary>
    /// the graph must be a dag!
    /// </summary>
    /// <returns></returns>
    public List<TNode> CalculateTopologiacalSort()
    {
        List<TNode> result = new List<TNode>();

        foreach (TNode item in this.searchResult)
        {
            int index = 0;

            foreach (TNode temp in result)
            {
                //if (this.labels[item].Value.FinishTime > this.labels[temp].Value.FinishTime)
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
            throw new NotImplementedException();
        }
    }

    private List<TNode> GetSortedNodesBasedOnFinishTime()
    {
        List<TNode> result = new List<TNode>();

        for (int i = 1; i <= 2 * searchResult.NumberOfNodes; i++)
        {
            for (int j = 0; j < searchResult.NumberOfNodes; j++)
            {
                //if (this.labels[searchResult[j]].Value.FinishTime.Equals(i))
                if (this.labels[searchResult[j]].FinishTime.Equals(i))
                {
                    result.Add(searchResult[j]);
                }
            }
        }

        return result;
    }

    private List<TNode> GetSortedNodesBasedOnDiscoverTime()
    {
        List<TNode> result = new List<TNode>();

        for (int i = 1; i <= 2 * searchResult.NumberOfNodes; i++)
        {
            for (int j = 0; j < searchResult.NumberOfNodes; j++)
            {
                //if (this.labels[searchResult[j]].Value.DiscoverTime.Equals(i))
                if (this.labels[searchResult[j]].DiscoverTime.Equals(i))
                {
                    result.Add(searchResult[j]);
                }
            }
        }

        return result;
    }

    public override string ToString()
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();

        for (int i = 1; i <= 2 * searchResult.NumberOfNodes; i++)
        {
            for (int j = 0; j < searchResult.NumberOfNodes; j++)
            {
                //if (this.labels[searchResult[j]].Value.DiscoverTime.Equals(i))
                if (this.labels[searchResult[j]].DiscoverTime.Equals(i))
                {
                    result.Append(string.Format("({0} ", searchResult[j].ToString()));
                }
                //else if (this.labels[searchResult[j]].Value.FinishTime.Equals(i))
                else if (this.labels[searchResult[j]].FinishTime.Equals(i))
                {
                    result.Append(string.Format(" {0})", searchResult[j].ToString()));
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

        for (int i = 1; i < 2 * searchResult.NumberOfNodes * 2; i++)
        {
            for (int j = 0; j < searchResult.NumberOfNodes; j++)
            {
                //NodeInfo<TNode, TimeStamp> tempInfo = this.labels[this.searchResult[j]];
                FastDepthFirstSearchNode<TNode> tempInfo = this.labels[this.searchResult[j]];

                //if (tempInfo.Value.DiscoverTime == i)
                if (tempInfo.DiscoverTime == i)
                {
                    if (tempIndex == 0)
                    {
                        result.Add(new List<TNode>());

                        tempGraphNumber++;
                    }

                    tempIndex++;
                }
                //else if (tempInfo.Value.FinishTime == i)
                else if (tempInfo.FinishTime == i)
                {
                    tempIndex--;

                    result[tempGraphNumber].Add(this.searchResult[j]);
                }
            }
        }

        return result;
    }
}
