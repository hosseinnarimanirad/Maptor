# IRI.Maptor.Core.Graph

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Graph?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Core.Graph/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Graph data structures and algorithms for the Maptor stack, inspired by the graph chapters of CLRS (*Introduction to Algorithms*). Supports directed and undirected weighted graphs with the classic traversal, shortest-path, spanning-tree, and connectivity algorithms.

## Installation

```bash
dotnet add package IRI.Maptor.Core.Graph
```

## Features

- Adjacency-list representation (`AdjacencyList<TNode, TWeight>`) with directed and undirected weighted edges, plus a `DirectedAcyclicGraph` type; matrix-based algorithm variants accept a `Matrix` adjacency input
- Traversal: breadth-first search (`BreadthFirstSearch`) and depth-first search (`DepthFirstSearch`, `FastDepthFirstSearch`) with topological sort and cycle detection
- Shortest paths: Dijkstra (`DijkstraProblem`, adjacency-matrix input), Bellman-Ford (`BellmanFordProblem`, `BellmanFordMatrixProblem`; handles negative weights and detects negative cycles), Floyd-Warshall all-pairs (`FloydWarshallProblem`)
- Minimum spanning tree: Kruskal (`MinimumSpanningTree.CalculateByKruskal`) and Prim (`PrimAlgorithm`)
- Strongly connected components (`GraphHelper.GetStronglyConnectedComponents`)
- Minimum cut (`MinimumCut.GetMinCut`) and greedy clustering (`GreedyClustering`)

## Usage

Build a graph and run BFS:

```csharp
using IRI.Maptor.Core.Graph;

var g = new AdjacencyList<string, int>();

g.AddDirectedEdge("A", "B", 1);
g.AddDirectedEdge("A", "C", 1);
g.AddDirectedEdge("B", "D", 1);
g.AddDirectedEdge("C", "D", 1);

var bfs = new BreadthFirstSearch<string, int>(g, startNode: "A");

double level = bfs.GetLevel("D");   // 2
var path = bfs.GetPathTo("D");      // ["A", "B", "D"] or ["A", "C", "D"]
```

Bellman-Ford with negative weights:

```csharp
var graph = new AdjacencyList<string, double>();
graph.AddDirectedEdge("A", "B", 4.0);
graph.AddDirectedEdge("B", "C", -3.0);  // negative weight allowed
graph.AddDirectedEdge("C", "D", 5.0);

var bellmanFord = new BellmanFordProblem<string, double>(graph, "A");

if (!bellmanFord.HasNegativeCycle)
{
    double distance = bellmanFord.GetDistance("D");
    var path = bellmanFord.GetShortestPath("D");
}
```

Minimum spanning tree and strongly connected components:

```csharp
var ug = new AdjacencyList<string, int>();
ug.AddUndirectedEdge("A", "B", 4);
ug.AddUndirectedEdge("A", "C", 1);
ug.AddUndirectedEdge("B", "C", 3);

var mstKruskal = MinimumSpanningTree.CalculateByKruskal<string, int>(ug);
var mstPrim = new PrimAlgorithm<string, int>(ug).GetMinimumSpanningTree();

// each inner list is one strongly connected component of a directed graph
var components = GraphHelper.GetStronglyConnectedComponents<string, int>(g);
```

## See also

- [Graph representation](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Graph/GraphRepresentation/README.md)
- [Search (BFS/DFS)](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Graph/Search/README.md)
- [Shortest paths](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Graph/ShortestPaths/README.md)
- [Minimum spanning tree](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Graph/MinimumSpanningTree/README.md)
- [Minimum cut](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Graph/MinCut/README.md)
- [Clustering](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Graph/Clustering/README.md)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Core.Graph/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Core](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/README.md)
