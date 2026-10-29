# IRI.Maptor.Infrastructure.EfCore

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Infrastructure.EfCore?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.EfCore/)
[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Provider-neutral EF Core support for IRI.Maptor `Geometry<Point>` properties. This package holds what the database
provider plugins share. To use it, reference the plugin for your database, which brings this package in:

| Database | Package | Plugin call |
|---|---|---|
| SQL Server | [IRI.Maptor.Infrastructure.EfCore.SqlServer](../IRI.Maptor.Infrastructure.EfCore.SqlServer/README.md) | `UseSqlServer(cs, x => x.UseMaptor())` |
| PostgreSQL / PostGIS | [IRI.Maptor.Infrastructure.EfCore.PostgreSql](../IRI.Maptor.Infrastructure.EfCore.PostgreSql/README.md) | `UseNpgsql(cs, x => x.UseMaptor())` |

## Installation

```bash
dotnet add package IRI.Maptor.Infrastructure.EfCore
```

## Features

- `ConfigureMaptorGeometry()` — a convention that maps every `Geometry<Point>` property to a `geography` (default) or
  `geometry` column
- `IsGeography()` / `IsGeometry()` — per-property column-type configuration
- `MaptorGeometryValueComparer` — a reusable change-tracking comparer (SRID + WKB)
- `Querying.FeatureQueryExecutor` — runs a `FeatureQueryDto` (Core.Spatial: bounding box, filters, sort,
  page or extent mode, count) on any entity set, on SQL Server or PostgreSQL. The spatial condition is raw SQL
  per provider (`STIntersects` / `ST_Intersects`, every mapped column selected by name so PostgreSQL's `xmin`
  comes along); filters, sort and paging are composed LINQ. Field names must be scalar properties of the
  entity, values become SQL parameters, and an invalid query throws `FeatureQueryException` (Core.Spatial).
  Limits (`FeatureQueryLimits`: page size 1,000, extent cap 100,000, In values 1,000) bind from `LayerQuery`.
  Used by the Gas and Barg `LayerQuery` endpoints for layers in Live mode.
- `Importing.ImportModelBuilder` (2026-10-23) — an app's import model (`Core.Spatial/Importing/ImportModelDto`: tables,
  columns, types, required, defaults, domains, column roles) from its EF model and entity attributes, for the
  `LayerSetting/Admin/ImportModel` endpoint of Gas and Saba and the import tool. See
  `docs/future-improvements/layer-data-import.md`.
- `Querying.FeatureStatisticsExecutor` (2026-10-23) — whole-layer statistics computed by the database for the
  Statistics tab of a Live layer: one attribute (counts, values with their counts up to `MaxDistinctValues`, numeric
  min/max/sum/mean/exact median/standard deviation with histogram, date range), features grouped by an attribute
  (count, total of a numeric field, geodesic length/area per group via `STLength`/`STArea` or `ST_Length`/`ST_Area`
  on geography) and the geometries (empty, unmeasured, per-feature measure summary). Raw SQL per provider with
  identifiers from the EF model only; DTOs in Core.Spatial (`LayerStatisticsDto.cs`); histogram bins from
  `Core.Spatial.Analysis.HistogramLayout`, shared with the client. Tested against LocalDB and PostGIS
  (`tests/.../Querying/FeatureStatisticsExecutorTest.cs`) by comparing with the client's in-memory calculators.

SQL Server and PostGIS both call their spatial types `geometry` and `geography`, so a `DbContext` configured with
these helpers works unchanged with either provider plugin.

## Usage

```csharp
using IRI.Maptor.Infrastructure.EfCore.Extensions;

protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    => configurationBuilder.ConfigureMaptorGeometry();          // maps Geometry<Point> -> geography

protected override void OnModelCreating(ModelBuilder modelBuilder)
    => modelBuilder.Entity<Parcel>().Property(e => e.LocalShape).IsGeometry();
```

## Upgrading from 3.0

Before the split, the SQL Server plugin (`UseMaptor()` and its type mappings) shipped in this package. It has moved to
`IRI.Maptor.Infrastructure.EfCore.SqlServer`. Reference that package and change
`using IRI.Maptor.Infrastructure.EfCore.Extensions;` to `using IRI.Maptor.Infrastructure.EfCore.SqlServer.Extensions;`
wherever `UseMaptor()` is called.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.EfCore/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Infrastructure](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Infrastructure/README.md)
