# 🌍 Maptor Spatial Library

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hosseinnarimanirad/Maptor/blob/master/LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/hosseinnarimanirad/Maptor/master-release.yml)](https://github.com/hosseinnarimanirad/Maptor/actions)
[![Contributions Welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)
![GitHub last commit](https://img.shields.io/github/last-commit/hosseinnarimanirad/Maptor)

**A comprehensive .NET GIS library for spatial data modeling, processing, and visualization**

Maptor is a powerful, open-source .NET library designed to make spatial operations, geospatial data processing, and map visualization accessible and efficient. Built for **.NET 8+**, it provides a complete toolkit for geometry operations, coordinate transformations, data I/O, and advanced spatial algorithms.

---

## 🚀 Quick Start

### Add a Console Project
Add a new C# console project

### Installation
```bash
# Core spatial functionality
dotnet add package IRI.Maptor.Sta.Spatial
```

### Basic Usage
```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

// Create and work with points
var point1 = new Point(51.5074, -0.1278); // London
var point2 = new Point(40.7128, -74.0060); // New York


// Create geometries
var line = Geometry<Point>.CreatePointOrLineString(SridHelper.GeodeticWGS84, point1, point2);


var ellipsoidal_length = line.GetEllipsoidalLength();
var spherical_length = line.GetSphericalLength();

// Calculate length
Console.WriteLine($"ellipsoidal distance: {ellipsoidal_length:N1} km");
Console.WriteLine($"  spherical distance: {spherical_length:N1} km");

// Convert to GeoJSON
var geoJsonLine = line.AsGeoJson().Serialize(indented: true);
Console.WriteLine($"line: {geoJsonLine}");

Console.Read();
```

---

## ✨ Key Features

### 🗺️ **Spatial Reference Systems**
- **15+ predefined ellipsoids** (WGS84, GRS80, Clarke 1866, etc.)
- **Coordinate transformations** (UTM, Mercator, WebMercator, Lambert, etc.)
- **Custom SRID support** for specialized projections
- **Geodetic calculations** with high precision

### 🔧 **Geometry Operations**
- **Complete geometry types**: Points, Lines, Polygons, MultiPoints, MultiLines, MultiPolygons
- **Advanced algorithms**: Delaunay triangulation, Voronoi diagrams, convex hulls
- **Spatial indexing**: KdTree, RTree for efficient spatial queries

### 📊 **Data I/O & Formats**
- **Vector formats**: 
  - **Standard formats**: Shapefile, GeoJSON, KML, KMZ, GPX, WKB, WKT
  - **Topology formats**: TopoJSON (with topology encoding and quantization support)
  - **CAD/Graphics formats**: DXF, SVG, EPS (with styling support and round-trip coordinate preservation)
  - **Document formats**: PDF (vector graphics export)
  - **Columnar formats**: GeoParquet (efficient columnar geospatial data storage)
  - **SQL Server Native Binary**: Native spatial data format
- **Raster support**: GeoTIFF (Worldfile), GRD file, custom raster formats
- **Terrain formats**: 
  - **Cesium Terrain**: quantized-mesh-1.0 (adaptive triangle meshes) and heightmap-1.0 (regular grids) for 3D terrain visualization
- **Tile formats**: 
  - **PMTiles**: Serverless tile archive format (vector and raster tiles)
  - **MBTiles**: SQLite-based tile storage for offline mapping
  - **GeoPackage tiles**: OGC standard tile storage
- **Database integration**: SQL Server Spatial, PostGIS, Personal GDB, SQLite/GeoPackage, MBTiles
- **OGC standards**: WFS, WMS, GML 2/3, SFA, SLD styling
- **Format features**: 
  - Round-trip conversion with exact coordinate preservation (SVG, EPS, DXF)
  - Styling support (colors, stroke width, opacity) for CAD/graphics formats
  - Topology preservation in TopoJSON
  - Efficient compression and quantization options

### 🧮 **Advanced Algorithms**
- **Graph algorithms**: BFS, DFS, Dijkstra, Minimum Spanning Tree, MinCut
- **Machine learning**: Clustering, Apriori, Logistic Regression
- **Computational geometry**: Triangulation, simplification, generalization
- **Spatial analysis**: Interpolation, statistics, terrain modeling

### 🖥️ **WPF Visualization**
- **Interactive map viewer** with zoom, pan, and layer management
- **Rich UI controls** for spatial data display
- **Custom markers and annotations** 

---

## 🏗️ Architecture

Maptor follows a modular architecture with clear separation of concerns:

```
Maptor/
├── 📦 IRI.Maptor.Sta/          # Core spatial operations & algorithms
│   ├── Spatial                 # Geometry types, spatial algorithms
│   │   ├── IO/                # Format I/O (DXF, SVG, EPS, TopoJSON, PMTiles, CesiumTerrain, etc.)
│   │   └── Analysis/           # Spatial analysis algorithms
│   ├── SpatialReferenceSystem  # Coordinate systems & transformations
│   ├── ShapefileFormat         # ESRI Shapefile I/O
│   ├── Ogc                     # OGC standards implementation
│   ├── Graph                   # Graph algorithms
│   ├── MachineLearning         # ML algorithms for spatial data
│   ├── GeoParquet              # GeoParquet format support
│   ├── Pdf                     # PDF vector format support
│   ├── Security                # Security/cryptography primitives
│   └── Persistence             # Persistence abstractions
├── 🔧 IRI.Maptor.Ket/          # Infrastructure & persistence
│   ├── SqlServerPersistence    # SQL Server integration
│   ├── PostgreSqlPersistence   # PostGIS integration
│   ├── SqlitePersistence       # SQLite/GeoPackage/MBTiles support
│   ├── PersonalGdbPersistence # Personal Geodatabase support
│   ├── GdiPlus                 # Raster data handling
│   └── WebApiPersistence       # Web API data sources
├── 🖥️ IRI.Maptor.Jab/          # WPF UI components
│   ├── Controls                # Map viewer, dialogs
│   ├── Common                  # MVVM infrastructure 
└── 🧪 Tests & Samples/         # Comprehensive test suite & examples
```

---

## 📦 NuGet Packages

| Package | Description | Version |
|---------|-------------|---------|
| [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial) | Core spatial functionalities (GeoJSON, analysis, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square) |
| [IRI.Maptor.Sta.ShapefileFormat](https://www.nuget.org/packages/IRI.Maptor.Sta.ShapefileFormat) | Read/Write shapefile (shp, shx, dbf, prj, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.ShapefileFormat.svg?style=flat-square) |
| [IRI.Maptor.Sta.SpatialReferenceSystem](https://www.nuget.org/packages/IRI.Maptor.Sta.SpatialReferenceSystem) | Coordinate system transformations | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.SpatialReferenceSystem.svg?style=flat-square) |
| [IRI.Maptor.Sta.Ogc](https://www.nuget.org/packages/IRI.Maptor.Sta.Ogc) | OGC standard implementations | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Ogc.svg?style=flat-square) |
| [IRI.Maptor.Sta.Graph](https://www.nuget.org/packages/IRI.Maptor.Sta.Graph) | Graph Algorithms (BFS, DFS, Dijkstra, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Graph.svg?style=flat-square) |
| [IRI.Maptor.Jab.Common](https://www.nuget.org/packages/IRI.Maptor.Jab.Common) | WPF Map user controls | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Common.svg?style=flat-square) |

<details>
<summary>▶ Show more packages</summary>

| Package | Description | Version |
|---------|-------------|---------|
| [IRI.Maptor.Jab.Common](https://www.nuget.org/packages/IRI.Maptor.Jab.Common) | Basic UI models, rendering methods | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Common.svg?style=flat-square) |
| [IRI.Maptor.Ket.SqlServerPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence) | SQL Server spatial integration | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerPersistence.svg?style=flat-square) |
| [IRI.Maptor.Ket.PostgreSqlPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence) | PostGIS integration | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PostgreSqlPersistence.svg?style=flat-square) |
| [IRI.Maptor.Ket.SqlitePersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlitePersistence) | SQLite/GeoPackage/MBTiles support | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlitePersistence.svg?style=flat-square) |
| [IRI.Maptor.Sta.MachineLearning](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning) | Clustering, Apriori, Logistic Regression | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.MachineLearning.svg?style=flat-square) |
| [IRI.Maptor.Sta.Pdf](https://www.nuget.org/packages/IRI.Maptor.Sta.Pdf) | PDF vector format support | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Pdf.svg?style=flat-square) |
| [IRI.Maptor.Sta.Common](https://www.nuget.org/packages/IRI.Maptor.Sta.Common) | Foundational utilities and abstractions | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Common.svg?style=flat-square) |

</details>

👉 [Browse all packages on NuGet.org](https://www.nuget.org/packages?q=IRI.Maptor)

---

## 💻 Code Examples

### Working with Shapefiles
```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.ShapefileFormat;

// Read shapefile
var shapes = await Shapefile.ReadShapesAsync("countries.shp");
foreach (var shape in shapes)
{
    Console.WriteLine($"Country: {shape.AsSqlServerWkt()}");
}

// Convert to GeoJSON
var geoJson = shapes.Select(s => s.AsGeometry().AsGeoJson()).ToList();
```

### Coordinate Transformations
```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

// Transform WGS84 to UTM
var wgs84Point = new Point(51.5074, -0.1278); // London
var utmPoint = MapProjects.GeodeticToUTM(wgs84Point);

Console.WriteLine($"London (UTM): {utmPoint}");
```
 

---

## 🎯 Use Cases

- **🗺️ GIS Applications**: Desktop and web mapping applications
- **📊 Data Analysis**: Spatial data processing and analysis
- **🏗️ Engineering**: Surveying, and infrastructure projects
- **🚗 Transportation**: Route optimization, logistics 
- **🔬 Research**: Academic and scientific spatial research

---

## 🚀 Getting Started

### 1. Clone & Build
```bash
git clone https://github.com/hosseinnarimanirad/Maptor.git
cd Maptor
dotnet build
```

### 2. Run Samples
```bash
# WPF Sample Application
cd samples/IRI.Maptor.Tag.SampleWpfApp
dotnet run

# Console Samples
cd samples/IRI.Maptor.Tag.SampleCodes
dotnet run
```

### 3. Explore Documentation
- 📚 [Full documentation and guides](https://github.com/hosseinnarimanirad/Maptor/wiki)
- 🎓 [Tutorial PDFs](docs/) - Step-by-step guides
- 💡 [Sample applications](samples/) - Real-world examples

---

## 🧪 Testing & Quality

- **1,300+ C# files** with comprehensive test coverage
- **Unit tests** for core functionality 
- **Performance benchmarks** for some algorithms
- **Continuous integration** with GitHub Actions

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) and [Code of Conduct](CODE_OF_CONDUCT.md).

### Ways to Contribute
- 🐛 **Bug Reports** – Found an issue? Report it with steps to reproduce
- 💡 **Feature Requests** – Got an idea? Open a discussion or issue
- 🔧 **Code Contributions** – Implement fixes, new features, or refactor for better performance
- 📚 **Documentation** – Improve README, tutorials, XML docs, or add usage examples
- 🧪 **Testing** – Write unit/integration tests to ensure quality

---

## 📈 Performance

Maptor is designed for performance with:
- **Optimized algorithms** for large datasets
- **Spatial indexing** (KdTree, RTree) for fast queries
- **Memory-efficient** streaming APIs
- **Async/await** support for I/O operations
- **Parallel processing** where applicable

---

## 🌍 Internationalization

- **Multi-language support** with localization framework
- **RTL language support** (Arabic, Persian, Hebrew)
- **Regional data providers** (Iran-specific datasets)
- **Cultural formatting** for numbers, dates, and coordinates

---

## 📜 License

Maptor is released under the [MIT License](LICENSE.txt) - see the LICENSE file for details.

---

## 🙏 Acknowledgments

- Built with modern .NET technologies
- Inspired by OGC standards and best practices
- Community-driven development
- Academic research integration

---

<div align="center">

**⭐ Star this repository if you find it useful!**

[Report Bug](https://github.com/hosseinnarimanirad/Maptor/issues) · [Request Feature](https://github.com/hosseinnarimanirad/Maptor/issues) · [Join Discussion](https://github.com/hosseinnarimanirad/Maptor/discussions)

</div>
