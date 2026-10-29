# Simplification algorithms checked against their sources (2026-10-06)

Code changes of the 2026-10-06 session, uncommitted at the time of writing (the author commits). Every algorithm of
`src\Core\IRI.Maptor.Core.Spatial\Analysis\Simplification\Simplifications.cs` was compared with its primary source (PDFs in
`E:\University.Ph.D\Articles\`) or, where the original is not available, with the best secondary source and with
reference implementations (psimpl, rdp, simplify-js, JTS, GEOS, urschrei/simplification, visvalingamwyatt).

## Distance definition

`SpatialUtility.GetPointToLineSegmentDistance` is renamed **`SpatialUtility.GetPointToLineDistance(linePoint1, linePoint2,
targetPoint)`** (same argument order). It always measured the perpendicular distance to the infinite line through the two
points, which is the distance of Ramer (1972, p. 250: Δy = d/cos θ) and Douglas and Peucker (1973, pp. 116-117: "the
straight line defined by the anchor and the floater"; Douglas's own Fortran does the same). Ebisch (2002) argues that
the segment distance was meant; that is his reading, not the original definition. The distance to the segment is
`TopologyUtility.GetPointToLineSegmentDistance(point, segmentStart, segmentEnd)` (point first).

## Verdict per algorithm

| Algorithm | Source | Verdict |
|---|---|---|
| RDP | Ramer 1972, DP 1973 | Faithful (line distance, split while d > tol at the farthest point). **Fixed (both variants):** a closed line (first = last) was always split at index Count/2; now at the point farthest from the start, as DP 1973 p. 117 prescribes. **Rings:** Maptor stores polygon rings without the closing vertex (shapefile and WKT readers drop it); with the new `SimplificationParameters.IsRing` RDP closes the ring, divides it at the vertex farthest from the first one and drops the closing vertex again. `Geometry.Simplify` sets `IsRing` for polygons on its own copy of the parameters (author's decision), so **RDP results on every polygon layer change** (LRS paper E7-E12, JSS, ISPRS). Direct callers of `Simplifications` set it themselves; in the research tool `Baseline.Apply` (segment baselines `RDPs`/`BOPWs` through `FilterPoints`) and the E6 timing baselines (`ResubmissionExperiments.TimedMethods`, per leaf from `feature.IsRingBase()`) now do. E1 `RdpLabelExperiment.Label` simplifies rings with `IsRing` and labels them with `LRSimplificationTrainingData.Create(..., isRingMode: true, ...)` (every vertex but the first labelled, test `MachineLearning\RingTrainingDataTest.cs`), so the **E1 RDP labels on polygon layers change**. |
| RDP segment variant | Ebisch 2002 | Faithful to Ebisch; reproduces the simplify-js fixture after a radial pass. |
| Reumann-Witkam | psimpl, Shi & Cheung 2006 | Faithful (RW 1974 not available). psimpl treats d >= tol as outside, the code d > tol (ties only). |
| NOPW / BOPW | Meratnia & de By 2004, p. 769 and SPT pseudo-code pp. 772-773 | Faithful: NOPW cuts at the first violating point, BOPW at the point before the float. No reference implementation exists. |
| Sleeve fitting | Zhao & Saalfeld 1997 | Not sleeve fitting; it is BOPW (caveat 13, unchanged). |
| Visvalingam-Whyatt | VW 1993, Visvalingam 2015 | **Fixed:** rings. Open lines match Visvalingam's published output at every threshold. The optional "never below the last eliminated area" rule is not implemented (only changes the order below the threshold). |
| Lang | Lang 1969 not available; psimpl, Shi & Cheung 2006 | Line distance kept (author's decision); psimpl measures to the segment. **Fixed:** shared parameters were mutated. |
| Perpendicular distance | Jenks 1981, psimpl | psimpl traversal (at most 50 % removed); line distance where psimpl uses the segment. |
| Modified perpendicular distance | none (repository variant) | **Fixed:** measured the end point instead of the middle point. |
| n-th point | Tobler 1966 via McMaster 1987 | Faithful. **Fixed:** N = 0 threw DivideByZeroException; now ArgumentOutOfRangeException. |
| Random point | Tobler 1966 via Shi & Cheung 2006, p. 28 | **Fixed:** kept every point with p = 0.5; now one random point per set of N consecutive points. |
| Radial (EuclideanDistance) | Tobler 1989, psimpl | Faithful. |
| Angle, CumulativeAngle, CumulativeAngleArea | Tobler 1966 (via DP 1973, p. 113) | **Fixed:** cos² lost the sign, so a hairpin scored as straight and was dropped. |
| CumulativeEuclideanDistance | none | **Fixed:** summed squared distances to the last kept point against a linear threshold; now the path length since the last kept point. |
| Ring flag | | `SimplificationParameters.IsRing` (+ `Clone()`); VW reads it instead of its former `bool isRing` argument (`SimplifyByVisvalingamWhyatt(points, parameters)`); `Geometry.Simplify` works on a clone, so callers' parameters are never changed. |
| Helpers | | `SignedSquareCosineOfTurn` and `AnyPerpendicularDistanceExceedTolerance` moved from `Simplifications` to `SpatialUtility` (public). |
| Triangle routine, MTR, CTR, APSC, Li-Openshaw, AdditiveAreaPlus | | Not changed (CTR verified 2026-09-26). AdditiveAreaPlus ignores Retain3Points and throws on closed lines (`IsClockwise`); no source, not fixed. |

## The Visvalingam-Whyatt ring bug

A ring is passed without its closing vertex: vertices v0 ... v(n-1), with v(n-1) joined back to v0. v0 is the fixed start;
every other vertex, v(n-1) included, gets an effective area and may be eliminated. After the elimination loop the method
ran the step shared by the open-line routines: "if the last output point is not the last input point, append the last
input point". For an open line this never fires (its last point has no effective area and is never eliminated), but for a
ring it fires whenever v(n-1) was eliminated, and puts that eliminated vertex back at the end of the output.

On Visvalingam's own test data (Koch island, 32 ring vertices, v31 = (1, 0) with effective area 0.5): at threshold 2 the
published result is the initiator square (0,0) (0,4) (4,4) (4,0); the code returned (0,0) (0,4) (4,4) (4,0) **(1,0)**, i.e.
a fifth vertex on the bottom edge. At threshold 8 it returned (0,0) (1,0) instead of (0,0). The rendered polygon gains a
spurious vertex on its closing edge (usually invisible when the vertex lies close to the closing edge, visible when it
does not). Every polygon run through VW is affected, because `Geometry.Simplify` passes `IsRingBase()`; this includes the
published JSS (ten algorithms) and ISPRS (RDP, VW, NOPW, CTR) runs, whose VW numbers on polygon layers no longer reproduce
exactly from the current code (author's decision: fix it). `Retain3Points` now stops the elimination at three points (its
old test `Count == 1` could never be true for an open line; JSS and ISPRS ran with `Retain3Points = false`).

## Tests (`tests\IRI.Maptor.Tests\Spatial`)

Only published data (author's decision): `PublishedSimplificationData.cs` holds the simplify-js fixture (100 points and its
33-point expected output, BSD-2), Ebisch (2002) Fig. 3 (read from the plot), and the Visvalingam (2015) Koch island with
its effective areas; JTS `TestSimplify.xml` line cases and the `rdp` package's test cases are inlined.

- `VisvalingamWhyattTest.cs` (20): open line and ring against the published effective areas at nine thresholds.
- `DistanceSimplificationTest.cs`: RDP (JTS, rdp package, Ebisch, recursive definition; segment variant reproduces
  simplify-js), NOPW/BOPW (error bound + cut rule of Meratnia & de By), RW, Lang, PD, radial (ports of psimpl with the line
  distance), n-th point, random point, Lang parameter mutation.
- `AngleAndCumulativeSimplificationTest.cs`: hairpin of Ebisch Fig. 3, turn-angle rule, path length, MPD middle point.

Mutation check: with the old code restored the new tests fail in 24 places, covering every fix above; with the fixes
all 103 pass. Closed-line RDP: 6 more tests (Koch island as a closed line), failing on the old split; ring tests (direct and through `Geometry.Simplify` on a polygon) for RDP and VW. The 58 failures of the full test project (GeoJSON serializer options, Esri, MVT, DXF) are unrelated.
