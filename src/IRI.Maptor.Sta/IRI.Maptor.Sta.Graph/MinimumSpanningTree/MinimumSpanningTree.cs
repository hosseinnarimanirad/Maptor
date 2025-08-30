// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Sta.DataStructures.CustomStructures;
using System;
using System.Collections.Generic; 

namespace IRI.Maptor.Sta.Graph;

public static class MinimumSpanningTree
{
    public static AdjacencyList<TNode, TWeight> CalculateByKruskal<TNode, TWeight>(AdjacencyList<TNode, TWeight> graph)
        where TWeight : IComparable
    {

        List<Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>> list = new List<Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>>();

        // what is the use of jungle?
        List<LinkedList<TNode>> jungle = new List<LinkedList<TNode>>();

        Dictionary<TNode, LinkedListNode<TNode>> nodeList = new Dictionary<TNode, LinkedListNode<TNode>>();

        AdjacencyList<TNode, TWeight> result = new AdjacencyList<TNode, TWeight>();

        foreach (TNode item in graph)
        {
            LinkedList<TNode> temp = new LinkedList<TNode>();

            temp.AddFirst(item);

            nodeList.Add(item, temp.First);

            jungle.Add(temp);
        }


        for (int i = 0; i < graph.NumberOfNodes; i++)
        {
            foreach (Connection<TNode, TWeight> item in graph.GetConnections(graph[i]))
            {
                list.Add(new Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>(nodeList[graph[i]],
                                                                                            item.Weight,
                                                                                            nodeList[item.Node]));
            }
        }

        list.Sort(new Comparison<Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>>(CompareEdge));

        foreach (Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>> item in list)
        {
            LinkedListNode<TNode> first = item.First;

            LinkedListNode<TNode> second = item.Third;

            if (first.List != second.List)
            {
                result.AddDirectedEdge(first.Value, second.Value, item.Second);

                result.AddDirectedEdge(second.Value, first.Value, item.Second);

                LinkedList<TNode> tempList = second.List;

                do
                {
                    LinkedListNode<TNode> tempNode = tempList.First;

                    tempList.RemoveFirst();

                    first.List.AddLast(tempNode);

                } while (tempList.Count > 0);

                jungle.Remove(tempList);
            }
        }

        return result;
    }

    private static int CompareEdge<TNode, TWeight>(Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>> first,
        Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>> second)
        where TWeight : IComparable
    {
        return first.Second.CompareTo(second.Second);
    }






    //public static AdjacencyList<TNode, TWeight> CalculateByPrim<TNode, TWeight>(AdjacencyList<TNode, TWeight> graph)
    //  where TWeight : IComparable
    //{
    //    List<Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>> list = new List<Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>>();

    //    // what is the use of jungle?
    //    List<LinkedList<TNode>> jungle = new List<LinkedList<TNode>>();

    //    Dictionary<TNode, LinkedListNode<TNode>> nodeList = new Dictionary<TNode, LinkedListNode<TNode>>();

    //    AdjacencyList<TNode, TWeight> result = new AdjacencyList<TNode, TWeight>();

    //    foreach (TNode item in graph)
    //    {
    //        LinkedList<TNode> temp = new LinkedList<TNode>();

    //        temp.AddFirst(item);

    //        nodeList.Add(item, temp.First);

    //        jungle.Add(temp);
    //    }


    //    for (int i = 0; i < graph.NumberOfNodes; i++)
    //    {
    //        foreach (Connection<TNode, TWeight> item in graph.GetConnections(graph[i]))
    //        {
    //            list.Add(new Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>(nodeList[graph[i]],
    //                                                                                        item.Weight,
    //                                                                                        nodeList[item.Node]));
    //        }
    //    }

    //    //list.Sort(new Comparison<Triple<LinkedListNode<TNode>, TWeight, LinkedListNode<TNode>>>(CompareEdge));

    //    TNode currentNode = graph[0];

    //    List<Connection<TNode, TWeight>> connections = new List<Connection<TNode, TWeight>>();

    //    List<TNode> nodes = new List<TNode>(graph.NumberOfNodes);

    //    foreach (TNode item in graph)
    //    {
    //        nodes.Add(item);
    //    }

    //    while (jungle.Count > 1)
    //    {
    //        LinkedListNode<Connection<TNode, TWeight>> neighbour = graph.GetConnections(currentNode).First;

    //        while (neighbour != null)
    //        {
    //            connections.Add(neighbour.Value);

    //            //parents.Add(neighbour.Value, currentNode);

    //            neighbour = neighbour.Next;
    //        }

    //        Connection<TNode, TWeight> selectedConnection = GetMinimumConnection(connections);

    //        connections.Remove(selectedConnection);

    //    }

    //    return result;
    //}


}
