# 🗂️ Esri JSON Support in Maptor

![Esri JSON](https://img.shields.io/badge/Esri_JSON-ArcGIS-blue)
![.NET](https://img.shields.io/badge/.NET-Standard_2.1-green)

A .NET Standard implementation of the Esri JSON geometry/feature format used by the ArcGIS REST API. Read and write Esri JSON, and convert to/from the library's `Geometry<T>` and `FeatureSet<Point>` types.

## ✨ Features

- Geometry types: point (`esriGeometryPoint`), multipoint (`esriGeometryMultipoint`), polyline (`esriGeometryPolyline`), polygon (`esriGeometryPolygon`)
- Feature sets with fields, attributes, and spatial reference (`wkid` / `latestWkid`)
- Conversion both ways: Esri JSON ⇄ `FeatureSet<Point>` and `Geometry<T>` ⇄ `EsriJsonGeometry`
- Optional `Z` / `M` on geometry (features are read as `Feature<Point>`, i.e. flattened to X/Y)
- WKT output for a geometry via `AsWkt()`

## ⚙️ Installation

```bash
dotnet add package IRI.Maptor.Sta.Spatial
```

## 🚀 Getting Started

Types live in `IRI.Maptor.Sta.Spatial.IO.EsriJson`; the `Geometry<T>` extension lives in `IRI.Maptor.Extensions`.

### Feature Sets

```csharp
using IRI.Maptor.Sta.Spatial.IO.EsriJson;
using IRI.Maptor.Sta.Spatial.Primitives;

// Read
EsriJsonFeatureSet? esri = EsriJsonFeatureSet.Parse(jsonString);   // from a string
EsriJsonFeatureSet? fromFile = await EsriJsonFeatureSet.Load("features.json");

// Convert to the library type
FeatureSet<Point> featureSet = esri!.AsFeatureSet();

// Write
EsriJsonFeatureSet back = EsriJsonFeatureSet.Parse(featureSet)!;   // from a FeatureSet<Point>
await back.Save("out.json", indented: true);
```

### Geometry

```csharp
using IRI.Maptor.Sta.Spatial.IO.EsriJson;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;

// Esri JSON string → EsriJsonGeometry → library geometry
EsriJsonGeometry? esriGeom = EsriJsonGeometry.Parse(geometryJson);
IGeometry geometry = esriGeom!.Parse(srid: 4326);

// library geometry → EsriJsonGeometry → Esri JSON string
var polygon = Geometry<Point>.Create(points, GeometryType.Polygon, 4326);
EsriJsonGeometry esri = polygon.AsEsriJsonGeometry();
string json = esri.ToString();   // Esri JSON
string wkt  = esri.AsWkt();       // WKT
```

## 🌐 Spatial Reference

`EsriJsonSpatialReference` carries `Wkid`, `LatestWkid`, `VcsWkid`, and `LatestVcsWkid`. On read, the SRID is resolved as `LatestWkid ?? Wkid`; on write from a `FeatureSet<Point>`, `LatestWkid` is set from the feature set's SRID.

## 📋 Format Details

| Aspect | Esri JSON in Maptor |
|--------|---------------------|
| **Coordinate system** | Esri JSON carries an explicit `spatialReference` (`wkid` / `latestWkid`) and supports any CRS. Maptor preserves it; SRID resolves as `latestWkid ?? wkid`. `EsriJsonFeature.AsFeature(srid, targetSrs)` can optionally project to another SRS. |
| **Z / M** | The geometry model has `hasZ` / `hasM`; `EsriJsonGeometry.Parse(srid)` returns `Geometry<PointZM/PointZ/Point>` accordingly. Reading a full feature via `AsFeature` flattens to `Geometry<Point>` (X/Y). |
| **Polygon rings** | Esri's convention is the **opposite** of GeoJSON — exterior rings **clockwise**, holes **counterclockwise**, closed (first = last). The writer reverses `Geometry<Point>` winding to match Esri's order. |
| **Serialization** | System.Text.Json (camelCase). Deserialize: `EsriJsonGeometry.Parse`, `EsriJsonFeatureSet.Parse` / `Load`. Serialize: `EsriJsonGeometry.ToString`, `EsriJsonFeatureSet.Save`. |
| **Specification** | [ArcGIS REST API — Geometry objects](https://developers.arcgis.com/rest/services-reference/enterprise/geometry-objects/) |

## 📝 Notes & Limitations

- Polygon `Rings` and polyline `Paths` follow the Esri convention; multiple rings/paths become `MultiPolygon`/`MultiLineString`.
- `esriGeometryEnvelope` is defined in the type enum but is **not** converted (it throws).
- `EsriJsonFeatureSet.Save(...)` writes to a file; there is no method that returns the full feature-set JSON as a string. A single geometry's Esri JSON is available via `EsriJsonGeometry.ToString()`.
