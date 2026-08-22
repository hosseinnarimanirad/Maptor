using IRI.Maptor.Samples.Core.Runner;
using IRI.Maptor.Core.Graph;

namespace IRI.Maptor.Samples.Core.Graph;

/// <summary>
/// A tour of IRI.Maptor.Core.Graph: build an adjacency list, then run BFS, DFS + topological sort,
/// strongly connected components, and minimum spanning trees (Kruskal and Prim).
/// </summary>
public static class GraphAlgorithms
{
    [Sample("graph/algorithms", "BFS, DFS, topological sort, SCC and MST on an adjacency list")]
    public static void Run()
    {
        // TNode can be any comparable key (string, int, ...); TWeight must be IComparable.
        var g = new AdjacencyList<string, int>();
        g.AddDirectedEdge("A", "B", 1);
        g.AddDirectedEdge("A", "C", 1);
        g.AddDirectedEdge("B", "D", 1);
        g.AddDirectedEdge("C", "D", 1);

        // ---- Breadth-first search -------------------------------------------------------
        var bfs = new BreadthFirstSearch<string, int>(g, startNode: "A");

        Console.WriteLine($"BFS level of D from A : {bfs.GetLevel("D")}");                      // 2
        Console.WriteLine($"BFS path A -> D       : {string.Join(" -> ", bfs.GetPathTo("D"))}"); // A -> B -> D or A -> C -> D

        // ---- Depth-first search and topological sort -----------------------------------
        var dag = new AdjacencyList<string, int>();
        dag.AddDirectedEdge("5", "2", 1);
        dag.AddDirectedEdge("5", "0", 1);
        dag.AddDirectedEdge("4", "0", 1);
        dag.AddDirectedEdge("4", "1", 1);
        dag.AddDirectedEdge("2", "3", 1);
        dag.AddDirectedEdge("3", "1", 1);

        var dfs = new DepthFirstSearch<string, int>(dag, startNode: "5");

        Console.WriteLine($"Topological order     : {string.Join(" ", dfs.CalculateTopologicalSort())}");
        Console.WriteLine($"By finish time        : {string.Join(" ", dfs.GetSortedNodes(SortType.BasedOnFinishTime))}");
        Console.WriteLine($"DAG is cyclic         : {dfs.IsOriginalGraphCyclic}");              // false

        // ---- Strongly connected components ---------------------------------------------
        var components = GraphHelper.GetStronglyConnectedComponents<string, int>(g);

        Console.WriteLine($"SCCs of g             : {string.Join(" | ", components.Select(c => string.Join(",", c)))}");

        // ---- Minimum spanning tree (undirected, weighted) -------------------------------
        var ug = new AdjacencyList<string, int>();
        ug.AddUndirectedEdge("A", "B", 4);
        ug.AddUndirectedEdge("A", "C", 1);
        ug.AddUndirectedEdge("B", "C", 3);
        ug.AddUndirectedEdge("B", "D", 2);
        ug.AddUndirectedEdge("C", "D", 5);

        var mstKruskal = MinimumSpanningTree.CalculateByKruskal<string, int>(ug);
        var mstPrim = new PrimAlgorithm<string, int>(ug).GetMinimumSpanningTree();

        Console.WriteLine("MST (Kruskal), as adjacency list:");
        Console.WriteLine(mstKruskal);
        Console.WriteLine("MST (Prim), as adjacency list:");
        Console.WriteLine(mstPrim);
    }
}
