# IRI.Maptor.Infrastructure.EfCore.SqlServer

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Infrastructure.EfCore.SqlServer?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.EfCore.SqlServer/)
[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

EF Core support for reading SQL Server spatial columns directly into IRI.Maptor `Geometry<Point>`. It is a
drop-in replacement for `UseNetTopologySuite()` and removes the NetTopologySuite and Microsoft.SqlServer.Types
dependencies from applications. Geometry is read and written in the SQL Server native binary (MS-SSCLRT) format by
`IRI.Maptor.Core.Spatial`'s `SqlServerSpatialNativeBinary` serializer, so no NTS objects or WKB conversion are
involved.

The provider-neutral parts (`MaptorGeometryValueComparer`, `ConfigureMaptorGeometry()`, `IsGeography()` /
`IsGeometry()`) live in [IRI.Maptor.Infrastructure.EfCore](../IRI.Maptor.Infrastructure.EfCore/README.md), which
this package references. The PostgreSQL/PostGIS counterpart is
[IRI.Maptor.Infrastructure.EfCore.PostgreSql](../IRI.Maptor.Infrastructure.EfCore.PostgreSql/README.md).

## Installation

```bash
dotnet add package IRI.Maptor.Infrastructure.EfCore.SqlServer
```

## Features

- A provider plugin for the SQL Server EF Core provider: `UseMaptor()` replaces `UseNetTopologySuite()`
- Reads and writes `geometry` and `geography` columns (points, lines, polygons, multi-* and geometry collections; 2D)
- `SqlServerMaptorGeometryTypeMapping` — the `RelationalTypeMapping` for `Geometry<Point>`
- `SqlServerSpatialPassThroughTypeMapping` — a pass-through mapping for spatial columns in models compiled
  from migration snapshots and designer files (columns typed `SqlBytes`, or the legacy NetTopologySuite `Geometry`), so
  `dotnet ef migrations add`/`remove` keep working after switching from `UseNetTopologySuite()`
- `SqlServerMaptorGeometryTypeMappingSourcePlugin` / `SqlServerMaptorGeometryOptionsExtension` — provider wiring

## Usage

Enable the provider plugin where you configure the context (replaces `x.UseNetTopologySuite()`):

```csharp
using IRI.Maptor.Infrastructure.EfCore.SqlServer.Extensions;

options.UseSqlServer(connectionString, x => x.UseMaptor());
```

Declare geometry properties on your entities as `Geometry<Point>`:

```csharp
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;

public Geometry<Point> SHAPE { get; set; }
```

To choose the column type, either set a default for all geometry properties in `ConfigureConventions`:

```csharp
using IRI.Maptor.Infrastructure.EfCore.Extensions;

protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    => configurationBuilder.ConfigureMaptorGeometry();          // maps Geometry<Point> -> geography
```

or set it per property in `OnModelCreating`:

```csharp
entity.Property(e => e.SHAPE).IsGeography();   // or .IsGeometry()
```

If no column type is configured, `Geometry<Point>` defaults to `geography`.

## Limitations

- Z/M values are rejected with a clear error, so store 2D data. (The PostgreSQL plugin drops them instead.)
- Server-side spatial operations go through raw SQL (`FromSql` / `FromSqlRaw`, e.g. `SHAPE.STIntersects(...)`).
  LINQ spatial method translation is intentionally not provided.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.EfCore.SqlServer/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Infrastructure](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Infrastructure/README.md)
