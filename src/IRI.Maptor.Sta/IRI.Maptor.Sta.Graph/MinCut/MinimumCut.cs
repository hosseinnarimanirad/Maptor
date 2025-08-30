using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Sta.Graph;

public static class MinimumCut
{
    public static int GetMinCut<TNode, TWeight>(AdjacencyList<TNode, TWeight> graph) 
    {
        if (graph.NumberOfNodes <= 2)
        {
            //int count = 0;

            //foreach (TNode item in graph)
            //{
            //    List<Connection<TNode, TWeight>> temp = graph.GetConnections(item);

            //    count += temp.Count;
            //}

            //return count;
            return graph.NumberOfEdges;
        }

        Edge<TNode, TWeight> randomEdge = GetRandomEdge(graph);

        return GetMinCut(EdgeContraction(graph, randomEdge));
    }

    private static Edge<TNode, TWeight> GetRandomEdge<TNode, TWeight>(AdjacencyList<TNode, TWeight> graph)
    { 
        int startNode = RandomHelper.Get(graph.NumberOfEdges);
         
        for (int i = 0; i < graph.NumberOfNodes; i++)
        {
            int temp = graph.GetConnectionsByNodeIndex(i).Count;

            if (temp > startNode)
            {
                return new Edge<TNode, TWeight>(graph[i], graph.GetConnections(graph[i]).ToList()[startNode]);
            }
            else
            {
                startNode = startNode - temp;
            }
        }

        throw new NotImplementedException();
    }

    private static AdjacencyList<TNode, TWeight> EdgeContraction<TNode, TWeight>(AdjacencyList<TNode, TWeight> graph, Edge<TNode, TWeight> edge) 
    {
        TNode first = edge.Node;

        TNode second = edge.Connection.Node;

        AdjacencyList<TNode, TWeight> result = new AdjacencyList<TNode, TWeight>();

        foreach (TNode node in graph)
        {
            if (object.Equals(node, second) || object.Equals(node, first))
            {
                LinkedList<Connection<TNode, TWeight>> connections = graph.GetConnections(node);

                foreach (Connection<TNode, TWeight> connection in connections)
                {
                    if (!object.Equals(connection.Node, first) && !object.Equals(connection.Node, second))
                    {
                        result.AddDirectedEdge(first, connection.Node, connection.Weight);
                    }
                }
            }
            else
            {
                LinkedList<Connection<TNode, TWeight>> connections = graph.GetConnections(node);

                foreach (Connection<TNode, TWeight> connection in connections)
                {
                    if (object.Equals(connection.Node, second))
                    {
                        result.AddDirectedEdge(node, first, connection.Weight);
                    }
                    else
                    {
                        result.AddDirectedEdge(node, connection.Node, connection.Weight);
                    }
                }
            }

        }

        return result;
    }
}
