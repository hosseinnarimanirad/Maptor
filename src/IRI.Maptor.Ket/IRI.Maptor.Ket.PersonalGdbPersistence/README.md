# IRI.Maptor.Ket.PersonalGdbPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PersonalGdbPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8 (Windows)** persistence adapter for **ESRI Personal Geodatabases** (`.mdb` files) — accessed via OleDb / Microsoft Access Database Engine.

Geodatabases authored by any **ArcGIS 10.x or 11.x** version are supported — the version-stamped ESRI schema namespace in the geodatabase metadata is normalized automatically, so reads are independent of the authoring ArcGIS version. The adapter also picks whichever **ACE OLEDB provider** (12.0 or 16.0) is installed on the machine.

---

## Features

- `PersoanlGdbDataSource` — implements the Maptor `IVectorDataSource` interface for Personal GDB feature classes
- `PersonalGdbInfrastructure` — connection and schema helpers (open `.mdb`, list feature classes, read geometries and attributes)
- `PersonalGdb` — **write support**: create new geodatabases, create feature classes, and insert features (ArcGIS-compatible output)
- Geometry is read from the Personal GDB binary format and converted to native `Geometry<Point>` objects
- Attribute table access with type-mapped field reading

---

## Writing a Personal Geodatabase

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

- Supported geometry types: Point, MultiPoint, LineString/MultiLineString, Polygon/MultiPolygon — **XY only** (no Z/M).
- `CreateEmpty` extracts an embedded template (`template.mdb`), an empty personal geodatabase authored in ArcCatalog (ArcGIS 10.7 catalog schema), and all catalog metadata (`GDB_Items`, `GDB_ItemRelationships`, `GDB_GeomColumns`, `GDB_SpatialRefs`) plus the per-feature-class spatial index tables are maintained on write, so the result opens in ArcMap/ArcCatalog.
- `OBJECTID`, `SHAPE`, and (for lines/polygons) `SHAPE_Length`/`SHAPE_Area` columns are created and populated automatically.

---

## Requirements

- Windows only (OleDb / ACE provider)
- Microsoft Access Database Engine (ACE OLEDB 12.0 or 16.0) must be installed

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.PersonalGdbPersistence
```

---

## Project Structure

```
Ket.PersonalGdbPersistence/
├── PersoanlGdbDataSource.cs   # IVectorDataSource implementation (read)
├── PersonalGdbInfrastructure.cs
├── PersonalGdb.cs             # write API: CreateEmpty / Open / CreateFeatureClass / Insert
├── template.mdb               # embedded empty pgdb (ArcCatalog-authored, ArcGIS 10.7 schema)
├── Enums/
├── Model/
├── Write/                     # catalog XML templates, Access DDL/DML builders, spatial index math
└── Xml/
```

---

📦 **NuGet**: [IRI.Maptor.Ket.PersonalGdbPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
