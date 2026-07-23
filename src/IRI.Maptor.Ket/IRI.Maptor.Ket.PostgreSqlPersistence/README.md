# IRI.Maptor.Ket.PostgreSqlPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PostgreSqlPersistence?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence/)
[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Persistence adapter for PostgreSQL / PostGIS — a Maptor vector data source for reading spatial
features stored in PostGIS geometry columns, built on Npgsql.

## Installation

```bash
dotnet add package IRI.Maptor.Ket.PostgreSqlPersistence
```

## Features

- `PostGisDataSource` — Maptor vector data source (`VectorDataSource`) over a PostGIS table
- Geometries are fetched as WKB (`ST_AsBinary`) and parsed into native `Geometry<Point>` objects
- Spatial filtering with `ST_Intersects` against a WKT polygon region (`GetGeometriesWhereIntersects`, `GetAttributeColumnsWhereIntersects`)
- Attribute-table reads and raw SQL execution returning `DataTable` (`GetAttributeColumns`, `ExecuteSql`)
- `PostgreSqlInfrastructure` — connection-string builder, column discovery, table existence check, bulk `DataTable` insert, table delete, and connection testing

## Usage

```csharp
using IRI.Maptor.Ket.PostgreSqlPersistence;

var source = new PostGisDataSource(
    server: "localhost", user: "postgres", password: "...",
    database: "gis", port: "5432",
    tableName: "roads", spatialColumnName: "geom");

// all geometries in the table
var geometries = source.GetGeometries();

// geometries intersecting a WKT polygon
var filtered = source.GetGeometriesWhereIntersects(
    "POLYGON((51 35, 52 35, 52 36, 51 36, 51 35))");
```

## Limitations

- Read-oriented: feature-level insert/update/delete through the data source is not implemented
  (bulk `DataTable` insert is available via `PostgreSqlInfrastructure.Insert`).
- The `FeatureSet` and search APIs of the data-source base class are not implemented for PostGIS yet.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Ket](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Ket/README.md)
