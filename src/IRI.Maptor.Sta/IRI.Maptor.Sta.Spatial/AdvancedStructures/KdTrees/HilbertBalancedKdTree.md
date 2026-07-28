# Hilbert-balanced k-d tree — when insertion order matters

A k-d tree only earns its `O(log n)` reputation if it stays balanced — and the simple insertion algorithm makes no such promise. Its shape depends entirely on the order points arrive. This page shows the failure mode with real output from the classes in this folder, and how `BalancedKdTree<T>` avoids it by ranking points along a **Hilbert space-filling curve**.

<p align="center">
  <img src="../../images/simple-vs-hilbert-kdtree.png" alt="Simple k-d tree (height 9) vs Hilbert-balanced k-d tree (height 4)" width="800">
</p>

## The failure mode: sorted input

`KdTree<T>` inserts each point by walking from the root — comparing on x at even levels, y at odd levels — and hangs the new node wherever it falls off the tree. Nothing ever rebalances. Feed it points that lie on a line (GPS tracks, road vertices, scanline output — sorted data is *common* in GIS), and every comparison sends the new point to the same side:

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

// 10 points on a descending line — already sorted by x
var points = new Point[]
{
    new(8, 92), new(18, 82), new(26, 74), new(34, 66), new(42, 58),
    new(52, 48), new(60, 40), new(70, 30), new(80, 20), new(90, 10),
};

// even levels split on x, odd levels on y
var xyComparers = new List<Func<Point, Point, int>>
{
    (p1, p2) => p1.X.CompareTo(p2.X),
    (p1, p2) => p1.Y.CompareTo(p2.Y),
};

var simple = new KdTree<Point>(points, xyComparers);
// height = 9 — ten points, ten levels
```

Every insertion descends past all previous nodes and adds a new level: **10 points, height 9**. The "tree" is a linked list, the splitting planes carve the map into a staircase of slivers, and search is `O(n)`. The left panel of the figure above is this exact tree.

## The fix: rank points on a Hilbert curve, balance on the rank

The trouble is that a k-d tree has no cheap rotation: a node's depth decides its splitting axis, so the red-black trick of rotating subtrees would invalidate every split below the rotation point.

`BalancedKdTree<T>` sidesteps this by changing the key. A **Hilbert curve** threads through the whole extent and visits every location exactly once, so it induces a total order on 2-D points — one that famously *preserves locality* (points near each other on the map get similar ranks). Compare points by their Hilbert rank and the spatial index becomes a plain 1-D binary search tree — and 1-D trees know how to stay balanced: `BalancedKdTree<T>` reorders nodes on every insertion with classic red-black rotations (`InsertFixup`).

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.AdvancedStructures;
using IRI.Maptor.Sta.Spatial.Analysis.SFC;

// rank points along a Hilbert curve spanning the data's extent
var boundary = PointOrdering.GetBoundary(points, 0.5);

var hilbertComparer = new List<Func<Point, Point, int>>
{
    (p1, p2) => PointOrdering.HilbertComparer(p1, p2, boundary)
};

var balanced = new BalancedKdTree<Point>(points, hilbertComparer, Point.NaN, p => p);
// height = 4 — same 10 points, rotations kept it shallow

var nearest = balanced.FindNearestNeighbour(new Point(50, 50));
var inRange = balanced.FindNeighbours(new Point(50, 50), distance: 15);
```

Same 10 points, worst-case input — **height 4** instead of 9. And because every node caches its subtree's `MinimumBoundingBox`, the spatial queries still prune geometrically: `FindNearestNeighbour` and `FindNeighbours` skip any subtree whose box can't beat the current best, so Hilbert locality pays off twice — a balanced tree *and* tight boxes.

**Pass one comparer, not two.** The single Hilbert rank is what makes the rotations legitimate: with `comparers.Count == 1` every node compares on the same key, so `level % 1` is always `0` and the depth-to-axis mapping the first paragraph warns about never comes into play. Hand `BalancedKdTree<T>` the usual x/y pair instead and it still balances, and its queries still return correct answers — they prune on the cached boxes and never read the comparers — but the ordering invariant is gone, so the boxes drift wider and each query rules out less. See [One comparer or two](README.md#one-comparer-or-two).

## Why it works

| | Simple `KdTree<T>` | `BalancedKdTree<T>` + Hilbert |
|---|---|---|
| Key | x or y, alternating by level | 1-D Hilbert rank |
| Balancing | none — shape = insertion order | red-black rotations on every insert |
| Sorted input | degenerates to a list, `O(n)` | stays `O(log n)` |
| This example | height **9** | height **4** |

The Hilbert curve is one of several space-filling curves implemented in [`Analysis/SFC`](../../Analysis/SFC/README.md) as higher-order functions — `PointOrdering` also offers Peano, Moore, Gray and Z-order comparers, and any of them can play the same role. The construction and comparison of these curves (and this functional formulation) is the subject of the article below.

## Reference

> Narimani Rad, H., & Karimipour, F. (2021). *Representation and generation of space-filling curves: a higher-order functional approach.* Journal of Spatial Science, 66(3), 459–479. [doi:10.1080/14498596.2019.1668870](https://doi.org/10.1080/14498596.2019.1668870)

---

**NuGet**: [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

**Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)

[Back to K-d trees](README.md) · [Back to Advanced structures](../README.md)
