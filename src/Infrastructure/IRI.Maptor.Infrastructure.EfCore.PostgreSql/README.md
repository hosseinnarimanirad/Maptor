# IRI.Maptor.Infrastructure.EfCore.PostgreSql

[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

PostgreSQL/PostGIS support for IRI.Maptor `Geometry<Point>`, for both EF Core and plain Npgsql. It is the PostGIS
counterpart of [IRI.Maptor.Infrastructure.EfCore.SqlServer](../IRI.Maptor.Infrastructure.EfCore.SqlServer/README.md)
and replaces Npgsql's `UseNetTopologySuite()` without depending on NetTopologySuite. Values move between .NET and
PostGIS as EWKB, which `IRI.Maptor.Core.Spatial` reads and writes.

The provider-neutral parts (`MaptorGeometryValueComparer`, `ConfigureMaptorGeometry()`, `IsGeography()` /
`IsGeometry()`) come from [IRI.Maptor.Infrastructure.EfCore](../IRI.Maptor.Infrastructure.EfCore/README.md).

## Installation

```bash
dotnet add package IRI.Maptor.Infrastructure.EfCore.PostgreSql
```

## Features

- **EF Core** — `UseMaptor()` on the Npgsql provider's options builder maps `Geometry<Point>` properties to
  `geometry`, `geography` or modified types such as `geometry(Polygon,4326)`. It also adds the `postgis` extension to
  any model with a spatial property, so migrations and `EnsureCreated` run `CREATE EXTENSION IF NOT EXISTS postgis`.
- **ADO.NET** — `UseMaptor()` on an `NpgsqlDataSourceBuilder` makes Npgsql itself understand `Geometry<Point>`:
  parameters, readers (`GetFieldValue`, `GetValue`), arrays and binary `COPY`.
- `PostGisGeometrySerializer` — converts between `Geometry<Point>` and EWKB, for code that handles the bytes itself.

## Usage — EF Core

```csharp
using IRI.Maptor.Infrastructure.EfCore.Extensions;             // ConfigureMaptorGeometry / IsGeometry
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;  // UseMaptor

options.UseNpgsql(connectionString, x => x.UseMaptor());

protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    => configurationBuilder.ConfigureMaptorGeometry();          // Geometry<Point> -> geography

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Parcel>().Property(e => e.Footprint).HasColumnType("geometry(Polygon,4326)");
    modelBuilder.Entity<Parcel>().Property(e => e.LocalShape).IsGeometry();
}
```

If no column type is configured, `Geometry<Point>` defaults to `geography`, as in the SQL Server plugin. The same
`DbContext` configuration therefore works with either provider.

**Migrations** work as usual (`dotnet ef migrations add`, `database update`, `script`, `remove`). The first migration
enables `postgis`, spatial columns keep their type modifiers, and `HasData` seed geometries are written as
`Geometry<Point>.FromWkt("...", srid)` in the migration code. They compare by value, so an unchanged seed never shows up
as a pending model change.

With a connection string, the provider builds its own data source and the plugin is applied to it automatically.
If you hand the context a ready-made `NpgsqlDataSource`, call `UseMaptor()` on that data source's builder as well.

Server-side spatial work goes through raw SQL, e.g.
`db.Parcels.FromSql($"SELECT * FROM parcels WHERE ST_Intersects(\"Shape\", {area}::geography)")`. Interpolated
`Geometry<Point>` values are sent as `geometry`, hence the cast. LINQ spatial method translation is not provided.

## Usage — ADO.NET

```csharp
using Npgsql;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;

var builder = new NpgsqlDataSourceBuilder(connectionString);
builder.UseMaptor();
await using var dataSource = builder.Build();

await using var command = dataSource.CreateCommand(
    "SELECT geom FROM parcels WHERE ST_Intersects(geom, @area)");
command.Parameters.AddWithValue("area", area);          // Geometry<Point>, sent as geometry

await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var geometry = reader.GetFieldValue<Geometry<Point>>(0);
}
```

An untyped `Geometry<Point>` parameter is sent as `geometry`. PostGIS casts it implicitly when the target is a
`geography` column. To send `geography` explicitly, set `DataTypeName = "geography"` on the parameter; EF Core does
this itself, based on the column's type.

## Behaviour notes

- **SRID** — the SRID travels inside the EWKB, so `Geometry<Point>.Srid` is preserved in both directions.
  SRID 0 means "unknown" for `geometry`; for `geography`, PostGIS treats SRID 0 as 4326.
- **Z/M** — `Geometry<Point>` is 2D. Z and M values read from the database are **dropped**, not rejected.
  (The SQL Server plugin rejects them instead.)
- **Experimental Npgsql API** — the plugin is built on Npgsql's type-resolver API (`Npgsql.Internal`), which
  Npgsql marks experimental (`NPG9001`), as its own NetTopologySuite plugin is. Re-test when upgrading Npgsql.

---
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Infrastructure](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Infrastructure/README.md)
