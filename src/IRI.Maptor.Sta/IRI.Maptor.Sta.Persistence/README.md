# IRI.Maptor.Sta.Persistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Persistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Persistence)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

A .NET Standard 2.1 library of **persistence abstractions** for the Maptor GIS stack. Defines the interfaces and base types that concrete persistence adapters (SQL Server, PostGIS, SQLite, etc.) implement in the `IRI.Maptor.Ket.*` packages.

---

## Key Abstractions

| Type | Description |
|---|---|
| `IDataSource` | Root data-source interface |
| `IVectorDataSource` | Read-only vector (feature) data source |
| `IEditableVectorDataSource` | Vector data source with write support |
| `IRasterDataSource` | Raster/image data source |
| `VectorDataSource` | Base implementation for vector sources |
| `RasterDataSource` | Base implementation for raster sources |
| `BaseDataSource` | Common base for all data sources |
| `ConnectedFeatureSet` | Feature set tied to a live data source |
| `DataSourceType` | Enum of supported backend types |

### Specialised base types
- **`MemorySources/`** — in-memory vector/raster data sources for testing and in-process data
- **`RasterDataSources/`** — base types for tile-based raster sources
- **`ScaleDependentDataSources/`** — data sources that vary content by map scale

---

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Persistence
```

---

## Project Structure

```
Sta.Persistence/
├── Abstractions/
│   ├── IDataSource.cs
│   ├── IVectorDataSource.cs
│   ├── IEditableVectorDataSource.cs
│   └── IRasterDataSource.cs
├── DataSources/
│   ├── BaseDataSource.cs
│   ├── VectorDataSource.cs
│   ├── RasterDataSource.cs
│   ├── MemorySources/
│   ├── RasterDataSources/
│   └── ScaleDependentDataSources/
├── Model/
├── Infrastructure/
├── DataSourceType.cs
└── ConnectedFeatureSet.cs
```

---

📦 **NuGet**: [IRI.Maptor.Sta.Persistence](https://www.nuget.org/packages/IRI.Maptor.Sta.Persistence)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
