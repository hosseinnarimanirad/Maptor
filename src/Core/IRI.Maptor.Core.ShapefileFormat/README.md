# IRI.Maptor.Core.ShapefileFormat

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Core.ShapefileFormat?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Core.ShapefileFormat/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

ESRI shapefile read and write support for the Maptor stack: SHP geometry, SHX index, DBF attributes with code-page-aware text decoding, PRJ coordinate system files, and reprojection on load or save.

## Installation

```bash
dotnet add package IRI.Maptor.Core.ShapefileFormat
```

## Features

- Read shapefiles as raw ESRI shapes (`Shapefile.ReadShapes`/`ReadShapesAsync`) or as attributed features (`Shapefile.ReadAsFeature`/`ReadAsFeatureAsync` returning `Feature<Point>`)
- All ESRI shape types: `Point`, `Polyline`, `Polygon`, `MultiPoint` plus their M and Z variants
- Write shapefiles from ESRI shapes, `Feature<Point>` lists, or GeoJSON features (`Shapefile.Save`, `SaveAsShapefile`)
- DBF attribute reading/writing with configurable encodings, FoxPro code-page detection, `.cpg` file support, and optional Persian (Farsi) character correction
- PRJ support: `TryReadPrjFile`/`TryGetSrs` on read, `SaveAsPrj` on write
- Reprojection: `Project`, `ProjectAsync`, and `ProjectAndSaveAsShapefile` between spatial reference systems (`SrsBase`)
- SHX index reading and writing
- Conversion helpers to the native geometry model (`AsGeometry`), SQL Server WKT (`AsSqlServerWkt`), and GeoJSON

## Usage

Read a shapefile:

```csharp
using IRI.Maptor.Core.ShapefileFormat;

// raw ESRI shapes
var shapes = await Shapefile.ReadShapesAsync("data/parcels.shp");

foreach (var shape in shapes)
{
    Console.WriteLine(shape.AsSqlServerWkt());
}

// as features with DBF attributes
var features = Shapefile.ReadAsFeature("data/parcels.shp", defaultSrid: 4326);
```

Write features back to a shapefile:

```csharp
using IRI.Maptor.Core.ShapefileFormat;

Shapefile.SaveAsShapefile("output/result.shp", features);
```

Reproject a shapefile on save:

```csharp
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

Shapefile.ProjectAndSaveAsShapefile(
    "data/parcels.shp", "output/parcels-mercator.shp",
    SrsBases.WebMercator, overwrite: true);
```

## Dependencies

- `IRI.Maptor.Core.Common`, `IRI.Maptor.Core.Ogc`, `IRI.Maptor.Core.Spatial`

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Core.ShapefileFormat/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Core](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Core/README.md)
