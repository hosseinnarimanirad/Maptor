# IRI.Maptor.Core

The **Sta** tier is the heart of Maptor: twelve UI-free core spatial class libraries targeting **.NET Standard 2.1** (usable from .NET Core 3.0+/.NET 5+ on any platform). They cover spatial data modeling, geometry algorithms, coordinate reference systems, and file-format I/O, and every project is published to [NuGet](https://www.nuget.org/packages?q=IRI.Maptor).

These libraries have no dependency on WPF or any UI framework — the UI layer ([IRI.Maptor.Presentation](../Presentation/README.md)) and the persistence adapters ([IRI.Maptor.Infrastructure](../Infrastructure/README.md)) build on top of them.

## Projects

| Project | NuGet | Target | Description |
|---------|-------|--------|-------------|
| [IRI.Maptor.Core.Common](IRI.Maptor.Core.Common/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Common.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Common) | netstandard2.1 | Foundational primitives and abstractions used by the whole suite: core interfaces and the `Point` type, linear algebra and statistics helpers, data structures (trees, heaps, disjoint sets), unit conversions, and service models for external map APIs (Google, Bing, Here). |
| [IRI.Maptor.Core.Spatial](IRI.Maptor.Core.Spatial/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Spatial.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Spatial) | netstandard2.1 | The core spatial engine: OGC geometry model with typed generics, geometry operations, spatial analysis (Delaunay triangulation, Voronoi diagrams, convex hull, TIN/DTM, IDW interpolation, simplification), spatial indexes (KdTree, RTree), and file-format I/O (see below). |
| [IRI.Maptor.Core.SpatialReferenceSystem](IRI.Maptor.Core.SpatialReferenceSystem/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.SpatialReferenceSystem.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.SpatialReferenceSystem) | netstandard2.1 | Spatial reference systems and map projections: predefined ellipsoids, datum and geodetic transformations, UTM, Mercator/Web Mercator, Lambert and other projections, plus terrestrial, celestial, and orbital coordinate systems. |
| [IRI.Maptor.Core.ShapefileFormat](IRI.Maptor.Core.ShapefileFormat/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.ShapefileFormat.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.ShapefileFormat) | netstandard2.1 | ESRI Shapefile reading, writing, and conversion: SHP/SHX/DBF/PRJ/CPG files, all point, polyline, polygon and multipoint shape types (including M and Z variants), international character encodings, and coordinate transformation on the fly. |
| [IRI.Maptor.Core.Ogc](IRI.Maptor.Core.Ogc/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Ogc.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Ogc) | netstandard2.1 | OGC standards support: Simple Features WKT/WKB for all geometry types, GML 2/3, KML/KMZ 2.2, WMS and WFS service models, SLD styling, and Filter Encoding. |
| [IRI.Maptor.Core.Graph](IRI.Maptor.Core.Graph/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Graph.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Graph) | netstandard2.1 | Graph data structures (directed/undirected/weighted, adjacency list or matrix) and classic algorithms: BFS, DFS, Dijkstra, Bellman-Ford, Floyd-Warshall, minimum spanning tree, and strongly connected components. |
| [IRI.Maptor.Core.Pdf](IRI.Maptor.Core.Pdf/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Pdf.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Pdf) | netstandard2.1 | Vector PDF export of spatial data: single geometries/features, or a decorated print-ready map layout with title, scale bar, graticule, legend and logos, and toggleable PDF layers. |
| [IRI.Maptor.Core.MachineLearning](IRI.Maptor.Core.MachineLearning/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.MachineLearning.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.MachineLearning) | netstandard2.1 | Machine learning and statistics for spatial data: DBSCAN clustering, Apriori association-rule mining, logistic regression, and descriptive statistics. |
| [IRI.Maptor.Core.GeoParquet](IRI.Maptor.Core.GeoParquet/) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.GeoParquet.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.GeoParquet) | netstandard2.1 | Reader and writer for the GeoParquet columnar geospatial format. |
| [IRI.Maptor.Core.Persistence](IRI.Maptor.Core.Persistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Persistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Persistence) | netstandard2.1 | Persistence abstractions — data-source interfaces for vector and raster data plus in-memory sources — implemented by the [IRI.Maptor.Infrastructure](../Infrastructure/README.md) adapters. |
| [IRI.Maptor.Core.Security](IRI.Maptor.Core.Security/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Security.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Security) | netstandard2.1 | Cryptography primitives: AES/Rijndael and RSA encryption, signing and verification, hashing (MD5, SHA family), and signed JWT creation/validation. |
| [IRI.Maptor.Core.GsmGprs](IRI.Maptor.Core.GsmGprs/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.GsmGprs.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.GsmGprs) | netstandard2.1 | GSM/GPRS communication: SMS PDU encoding/decoding (SMS-SUBMIT and SMS-DELIVER) and serial-port GSM modem interaction. |

## Supported formats

Format support is spread across `IRI.Maptor.Core.Spatial` (its `IO/` folder) and the sibling projects above:

- **Vector**: GeoJSON, Esri JSON, Shapefile, KML/KMZ, GPX, WKT/WKB, TopoJSON (with quantization), DXF, SVG, EPS
- **Tiles**: PMTiles, Mapbox vector tiles
- **Raster / grid**: GeoTIFF metadata, world files, GRD
- **Terrain**: Cesium quantized-mesh and heightmap terrain (**read only**)
- **Columnar**: GeoParquet
- **Database-native**: SQL Server native binary (MS-SSCLRT)
- **Metadata**: PRJ / projection files

Each format folder under `IRI.Maptor.Core.Spatial/IO/` has its own README with details.

---

[Back to the solution README](../../README.md)
