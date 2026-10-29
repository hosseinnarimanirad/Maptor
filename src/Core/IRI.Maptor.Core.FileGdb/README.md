# IRI.Maptor.Core.FileGdb

[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Read-only support for ESRI File Geodatabases (`.gdb` folders), written from scratch in managed C#
with no third-party or native dependencies (no GDAL, no Esri FileGDB API).

## Usage

```csharp
using IRI.Maptor.Core.FileGdb;
using IRI.Maptor.Core.FileGdb.Enums;

var gdb = FileGeodatabase.Open(@"D:\data\Parcels.gdb");   // reads the catalog; keeps no files open

foreach (var dataset in gdb.Datasets)
    Console.WriteLine($"{dataset.Name} [{dataset.AliasName}] {dataset.GeometryType} SRID={dataset.Srid} rows={dataset.RowCount}");

var parcels = gdb.GetDataset("Parcels")!;

foreach (var field in parcels.Fields)
    Console.WriteLine($"{field.Name} [{field.AliasName}] {field.FieldType} domain={field.DomainName}");

var errors = new List<FileGdbRowError>();

foreach (var feature in parcels.ReadFeatures(new FileGdbReadOptions { OnRowError = errors.Add }))
{
    long oid = feature.ObjectId;                    // 64-bit since ArcGIS Pro 3.2
    var shape = feature.Geometry;                   // Geometry<Point>, null for a null shape
    object? code = feature["Use"];                  // the stored value
    string? name = feature.GetCodedValueName("Use");  // its coded-value name, subtype-aware
    var maptorFeature = feature.ToFeature(resolveCodedValues: true);  // Feature<Point>
}
```

- `FileGeodatabase.Open(folder)` lists datasets (feature classes and standalone tables), feature
  datasets, domains and relationship classes. Definitions that cannot be read are reported in
  `Warnings` instead of failing the whole geodatabase.
- `FileGdbDataset`:
  - Catalog data: alias, feature dataset, subtypes and relationship classes.
  - Physical schema, read on first use: `Fields`, `RowCount`, `GeometryType`, `HasZ`/`HasM`, `Extent`.
  - `Srid`/`SpatialReference`. The SRID comes from `LatestWKID`, else `WKID`, else the WKT, else 0. For example, Esri's 102100 becomes 3857.
  - `Storage` says whether the table is readable. Compressed and missing tables are listed, but reading them throws.
- `FileGdbField` combines the stored field (type, length, nullability, default) with the alias and domain from the dataset's definition.
- `ReadFeatures` streams the rows one at a time in OBJECTID order:
  - With `OnRowError` set, a corrupt row or geometry is reported and skipped; without it, the read throws.
  - `BoundingBox` filters by envelope. It still reads every row, because no spatial index is used.
- `FileGdbFeature`:
  - OBJECTID, geometry and stored values, by name or by index.
  - The row's subtype, and coded-value names found through the domain of that subtype (falling back to the field's own domain).
  - `GetDisplayValue` keeps values that are not in their domain, rather than losing them.
  - `ToFeature` converts to `Feature<Point>`.
- Datasets that are not listed: topology bookkeeping tables (`T_{id}_DirtyAreas`, `_PointErrors`, `_LineErrors`, `_PolyErrors`), attachment tables and their relationship classes, and raster datasets with their `fras_*` helper tables.

## What is read

- ArcGIS 10.x and ArcGIS Pro tables:
  - 32-bit and 64-bit OBJECTIDs
  - every field type, including the ArcGIS Pro 3.2 types: Int64, DateOnly, TimeOnly, DateTimeWithOffset
  - deleted rows and sparse row-offset blocks
- Geometries:
  - Point, Multipoint, Polyline (one part gives a `LineString`, several parts a `MultiLineString`) and Polygon.
  - Polygon rings are grouped into holes and polygons by the shared `Geometry<T>.CreatePolygonOrMultiPolygon`, with OGC orientation (outer ring counter-clockwise).
  - Z and M values are dropped.
  - True curves (arcs, Béziers, elliptic arcs) become straight segments between their end vertices, and the feature reports `HasCurves`.
- Coded-value lookups ignore the numeric storage type, so a `short` value finds an `xs:int` code.

## Not supported

- ArcGIS 9.x geodatabases. Upgrade them in ArcGIS first.
- Compressed tables (CDF/SDC). Uncompress them in ArcGIS first.
- Multipatch geometries (the dataset is listed; reading it throws).
- Spatial and attribute indexes (`.spx`, `.atx`) are not used.
- Writing.

## Format reference

The format is not published by Esri. This library follows the reverse-engineered description at
https://github.com/rouault/dump_gdbtable/wiki/FGDB-Spec. Where the wiki and GDAL's `OpenFileGDB`
driver disagree, it follows GDAL:
- the Z/M bits of the geometry-field flags
- when default-value bytes are present
