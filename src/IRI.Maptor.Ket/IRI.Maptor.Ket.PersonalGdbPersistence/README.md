# IRI.Maptor.Ket.PersonalGdbPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PersonalGdbPersistence?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence/)
[![Target](https://img.shields.io/badge/net8.0--windows-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Persistence adapter for ESRI Personal Geodatabases (`.mdb` files), accessed via OleDb / Microsoft
Access Database Engine. Geodatabases authored by any ArcGIS 10.x or 11.x version are supported —
the version-stamped ESRI schema namespace in the geodatabase metadata is normalized automatically,
so reads are independent of the authoring ArcGIS version. The adapter also picks whichever ACE
OLEDB provider (12.0 or 16.0) is installed on the machine.

## Installation

```bash
dotnet add package IRI.Maptor.Ket.PersonalGdbPersistence
```

## Features

- `PersonalGdbDataSource` — Maptor vector data source (`IVectorDataSource`) over Personal GDB feature classes
- `PersonalGdbInfrastructure` — connection and schema helpers (open `.mdb`, list feature classes, read geometries and attributes)
- `PersonalGdb` — write support: create new geodatabases, create feature classes, and insert features (ArcGIS-compatible output)
- Geometry is read from the Personal GDB binary format and converted to native `Geometry<Point>` objects
- Attribute table access with type-mapped field reading

## Usage

Writing a Personal Geodatabase:

```csharp
using IRI.Maptor.Ket.PersonalGdbPersistence;
using IRI.Maptor.Ket.PersonalGdbPersistence.Enums;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;

// create a new empty .mdb (or PersonalGdb.Open(...) for an existing one)
var gdb = PersonalGdb.CreateEmpty(@"c:\data\my.mdb");

// add a feature class ("layer") with its CRS and attribute schema
gdb.CreateFeatureClass("Roads", GeometryType.LineString, SrsBases.GeodeticWgs84,
    new List<PersonalGdbField>
    {
        new() { Name = "Name", FieldType = GdbEsriFieldType.esriFieldTypeString, Length = 100 },
        new() { Name = "Code", FieldType = GdbEsriFieldType.esriFieldTypeInteger },
    });

// insert features (geometries must already be in the feature class CRS)
gdb.Insert("Roads", features);
```

Notes:

- Supported geometry types: Point, MultiPoint, LineString/MultiLineString, Polygon/MultiPolygon — XY only (no Z/M).
- `CreateEmpty` extracts an embedded template (`template.mdb`), an empty personal geodatabase
  authored in ArcCatalog (ArcGIS 10.7 catalog schema). All catalog metadata (`GDB_Items`,
  `GDB_ItemRelationships`, `GDB_GeomColumns`, `GDB_SpatialRefs`) plus the per-feature-class
  spatial index tables are maintained on write, so the result opens in ArcMap/ArcCatalog.
- `OBJECTID`, `SHAPE`, and (for lines/polygons) `SHAPE_Length`/`SHAPE_Area` columns are created and populated automatically.

## Limitations

- Windows only (OleDb / ACE provider).
- Microsoft Access Database Engine (ACE OLEDB 12.0 or 16.0) must be installed.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Ket](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Ket/README.md)
