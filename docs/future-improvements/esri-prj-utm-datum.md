# `EsriPrjFile` guesses WGS 84 UTM codes for any datum

- **Status:** known and left as is (user decision 2026-10-13). Not fixed.
- **Area:** `src/Core/IRI.Maptor.Core.Spatial/IO/Prj/EsriPrjFile.cs`, `GetCrsSrid` / `TryDetectUtm`
- **Found by:** the File Geodatabase reader, on GDAL's `weird_winding_order_fgdb` sample (see
  [`file-geodatabase.md`](file-geodatabase.md))
- **Risk of fixing:** medium. The class is shared by shapefile, Personal GDB, DXF and File GDB
  reading, so the fix changes the SRID that some existing files report.

## The problem

When an Esri WKT has no `AUTHORITY["EPSG",n]` node, `GetCrsSrid` tries to guess the SRID. For a
`Transverse_Mercator` projection, `TryDetectUtm` checks only the projection parameters:

- scale factor 0.9996
- false easting 500000
- a central meridian that maps to a zone from 1 to 60
- false northing 0 (north) or 10000000 (south)

When these match, it returns `32600 + zone` or `32700 + zone`, which are the **WGS 84** UTM codes.
The datum and spheroid are never checked, so any UTM-style system on another datum gets a WGS 84 code:

| WKT name | Correct EPSG | `EsriPrjFile.Srid` today |
|---|---|---|
| `GDA94_/_MGA_zone_54` (GRS 1980, GDA94) | 28354 | 32754 |
| `ED_1950_UTM_Zone_39N` (International 1924, ED50) | 23039 | 32639 |
| `NAD_1983_UTM_Zone_10N` (GRS 1980, NAD83) | 26910 | 32610 |

A wrong code is worse than 0 ("unknown"). With 0, the caller knows the system is unknown. With a
wrong code, the data is silently labelled with the wrong datum, which can shift it by metres up to
hundreds of metres, depending on the datum.

## Who is affected

`EsriPrjFile.Parse(...)` / `new EsriPrjFile(...)` is called from:

- `Core.ShapefileFormat/Shapefile.cs`: reading a shapefile's `.prj`, and `AsMapProjection()` for
  reprojection.
- `Infrastructure.PersonalGdb/PersonalGdbInfrastructure.cs`: the Personal GDB spatial reference.
- `Core.Spatial/IO/Dxf/DxfReader.cs`.
- `Core.FileGdb/Catalog/GdbCatalog.cs`: only when the item definition has no usable `WKID` or
  `LatestWKID`. ArcGIS almost always writes one, so real File GDBs are rarely affected.

Only WKTs **without** an EPSG authority node take the guessing path. ArcGIS `.prj` files usually
have no authority node, so shapefiles are the most exposed case.

## Options (when this is picked up)

1. **Fix `TryDetectUtm`:** return 326xx/327xx only when the datum is WGS 84 (`D_WGS_1984` /
   spheroid `WGS_1984`), and 0 otherwise. This is small and safe.
   - Behaviour change: ED50/NAD83/GDA94 UTM files go from a wrong code to 0. Check whether any
     caller (Saba, SpatialDataManagement import, reprojection in `Shapefile`) relies on a non-zero
     SRID and would now fail or ask the user.
2. **Option 1 plus datum-specific tables:** also map well-known datums to their own UTM series, for
   example ED50 → 23028–23038, NAD83 → 26901–26923, GDA94 → 28348–28358, GDA2020 → 7846–7859, and
   Iran's datums where EPSG has codes. This gives more correct codes, but each table must be checked
   against the EPSG registry.
3. **Fix only in File GDB:** `GdbCatalog.GetSridFromWkt` trusts only an explicit authority node and
   otherwise returns 0. This leaves the shared behaviour alone, but it needs `EsriPrjFile` to expose
   the authority-only SRID (today `GetSridFromAuthorityNode` is private).

For whichever option is chosen, add cases to `tests/IRI.Maptor.Tests/Esri/PrjFileTest.cs` for:

- WGS 84 UTM north and south, which must keep their codes;
- ED50, NAD83 and GDA94 UTM, which must not get 326xx/327xx;
- a WKT with an authority node, which must keep using it.

## Repro

`GDA94_/_MGA_zone_54` from GDAL's test data (`autotest/ogr/data/filegdb/weird_winding_order_fgdb.zip`,
dataset `roads_clip`, stored with `WKID=0`):

```csharp
var gdb = FileGeodatabase.Open(@"...\roads_clip Drawing.gdb");
gdb.GetDataset("roads_clip")!.Srid;   // 32754, should be 28354
```
