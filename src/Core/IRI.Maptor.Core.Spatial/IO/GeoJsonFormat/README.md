# GeoJSON

A .NET Standard implementation of GeoJSON (RFC 7946) for spatial data interchange: read and write features, and convert to/from the library's `Geometry<T>` types.

## Supported capabilities

| Capability | Supported |
|---|---|
| Read | Yes — `GeoJson.DeserializeGeometry`, `GeoJson.LoadFromFile`, `GeoJsonFeatureSet.Parse` / `LoadAsync` |
| Write | Yes — `IGeoJsonGeometry.Serialize`, `GeoJson.SaveFeatures`, `GeoJsonFeatureSet.SaveAsync` |
| Z coordinates | Yes (3rd position element, per RFC 7946) |
| M coordinates | Yes, as a non-standard 4D extension (`Geometry<PointZM>`) |
| GeometryCollection | No — conversion throws |

Geometry types: `Point`, `MultiPoint`, `LineString`, `MultiLineString`, `Polygon`, `MultiPolygon`, plus `Feature` and `FeatureCollection`.

## Coordinate reference system

Per **RFC 7946 §4**, GeoJSON coordinates are WGS 84 (longitude, latitude in decimal degrees), equivalent to `urn:ogc:def:crs:OGC::CRS84`. An optional third position element is height in meters. Conversion methods take an `isLongitudeFirst` flag and an `srid` so you can adapt to non-default inputs.

### 4D coordinate support (extension)

This implementation supports 4D coordinates (`PointZM` with measure) as an **extension beyond RFC 7946**. While RFC 7946 Section 3.1.1 states that implementations SHOULD NOT extend position arrays beyond 3 elements, this library provides 4D support for compatibility with systems that require measure values. This is provided as extra functionality and is not part of the GeoJSON standard.

### Polygon rings (RFC 7946 section 3.1.6)

Rings **MUST** be closed — the first and last positions contain identical values:

```json
{
  "type": "Polygon",
  "coordinates": [
    [[30, 10], [40, 40], [20, 40], [10, 20], [30, 10]]
  ]
}
```

Rings **MUST** follow the right-hand rule: external rings **counterclockwise**, holes **clockwise**. Maptor's `Geometry<Point>` uses the same orientation invariant, and `GeoJsonPolygon` / `GeoJsonMultiPolygon` provide a validation method:

```csharp
var polygon = GeoJson.DeserializeGeometry(polygonJson) as GeoJsonPolygon;
var (isValid, errors) = polygon.ValidateRingOrientations();
if (!isValid)
{
    foreach (var error in errors)
        Console.WriteLine(error);
}
```

## Usage

All types live in `IRI.Maptor.Core.Spatial.GeoJsonFormat`; the geometry extension methods live in `IRI.Maptor.Extensions`.

### Reading

```csharp
using IRI.Maptor.Core.Spatial.GeoJsonFormat;

// Read features from a .geojson file
IEnumerable<GeoJsonFeature> features = GeoJson.LoadFromFile("path/to/file.geojson");

// Deserialize a single geometry from a string
IGeoJsonGeometry? point = GeoJson.DeserializeGeometry("{\"type\":\"Point\",\"coordinates\":[30,10]}");

// Parse a FeatureCollection from a string
GeoJsonFeatureSet featureSet = GeoJsonFeatureSet.Parse(featureCollectionJson);
```

### Writing

```csharp
// Write a list of features to a file
GeoJson.SaveFeatures("output.geojson", features);

// Or save a feature set (async)
var set = new GeoJsonFeatureSet { Features = features.ToList(), TotalFeatures = features.Count() };
await set.SaveAsync("output.geojson", indented: true);

// Serialize a single geometry to a string
string json = point.Serialize(indented: true);
```

### Converting GeoJSON to geometry

```csharp
using IRI.Maptor.Core.Spatial.Primitives;

IGeoJsonGeometry geoJson = GeoJson.DeserializeGeometry("{\"type\":\"Point\",\"coordinates\":[30,10]}");

// Returns Geometry<Point>, Geometry<PointZ>, or Geometry<PointZM> based on coordinate dimensions
IGeometry geometry = geoJson.Parse(isLongitudeFirst: true, srid: 4326);
```

### Converting geometry to GeoJSON

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Primitives;

var point = Geometry<Point>.Create(30, 10);
IGeoJsonGeometry geoJsonPoint = point.AsGeoJson(isLongitudeFirst: true);

string json = geoJsonPoint.Serialize(indented: true);
```

## Format details

| Aspect | GeoJSON in Maptor |
|--------|-------------------|
| **Coordinate system** | Spec mandates WGS 84 (`urn:ogc:def:crs:OGC::CRS84`), longitude/latitude in degrees. `Parse(isLongitudeFirst, srid)` lets you set axis order and SRID; Maptor does not reproject or enforce WGS 84. |
| **Z / M** | Z (elevation) is part of the spec (3rd position element). M is a non-standard extension surfaced as `Geometry<PointZM>`; it is not part of RFC 7946 output. |
| **Polygon rings** | Right-hand rule — exterior **CCW**, holes **CW** (same as `Geometry<Point>`). The writer repeats the first vertex as the last to close each ring. |
| **Serialization** | System.Text.Json. Deserialize: `GeoJson.DeserializeGeometry`, `GeoJsonFeatureSet.Parse`, `GeoJson.LoadFromFile`. Serialize: `IGeoJsonGeometry.Serialize`, `GeoJson.SaveFeatures`, `GeoJsonFeatureSet.SaveAsync`. |
| **Specification** | [GeoJSON — RFC 7946](https://datatracker.ietf.org/doc/html/rfc7946) |

## Limitations

- `AsGeoJson` covers `Point`/`MultiPoint`/`LineString`/`MultiLineString`/`Polygon`/`MultiPolygon`. `GeometryCollection` and curve geometries are not supported and throw.
- Measure (`M`) values are a 4D extension and are not part of standard RFC 7946 output.

---
[Back to IRI.Maptor.Core.Spatial](../../README.md)
