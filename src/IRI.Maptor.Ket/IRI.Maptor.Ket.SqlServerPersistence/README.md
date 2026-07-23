# IRI.Maptor.Ket.SqlServerPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerPersistence?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence/)
[![Target](https://img.shields.io/badge/net8.0--windows-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Persistence adapter for SQL Server Spatial — a Maptor vector data source for reading spatial
features stored in SQL Server `geometry` / `geography` columns, materialized through
`Microsoft.SqlServer.Types` (`SqlGeometry`) and converted to native `Geometry<Point>` objects.

## Installation

```bash
dotnet add package IRI.Maptor.Ket.SqlServerPersistence
```

## Features

- `SqlServerDataSource` — Maptor vector data source (`VectorDataSource`, `IEditableVectorDataSource`) over a SQL Server spatial table or a free-form select query (`CreateForQueryString`)
- Reads spatial rows as `SqlGeometry` and converts them to native `Geometry<Point>` objects
- Spatial filtering: bounding-box queries (`GetAsFeatureSetAsync(BoundingBox)`), WKT/WKB intersects filters (`GetGeometriesWhereIntersects`), and custom where clauses
- `FeatureSet` loading for map display, including a map-scale-aware overload
- `SqlServerInfrastructure` — connection helpers, feature selection to dictionaries, bulk `DataTable` insert (`Insert`, `InsertTable`), `ExecuteNonQuery`, and table delete
- `SqlServerSourceParameter` — strongly-typed source parameters (connection string, table, spatial/label columns, query string)

## Usage

```csharp
using IRI.Maptor.Ket.SqlServerPersistence;

var source = new SqlServerDataSource(
    connectionString, "Roads", spatialColumnName: "SHAPE", labelColumnName: "Name");

// features inside a bounding box
var featureSet = await source.GetAsFeatureSetAsync(boundingBox);

// geometries intersecting a WKT polygon
var geometries = source.GetGeometriesWhereIntersects(
    "POLYGON((51 35, 52 35, 52 36, 51 36, 51 35))");
```

## Limitations

- Windows only (depends on `Microsoft.SqlServer.Types`).
- Editing is partially implemented: `Add`/`Remove` delegate to caller-supplied
  `AddAction`/`RemoveAction` callbacks; `UpdateGeometry`, `UpdateAttributes`,
  `SaveChangesAsync`, and `SearchAsync` are not implemented yet.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Ket](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Ket/README.md)
