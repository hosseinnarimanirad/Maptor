# IRI.Maptor.Ket.SqlServerSpatialExtension

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerSpatialExtension.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerSpatialExtension)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8** library of extension methods and helpers for working with **SQL Server spatial types** (`SqlGeometry`, `SqlGeography`) alongside the Maptor geometry model.

---

## Features

### Extension Methods
- `SqlGeometryExtensions` — convert `SqlGeometry` ↔ `Geometry<Point>`, extract coordinates, test topology
- `SqlGeographyExtensions` — convert `SqlGeography` ↔ `Geometry<Point>`
- `GeometryExtensions` — convert Maptor `Geometry<Point>` → `SqlGeometry` / `SqlGeography`
- `BoundingBoxExtensions` — convert Maptor `BoundingBox` ↔ SQL Server envelope
- `FeatureExtensions` — convert Maptor `Feature` objects to/from SQL Server spatial rows
- `GeoJsonExtensions` — convert GeoJSON ↔ `SqlGeometry`
- `GmlExtensions` — convert GML ↔ `SqlGeometry`
- `GpxExtensions` — convert GPX tracks ↔ `SqlGeometry`
- `ShapefileExtension` — convert ESRI Shapefile geometries ↔ `SqlGeometry`

### Utilities
- `SqlSpatialHelper` / `SqlSpatialUtility` — general helpers (well-known binary, bounding boxes, spatial reference IDs)

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.SqlServerSpatialExtension
```

---

📦 **NuGet**: [IRI.Maptor.Ket.SqlServerSpatialExtension](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerSpatialExtension)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
