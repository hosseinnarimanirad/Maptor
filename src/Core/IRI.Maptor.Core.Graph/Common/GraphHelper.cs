// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;

namespace IRI.Maptor.Core.Graph;

/// <summary>
/// Provides utility methods for graph operations.
/// </summary>
public static class GraphHelper
{
    /// <summary>
    /// Computes the strongly connected components (SCC) of a directed graph using Kosaraju's algorithm.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
    /// <typeparam name="TWeight">The type of edge weights. Must be comparable.</typeparam>
    /// <param name="graph">The directed graph to analyze.</param>
    /// <returns>A list of strongly connected components, where each component is a list of nodes.</returns>
    public static List<List<TNode>> GetStronglyConnectedComponents<TNode, TWeight>(AdjacencyList<TNode, TWeight> graph)
           where TWeight : IComparable
    {
        DepthFirstSearch<TNode, TWeight> dfs = new DepthFirstSearch<TNode, TWeight>(graph, graph[0]);

        AdjacencyList<TNode, TWeight> transpose = graph.Transpose();

        List<TNode> nodeOrder = dfs.GetSortedNodes(SortType.BasedOnFinishTime);

        nodeOrder.Reverse();

        DepthFirstSearch<TNode, TWeight> tempResult = new DepthFirstSearch<TNode, TWeight>(transpose, nodeOrder);

        return tempResult.GetComponents();
    }
}
