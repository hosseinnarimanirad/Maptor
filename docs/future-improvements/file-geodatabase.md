# File Geodatabase — reader status and deferred work

- **Status (2026-10-13):** read-only library `src/Core/IRI.Maptor.Core.FileGdb` built in four steps:
  table engine, geometry decoding, catalog (domains, subtypes, relationship classes, feature
  datasets) and the public API (`FileGeodatabase`). Step 5 added File GDB as a source in
  `IRI.Maptor.Tools.SpatialDataManagement`'s "Transfer Geodatabase" window (`.gdb...` button,
  `Common/Sources/FileGdbSource`), SQL Server only. Coded values and the subtype field are written
  as names; Z/M dropped and straightened curves are reported to the user. PostGIS as a destination
  for this window is not done yet.
- **Approach:** written from scratch in C#, with no GDAL and no Esri FileGDB API (user decision).
  Format reference: https://github.com/rouault/dump_gdbtable/wiki/FGDB-Spec. Where it and GDAL's
  `OpenFileGDB` driver disagree, GDAL wins.

## Using it

```csharp
var gdb = FileGeodatabase.Open(@"D:\data\DataModel.gdb");

foreach (var dataset in gdb.Datasets)          // topology, attachment and raster tables are left out
{
    if (dataset.Storage != GdbTableStorage.Table)
        continue;                              // compressed or missing: listed, not readable

    foreach (var feature in dataset.ReadFeatures(new FileGdbReadOptions { OnRowError = e => log(e) }))
    {
        Feature<Point> f = feature.ToFeature(resolveCodedValues: true);   // subtype-aware names
    }
}
```

## Decisions already made (do not reopen casually)

| Topic | Decision |
|---|---|
| Write support | Not planned; the library is read-only |
| Curves (arcs, Béziers, elliptic arcs) | Straight segment between end vertices; `FileGdbFeature.HasCurves` flags it |
| Z and M | Not read; `FileGdbDataset.HasZ` / `HasM` tell the caller they were dropped |
| Multipatch | Listed; `ReadFeatures` throws `NotSupportedException` |
| Topology, attachment and raster helper tables | Left out of the catalog entirely |
| Entry class name | `FileGeodatabase`, not `FileGdb`: a class named like the `…FileGdb` namespace breaks name lookup in any namespace that has a `FileGdb` segment (the tests already do) |
| Public model type names | `FileGdb` prefix (`FileGdbCodedValueDomain`, …) because the Personal GDB library already has public `GdbCodedValue` / `GdbCodedValueDomain` |
| Feature type | Own `FileGdbFeature` (64-bit OBJECTID, curve flag, subtype) plus `ToFeature()`; `Feature<Point>.Id` is 0 when the OBJECTID does not fit in an `int` |
| Values outside their domain | Kept as stored, never replaced by null |

## Deferred

1. **Curves as true curves.** The curve descriptions follow the XY array in the shape blob and are
   skipped today. Densify arcs and Béziers into vertices, or carry them as curves if `Geometry<T>`
   ever supports them.
2. **Multipatch.** Needs a 3D/TIN representation Maptor does not have.
3. **Spatial index (`.spx`).** `FileGdbReadOptions.BoundingBox` reads every row and tests envelopes.
   Reading the `.spx` grid index would make small-window queries on big layers fast.
4. **`FileGdbDataSource` for the map stack.** A `VectorDataSource` over `FileGdbDataset`,
   so a `.gdb` layer can be shown directly like a shapefile, plus wiring in `DataSourceType`.
5. **Attribute indexes (`.atx`).** Not used; every lookup is a scan.
6. **Compressed (CDF/SDC) tables.** Undocumented compression; users must uncompress in ArcGIS.
7. **Relationship joins.** Relationship classes are metadata only; rows are not joined.
8. **SRID guessing from WKT.** When the definition has no WKID, the SRID comes from
   `EsriPrjFile.Parse(wkt).Srid`, which treats any UTM-style system as WGS 84 UTM. Left as is
   (user decision 2026-10-13). Details and options are in [`esri-prj-utm-datum.md`](esri-prj-utm-datum.md).

## Test data

- Unit tests build synthetic geodatabases (`tests/IRI.Maptor.Tests/FileGdb/GdbTestTableBuilder.cs`,
  `GdbTestGeodatabase.cs`).
- `FileGdbSampleTest` reads two real geodatabases from a local folder and skips when they are
  absent.
- GDAL's `autotest/ogr/data/filegdb` samples were used as one-off checks and are deliberately not
  in the repository.
