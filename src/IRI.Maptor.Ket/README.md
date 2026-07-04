# IRI.Maptor.Ket — Infrastructure & Persistence Adapters

The **Ket** tier connects the core [Sta libraries](../IRI.Maptor.Sta/README.md) to real data stores and platform services. Most projects implement the data-source abstractions from `IRI.Maptor.Sta.Persistence` (such as `IVectorDataSource`), so a map layer can be backed by SQL Server, PostgreSQL, SQLite, a personal geodatabase, or a web API without changing application code. Every project is published to [NuGet](https://www.nuget.org/packages?q=IRI.Maptor).

Two target-framework groups:

- **Cross-platform (`net8.0`)**: EfCorePersistence, PostgreSqlPersistence, SqlitePersistence, SqlServerSpatialExtension, WebApiPersistence
- **Windows-only (`net8.0-windows`)**: GdiPlus, PersonalGdbPersistence, SqlServerPersistence, WindowsBase

## Projects

| Project | NuGet | Summary |
|---------|-------|---------|
| [IRI.Maptor.Ket.SqlServerPersistence](IRI.Maptor.Ket.SqlServerPersistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence) | SQL Server Spatial adapter for `geometry`/`geography` columns: reads the native binary format directly into Maptor geometries, with scale-dependent and bounding-box queries and write support. |
| [IRI.Maptor.Ket.PostgreSqlPersistence](IRI.Maptor.Ket.PostgreSqlPersistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PostgreSqlPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence) | PostgreSQL/PostGIS adapter: reads PostGIS WKB into Maptor geometries, supports bounding-box and scale-dependent queries plus insert/update/delete. |
| [IRI.Maptor.Ket.SqlitePersistence](IRI.Maptor.Ket.SqlitePersistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlitePersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlitePersistence) | SQLite-based geospatial storage: MBTiles (raster and vector tile archives) and OGC GeoPackage (vector features and tiles). Cross-platform. |
| [IRI.Maptor.Ket.EfCorePersistence](IRI.Maptor.Ket.EfCorePersistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.EfCorePersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.EfCorePersistence) | Entity Framework Core plugin that materializes SQL Server spatial columns directly into Maptor geometries — a drop-in alternative to `UseNetTopologySuite()` with no NetTopologySuite or Microsoft.SqlServer.Types dependency. |
| [IRI.Maptor.Ket.PersonalGdbPersistence](IRI.Maptor.Ket.PersonalGdbPersistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PersonalGdbPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence) | Reads ESRI Personal Geodatabases (`.mdb`) via OleDb/ACE, handling ArcGIS 10.x/11.x schema differences and ACE provider selection automatically. |
| [IRI.Maptor.Ket.WebApiPersistence](IRI.Maptor.Ket.WebApiPersistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.WebApiPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.WebApiPersistence) | Loads features from an HTTP web API (JSON/GeoJSON) as a vector data source, with bounding-box filtering via query parameters — remote feature services behave like any other layer. |
| [IRI.Maptor.Ket.SqlServerSpatialExtension](IRI.Maptor.Ket.SqlServerSpatialExtension/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerSpatialExtension.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerSpatialExtension) | Conversion helpers between SQL Server `SqlGeometry`/`SqlGeography` and the Maptor model, plus GeoJSON/GML/GPX/Shapefile interchange and bounding-box/WKB utilities. |
| [IRI.Maptor.Ket.GdiPlus](IRI.Maptor.Ket.GdiPlus/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.GdiPlus.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.GdiPlus) | GDI+/System.Drawing bridge: georeferenced raster I/O (GeoTIFF, world files), digital image processing (image matrices, spatial-domain enhancement, template matching), and remote-sensing helpers. |
| [IRI.Maptor.Ket.WindowsBase](IRI.Maptor.Ket.WindowsBase/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.WindowsBase.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.WindowsBase) | Windows-specific services for desktop applications: hardware detection and machine fingerprinting, geolocation, and Wi-Fi network scanning. |

---

[⬅ Back to the solution README](../../README.md)
