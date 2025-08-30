using IRI.Maptor.Sta.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Tag.SampleCodes.Graph;

public static class GraphSampleCodes
{
    public static void ReadmeSampleCodes()
    {

        // *******************************************
        // Build a graph (Adjacency List)
        // TNode can be any comparable key (e.g., string, int)
        // TWeight must be IComparable (e.g., int, double)
        var g = new AdjacencyList<string, int>();

        // Directed edges
        g.AddDirectedEdge("A", "B", 1);
        g.AddDirectedEdge("A", "C", 1);
        g.AddDirectedEdge("B", "D", 1);
        g.AddDirectedEdge("C", "D", 1);


        // *******************************************
        // Breadth-First Search (BFS)
        // Start BFS from source "A"
        var bfs = new BreadthFirstSearch<string, int>(g, startNode: "A");

        // Distance (level) from A to D
        double level = bfs.GetLevel("D"); // 2

        // Reconstruct a shortest path from A to D
        var path = bfs.GetPathTo("D");    // ["A", "B", "D"] or ["A", "C", "D"]

        // The BFS tree discovered from A (as an adjacency list)
        var bfsTree = bfs.searchResult;

        // *******************************************
        // Depth-First Search (DFS) & Topological Sort
        // Build a small DAG
        var dag = new AdjacencyList<string, int>();
        dag.AddDirectedEdge("5", "2", 1);
        dag.AddDirectedEdge("5", "0", 1);
        dag.AddDirectedEdge("4", "0", 1);
        dag.AddDirectedEdge("4", "1", 1);
        dag.AddDirectedEdge("2", "3", 1);
        dag.AddDirectedEdge("3", "1", 1);

        // Run DFS (start node can be any existing node)
        var dfs = new DepthFirstSearch<string, int>(dag, startNode: "5");

        // Topological order (method name as implemented in the code)
        var topo = dfs.CalculateTopologiacalSort(); // e.g., ["4","5","2","3","1","0"]

        // Get nodes sorted by finish time, if needed:
        var finishOrder = dfs.GetSortedNodes(SortType.BasedOnFinishTime);

        // Is the original graph cyclic?
        bool hasCycle = dfs.IsOriginalGraphCyclic; // should be false for a DAG



        // *******************************************
        // Strongly Connected Components (SCC)

        // For a directed graph g:
        var components = GraphHelper.GetStronglyConnectedComponents<string, int>(g);

        // components is List<List<string>>;
        // each inner list is one strongly connected component.



        // *******************************************
        // Minimum Spanning Tree (MST)
        //Build an **undirected weighted** graph and compute MSTs with **Kruskal * *or * *Prim * *.

        var ug = new AdjacencyList<string, int>();
        ug.AddUndirectedEdge("A", "B", 4);
        ug.AddUndirectedEdge("A", "C", 1);
        ug.AddUndirectedEdge("B", "C", 3);
        ug.AddUndirectedEdge("B", "D", 2);
        ug.AddUndirectedEdge("C", "D", 5);

        // Kruskal
        var mstKruskal = MinimumSpanningTree.CalculateByKruskal<string, int>(ug);

        // Prim
        var prim = new PrimAlgorithm<string, int>(ug);
        var mstPrim = prim.GetMinimumSpanningTree();
 


    }
}
