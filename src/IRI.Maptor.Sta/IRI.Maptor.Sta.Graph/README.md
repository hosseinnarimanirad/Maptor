# IRI.Maptor.Sta.Graph

The **IRI.Maptor.Sta.Graph** library provides a set of **graph data structures and algorithms** inspired by the *Graph* chapter of the classic **CLRS (Introduction to Algorithms)** book.  
It supports **directed and undirected graphs**, **weighted graphs**, and implements core algorithms for **traversal, shortest paths, and more**.

---

## ✅ Features

- Graph representation: **Adjacency List** and **Adjacency Matrix**
- Traversal algorithms:
  - **Breadth-First Search (BFS)**
  - **Depth-First Search (DFS)**
- Shortest path algorithms:
  - **Dijkstra**
  - **Floyd Warshall**
- Minimum spanning tree:
  - **Prim**
- Strongly Connected Components (SCC)
- Utility functions for graph creation and manipulation

---

## ✅ Installation

Add the library to your .NET project:

```powershell
dotnet add package IRI.Maptor.Sta.Graph
```

Or via NuGet package manager:

```
Install-Package IRI.Maptor.Sta.Graph
```

---

## ✅ Basic Usage

### 1. Create a Simple Graph

```csharp
 
using IRI.Maptor.Sta.Graph;

// TNode can be any comparable key (e.g., string, int)
// TWeight must be IComparable (e.g., int, double)
var g = new AdjacencyList<string, int>();

// Directed edges
g.AddDirectedEdge("A", "B", 1);
g.AddDirectedEdge("A", "C", 1);
g.AddDirectedEdge("B", "D", 1);
g.AddDirectedEdge("C", "D", 1);

```

---

### Breadth-First Search (BFS)

```csharp
// Start BFS from source "A"
var bfs = new BreadthFirstSearch<string, int>(g, startNode: "A");

// Distance (level) from A to D
double level = bfs.GetLevel("D"); // 2

// Reconstruct a shortest path from A to D
var path = bfs.GetPathTo("D");    // ["A", "B", "D"] or ["A", "C", "D"]

// The BFS tree discovered from A (as an adjacency list)
var bfsTree = bfs.searchResult;
```

### Depth-First Search (DFS) & Topological Sort

```csharp
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
```

### Strongly Connected Components (SCC)

```csharp
// For a directed graph g:
var components = Graph.GetStronglyConnectedComponents<string, int>(g);

// components is List<List<string>>;
// each inner list is one strongly connected component.
```

### Minimum Spanning Tree (MST)

Build an **undirected weighted** graph and compute MSTs with **Kruskal** or **Prim**.

```csharp
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
```

---

## ✅ Algorithms Implemented

| Algorithm         | Description                                |
|-------------------|--------------------------------------------|
| BFS               | Breadth-First Search traversal            |
| DFS               | Depth-First Search traversal              |
| Dijkstra          | Single-source shortest path (non-negative weights) |
| FloydWarshall     | AllPairs shortest path (handles negative weights) |
| Kruskal           | Minimum Spanning Tree                    |  
| Prim              | Minimum Spanning Tree                    |  
| SCC               | Strongly Connected Components            |

---

## ✅ References
- *Introduction to Algorithms* by Cormen, Leiserson, Rivest, and Stein (CLRS) 
