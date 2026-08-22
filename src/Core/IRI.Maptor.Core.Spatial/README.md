# IRI.Maptor.Core.Spatial

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.Spatial?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Core.Spatial/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

The core spatial engine of the Maptor stack. Provides the `Geometry<T>` model, spatial analysis algorithms, spatial indexes, and a wide range of geospatial format I/O — all UI-free and netstandard2.1-compatible.

## Installation

```bash
dotnet add package IRI.Maptor.Core.Spatial
```

## Features

- OGC geometry model via `Geometry<T>`: point, linestring, polygon, their multi-variants, and geometry collections, with WKT/WKB round-trip (`FromWkt`, `FromWkb`)
- Feature model: `Feature<T>` and `FeatureSet<T>` with fields and change tracking
- Spatial analysis: Delaunay triangulation, Voronoi diagrams, convex hull, area statistics, topology helpers, and shape characteristics metrics
- Line simplification: Ramer-Douglas-Peucker, Visvalingam-Whyatt, and other point-reduction methods, plus simplification quality metrics
- Digital terrain modeling: regular (grid) and irregular (TIN) DTMs
- Spatial interpolation: inverse distance weighting (IDW)
- Space-filling curves: Hilbert and Z-order (Morton) point ordering
- Spatial indexes: k-d tree (plain and balanced), R-tree, and space-filling-curve R-tree
- Map sheet/tile indexes for geodetic and UTM grids
- Format I/O (see table below)

## Format I/O

| Format | Read | Write | Notes |
|---|---|---|---|
| GeoJSON | Yes | Yes | Geometries, features, feature sets |
| WKT / WKB (OGC SFA) | Yes | Yes | |
| TopoJSON | Yes | Yes | Topology encoding and quantization |
| DXF | Yes | Yes | AutoCAD interchange with styling |
| SVG | Yes | Yes | |
| EPS | Yes | Yes | |
| GPX | Yes | Yes | Waypoints, routes, tracks |
| PMTiles | Yes | Yes | Serverless tile archive (v3) |
| SQL Server native binary | Yes | Yes | MS-SSCLRT spatial binary |
| Vector tiles (MVT) | Yes | No | |
| Cesium terrain | Yes | No | `quantized-mesh-1.0` and `heightmap-1.0`; writing not implemented |
| GeoTIFF | Yes | No | Georeferenced raster |
| GRD | Yes | No | Grid raster format |
| ESRI JSON | Yes | No | ArcGIS REST JSON geometry |
| PRJ | Yes | No | ESRI projection WKT |

Shapefiles are handled by the companion package `IRI.Maptor.Core.ShapefileFormat`; KML/KMZ by `IRI.Maptor.Core.Ogc`; MBTiles/GeoPackage by `IRI.Maptor.Infrastructure.Sqlite`.

## Usage

```csharp
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Extensions;

var points = new List<Point> { new Point(51.33, 35.70), new Point(51.42, 35.75) };

// note: (List<T> points, int srid) is the only valid overload
var line = Geometry<Point>.CreatePointOrLineString(points, srid: 4326);

// per-segment lengths on the ellipsoid
double meters = SpatialUtility.GetEllipsoidalLength(points[0], points[1]);

// export to GeoJSON
string geoJson = line.AsGeoJson().Serialize(indented: true);

// WKT round-trip
var parsed = Geometry<Point>.FromWkt("POINT (51.39 35.69)", 4326);
```

## See also

- [Analysis](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/Analysis/README.md) · [Digital terrain modeling](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/Analysis/DigitalTerrainModeling/README.md) · [Interpolation](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/Analysis/Interpolation/README.md) · [Space-filling curves](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/Analysis/SFC/README.md)
- [Advanced structures](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/AdvancedStructures/README.md) · [K-d trees](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/AdvancedStructures/KdTrees/README.md) · [R-trees](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/AdvancedStructures/RTrees/README.md)
- IO formats: [GeoJSON](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/GeoJsonFormat/README.md) · [WKT/WKB](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/OgcSFA/README.md) · [TopoJSON](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/TopoJson/README.md) · [DXF](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/Dxf/README.md) · [SVG](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/Svg/README.md) · [EPS](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/Eps/README.md) · [PMTiles](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/PmTiles/README.md) · [Vector tiles](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/VectorTiles/README.md) · [Cesium terrain](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/CesiumTerrain/README.md) · [SQL Server native binary](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/SqlServerNativeBinary/README.md) · [ESRI JSON](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/IRI.Maptor.Core.Spatial/IO/EsriJson/README.md)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Core.Spatial/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Core](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/README.md)
