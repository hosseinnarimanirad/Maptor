# Advanced structures

Spatial indexes and point clustering: a spatial query shouldn't touch every feature — these structures carve space so lookups skip almost everything.

<p align="center">
  <img src="../images/kdtree-vs-rtree.png" alt="KdTree vs RTree" width="800">
</p>

| Folder | Structures | Index what | Status |
|---|---|---|---|
| [**KdTrees**](KdTrees/README.md) | `KdTree<T>`, `BalancedKdTree<T>` | points | production — both queries verified against a brute-force scan |
| [**RTrees**](RTrees/README.md) | `RTree`, `SFCRTree` | bounding boxes — lines, polygons, anything with extent | early-stage — builds the index, no queries yet |
| **GeoStatistics** | `PointClusters<T>`, `KdTreePointClusters<T>` | groups of points | production |

## [K-d trees →](KdTrees/README.md)

Split space in half at every node, alternating axes. `BalancedKdTree<T>` is the one to reach for: red-black balancing keeps it shallow under any insertion order, every node caches its subtree's bounding box, and it answers both classic queries.

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

var comparers = new List<Func<Point, Point, int>>
{
    (p1, p2) => p1.X.CompareTo(p2.X),   // even levels split on x
    (p1, p2) => p1.Y.CompareTo(p2.Y),   // odd levels split on y
};

var tree = new BalancedKdTree<Point>(points, comparers, Point.NaN, p => p);

var nearest              = tree.FindNearestNeighbour(new Point(51.4, 35.7));
var withinToleranceRange = tree.FindNeighbours(new Point(51.4, 35.7), distance: 0.05);
```

Insertion order is the catch — sorted input degenerates a plain k-d tree into a linked list. Ranking points along a Hilbert curve fixes it: [Hilbert-balanced k-d tree](KdTrees/HilbertBalancedKdTree.md).

## [R-trees →](RTrees/README.md)

Group nearby `Rectangle`s into nested bounding boxes, B-tree style, so a query can discard whole subtrees at once — and, unlike a k-d tree, index features that have extent rather than just points.

```csharp
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

var tree = new RTree(boxes, minimumDegree: 2);           // least-enlargement descent

// or pack leaves in space-filling-curve order, so map neighbours stay disk neighbours
var packed = new SFCRTree(boxes, SFCRTree.HilbertComparer, minimumDegree: 4);
```

## GeoStatistics — point clustering

`PointClusters<T>` greedily groups points by any membership rule you supply; `KdTreePointClusters<T>` runs the same idea on top of a `BalancedKdTree` so each membership test only probes nearby candidates. The one-shot helper covers the common case — cluster by radius and keep one representative per group:

```csharp
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

// thin out a dense point set: one center per 0.01°-radius cluster
var centers = KdTreePointClusters<Point>.GetClusterCenters(points, Point.NaN, radius: 0.01);
```

> `GetClusterCenters` builds on `BalancedKdTree.FindNeighbours`, so every point within `radius` of a chosen center is absorbed into that center's cluster.

## Reference

Space-filling curves underpin both the balanced k-d tree's ordering and `SFCRTree`'s leaf packing:

> Narimani Rad, H., & Karimipour, F. (2021). *Representation and generation of space-filling curves: a higher-order functional approach.* Journal of Spatial Science, 66(3), 459–479. [doi:10.1080/14498596.2019.1668870](https://doi.org/10.1080/14498596.2019.1668870)

---

**NuGet**: [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

**Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)

[Back to IRI.Maptor.Sta.Spatial](../README.md)
