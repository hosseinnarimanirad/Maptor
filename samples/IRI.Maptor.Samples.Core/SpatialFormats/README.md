# GeoJSON vs Shapefile

`formats/geojson-vs-shapefile` — [GeoJsonVsShapefile.cs](GeoJsonVsShapefile.cs)

Writes the same polygons as a GeoJSON file and as a Shapefile (`.shp` + `.shx`) and prints the
sizes. The answer depends on the coordinates, so the sample uses two generated data sets:

- a grid of 10,000 squares whose coordinates have at most two decimals;
- 2,000 circle-like polygons whose coordinates use the full `double` precision.

```csharp
// GeoJSON: geometry -> IGeoJsonGeometry, then serialize
File.WriteAllText(geoJsonPath, JsonHelper.Serialize(polygons.Select(p => p.AsGeoJson()).ToList()));

// Shapefile: geometry -> Esri shape, then save (.dbf omitted — no attributes here)
var esriShapes = polygons.Select(p => p.AsEsriShape()).OfType<EsriShapeBase>().ToList();
Shapefile.Save(shapefilePath, esriShapes, createDbf: false, overwrite: true);
```

Typical output:

| data set | GeoJSON | Shapefile | bytes per vertex (GeoJSON / Shapefile) |
|---|---|---|---|
| grid, short coordinates | 1.24 MB | 1.44 MB | 31 / 36 |
| circles, full precision | 2.01 MB | 0.93 MB | 42 / 19 |

A Shapefile spends a fixed 16 bytes per vertex (two doubles) plus small record headers; GeoJSON
spends one character per digit. Short coordinates favour text, real-world coordinates favour the
binary format — and both lose to a compressed or tiled format for large data.

Output goes to `%TEMP%\maptor-samples\geojson-vs-shapefile`.

## How to run

```bash
dotnet run --project samples/IRI.Maptor.Samples.Core -- formats/geojson-vs-shapefile
```

See also: [GeoJSON](../../../src/Core/IRI.Maptor.Core.Spatial/IO/GeoJsonFormat/README.md) ·
[IRI.Maptor.Core.ShapefileFormat](../../../src/Core/IRI.Maptor.Core.ShapefileFormat/README.md).

---
[Back to IRI.Maptor.Samples.Core](../README.md)
