# Graph algorithms

`graph/algorithms` — [GraphAlgorithms.cs](GraphAlgorithms.cs)

A tour of `IRI.Maptor.Core.Graph` on a few tiny graphs: build an adjacency list, then run the
classic algorithms on it. Node keys can be any comparable type; weights any `IComparable`.

```csharp
var g = new AdjacencyList<string, int>();
g.AddDirectedEdge("A", "B", 1);
g.AddDirectedEdge("A", "C", 1);
g.AddDirectedEdge("B", "D", 1);
g.AddDirectedEdge("C", "D", 1);

var bfs = new BreadthFirstSearch<string, int>(g, startNode: "A");
bfs.GetLevel("D");                                   // 2
bfs.GetPathTo("D");                                  // A -> B -> D

var dfs = new DepthFirstSearch<string, int>(dag, startNode: "5");
dfs.CalculateTopologicalSort();                      // a valid topological order
dfs.IsOriginalGraphCyclic;                           // false for a DAG

GraphHelper.GetStronglyConnectedComponents<string, int>(g);

MinimumSpanningTree.CalculateByKruskal<string, int>(undirected);
new PrimAlgorithm<string, int>(undirected).GetMinimumSpanningTree();
```

What it prints: the BFS level and path, the topological order and finish-time order, the cycle
check, the strongly connected components, and both minimum spanning trees as adjacency lists.

## How to run

```bash
dotnet run --project samples/IRI.Maptor.Samples.Core -- graph/algorithms
```

See also: [IRI.Maptor.Core.Graph](../../../src/Core/IRI.Maptor.Core.Graph/README.md).

---
[Back to IRI.Maptor.Samples.Core](../README.md)
