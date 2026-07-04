# 🌍 Maptor Spatial Library

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hosseinnarimanirad/Maptor/blob/master/LICENSE.txt)
[![Build](https://img.shields.io/github/actions/workflow/status/hosseinnarimanirad/Maptor/master-release.yml)](https://github.com/hosseinnarimanirad/Maptor/actions)
[![Contributions Welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)
![GitHub last commit](https://img.shields.io/github/last-commit/hosseinnarimanirad/Maptor)

**A comprehensive .NET GIS library for spatial data modeling, processing, and visualization**

Maptor is a powerful, open-source .NET library designed to make spatial operations, geospatial data processing, and map visualization accessible and efficient. Core packages target **.NET Standard 2.1** (compatible with .NET 5+). The WPF map viewer targets **.NET 8 (Windows)**.

---

## 🚀 Quick Start

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

// Create a line from a list of points
var line = Geometry<Point>.CreatePointOrLineString(
    new List<Point> { point1, point2 },
    SridHelper.GeodeticWGS84);

// Calculate length
Console.WriteLine($"ellipsoidal distance: {line.GetEllipsoidalLength():N1} km");
Console.WriteLine($"  spherical distance: {line.GetSphericalLength():N1} km");

// Convert to GeoJSON
var geoJsonLine = line.AsGeoJson().Serialize(indented: true);
Console.WriteLine($"line: {geoJsonLine}");

Console.Read();
```

---

## ✨ Key Features

### 🗺️ **Spatial Reference Systems**
- **25+ predefined ellipsoids** (WGS84, GRS80, Clarke 1866, etc.)
- **Coordinate transformations** (UTM, Mercator, WebMercator, Lambert, etc.)
- **Custom SRID support** for specialized projections
- **Geodetic distance calculations** (ellipsoidal and spherical)

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
- **Raster support**: GeoTIFF, world files, GRD files
- **Terrain formats**: 
  - **Cesium Terrain (read)**: quantized-mesh-1.0 (adaptive triangle meshes) and heightmap-1.0 (regular grids)
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
- **Graph algorithms**: BFS, DFS, Dijkstra, Bellman-Ford, Floyd-Warshall, Kruskal, Prim, SCC
- **Machine learning**: Clustering, Apriori, Logistic Regression
- **Computational geometry**: Triangulation, simplification, generalization
- **Spatial analysis**: Interpolation, statistics, terrain modeling

### 🖥️ **WPF Visualization**
- **Interactive map viewer** with zoom, pan, and layer management
- **Rich UI controls** for spatial data display
- **Custom markers and annotations** 

---

## 🏗️ Architecture

Maptor follows a modular architecture with clear separation of concerns — three tiers where dependencies flow downward (UI → infrastructure → core):

```
Maptor/
├── 📦 IRI.Maptor.Sta/          # Core spatial libraries (netstandard2.1, UI-free)
│   ├── Common                  # Foundational primitives & abstractions
│   ├── Spatial                 # Geometry types, algorithms, indexes, format I/O
│   │   └── IO/                 # GeoJSON, DXF, SVG, EPS, TopoJSON, PMTiles, GPX, …
│   ├── SpatialReferenceSystem  # Ellipsoids, datums, projections, transformations
│   ├── ShapefileFormat         # ESRI Shapefile I/O
│   ├── Ogc                     # OGC standards (SFA WKT/WKB, GML, KML, WMS, WFS, SLD)
│   ├── Graph                   # Graph algorithms
│   ├── MachineLearning         # ML algorithms for spatial data
│   ├── GeoParquet              # GeoParquet format support
│   ├── Pdf                     # Vector PDF map export
│   ├── Security                # Security/cryptography primitives
│   ├── Persistence             # Persistence abstractions
│   └── GsmGprs                 # SMS PDU & GSM modem support
├── 🔧 IRI.Maptor.Ket/          # Infrastructure & persistence adapters (net8.0 / net8.0-windows)
│   ├── SqlServerPersistence    # SQL Server Spatial integration
│   ├── PostgreSqlPersistence   # PostGIS integration
│   ├── SqlitePersistence       # SQLite / GeoPackage / MBTiles
│   ├── EfCorePersistence       # EF Core spatial mapping (NTS-free)
│   ├── PersonalGdbPersistence  # ESRI Personal Geodatabase (.mdb)
│   ├── WebApiPersistence       # Web API data sources
│   ├── SqlServerSpatialExtension # SqlGeometry/SqlGeography conversions
│   ├── GdiPlus                 # Raster & image processing (GDI+)
│   └── WindowsBase             # Windows-specific services
├── 🖥️ IRI.Maptor.Jab/          # UI & presentation (WPF)
│   ├── Common                  # MapViewer control, MVVM, layers, symbology
│   ├── Core                    # UI-independent core, tile services, localization
│   └── IranRepo                # Iran-specific data repositories
├── 🧪 tests/                   # xUnit test suite
└── 💡 samples/                 # Sample applications
```

**Tier guides** — a structured overview of every project in each tier:
- 📦 [Core spatial libraries (Sta)](src/IRI.Maptor.Sta/README.md)
- 🔧 [Infrastructure & persistence adapters (Ket)](src/IRI.Maptor.Ket/README.md)
- 🖥️ [UI & presentation libraries (Jab)](src/IRI.Maptor.Jab/README.md)

---

## 📦 NuGet Packages

| Package | Description | Version |
|---------|-------------|---------|
| [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial) | Core spatial functionalities (GeoJSON, analysis, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square) |
| [IRI.Maptor.Sta.ShapefileFormat](https://www.nuget.org/packages/IRI.Maptor.Sta.ShapefileFormat) | Read/Write shapefile (shp, shx, dbf, prj, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.ShapefileFormat.svg?style=flat-square) |
| [IRI.Maptor.Sta.SpatialReferenceSystem](https://www.nuget.org/packages/IRI.Maptor.Sta.SpatialReferenceSystem) | Coordinate system transformations | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.SpatialReferenceSystem.svg?style=flat-square) |
| [IRI.Maptor.Sta.Ogc](https://www.nuget.org/packages/IRI.Maptor.Sta.Ogc) | OGC standard implementations (WFS, WMS, GML, KML, SLD) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Ogc.svg?style=flat-square) |
| [IRI.Maptor.Sta.Graph](https://www.nuget.org/packages/IRI.Maptor.Sta.Graph) | Graph Algorithms (BFS, DFS, Dijkstra, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Graph.svg?style=flat-square) |
| [IRI.Maptor.Jab.Common](https://www.nuget.org/packages/IRI.Maptor.Jab.Common) | WPF Map viewer and UI controls | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Common.svg?style=flat-square) |

Every library project in the Sta, Ket, and Jab tiers is published to NuGet — see the per-tier package tables in the [Sta](src/IRI.Maptor.Sta/README.md), [Ket](src/IRI.Maptor.Ket/README.md), and [Jab](src/IRI.Maptor.Jab/README.md) guides.

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
# WPF sample application
dotnet run --project samples/IRI.Maptor.Tag.SampleWpfApp

# Console samples
dotnet run --project samples/IRI.Maptor.Tag.SampleCodes

# Full-featured WPF demo
dotnet run --project samples/IRI.Maptor.MasterProjectWPF
```

### 3. Explore Documentation
- 📚 [Full documentation and guides](https://github.com/hosseinnarimanirad/Maptor/wiki)
- 🎓 [Tutorial PDFs](docs/) - Step-by-step guides
- 💡 [Sample applications](samples/) - Real-world examples

---

## 🧪 Testing & Quality

- **1,300+ C# files** across 28 projects
- **Unit tests** for core functionality (xUnit, `tests/IRI.Maptor.Tst.Main`)
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

- **Spatial indexing** (KdTree, RTree) for fast spatial queries
- **Scale- and bounding-box-aware queries** in the database adapters, so only visible features are fetched
- **Async/await** support for I/O operations

---

## 🌍 Internationalization

- **Multi-language UI** — localization resources for 15 cultures, switchable at runtime
- **RTL language support** (Persian, Arabic)
- **Regional data providers** (Iran-specific datasets via `IRI.Maptor.Jab.IranRepo`)

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
