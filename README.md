# 🌍 Maptor

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hosseinnarimanirad/Maptor/blob/master/LICENSE.txt)
[![Build](https://img.shields.io/github/actions/workflow/status/hosseinnarimanirad/Maptor/master-release.yml)](https://github.com/hosseinnarimanirad/Maptor/actions)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue.svg)](https://learn.microsoft.com/dotnet/standard/net-standard)
[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Spatial.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Core.Spatial)

**A comprehensive .NET GIS library for spatial data modeling, processing, and visualization**

## What is Maptor

Maptor is an open-source .NET library suite for spatial operations, geospatial data processing, and map visualization. Core packages target **.NET Standard 2.1** (usable from .NET Core 3.0+/.NET 5+ on any platform); the WPF map viewer targets **.NET 8 (Windows)**.

- **Spatial reference systems** — predefined ellipsoids, datum transformations, UTM/Mercator/Web Mercator/Lambert projections, geodetic distance calculations (ellipsoidal and spherical)
- **Geometry and algorithms** — OGC geometry model (points, lines, polygons and multi-variants), Delaunay triangulation, Voronoi diagrams, convex hull, simplification, KdTree/RTree spatial indexes, graph algorithms, machine learning for spatial data
- **Data I/O** — Shapefile, GeoJSON, Esri JSON, KML/KMZ, GPX, WKT/WKB, TopoJSON, DXF, SVG, EPS, PDF export, GeoParquet, PMTiles, Mapbox vector tiles, MBTiles, GeoPackage, Cesium terrain (read), GeoTIFF metadata, world files, and OGC services (WMS, WFS, GML, SLD)
- **Database integration** — SQL Server Spatial, PostgreSQL/PostGIS, SQLite, ESRI Personal Geodatabase, EF Core spatial mapping without NetTopologySuite
- **WPF visualization** — interactive `MapViewer` control with layers, symbology, tile basemaps, markers, and a localization system covering 15 cultures with right-to-left support

## Repository layout

```
IRI.Maptor.sln
├── src/
│   ├── Core/   # 12 core spatial libraries (netstandard2.1, UI-free)
│   ├── Infrastructure/   # 9 infrastructure & persistence adapters (net8.0 / net8.0-windows)
│   └── Presentation/   # WPF UI tier: MapViewer control, MVVM, symbology, localization
├── tests/                # xUnit test suite
├── samples/              # sample code: core cookbook, WPF HelloMap, WPF gallery
├── research/             # algorithm research prototypes
└── docs/                 # tutorials, the README style guide, and "future-improvements/"
                          # (one .md per planned improvement or feature, ready to implement)
```

Three library tiers where dependencies flow downward (Jab UI → Ket infrastructure → Sta core):

| Area | Target | Contents |
|------|--------|----------|
| [`src/IRI.Maptor.Core`](src/Core/README.md) | netstandard2.1 | Twelve UI-free core libraries: primitives, spatial engine and format I/O, reference systems, Shapefile, OGC standards, graphs, ML, PDF export, security, persistence abstractions |
| [`src/IRI.Maptor.Infrastructure`](src/Infrastructure/README.md) | net8.0 / net8.0-windows | Nine infrastructure and persistence adapters: SQL Server, PostgreSQL/PostGIS, SQLite (MBTiles/GeoPackage), EF Core, Personal GDB, web APIs, GDI+ raster |
| [`src/IRI.Maptor.Presentation`](src/Presentation/README.md) | net8.0 / net8.0-windows | UI and presentation: the WPF `MapViewer` control, MVVM infrastructure, layers and symbology, tile services, localization |
| [`tests/`](tests/) | net8.0 | xUnit test suite |
| [`samples/`](samples/README.md) | net8.0 / net8.0-windows | Sample code: a console cookbook for the core libraries, a minimal WPF map app, and a WPF gallery with one page per UI feature |

## Installation

```bash
# Core spatial functionality
dotnet add package IRI.Maptor.Core.Spatial
```

Frequently used packages:

| Package | Description | Version |
|---------|-------------|---------|
| [IRI.Maptor.Core.Spatial](https://www.nuget.org/packages/IRI.Maptor.Core.Spatial) | Core spatial engine: geometry, analysis, format I/O | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Spatial.svg?style=flat-square) |
| [IRI.Maptor.Core.ShapefileFormat](https://www.nuget.org/packages/IRI.Maptor.Core.ShapefileFormat) | Shapefile read/write (SHP, SHX, DBF, PRJ) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.ShapefileFormat.svg?style=flat-square) |
| [IRI.Maptor.Core.SpatialReferenceSystem](https://www.nuget.org/packages/IRI.Maptor.Core.SpatialReferenceSystem) | Coordinate systems and map projections | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.SpatialReferenceSystem.svg?style=flat-square) |
| [IRI.Maptor.Core.Ogc](https://www.nuget.org/packages/IRI.Maptor.Core.Ogc) | OGC standards (WFS, WMS, GML, KML, SLD) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Ogc.svg?style=flat-square) |
| [IRI.Maptor.Core.Graph](https://www.nuget.org/packages/IRI.Maptor.Core.Graph) | Graph algorithms (BFS, DFS, Dijkstra, MST) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Graph.svg?style=flat-square) |
| [IRI.Maptor.Presentation.Wpf](https://www.nuget.org/packages/IRI.Maptor.Presentation.Wpf) | WPF map viewer and UI controls | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Presentation.Wpf.svg?style=flat-square) |

> **Renamed:** the WPF tier was previously published as `IRI.Maptor.Presentation.Common`. That package is
> deprecated — switch to [`IRI.Maptor.Presentation.Wpf`](https://www.nuget.org/packages/IRI.Maptor.Presentation.Wpf).
> The namespace changed accordingly: `IRI.Maptor.Presentation.Controls` is now `IRI.Maptor.Presentation.Wpf.Controls`.

Every library project in the Sta, Ket, and Jab tiers is published to NuGet — see the per-tier package tables in the [Sta](src/Core/README.md), [Ket](src/Infrastructure/README.md), and [Jab](src/Presentation/README.md) guides, or [browse all packages on NuGet.org](https://www.nuget.org/packages?q=IRI.Maptor).

## Quick start

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

// Create and work with points
var point1 = new Point(51.33, 35.70);
var point2 = new Point(51.42, 35.75);

// Create a line from a list of points
var line = Geometry<Point>.CreatePointOrLineString(
    new List<Point> { point1, point2 },
    SridHelper.GeodeticWGS84);

// Distance on the ellipsoid
double meters = SpatialUtility.GetEllipsoidalLength(point1, point2);
Console.WriteLine($"ellipsoidal distance: {meters:N0} m");

// Convert to GeoJSON
var geoJsonLine = line.AsGeoJson().Serialize(indented: true);
Console.WriteLine($"line: {geoJsonLine}");
```

Reading a shapefile and converting to GeoJSON:

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.ShapefileFormat;

var shapes = await Shapefile.ReadShapesAsync("countries.shp");
var geoJson = shapes.Select(s => s.AsGeometry().AsGeoJson()).ToList();
```

Clone and build the whole solution:

```bash
git clone https://github.com/hosseinnarimanirad/Maptor.git
cd Maptor
dotnet build
```

Run the samples (see the [samples index](samples/README.md) for the full list):

```bash
# the smallest complete WPF map application
dotnet run --project samples/IRI.Maptor.Samples.Wpf.HelloMap

# WPF gallery: one page per UI feature (legend, go to, identify, drawing, ...)
dotnet run --project samples/IRI.Maptor.Samples.Wpf.Gallery

# console cookbook for the core libraries
dotnet run --project samples/IRI.Maptor.Samples.Core -- all
```

## Documentation

- [README style guide](docs/readme-style-guide.md) — conventions for documentation in this repo
- [Tutorial PDFs](docs/) — step-by-step guides
- Tier guides: [Sta core libraries](src/Core/README.md) · [Ket adapters](src/Infrastructure/README.md) · [Jab UI libraries](src/Presentation/README.md)
- Format deep-dives: each format folder under [`src/Core/IRI.Maptor.Core.Spatial/IO/`](src/Core/IRI.Maptor.Core.Spatial/IO/) has its own README
- [Samples](samples/README.md) — one folder per sample, each with its own README

## Contributing

Contributions are welcome — bug reports, feature requests, code, documentation, and tests. Please see the [Contributing Guide](CONTRIBUTING.md) and [Code of Conduct](CODE_OF_CONDUCT.md).

## License

Maptor is released under the [MIT License](LICENSE.txt).

---

⭐ Star this repository if you find it useful!

[Report a bug](https://github.com/hosseinnarimanirad/Maptor/issues) · [Request a feature](https://github.com/hosseinnarimanirad/Maptor/issues) · [Join the discussion](https://github.com/hosseinnarimanirad/Maptor/discussions)
