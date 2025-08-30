// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;

namespace IRI.Maptor.Sta.Graph;


public static class GraphHelper
{
    public static List<List<TNode>> GetStronglyConnectedComponents<TNode, TWeight>(AdjacencyList<TNode, TWeight> graph)
           where TWeight : IComparable
    {
        DepthFirstSearch<TNode, TWeight> dfs = new DepthFirstSearch<TNode, TWeight>(graph, graph[0]);

        AdjacencyList<TNode, TWeight> transpose = graph.Transpose();

        List<TNode> nodeOrder = dfs.GetSortedNodes(SortType.BasedOnFinishTime);

        nodeOrder.Reverse();

        DepthFirstSearch<TNode, TWeight> tempResult = new DepthFirstSearch<TNode, TWeight>(transpose, nodeOrder);

        //List<AdjacencyList<TNode, TWeight>> result = new List<AdjacencyList<TNode, TWeight>>();

        List<TNode> nodeOrder2 = tempResult.GetSortedNodes(SortType.BasedOnFinishTime);

        return tempResult.GetComponents();
    }

    //public static List<List<int>> GetStronglyConnectedComponents(FastAdjacencyList graph)
    //{
    //    FastDepthFirstSearch dfs = new FastDepthFirstSearch(graph, 1);

    //    FastAdjacencyList transpose = graph.Transpose();

    //    List<int> nodeOrder = dfs.GetSortedNodes(SortType.BasedOnFinishTime);

    //    nodeOrder.Reverse();

    //    FastDepthFirstSearch tempResult = new FastDepthFirstSearch(transpose, nodeOrder);

    //    //List<AdjacencyList<TNode, TWeight>> result = new List<AdjacencyList<TNode, TWeight>>();

    //    List<int> nodeOrder2 = tempResult.GetSortedNodes(SortType.BasedOnFinishTime);

    //    return tempResult.GetComponents();
    //}
}
