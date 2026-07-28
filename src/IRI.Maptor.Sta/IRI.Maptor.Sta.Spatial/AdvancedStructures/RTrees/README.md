# R-trees

Where a k-d tree indexes points by cutting space, an R-tree indexes **extents** by grouping them: nearby rectangles get wrapped in a parent box, parents get wrapped in grandparents, and a query that misses a box can drop everything inside it.

<p align="center">
  <img src="../../images/kdtree-vs-rtree.png" alt="KdTree splitting planes vs RTree nested boxes" width="800">
</p>

That difference matters because most map data is not points. Roads, parcels and rivers have area, and a k-d tree has nowhere to put them — an R-tree stores each feature's bounding box, so lines and polygons index as naturally as points do.

## Rectangle — the key type

Everything in this folder is keyed by `Rectangle`, a plain struct of `minX, minY, maxX, maxY`:

```csharp
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

var parcel = new Rectangle(minX: 51.30, minY: 35.68, maxX: 51.36, maxY: 35.72);

double area      = parcel.GetArea();
double perimeter = parcel.GetPerimeter;
var    center    = (parcel.CenterX, parcel.CenterY);

// union of two boxes — how a parent node grows to hold a child
var merged = parcel + new Rectangle(51.34, 35.70, 51.40, 35.75);

// the cost function behind every insertion: how much would this box have to grow?
double growth = parcel.GetEnlargementArea(newFeature);
```

## RTree — least-enlargement insertion

`RTree` is the classic B-tree-shaped index. A new key descends into whichever child would have to grow the least to contain it, and nodes split when they overflow `2 × minimumDegree - 1` keys.

```csharp
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

var boxes = new[]
{
    new Rectangle(51.30, 35.68, 51.36, 35.72),
    new Rectangle(51.34, 35.70, 51.40, 35.75),
    new Rectangle(51.44, 35.60, 51.49, 35.64),
    new Rectangle(51.46, 35.62, 51.52, 35.67),
};

var tree = new RTree(boxes, minimumDegree: 2);   // must be >= 2

tree.Insert(new Rectangle(51.31, 35.69, 51.33, 35.71));

var root = tree.Root;                 // RTreeNode: Boundary, IsLeaf, NumberOfKeys
```

You can supply your own descent rule instead of least-enlargement — the comparer returns the index of the child to descend into:

```csharp
// the built-in rule, passed explicitly
var tree = new RTree(new RTreeNode(), RTree.FindTheBestRectangle, minimumDegree: 4);
```

## SFCRTree — bulk-loading along a space-filling curve

`SFCRTree` builds the same nested-box structure, but instead of choosing by area growth it keeps keys in **space-filling-curve order**. Sorting 2-D boxes by their position along a Hilbert curve gives a 1-D sequence in which map neighbours stay sequence neighbours, so leaves fill with features that are genuinely near each other — and on disk, a small map query touches a few consecutive pages instead of scattered ones.

<p align="center">
  <img src="../../images/space-filling-curves.png" alt="Insertion order vs Hilbert order" width="800">
</p>

```csharp
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

// the comparer is required — there is no parameterless Hilbert overload
var tree = new SFCRTree(boxes, SFCRTree.HilbertComparer, minimumDegree: 4);

var extent = tree.Boundary;    // grows as keys arrive; the curve is fitted to it
```

Each comparer ranks a rectangle by its centre point along a different curve. Hilbert has the best locality and is the sensible default; the rest are there because the curves themselves are comparable:

| Comparer | Curve |
|---|---|
| `SFCRTree.HilbertComparer` | Hilbert — best locality preservation |
| `SFCRTree.ZOrderingComparer` | Z-order (Morton) — cheapest to compute |
| `SFCRTree.GrayComparer` | Gray-code ordering |
| `SFCRTree.NOrderingComparer` | N-ordering |
| `SFCRTree.PeanoComparer`, `Peano02Comparer`, `Peano03Comparer` | Peano, Wunderlich, Peano-meander |
| `SFCRTree.DiagonalLebesgueComparer`, `UOrderOrLebesgueSquareComparer` | Lebesgue variants |

The curves come from [`Analysis/SFC`](../../Analysis/SFC/README.md), where they are generated from higher-order functions rather than hard-coded.

## Status

Both R-tree types are early-stage: insertion paths are marked untested in source, and **neither exposes a range or nearest-neighbour query yet** — they build the structure, and you traverse from `Root` yourself. For point queries today, use [`BalancedKdTree`](../KdTrees/README.md), which has both queries implemented and tested.

## Files

`Rectangle.cs` — the key struct. `RTree.cs`, `RTreeNode.cs` — the tree and its nodes. `SFCRTree.cs` — the space-filling-curve variant. All in namespace `IRI.Maptor.Sta.Spatial.AdvancedStructures`.

## Reference

The curve orderings `SFCRTree` packs its leaves along, and how they are generated and compared:

> Narimani Rad, H., & Karimipour, F. (2021). *Representation and generation of space-filling curves: a higher-order functional approach.* Journal of Spatial Science, 66(3), 459–479. [doi:10.1080/14498596.2019.1668870](https://doi.org/10.1080/14498596.2019.1668870)

---

**NuGet**: [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

**Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)

[Back to Advanced structures](../README.md)
