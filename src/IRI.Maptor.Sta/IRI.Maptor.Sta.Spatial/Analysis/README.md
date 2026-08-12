# Spatial analysis

The algorithm toolbox of `IRI.Maptor.Sta.Spatial`: triangulation and Voronoi diagrams, convex hulls, line simplification, topology predicates, space-filling curves, and network building — all on plain geometry types, no UI dependencies.

## Delaunay triangulation & Voronoi diagrams

`DelaunayTriangulation` is a Bowyer–Watson incremental insertion. Triangles come back as CCW vertex indices with per-edge neighbour links (`-1` on the convex hull), and the dual Voronoi diagram comes for free: every triangle's circumcenter is a Voronoi vertex, and hull cells stay open as infinite rays.

<p align="center">
  <img src="../images/voronoi-delaunay-duality.png" alt="Delaunay–Voronoi duality" width="800">
</p>

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;

var triangulation = DelaunayTriangulation.Create(points);      // IReadOnlyList<Point>, ≥ 3 points

var triangles = triangulation.Triangles;                       // CCW indices + neighbours
int hit       = triangulation.FindContainingTriangle(p);       // walk; -1 outside the hull

var voronoi = triangulation.GetVoronoiDiagram();               // or VoronoiDiagram.Create(points)
// voronoi.Cells (IsClosed = false on the hull), voronoi.Edges (rays have VertexB == -1)
```

## Convex hull

The convex hull is the smallest convex polygon that contains every point of a set — stretch a rubber band around the points and let it snap tight: it hooks onto the outermost points and skips everything inside. Where a bounding box spends two corners enclosing plenty of dead space, the hull is the tightest convex fence the data admits — the natural answer to "what area does this dataset actually cover?".

`ComputationalGeometry.CreateConvexHull` is a Graham scan — sort by polar angle around the lowest point, keep only the left turns, O(n log n). It returns the hull vertices counter-clockwise; duplicates and collinear edge points are dropped.

<p align="center">
  <img src="../images/convex-hull.png" alt="Convex hull vs bounding box" width="800">
</p>

```csharp
// a five-pointed star: five outer tips plus five inner notch points
var points = new List<Point>
{
    new Point(0, 10),     new Point(-9.5, 3.1),  new Point(-5.9, -8.1),  // outer tips …
    new Point(5.9, -8.1), new Point(9.5, 3.1),
    new Point(-2.4, 3.2), new Point(-3.8, -1.2), new Point(0, -4),       // … inner notches
    new Point(3.8, -1.2), new Point(2.4, 3.2),
};

List<Point> hull = ComputationalGeometry.CreateConvexHull(points);
// → the five tips, counter-clockwise — a pentagon; the rubber band bridges
//   every notch straight across, and the inner star vertices vanish

var hullPolygon = geometry.GetConvexHull();   // same thing on any Geometry<T>
```

## Simplification

`Simplifications` collects ~20 line-simplification algorithms behind one shape: `SimplifyByXxx<T>(List<T> points, SimplificationParameters parameters)` where `T : IPoint`. Thresholds ride in `SimplificationParameters` (`DistanceThreshold`, `AreaThreshold`, `AngleThreshold` — the *square cosine* of the angle — `N`, `LookAhead`, `Retain3Points`).

| Family | Methods |
|---|---|
| Global tolerance | `SimplifyByRamerDouglasPeucker` |
| Area-based | `SimplifyByVisvalingamWhyatt` (extra `bool isRing`), `SimplifyByAdditiveAreaPlus`, triangle-routine family |
| Corridor / sleeve | `SimplifyByReumannWitkam`, `SimplifyByPerpendicularDistance`, `SimplifyBySleeveFitting`, opening-window pair |
| Local geometry | `SimplifyByAngle`, `SimplifyByEuclideanDistance` and their cumulative variants, `SimplifyByLang` |
| Sampling | `SimplifyByNthPoint`, `SimplifyByRandomPointSelection` |
| Segment collapse | `SimplifyByAPSC` (Kronenfeld et al. 2020 — minimizes areal displacement) |

```csharp
var parameters = new SimplificationParameters { DistanceThreshold = 0.001 };

var simplified = Simplifications.SimplifyByRamerDouglasPeucker(line.Points, parameters);
```

`SimplificationMetrics` scores the result with McMaster's measures: `PercentageChangeInLineLength`, `PercentageChangeInCoordinates`, `PercentageChangeInPointDensity`.

## Topology

The `Topology/` enums (`PointPolygonRelation`, `LineLineRelation`, `PointCircleRelation`, …) name the answers; the predicates live in `TopologyUtility` (namespace `IRI.Maptor.Sta.Spatial.Helpers`). Point-in-polygon ships in both classic flavours:

<p align="center">
  <img src="../images/point-in-polygon.png" alt="Point in polygon: ray casting vs winding number" width="800">
</p>

```csharp
using IRI.Maptor.Sta.Spatial.Helpers;

bool inside  = TopologyUtility.IsPointInRing(ring, point);                  // even-odd ray casting
bool winding = TopologyUtility.IsPointInRingUsingSignedAngles(ring, point); // winding number
bool inPoly  = TopologyUtility.IsPointInPolygon(polygonOrMultiPolygon, point);
```

Also here: segment–segment intersection (`LineSegmentsIntersects`), point–segment distance, the point/circumcircle test (`GetPointCircleRelation`), and left/right-of-vector classification (`GetPointVectorRelation`, with an optional `tolerance`).

## Point-in-polygon across spatial reference systems

Everything above assumes both operands live in the same SRS. When they don't — a point layer in one system, a polygon layer in another — the *direction* you reproject decides whether the answers are true:

> Run the containment test in the **polygon layer's SRS**: reproject the **point layer** into the polygon's SRS, never the polygon into the point's. A point reprojects exactly; a polygon does not.

<p align="center">
  <img src="../images/point-in-polygon-srs.png" alt="Point in polygon across SRS: reprojecting the polygon bends its edges; reprojecting the points is exact" width="800">
</p>

### The tempting optimization

Given points in SRS A and polygons in SRS B, the cheap way to reconcile them is to transform the **polygons** into A and test there: a handful of features with a few hundred vertices, against a point layer that may hold millions of rows. The cost argument is real. The result is wrong.

### Why moving the polygon breaks the answer

A polygon is stored as **vertices plus implied straight edges**. Reprojection transforms only the vertices; each edge is then redrawn as a straight chord between the transformed endpoints in the target SRS.

But a straight line in one SRS is not a straight line in another. Any non-linear transform — geographic to projected, or between two projections — maps a straight segment to a **curve**. The redrawn chord therefore deviates from the true image of the boundary:

- the deviation is **zero at the vertices** and largest at the **middle of each edge**;
- it grows with **edge length** — long, sparse edges are the worst case — and with the **curvature of the transform** in that area.

Every point sitting in the sliver between the chord and the true curve is assigned to the **wrong polygon**, which is precisely the near-border population a count is most sensitive to.

A point, by contrast, is dimensionless: it has no edges to redraw. Reprojection maps one coordinate pair to one coordinate pair, exactly (up to negligible numeric error). Move the points and nothing is distorted — the test then runs against the polygon **where its edges are authoritative**, the SRS its geometry was authored in.

### What the figure shows

Three regions with long shared borders and 350 points, under a simulated non-linear transform:

- **Left — reproject the polygon.** Vertices land true, but the solid chord edges drift off the dashed true boundary, and **25 points** (the × marks) fall in the wrong region.
- **Right — reproject the points.** The same data in the polygon's SRS: edges straight and authoritative, every point mapped exactly, every count true.

Note the treacherous detail in the left card's labels. Although 25 individual points are misassigned, the per-region totals barely move — **83 vs 85, 168 vs 169, 99 vs 96** — because flips across each border go in *both* directions and largely cancel. Aggregate counts can look entirely plausible while the individual answers are wrong, so a "the totals look about right" spot-check will not catch this bug.

### Doing it right

Transform the point layer with the projections in [`IRI.Maptor.Sta.SpatialReferenceSystem`](../../IRI.Maptor.Sta.SpatialReferenceSystem/MapProjections/README.md), then count with the point-in-ring predicates above — in the polygon's SRS:

```csharp
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

// polygons authored in UTM zone 39N; points arriving as WGS84 lon/lat
var utm = UTM.CreateForZone(39);
var ringBoundingBox = ring.GetBoundingBox();      // reused across every test

int count = geodeticPoints
    .Select(p => utm.FromGeodetic(p))                                     // move the POINTS …
    .Count(p => TopologyUtility.IsPointInRing(ring, p, ringBoundingBox));  // … then test there
```

### It is a rule about dimension

Nothing here is specific to polygons, or to which layer you consider primary. Any predicate that mixes points with shaped geometry follows the same logic: reproject the dimensionless operand into the SRS of the one whose shape carries meaning.

Asking **which transmission towers sit on a power line** is the same problem one dimension down. The line is vertices plus implied straight spans; reprojecting it redraws every span as a chord that bows away from the true path, and towers genuinely on the line fall off it — worst at mid-span, exactly where the long crossings are. Move the **towers** into the **power line's** SRS and test there.

### Nuances and edge cases

- **If you truly must move the polygon** — you need it in the other SRS anyway — **densify** its edges first: insert intermediate vertices along each segment, then reproject. Shorter segments mean smaller chords, which bounds the error; it is still an approximation. Moving the points is exact.
- **The one exception.** If the two SRS are related by a purely **affine** transform, straight lines stay straight and either direction works. In practice any projection change is non-linear, so treat the rule as universal.
- **Authoritativeness.** The result is correct *with respect to the SRS the polygon was authored in*. If a boundary is legally defined by geodesics on the ellipsoid rather than by its stored projected geometry, the authoritative form is the geodesic one — the rule generalizes to "test containment where the boundary definition is authoritative".
- **Performance.** Transforming millions of points does cost more than transforming a few polygons; that is the whole temptation. The cost is linear and parallelizes cleanly, and correctness is worth it.

## Space-filling curves (SFC)

Hilbert, Z-order (Morton) and other curve orderings that linearize 2-D data while keeping neighbours close — the backbone of `SFCRTree` bulk-loading and locality-preserving sorts. See the dedicated [SFC README](SFC/README.md).

<p align="center">
  <img src="../images/space-filling-curves.png" alt="Space-filling curves" width="800">
</p>

## Network

`LineNetworkBuilder<T>` turns a pile of line features into edge–node topology: coincident endpoints snap together within a tolerance, shared vertices become junctions, and the result bridges straight into the Sta.Graph algorithms.

```csharp
using IRI.Maptor.Sta.Spatial.Analysis.Network;

var network = new LineNetworkBuilder<Point>(LineNetworkBuilder<Point>.GetDefaultTolerance(srid))
                    .Build(lineFeatures);

var components = network.GetConnectedComponents();  // disconnected islands
var graph      = network.ToAdjacencyList();         // → Sta.Graph: Dijkstra, BFS, …
```

## Statistics & shape characteristics

`AreaStatistics` summarizes the area distribution of a set of `Geometry<Point>` polygons (mean, standard deviation, histogram, CSV export). The `CharacteristicsMeasure` enum names McMaster's displacement measures (PCC, PDD, PCLL, …) computed by the simplification metrics above.

## Going further

- **[Digital Terrain Modeling](DigitalTerrainModeling/README.md)** — grid DEMs, TINs, slope/aspect, volume
- **[Interpolation](Interpolation/README.md)** — inverse distance weighting (IDW)

---

**NuGet**: [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

**Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)

[Back to IRI.Maptor.Sta.Spatial](../README.md)
