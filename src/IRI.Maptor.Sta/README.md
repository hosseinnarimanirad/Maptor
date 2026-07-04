# IRI.Maptor.Sta — Core Spatial Libraries

The **Sta** tier is the heart of Maptor: twelve UI-free class libraries targeting **.NET Standard 2.1** (usable from .NET Core 3.0+/.NET 5+ on any platform). They cover spatial data modeling, geometry algorithms, coordinate reference systems, and file-format I/O, and every project is published to [NuGet](https://www.nuget.org/packages?q=IRI.Maptor).

These libraries have no dependency on WPF or any UI framework — the UI layer ([IRI.Maptor.Jab](../IRI.Maptor.Jab/README.md)) and the persistence adapters ([IRI.Maptor.Ket](../IRI.Maptor.Ket/README.md)) build on top of them.

## Projects

| Project | NuGet | Summary |
|---------|-------|---------|
| [IRI.Maptor.Sta.Common](IRI.Maptor.Sta.Common/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Common.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Common) | Foundational primitives and abstractions used by the whole suite: core interfaces and the `Point` type, linear algebra and statistics helpers, data structures (trees, heaps, disjoint sets), unit conversions, and service models for external map APIs (Google, Bing, Here). |
| [IRI.Maptor.Sta.Spatial](IRI.Maptor.Sta.Spatial/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial) | The core spatial engine: OGC geometry model with typed generics, geometry operations, spatial analysis (Delaunay triangulation, Voronoi diagrams, convex hull, TIN/DTM, IDW interpolation, simplification), spatial indexes (KdTree, RTree), and file-format I/O (see below). |
| [IRI.Maptor.Sta.SpatialReferenceSystem](IRI.Maptor.Sta.SpatialReferenceSystem/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.SpatialReferenceSystem.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.SpatialReferenceSystem) | Spatial reference systems and map projections: 25+ predefined ellipsoids, datum and geodetic transformations, UTM, Mercator/Web Mercator, Lambert and other projections, plus terrestrial, celestial, and orbital coordinate systems. |
| [IRI.Maptor.Sta.ShapefileFormat](IRI.Maptor.Sta.ShapefileFormat/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.ShapefileFormat.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.ShapefileFormat) | ESRI Shapefile reading, writing, and conversion: SHP/SHX/DBF/PRJ/CPG files, all point, polyline, polygon and multipoint shape types (including M and Z variants), international character encodings, and coordinate transformation on the fly. |
| [IRI.Maptor.Sta.Ogc](IRI.Maptor.Sta.Ogc/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Ogc.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Ogc) | OGC standards support: Simple Features WKT/WKB for all geometry types, GML 2/3, KML/KMZ 2.2, WMS and WFS service models, SLD styling, and Filter Encoding. |
| [IRI.Maptor.Sta.Graph](IRI.Maptor.Sta.Graph/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Graph.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Graph) | Graph data structures (directed/undirected/weighted, adjacency list or matrix) and classic algorithms: BFS, DFS, Dijkstra, Bellman-Ford, Floyd-Warshall, minimum spanning tree, and strongly connected components. |
| [IRI.Maptor.Sta.Pdf](IRI.Maptor.Sta.Pdf/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Pdf.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Pdf) | Vector PDF export of spatial data: single geometries/features, or a decorated print-ready map layout with title, scale bar, graticule, legend and logos, and toggleable PDF layers. |
| [IRI.Maptor.Sta.MachineLearning](IRI.Maptor.Sta.MachineLearning/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.MachineLearning.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning) | Machine learning and statistics for spatial data: DBSCAN clustering, Apriori association-rule mining, logistic regression, and descriptive statistics. |
| [IRI.Maptor.Sta.GeoParquet](IRI.Maptor.Sta.GeoParquet/) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.GeoParquet.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.GeoParquet) | Reader and writer for the GeoParquet columnar geospatial format. |
| [IRI.Maptor.Sta.Persistence](IRI.Maptor.Sta.Persistence/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Persistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Persistence) | Persistence abstractions — data-source interfaces for vector and raster data plus in-memory sources — implemented by the [IRI.Maptor.Ket](../IRI.Maptor.Ket/README.md) adapters. |
| [IRI.Maptor.Sta.Security](IRI.Maptor.Sta.Security/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Security.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Security) | Cryptography primitives: AES/Rijndael and RSA encryption, signing and verification, hashing (MD5, SHA family), and signed JWT creation/validation. |
| [IRI.Maptor.Sta.GsmGprs](IRI.Maptor.Sta.GsmGprs/README.md) | [![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.GsmGprs.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.GsmGprs) | GSM/GPRS communication: SMS PDU encoding/decoding (SMS-SUBMIT and SMS-DELIVER) and serial-port GSM modem interaction. |

## Supported formats

Format support is spread across `IRI.Maptor.Sta.Spatial` (its `IO/` folder) and the sibling projects above:

- **Vector**: GeoJSON, Esri JSON, Shapefile, KML/KMZ, GPX, WKT/WKB, TopoJSON (with quantization), DXF, SVG, EPS
- **Tiles**: PMTiles, Mapbox vector tiles
- **Raster / grid**: GeoTIFF metadata, world files, GRD
- **Terrain**: Cesium quantized-mesh and heightmap terrain (**read only**)
- **Columnar**: GeoParquet
- **Database-native**: SQL Server native binary (MS-SSCLRT)
- **Metadata**: PRJ / projection files

Each format folder under `IRI.Maptor.Sta.Spatial/IO/` has its own README with details.

---

[⬅ Back to the solution README](../../README.md)
