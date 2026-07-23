# IRI.Maptor.Sta.SpatialReferenceSystem

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.SpatialReferenceSystem?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Sta.SpatialReferenceSystem/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Spatial reference systems, geodetic transformations, and map projections for the Maptor stack. Implements the horizontal coordinate systems of geodesy — terrestrial (conventional/instantaneous terrestrial, geodetic, local geodetic, local astronomic), celestial (apparent place, right ascension, horizontal angle), and orbital — together with the transformations between them and the common map projections.

## Installation

```bash
dotnet add package IRI.Maptor.Sta.SpatialReferenceSystem
```

## Features

- Map projections: Mercator, Web Mercator, Transverse Mercator, UTM, cylindrical equal-area, Lambert conformal conic (1- and 2-parallel), and Albers equal-area conic (function form in `MapProjects`)
- Reference ellipsoids: predefined models (WGS84, GRS80, Clarke 1866/1880, Bessel 1841, International 1924, …) plus custom ellipsoids via semi-major/semi-minor axis parameters
- Coordinate system transformations (`Transformations`): geodetic to geocentric Cartesian and back, datum change between ellipsoids (`ChangeDatum`), average/instantaneous terrestrial, local astronomic and local geodetic, horizontal angle, apparent place, and orbital conversions
- Typed geodetic points (`GeodeticPoint<TLinear, TAngular>`) built on the unit types of `IRI.Maptor.Sta.Common`
- Ready-made SRS definitions (`SrsBases`: `GeodeticWgs84`, `WebMercator`, UTM zones, Lambert variants) and SRID helpers (`SridHelper`: 4326, 3857, 102100)

## Usage

Convert a geodetic position to geocentric Cartesian coordinates:

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

var wgs84 = Ellipsoids.WGS84;

var geodetic = new Point(51.123456, 35.123456);   // longitude, latitude in degrees

var cartesian = Transformations.ToCartesian(geodetic, wgs84);
```

Project geodetic coordinates to Mercator:

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

var projected = MapProjects.GeodeticToMercator(new Point(51.4, 35.7));
```

Work with typed geodetic points:

```csharp
using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.SpatialReferenceSystem;

var point = new GeodeticPoint<Meter, Degree>(
    Ellipsoids.WGS84,
    new Meter(0),
    new Degree(51.123456),
    new Degree(35.123456));

var cartesian = point.ToCartesian<Meter>();
```

## See also

- [Coordinate systems](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/IRI.Maptor.Sta.SpatialReferenceSystem/CoordinateSystems/README.md) — the geocentric/topocentric systems and how `Transformations` converts between them
- [Map projections](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/IRI.Maptor.Sta.SpatialReferenceSystem/MapProjections/README.md) — UTM, Web Mercator, and the other implemented projections
- [Models](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/IRI.Maptor.Sta.SpatialReferenceSystem/Models/README.md) — reference ellipsoids and horizontal datums

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Sta.SpatialReferenceSystem/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Sta](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/README.md)
