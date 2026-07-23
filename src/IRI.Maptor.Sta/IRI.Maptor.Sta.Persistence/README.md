# IRI.Maptor.Sta.Persistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Persistence?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Sta.Persistence/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Persistence abstractions for the Maptor GIS stack. Defines the data-source interfaces and base types that concrete adapters (SQL Server, PostgreSQL, SQLite, personal GDB, web API) implement in the `IRI.Maptor.Ket.*` packages, and ships ready-to-use in-memory and file-backed data sources.

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Persistence
```

## Features

- Core abstractions: `IDataSource`, `IVectorDataSource` (async feature queries by bounding box, geometry, or map scale), `IEditableVectorDataSource`, `IRasterDataSource`
- Base implementations: `BaseDataSource`, `VectorDataSource`, `RasterDataSource`
- In-memory vector sources built on `MemoryDataSource`, with file-backed variants for GeoJSON, shapefile, GPX, KML/KMZ, DXF, TopoJSON, ESRI JSON, and plain text/JSON lists
- Raster sources: image pyramids (plain and zipped) and online/offline Google-style tile sources
- Scale-dependent data sources that switch content by map scale (`MemoryScaleDependentDataSource`)
- `ConnectedFeatureSet` model and the `DataSourceType` enum of supported backends

## Usage

Load a GeoJSON file as an editable in-memory data source:

```csharp
using IRI.Maptor.Sta.Persistence.DataSources;

var source = await GeoJsonDataSource.CreateFromFileAsync(
    "parcels.geojson", isLongitudeFirst: true, sourceSrid: 4326);

var featureSet = await source.GetAsFeatureSetAsync();
```

Query any vector source through the shared abstraction:

```csharp
using IRI.Maptor.Sta.Persistence.Abstractions;

async Task PrintCountAsync(IVectorDataSource source, BoundingBox extent)
{
    var features = await source.GetAsFeatureSetAsync(extent);
    Console.WriteLine(features.Features.Count);
}
```

## Dependencies

- `IRI.Maptor.Sta.Common`, `IRI.Maptor.Sta.Spatial`, `IRI.Maptor.Sta.SpatialReferenceSystem`
- `IRI.Maptor.Sta.Ogc`, `IRI.Maptor.Sta.ShapefileFormat` (file-backed data sources)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Sta.Persistence/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Sta](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/README.md)
