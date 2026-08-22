# Geodesy samples

Two small tables that every map developer ends up needing: how much ground a decimal place of a
geographic coordinate covers, and how many metres a Web Mercator tile pixel covers at each zoom
level.

## Coordinate precision

`geodesy/precision` — [CoordinatePrecision.cs](CoordinatePrecision.cs)

The number of decimal places you keep in a latitude/longitude decides storage size, display
length and — most importantly — how precisely you can place something. The sample moves a point
east by 1°, 0.1°, 0.01° … and measures the great-circle distance, at the equator, at 45°
(mid-latitudes: Milan, Bordeaux, Minneapolis) and at 60°.

```csharp
var basePoint = new Point(0, latitude);
var shifted = new Point(0.00001, latitude);            // 5 decimal places

var meters = SpatialUtility.GetSphericalLength(basePoint, shifted);
```

Output at latitude 45°:

| decimal places | degrees | E/W distance |
|---|---|---|
| 0 | 1 | 78.6 km |
| 2 | 0.01 | 786 m |
| 4 | 0.0001 | 7.86 m |
| 5 | 0.00001 | 78.6 cm |
| 6 | 0.000001 | 7.86 cm |
| 8 | 0.00000001 | 0.8 mm |

Rules of thumb: 4 decimals (~10 m) for general mapping, 5 (~1 m) for urban applications,
6 (~10 cm) for navigation, 7 or more for surveying. East–west distances shrink with the cosine of
the latitude; north–south distances do not.

Further reading: [How precise should lat/long storage be?](https://gis.stackexchange.com/a/208739)

## Web Mercator ground resolution and scale

`geodesy/web-mercator-resolution` — [WebMercatorResolution.cs](WebMercatorResolution.cs)

Tile maps (Google, OSM, Bing, ...) use 256 px tiles in Web Mercator; at zoom level *z* the world
is 2^z tiles wide. `WebMercatorUtility` gives the metres-per-pixel and the map scale for a zoom
level and a latitude — the numbers behind `Scalebar` and the "nearest zoom level" shown in the
WPF samples.

```csharp
var metersPerPixel = WebMercatorUtility.CalculateGroundResolution(zoom, latitude);
var scale = WebMercatorUtility.CalculateMapScale(zoom, latitude);   // e.g. 1 / 36_978 at zoom 12, equator
```

A few rows of the output:

| zoom | resolution @0° | resolution @45° | tiles |
|---|---|---|---|
| 1 | 78.3 km | 55.3 km | 4 |
| 10 | 152.9 m | 108.1 m | 1,048,576 |
| 15 | 4.8 m | 3.4 m | 1,073,741,824 |
| 20 | 14.9 cm | 10.6 cm | 1,099,511,627,776 |

## How to run

```bash
dotnet run --project samples/IRI.Maptor.Samples.Core -- geodesy/precision
dotnet run --project samples/IRI.Maptor.Samples.Core -- geodesy/web-mercator-resolution
```

---
[Back to IRI.Maptor.Samples.Core](../README.md)
