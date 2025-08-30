besmellah

# 🌍 Maptor Spatial Library
**A .NET Library for Spatial Data Modeling, Transfer, Processing, and Visualization**  

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hosseinnarimanirad/IRI.Japey/blob/master/LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/hosseinnarimanirad/Maptor/master-release.yml)](https://github.com/hosseinnarimanirad/Maptor/actions)
[![Contributions Welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](CONTRIBUTING.md)

**Maptor** is an open-source **.NET GIS library** designed to make spatial operations, geospatial data processing, and map visualization easy and efficient.  
Built for **.NET 8+**, it provides a comprehensive set of tools for **geometry operations**, **coordinate transformations**, **raster & vector data handling**, and **OGC standards** support.
 
---

## ✨ Key Features  
✅ **Spatial Reference Systems (SRS)** – Coordinate transformations (WGS84, UTM, Mercator, WebMercator, custom SRIDs)   
✅ **Geometry Operations** – Points, Lines, Polygons, MultiPoints, MultiLines, MultiPolygons.   
✅ **Geodetic Calculations** – Distance, azimuth, and area computations.   
✅ **Raster & Vector I/O** – Shapefiles, GeoJSON, SQL Server, PostgreSQL, Personal GDB, GPX, WKB, WKT, and raster support.   
✅ **Graph algorithms** – BFS, DFS, Minimum spanning tree, Dijkstra, MinCut, FloydWarshall.   
✅ **OGC Standards** - WKB, WKT, WFS, WMS, GML 2, GML 3.  
✅ **Database integration** - SQL Server Spatial, PostGIS  
✅ WPF-based map viewer for desktop apps  

---

## 🏗 Repository Structure  

```
Maptor /
├── IRI.Maptor.Sta/                     # Core spatial data structures & algorithms
├── IRI.Maptor.Ket/                     # Utilities (data sources, file I/O)
├── IRI.Maptor.Jab/                     # WPF components (MapViewer, dialogs, etc.)
├── IRI.Maptor.Tst/                     # Unit tests
└── Examples/                    # Usage samples
```

---

## 📦 NuGet Packages

Maptor consists of multiple modular packages. 
 
 	 
| Package | Description | Version |
|---------|-------------|---------|
| [IRI.Maptor.Sta.Spatial](https://www.nuget.org/packages/IRI.Maptor.Sta.Spatial) | Core spatial functionalities (GeoJSON, analysis, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Spatial.svg?style=flat-square) |
| [IRI.Maptor.Sta.ShapefileFormat](https://www.nuget.org/packages/IRI.Maptor.Sta.ShapefileFormat) | Read/Write shapefile (shp, shx, dbf, prj, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.ShapefileFormat.svg?style=flat-square) |
| [IRI.Maptor.Sta.SpatialReferenceSystem](https://www.nuget.org/packages/IRI.Maptor.Sta.SpatialReferenceSystem) | Coordinate system transformations (UTM, Mercator, Geodetic, Lambert, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.SpatialReferenceSystem.svg?style=flat-square) |
| [IRI.Maptor.Sta.Ogc](https://www.nuget.org/packages/IRI.Maptor.Sta.Ogc) | OGC standard implementations (GML, WKB, WKT, KML, SLD, WMS, WFS, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Ogc.svg?style=flat-square) |
| [IRI.Maptor.Sta.Graph](https://www.nuget.org/packages/IRI.Maptor.Sta.Graph) | Graph Algorithms (BFS, DFS, Minimum spanning tree, Dijkstra, MinCut, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Graph.svg?style=flat-square) |
| [IRI.Maptor.Jab.Controls](https://www.nuget.org/packages/IRI.Maptor.Jab.Controls) | WPF Map user controls (map, map panel, map coordinate system, etc.) | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Controls.svg?style=flat-square) |

<details>
<summary>▶ Show more packages</summary>
 
| Package | Description | Version |
|---------|-------------|---------|
| [IRI.Maptor.Bas.SqlSpatialLoader](https://www.nuget.org/packages/IRI.Maptor.Bas.SqlSpatialLoader) | .NET dependency of Maptor | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Bas.SqlSpatialLoader.svg?style=flat-square) |
| [IRI.Maptor.Jab.Common](https://www.nuget.org/packages/IRI.Maptor.Jab.Common) | Basic UI models, rendering methods etc. | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Jab.Common.svg?style=flat-square) |
| [IRI.Maptor.Ket.GdiPlus](https://www.nuget.org/packages/IRI.Maptor.Ket.GdiPlus) | Raster data handling, Worldfile, PCA, raster calculation | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.GdiPlus.svg?style=flat-square) |
| [IRI.Maptor.Ket.PersonalGdbPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence) | Read/Write Personal GDB files | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PersonalGdbPersistence.svg?style=flat-square) |
| [IRI.Maptor.Ket.PostgreSqlPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence) | Read/Write PostgreSQL | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PostgreSqlPersistence.svg?style=flat-square) |
| [IRI.Maptor.Ket.SqlServerPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence) | Read/Write SQL Server spatial | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerPersistence.svg?style=flat-square) |
| [IRI.Maptor.Ket.SqlServerSpatialExtension](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerSpatialExtension) | Work with SqlGeometry & SqlGeography | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerSpatialExtension.svg?style=flat-square) |
| [IRI.Maptor.Sta.Common](https://www.nuget.org/packages/IRI.Maptor.Sta.Common) | Base functionalities | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Common.svg?style=flat-square) |
| [IRI.Maptor.Sta.GsmGprs](https://www.nuget.org/packages/IRI.Maptor.Sta.GsmGprs) | SMS encoding in GSM | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.GsmGprs.svg?style=flat-square) |
| [IRI.Maptor.Sta.MachineLearning](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning) | Clustering, Apriori, Logistic Regression | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.MachineLearning.svg?style=flat-square) |
| [IRI.Maptor.Sta.Persistence](https://www.nuget.org/packages/IRI.Maptor.Sta.Persistence) | Base classes for persistence layers, MemoryDataSource, GeoJsonDataSource, etc. | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Persistence.svg?style=flat-square) |
| [IRI.Maptor.Sta.Security](https://www.nuget.org/packages/IRI.Maptor.Sta.Security) | Encryption, hashing, etc. | ![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Security.svg?style=flat-square) |

</details>     
  	  
      
👉 [Browse all packages on NuGet.org](https://www.nuget.org/packages?q=IRI.Maptor)

---
  
**Installation Example via NuGet CLI:**
```bash
dotnet add package IRI.Maptor.Sta.Common
dotnet add package IRI.Maptor.Jab.Controls
```

---

## 🚀 Quick Start  
### 1. Clone & Build  
```sh
git clone https://github.com/hosseinnarimanirad/Maptor.git  
dotnet build  
```

--- 

## 📚 Documentation

➡ [Full documentation and guides](https://github.com/hosseinnarimanirad/Maptor/wiki)

---

## 🤝 Contributing 
We welcome contributions! Please see [CONTRIBUTING.md](https://github.com/hosseinnarimanirad/Maptor/blob/master/CONTRIBUTING.md) and our [Code of Conduct](https://github.com/hosseinnarimanirad/Maptor/blob/master/CODE_OF_CONDUCT.md).

---

## 📜 License
Maptor is released under the [MIT License](https://github.com/hosseinnarimanirad/Maptor/blob/master/LICENSE.txt).
