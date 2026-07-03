# IRI.Maptor.Ket.EfCorePersistence

Shared EF Core persistence support for materializing SQL Server spatial columns directly into
IRI.Maptor `Geometry<Point>` — a drop-in replacement for `UseNetTopologySuite()` that removes the
NetTopologySuite and Microsoft.SqlServer.Types dependencies from applications.

Geometry is read/written using the SQL Server native binary (MS-SSCLRT) format via
`IRI.Maptor.Sta.Spatial`'s `SqlServerSpatialNativeBinary` serializer, so no NTS object graph or WKB
round-trip is involved.

## Usage

**1. Enable the provider plugin** where you configure the context (replaces `x.UseNetTopologySuite()`):

```csharp
options.UseSqlServer(connectionString, x => x.UseMaptorGeometry());
```

**2. Declare geometry properties** on your entities using `Geometry<Point>`:

```csharp
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

public Geometry<Point> SHAPE { get; set; }
```

**3. Choose the column type.** Either set a default for all geometry properties in
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

## Scope

- Reads/writes `geometry` and `geography` columns (points, lines, polygons, multi-* and geometry
  collections; 2D). Z/M columns are rejected with a clear error — store 2D data.
- Server-side spatial operators are expected via raw SQL (`FromSql` / `FromSqlRaw`, e.g.
  `SHAPE.STIntersects(...)`); LINQ spatial method translation is intentionally **not** provided.

## Public building blocks

- `SqlServerMaptorGeometryTypeMapping` — the `RelationalTypeMapping` for `Geometry<Point>`.
- `SqlServerMaptorGeometryTypeMappingSourcePlugin` / `SqlServerMaptorGeometryOptionsExtension` — provider wiring.
- `MaptorGeometryValueComparer` — reusable change-tracking comparer (SRID + WKB).
