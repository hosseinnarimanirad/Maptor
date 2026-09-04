# Map grids

Builds a cartographic grid over a map view: the lines, and the values written against the edges.
Three kinds — a geographic graticule, a UTM grid, and a grid of any projection — each of which
picks how fine to draw itself from how much ground is on screen.

Everything produced is a **polyline**. That is the difference from `MgrsGridHelper`, which emits
polygons because an MGRS square is a named region; here a grid line is a line.

## Features

- **Geodetic** — meridians and parallels, labelled in degrees/minutes/seconds
- **UTM** — constant easting and northing, walked per zone strip and hemisphere, cut at the zone
  boundary, with the seam meridian drawn where the grid restarts
- **Projected** — constant x and y in any `SrsBase`: Web Mercator, Mercator, transverse Mercator,
  Lambert conformal conic, cylindrical equal-area
- Two weights: principal lines at the chosen interval, and round subdivisions between them
- Values written against all four edges: metric grids write the whole number of metres on every
  line, a graticule spells out the first value on each edge and abbreviates the rest
- Intervals chosen from a ladder so they are always round numbers, and never coarsen as the view
  narrows

## Installation

```bash
dotnet add package IRI.Maptor.Core.Spatial
```

## Usage

```csharp
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Core.SpatialReferenceSystem;

// A view around Tehran, in Web Mercator.
var extent = new BoundingBox(49.4, 33.7, 53.4, 37.7)
    .Transform(MapProjects.GeodeticWgs84ToWebMercator);

var grid = MapGridHelper.Create(extent, MapGridDefinition.Geodetic());

// grid.MajorInterval  -> 1        (degrees, chosen from the ladder)
// grid.MinorInterval  -> 0.25     (15 minutes)
// grid.Lines          -> polylines in Web Mercator, each Major or Minor
// grid.Labels         -> "51°E" on the first meridian met along an edge, then "30′" …
//                        (a UTM or projected grid instead writes "534000" on every line)
```

A UTM grid over the same view splits at every zone boundary:

```csharp
var utm = MapGridHelper.Create(extent, MapGridDefinition.Utm());

// utm.Lines carry a Zone, and no vertex of a zone-38 line crosses longitude 48.
// utm.ZoneSeams holds the meridians where the grid restarts.
```

Any projection can carry a grid:

```csharp
var lambert = MapGridDefinition.Projected(SrsBases.LccNiocWithClarke1880Rgs, "lccNioc");

var grid = MapGridHelper.Create(extent, lambert);
```

Pin the interval, choose which edges carry values, or push a second grid's numbers further in so two
grids can be drawn together:

```csharp
var definition = MapGridDefinition.Utm();

definition.MajorInterval = 10_000;                                  // metres; null follows the zoom
definition.LabelSides = MapGridSide.Bottom | MapGridSide.Left;      // default is all four
definition.LabelTier = 1;                                           // the second grid on the map
```

## How it works

**Intervals** come from a ladder — degrees run `30° 20° 10° 5° 2° 1° 30′ 20′ 15′ … 1″`, metres run
1-2-5 through every decade from 1 000 km to 10 m. `MapGridLadders.ChooseMajor` takes the coarsest
step that still puts at least three lines across the view, stepping back one when that overshoots
six. `MinorOf` subdivides it into the finest ladder step that divides it evenly into no more than
five parts: 1 km → 200 m, 2 km → 500 m, 1° → 15′, 10′ → 2′.

**Labels** are placed where a line crosses an anchor just inside the edge of the view, interpolated
along the line so they stay in a straight row even where the lines themselves bow.

The two families read differently, deliberately. A **metric** value is the whole number of metres,
written out on every line — `534000` — with no unit, because position already says which axis it
belongs to: eastings along the bottom and top, northings up the sides. A **geodetic** value follows
the convention a printed sheet uses: `51°10′E` on the first line met along each edge and again when
the degree rolls over, then `20′ 30′ 40′`. That works only because the short form keeps its own unit
mark. The metric grid was built the same way first — `⁵34⁰⁰⁰ mE` then `35 36 37`, with the digits a
sheet prints small as Unicode superscripts — and it was rejected on sight: two bare digits with no
unit and no anchor is a puzzle, not an abbreviation.

**Lines are sampled and re-projected.** A line of constant easting is straight in its own plane and
a curve on the map, so each is generated at 32 samples and every vertex converted. Meridians and
parallels are straight in Web Mercator and use two.

**UTM is the projected walk run per zone.** Each strip in view is walked in its own plane and its
lines are *cut* at the boundary — not clamped, which would draw a spurious segment along the
meridian. North and south of the equator are walked apart, being different origins. The nominal 6°
strips are used, not the widened Norway and Svalbard cells: those change which zone a position is
reported in, which is an MGRS concern.

**Accuracy.** A sampled vertex sits on its line to within a few micrometres — double-precision noise.
Only a cut point carries error, because the crossing is interpolated along a sampled chord: at worst
about 2 m on a 4° view, which is a twentieth of a pixel at that zoom, and it falls away
quadratically as the samples shorten.

**Crowded margins thin out.** A value that would print within about a label's width and height of one
already written is dropped — the cases that forces are a UTM zone seam and the corners, both
accidents of where the view sits rather than of the interval. Only numbers are dropped; every line is
still drawn. Two orderings matter: the collision test runs before the spelled-out/abbreviated state
is updated, so whichever value survives first on an edge carries the full reference; and zone seams
are placed before grid lines, so a crowded margin gives up a grid value rather than the caption
naming the two zones.

## Limits

- Labels are produced for principal lines only; `MapGridOptions.LabelMinorLines` turns the rest on.
- Views are clipped to ±180° and ±85.05°, beyond which Web Mercator is undefined.
- A definition built with `MapGridDefinition.Projected` rejects a geographic system — its
  coordinates are degrees, and every label would call them metres. Use `Geodetic()`.
- An unusable extent yields an empty grid rather than an exception: a grid layer is asked for one on
  every pan.

## See also

- [MGRS](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.SpatialReferenceSystem/MapProjections/README.md) — the military grid, which is drawn as named squares rather than lines
- [Spatial reference systems](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.SpatialReferenceSystem/README.md)

---
[Back to IRI.Maptor.Core.Spatial](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/README.md)
