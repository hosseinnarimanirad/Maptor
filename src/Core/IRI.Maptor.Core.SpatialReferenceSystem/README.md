# IRI.Maptor.Core.SpatialReferenceSystem

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.SpatialReferenceSystem?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Core.SpatialReferenceSystem/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Spatial reference systems, geodetic transformations, and map projections for the Maptor stack. Implements the horizontal coordinate systems of geodesy — terrestrial (conventional/instantaneous terrestrial, geodetic, local geodetic, local astronomic), celestial (apparent place, right ascension, horizontal angle), and orbital — together with the transformations between them and the common map projections.

## Installation

```bash
dotnet add package IRI.Maptor.Core.SpatialReferenceSystem
```

## Features

- Map projections: Mercator, Web Mercator, Transverse Mercator, UTM, cylindrical equal-area, Lambert conformal conic (1- and 2-parallel), and Albers equal-area conic (function form in `MapProjects`)
- MGRS (`MgrsConverter`): reads and writes Military Grid Reference System references such as `39S WV 53516 39501` at any of the six precisions, including the irregular Norway and Svalbard zones. Not a projection — a text encoding of UTM
- Reference ellipsoids: predefined models (WGS84, GRS80, Clarke 1866/1880, Bessel 1841, International 1924, …) plus custom ellipsoids via semi-major/semi-minor axis parameters
- Coordinate system transformations (`Transformations`): geodetic to geocentric Cartesian and back, datum change between ellipsoids (`ChangeDatum`), average/instantaneous terrestrial, local astronomic and local geodetic, horizontal angle, apparent place, and orbital conversions
- Typed geodetic points (`GeodeticPoint<TLinear, TAngular>`) built on the unit types of `IRI.Maptor.Core.Common`
- Ready-made SRS definitions (`SrsBases`: `GeodeticWgs84`, `WebMercator`, UTM zones, Lambert variants) and SRID helpers (`SridHelper`: 4326, 3857, 102100)

## Usage

Convert a geodetic position to geocentric Cartesian coordinates:

```csharp
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

var wgs84 = Ellipsoids.WGS84;

var geodetic = new Point(51.123456, 35.123456);   // longitude, latitude in degrees

var cartesian = Transformations.ToCartesian(geodetic, wgs84);
```

Project geodetic coordinates to Mercator:

```csharp
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

var projected = MapProjects.GeodeticToMercator(new Point(51.4, 35.7));
```

Work with typed geodetic points:

```csharp
using IRI.Maptor.Core.Common.Metrics;
using IRI.Maptor.Core.SpatialReferenceSystem;

var point = new GeodeticPoint<Meter, Degree>(
    Ellipsoids.WGS84,
    new Meter(0),
    new Degree(51.123456),
    new Degree(35.123456));

var cartesian = point.ToCartesian<Meter>();
```

## See also

- [Coordinate systems](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.SpatialReferenceSystem/CoordinateSystems/README.md) — the geocentric/topocentric systems and how `Transformations` converts between them
- [Map projections](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.SpatialReferenceSystem/MapProjections/README.md) — UTM, Web Mercator, the other implemented projections, and MGRS
- [Models](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.SpatialReferenceSystem/Models/README.md) — reference ellipsoids and horizontal datums

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Core.SpatialReferenceSystem/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Core](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/README.md)
