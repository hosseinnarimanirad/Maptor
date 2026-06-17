# IRI.Maptor.Sta.Spatial

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

The core spatial engine of the Maptor library. Provides geometry types, spatial algorithms, advanced data structures, and a wide range of format I/O — all targeting **.NET Standard 2.1** with no UI dependencies.

---

## Features

### Geometry Types
- **Full OGC geometry model**: `Point`, `LineString`, `Polygon`, `MultiPoint`, `MultiLineString`, `MultiPolygon`, `GeometryCollection`
- Typed generics (`Geometry<TPoint>`) that carry coordinate system information
- Geometry operations: union, intersection, difference, buffer, simplification

### Spatial Analysis
- **Delaunay triangulation** and **Voronoi diagrams**
- **Computational geometry**: convex hull, visibility, polygon decomposition
- **Simplification**: Douglas-Peucker, Visvalingam-Whyatt
- **Digital terrain modeling**: contour generation, TIN, slope/aspect
- **Interpolation**: IDW and other spatial interpolation methods
- **Shape characteristics**: compactness, shape index metrics
- **Area statistics** and topology analysis
- **Space-filling curves (SFC)**: Hilbert and Z-order (Morton) curve indexing

### Spatial Indexing & Data Structures
- **KdTree** — k-d tree for nearest-neighbour queries
- **RTree** — R-tree for range/window queries
- **Map indexes** — grid-based tile indexing

### Format I/O

| Format | Read | Write | Notes |
|---|---|---|---|
| GeoJSON | ✔ | ✔ | RFC 7946 compliant |
| WKT / WKB (OGC SFA) | ✔ | ✔ | ISO/OGC compliant |
| Shapefile (SHP/DBF/SHX/PRJ) | via `Sta.ShapefileFormat` | — | |
| TopoJSON | ✔ | ✔ | Topology encoding + quantization |
| KML / KMZ | via `Sta.Ogc` | via `Sta.Ogc` | |
| DXF | ✔ | ✔ | AutoCAD interchange with styling |
| SVG | ✔ | ✔ | Round-trip coordinate preservation |
| EPS | ✔ | ✔ | Round-trip coordinate preservation |
| GeoTIFF / Worldfile | ✔ | — | Georeferenced raster |
| GPX | ✔ | ✔ | GPS tracks, routes, waypoints |
| GRD | ✔ | — | Grid raster format |
| PMTiles | ✔ | ✔ | Serverless tile archive (v3) |
| Cesium Terrain | ✔ | — | `quantized-mesh-1.0` and `heightmap-1.0`; writing not yet implemented |
| SQL Server Native Binary | ✔ | ✔ | MS-SSCLRT spatial binary |
| ESRI JSON | ✔ | — | ArcGIS REST JSON geometry |
| PRJ | ✔ | — | ESRI projection WKT |

---

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Spatial
```

---

## Quick Start

```csharp
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Extensions;

// Create a line between two points
var london   = new Point(51.5074, -0.1278);
var newYork  = new Point(40.7128, -74.0060);
var line = Geometry<Point>.CreatePointOrLineString(new List<Point> { london, newYork }, SridHelper.GeodeticWGS84);

// Measure distance
Console.WriteLine($"Ellipsoidal: {line.GetEllipsoidalLength():N1} km");
Console.WriteLine($"  Spherical: {line.GetSphericalLength():N1} km");

// Export to GeoJSON
Console.WriteLine(line.AsGeoJson().Serialize(indented: true));
```

---

## Project Structure

```
Sta.Spatial/
├── Primitives/           # Geometry<T> and base spatial types
├── GeometryOperations/   # Boolean ops, buffering, overlays
├── Analysis/
│   ├── ComputationalGeometry.cs
│   ├── DelaunayTriangulation.cs
│   ├── VoronoiDiagram.cs
│   ├── Simplification/   # Douglas-Peucker, Visvalingam-Whyatt
│   ├── Topology/
│   ├── Interpolation/
│   ├── DigitalTerrainModeling/
│   ├── ShapeCharacteristics/
│   └── SFC/              # Space-filling curve indexing
├── AdvancedStructures/   # KdTree, RTree
├── MapIndexes/           # Tile/grid index helpers
├── IO/
│   ├── GeoJsonFormat/
│   ├── OgcSFA/           # WKT / WKB
│   ├── TopoJson/
│   ├── Dxf/
│   ├── Svg/
│   ├── Eps/
│   ├── GeoTiff/
│   ├── Gpx/
│   ├── Grd/
│   ├── PmTiles/
│   ├── CesiumTerrain/
│   ├── SqlServerNativeBinary/
│   ├── EsriJson/
│   ├── Worldfile/
│   └── Prj/
├── Extensions/           # Extension methods on geometry types
├── Helpers/
├── Models/
├── Dtos/
└── Services/
```

---

📦 **NuGet**: [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
