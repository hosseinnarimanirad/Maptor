# IRI.Maptor.Infrastructure.SqlServerSpatialExtension

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Infrastructure.SqlServerSpatialExtension?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.SqlServerSpatialExtension/)
[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Extension methods and helpers for working with SQL Server spatial types (`SqlGeometry`,
`SqlGeography`) alongside the Maptor geometry model — conversions to `Geometry<Point>`, GeoJSON,
GML, and helpers for building SQL Server spatial values from other formats. All extension methods
live in the `IRI.Maptor.Extensions` namespace.

## Installation

```bash
dotnet add package IRI.Maptor.Infrastructure.SqlServerSpatialExtension
```

## Features

- `SqlGeometryExtensions` — convert `SqlGeometry` to Maptor `Geometry<Point>` (`AsGeometry`), export GeoJSON (`AsGeoJson`), tile helpers (`TileInfo.AsSqlGeometry`), and WKT/projection utilities
- `SqlGeographyExtensions` — point extraction (`AsPoint`), reprojection to `SqlGeometry` (`Project`, `GeodeticWgs84ToWebMercator`, ...), GeoJSON and WKT export
- `GeometryExtensions` — geodesic area (`GetTrueArea`), `IPoint` to `SqlGeography`, OpenGIS geometry-type mapping
- `BoundingBoxExtensions` — Maptor `BoundingBox` to `SqlGeometry` envelope
- `FeatureExtensions` — `List<Feature<Point>>` to `DataTable`
- `GeoJsonExtensions` — GeoJSON geometry to `SqlGeometry` / `SqlGeography` (and back via `AsGeoJson`)
- `GmlExtensions` — `SqlGeometry` to GML3 (`AsGml3`) and GML3 parsing (`ParseGML3`)
- `GpxExtensions` — GPX waypoints, track points, segments, and tracks to `SqlGeography`
- `ShapefileExtension` — ESRI Shapefile shapes to `SqlGeometry`
- `SqlSpatialHelper` / `SqlSpatialUtility` — WKT and Esri JSON parsing, empty-geometry builders, bounding boxes from envelopes, union, and point-collection/linestring construction

## Usage

```csharp
using IRI.Maptor.Extensions;

// SqlGeometry -> Maptor geometry
Geometry<Point> geometry = sqlGeometry.AsGeometry();

// SqlGeometry -> GeoJSON
var geoJson = sqlGeometry.AsGeoJson();

// Maptor bounding box -> SqlGeometry envelope
SqlGeometry envelope = boundingBox.AsSqlGeometry(srid: 3857);
```

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.SqlServerSpatialExtension/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Infrastructure](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Infrastructure/README.md)
