# IRI.Maptor.Ket.EfCorePersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.EfCorePersistence?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Ket.EfCorePersistence/)
[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

EF Core persistence support for materializing SQL Server spatial columns directly into
IRI.Maptor `Geometry<Point>` — a drop-in replacement for `UseNetTopologySuite()` that removes the
NetTopologySuite and Microsoft.SqlServer.Types dependencies from applications. Geometry is
read/written using the SQL Server native binary (MS-SSCLRT) format via `IRI.Maptor.Sta.Spatial`'s
`SqlServerSpatialNativeBinary` serializer, so no NTS object graph or WKB round-trip is involved.

## Installation

```bash
dotnet add package IRI.Maptor.Ket.EfCorePersistence
```

## Features

- Provider plugin for the SQL Server EF Core provider: `UseMaptorGeometry()` replaces `UseNetTopologySuite()`
- Reads/writes `geometry` and `geography` columns (points, lines, polygons, multi-* and geometry collections; 2D)
- `SqlServerMaptorGeometryTypeMapping` — the `RelationalTypeMapping` for `Geometry<Point>`
- `SqlServerSpatialPassThroughTypeMapping` — pass-through mapping for spatial columns in models compiled
  from migration snapshots/designers (`SqlBytes` provider-typed, or legacy NetTopologySuite `Geometry`),
  so `dotnet ef migrations add`/`remove` keep working after switching from `UseNetTopologySuite()`
- `SqlServerMaptorGeometryTypeMappingSourcePlugin` / `SqlServerMaptorGeometryOptionsExtension` — provider wiring
- `MaptorGeometryValueComparer` — reusable change-tracking comparer (SRID + WKB)
- `ConfigureMaptorGeometry()` convention and per-property `IsGeography()` / `IsGeometry()` configuration

## Usage

Enable the provider plugin where you configure the context (replaces `x.UseNetTopologySuite()`):

```csharp
options.UseSqlServer(connectionString, x => x.UseMaptorGeometry());
```

Declare geometry properties on your entities using `Geometry<Point>`:

```csharp
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

public Geometry<Point> SHAPE { get; set; }
```

Choose the column type — either set a default for all geometry properties in
`ConfigureConventions`:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    => configurationBuilder.ConfigureMaptorGeometry();          // maps Geometry<Point> -> geography
```

or per property in `OnModelCreating`:

```csharp
entity.Property(e => e.SHAPE).IsGeography();   // or .IsGeometry()
```

If no column type is configured, `Geometry<Point>` defaults to `geography`.

## Limitations

- Z/M columns are rejected with a clear error — store 2D data.
- Server-side spatial operators are expected via raw SQL (`FromSql` / `FromSqlRaw`, e.g.
  `SHAPE.STIntersects(...)`); LINQ spatial method translation is intentionally not provided.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Ket.EfCorePersistence/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Ket](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Ket/README.md)
