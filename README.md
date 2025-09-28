# 🌍 Maptor Spatial Library

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hosseinnarimanirad/Maptor/blob/master/LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/hosseinnarimanirad/Maptor/master-release.yml)](https://github.com/hosseinnarimanirad/Maptor/actions)
[![Contributions Welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

**A comprehensive .NET GIS library for spatial data modeling, processing, and visualization**

Maptor is a powerful, open-source .NET library designed to make spatial operations, geospatial data processing, and map visualization accessible and efficient. Built for **.NET 8+**, it provides a complete toolkit for geometry operations, coordinate transformations, data I/O, and advanced spatial algorithms.

---

## 🚀 Quick Start

### Installation
```bash
# Core spatial functionality
dotnet add package IRI.Maptor.Sta.Spatial

# Shapefile support
dotnet add package IRI.Maptor.Sta.ShapefileFormat

# WPF map controls
dotnet add package IRI.Maptor.Jab.Controls
```

### Basic Usage
```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Create and work with points
var point1 = new Point(51.5074, -0.1278); // London
var point2 = new Point(40.7128, -74.0060); // New York

// Calculate distance
double distance = point1.SphericalDistance(point2);
Console.WriteLine($"Distance: {distance:F2} km");

// Create geometries
var geometry = new Geometry<Point>
{
    Type = GeometryType.Point,
    Points = new List<Point> { point1 }
};

// Convert to GeoJSON
string geoJson = geometry.AsGeoJson();
```

---

## ✨ Key Features

### 🗺️ **Spatial Reference Systems**
- **30+ predefined ellipsoids** (WGS84, GRS80, Clarke 1866, etc.)
- **Coordinate transformations** (UTM, Mercator, WebMercator, Lambert, etc.)
- **Custom SRID support** for specialized projections
- **Geodetic calculations** with high precision

### 🔧 **Geometry Operations**
- **Complete geometry types**: Points, Lines, Polygons, MultiPoints, MultiLines, MultiPolygons
- **Advanced algorithms**: Delaunay triangulation, Voronoi diagrams, convex hulls
- **Spatial indexing**: KdTree, RTree for efficient spatial queries
- **Topology operations**: Intersection, union, difference, buffer

### 📊 **Data I/O & Formats**
- **Vector formats**: Shapefile, GeoJSON, KML, GPX, WKB, WKT
- **Raster support**: GeoTIFF, Worldfile, custom raster formats
- **Database integration**: SQL Server Spatial, PostGIS, Personal GDB
- **OGC standards**: WFS, WMS, GML 2/3, SLD styling

### 🧮 **Advanced Algorithms**
- **Graph algorithms**: BFS, DFS, Dijkstra, Minimum Spanning Tree, MinCut
- **Machine learning**: Clustering, Apriori, Logistic Regression
- **Computational geometry**: Triangulation, simplification, generalization
- **Spatial analysis**: Interpolation, statistics, terrain modeling

### 🖥️ **WPF Visualization**
- **Interactive map viewer** with zoom, pan, and layer management
- **Rich UI controls** for spatial data display
- **Custom markers and annotations**
- **Real-time coordinate tracking**

---

## 🏗️ Architecture

Maptor follows a modular architecture with clear separation of concerns:

```
Maptor/
├── 📦 IRI.Maptor.Sta/          # Core spatial operations & algorithms
│   ├── Spatial                 # Geometry types, spatial algorithms
│   ├── SpatialReferenceSystem  # Coordinate systems & transformations
│   ├── ShapefileFormat        # ESRI Shapefile I/O
│   ├── Ogc                    # OGC standards implementation
│   ├── Graph                  # Graph algorithms
│   └── MachineLearning        # ML algorithms for spatial data
├── 🔧 IRI.Maptor.Ket/          # Infrastructure & persistence
│   ├── SqlServerPersistence   # SQL Server integration
│   ├── PostgreSqlPersistence  # PostGIS integration
│   ├── GdiPlus               # Raster data handling
│   └── WebApiPersistence     # Web API data sources
├── 🖥️ IRI.Maptor.Jab/          # WPF UI components
│   ├── Controls              # Map viewer, dialogs
│   ├── Common                # MVVM infrastructure
│   └── IranRepo              # Regional data support
└── 🧪 Tests & Samples/        # Comprehensive test suite & examples
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
| [IRI.Maptor.Jab.Controls](https://www.nuget.org/packages/IRI.Maptor.Jab.Controls) | WPF Map user controls | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Controls.svg?style=flat-square) |

<details>
<summary>▶ Show more packages</summary>

| Package | Description | Version |
|---------|-------------|---------|
| [IRI.Maptor.Jab.Common](https://www.nuget.org/packages/IRI.Maptor.Jab.Common) | Basic UI models, rendering methods | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Common.svg?style=flat-square) |
| [IRI.Maptor.Ket.GdiPlus](https://www.nuget.org/packages/IRI.Maptor.Ket.GdiPlus) | Raster data handling, Worldfile, PCA | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.GdiPlus.svg?style=flat-square) |
| [IRI.Maptor.Ket.SqlServerPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence) | SQL Server spatial integration | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerPersistence.svg?style=flat-square) |
| [IRI.Maptor.Ket.PostgreSqlPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence) | PostGIS integration | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PostgreSqlPersistence.svg?style=flat-square) |
| [IRI.Maptor.Sta.MachineLearning](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning) | Clustering, Apriori, Logistic Regression | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.MachineLearning.svg?style=flat-square) |

</details>

👉 [Browse all packages on NuGet.org](https://www.nuget.org/packages?q=IRI.Maptor)

---

## 💻 Code Examples

### Working with Shapefiles
```csharp
using IRI.Maptor.Sta.ShapefileFormat;

// Read shapefile
var shapes = await Shapefile.ReadShapesAsync("countries.shp");
foreach (var shape in shapes)
{
    Console.WriteLine($"Country: {shape.AsSqlServerWkt()}");
}

// Convert to GeoJSON
var geoJson = shapes.Select(s => s.AsGeoJson()).ToList();
```

### Coordinate Transformations
```csharp
using IRI.Maptor.Sta.SpatialReferenceSystem;

// Transform WGS84 to UTM
var wgs84Point = new Point(51.5074, -0.1278); // London
var utmPoint = CoordinateSystem.Transform(
    wgs84Point, 
    CoordinateSystem.WGS84, 
    CoordinateSystem.UTM_Zone30N
);
```

### Spatial Queries
```csharp
// Find points within radius
var center = new Point(51.5074, -0.1278);
var radius = 1000; // meters
var nearbyPoints = points.Where(p => 
    p.SphericalDistance(center) <= radius
).ToList();

// Spatial indexing for performance
var kdTree = new KdTree<Point>(points);
var nearest = kdTree.FindNearest(center, 5);
```

### WPF Map Integration
```csharp
// Add layers to map
var pointLayer = new SpecialPointLayer(
    name: "Cities",
    items: cityMarkers,
    opacity: 0.8,
    visibleRange: ScaleInterval.All
);

mapPresenter.AddLayer(pointLayer);
mapPresenter.ZoomToExtent(pointLayer.Extent);
```

---

## 🎯 Use Cases

- **🗺️ GIS Applications**: Desktop and web mapping applications
- **📊 Data Analysis**: Spatial data processing and analysis
- **🏗️ Engineering**: Surveying, construction, and infrastructure projects
- **🌍 Environmental**: Climate modeling, resource management
- **🚗 Transportation**: Route optimization, logistics
- **🏙️ Urban Planning**: City modeling, demographic analysis
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

- **1,359+ C# files** with comprehensive test coverage
- **Unit tests** for all core functionality
- **Integration tests** for database operations
- **Performance benchmarks** for critical algorithms
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