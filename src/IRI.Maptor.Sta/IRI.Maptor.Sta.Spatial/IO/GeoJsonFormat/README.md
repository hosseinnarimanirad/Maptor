# 🌍 GeoJson Support in Maptor

![GeoJson](https://img.shields.io/badge/GeoJson-RFC_7946_compliant-blue) 
![.NET](https://img.shields.io/badge/.NET-Standard_2.1-green)

A .NET Standard implementation of GeoJson (RFC 7946) for spatial data interchange, supporting read/write operations, conversion to/from geometry types, and validation.

![geo](https://github.com/user-attachments/assets/21ea02ee-f3a9-4fc7-bfe7-1f9c15977fd6)

## ✨ Features

- **Full GeoJSON Support**  
  ✅ RFC 7946 compliant  
  ✅ Geometry types: `Point`, `MultiPoint`, `LineString`, `MultiLineString`, `Polygon`, `MultiPolygon`  
  ✅ Feature collections with properties  
  ✅ Polygon ring orientation validation (RFC 7946 Section 3.1.6)
  
- **Conversion Tools**  
  🔄 GeoJSON ↔ Geometry  
  🔄 GeoJSON ↔ ESRI Shapefile

## 🌍 Coordinate Reference System

**Per RFC 7946 Section 4**, the coordinate reference system for all GeoJSON coordinates **MUST** be:
- **World Geodetic System 1984 (WGS 84)** [WGS84] datum
- Longitude and latitude units of decimal degrees
- Equivalent to OGC URN: `urn:ogc:def:crs:OGC::CRS84`

An OPTIONAL third-position element SHALL be the height in meters above or below the WGS 84 reference ellipsoid. In the absence of elevation values, applications sensitive to height or depth SHOULD interpret positions as being at local ground or sea level.

### 4D Coordinate Support (Extension)

This implementation supports 4D coordinates (`PointZM` with measure) as an **extension beyond RFC 7946**. While RFC 7946 Section 3.1.1 states that implementations SHOULD NOT extend position arrays beyond 3 elements, this library provides 4D support for compatibility with systems that require measure values. This is provided as extra functionality and is not part of the GeoJSON standard.

### Polygon Rings (RFC 7946 Section 3.1.6)

Polygon rings have specific requirements:

#### Ring Closure
**Per RFC 7946 Section 3.1.6**, polygon rings **MUST** be closed:
- The first and last positions are equivalent
- They **MUST** contain identical values
- Their representation **SHOULD** also be identical

Example of a valid closed ring:
```json
{
  "type": "Polygon",
  "coordinates": [
    [[30, 10], [40, 40], [20, 40], [10, 20], [30, 10]]
  ]
}
```
Note that the first coordinate `[30, 10]` is repeated as the last coordinate to close the ring.

#### Ring Orientation
Polygon rings **MUST** follow the right-hand rule:
- **External rings** (first ring): **counterclockwise** orientation
- **Internal rings** (holes): **clockwise** orientation

The library provides validation methods to check ring orientations:
```csharp
var polygon = GeoJson.Deserialize(polygonJson) as GeoJsonPolygon;
var (isValid, errors) = polygon.ValidateRingOrientations();
if (!isValid)
{
    foreach (var error in errors)
        Console.WriteLine(error);
}
```
  
## ⚙️ Installation
```bash
dotnet add package IRI.Maptor.Sta.Spatial
```

## 🚀 Getting Started

### Reading GeoJSON

#### Read from File
```csharp
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;

// Read features from a GeoJSON file
var features = GeoJson.ReadFeatures("path/to/file.geojson");

foreach (var feature in features)
{
    Console.WriteLine($"Feature Type: {feature.Type}");
    Console.WriteLine($"Geometry Type: {feature.Geometry?.Type}");
}
```

#### Deserialize from String
```csharp
// Deserialize a GeoJSON geometry string
var pointJson = "{\"type\": \"Point\", \"coordinates\": [30, 10]}";
IGeoJsonGeometry point = GeoJson.Deserialize(pointJson);

var multiPointJson = "{\"type\": \"MultiPoint\", \"coordinates\": [[10.1, 40.1], [40.1, 30.1], [20.1, 20.1], [30.1, 10.1]]}";
IGeoJsonGeometry multiPoint = GeoJson.Deserialize(multiPointJson);

var lineStringJson = "{\"type\": \"LineString\", \"coordinates\": [[30.1, 10.1], [10.1, 30.1], [40.1, 40.1]]}";
IGeoJsonGeometry lineString = GeoJson.Deserialize(lineStringJson);

// Parse FeatureCollection
var featureCollectionJson = "{\"type\": \"FeatureCollection\", \"features\": [...]}";
var featureSet = GeoJsonFeatureSet.Parse(featureCollectionJson);
var features = featureSet.Features;
```

### Writing GeoJSON

#### Write to File
```csharp
// Save features to a GeoJSON file
var features = new List<GeoJsonFeature> { /* ... */ };
GeoJson.SaveFeatures("output.geojson", features);

// Or use FeatureSet
var featureSet = new GeoJsonFeatureSet
{
    Features = features,
    TotalFeatures = features.Count
};
featureSet.Save("output.geojson", indented: true);
```

#### Serialize to String
```csharp
// Serialize geometry to JSON string
var geoJsonGeometry = GeoJson.Deserialize(pointJson);
string jsonString = geoJsonGeometry.Serialize(indented: true);

// Or serialize without indentation
string compactJson = geoJsonGeometry.Serialize(indented: false);
```

### Converting GeoJSON to IGeometry

```csharp
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

// Parse GeoJSON geometry to IGeometry
var geoJsonPoint = GeoJson.Deserialize("{\"type\": \"Point\", \"coordinates\": [30, 10]}");
IGeometry geometry = geoJsonPoint.Parse(isLongitudeFirst: true, srid: 4326);

// The returned geometry type depends on coordinate dimensions:
// - 2D coordinates → Geometry<Point>
// - 3D coordinates → Geometry<PointZ>
// - 4D coordinates → Geometry<PointZM>

// Access geometry properties
if (geometry is Geometry<Point> pointGeometry)
{
    var points = pointGeometry.Points;
    Console.WriteLine($"Number of points: {points.Count}");
}
```

### Converting IGeometry to GeoJSON

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

// Convert Geometry to GeoJSON
var point = Geometry<Point>.Create(30, 10);
IGeoJsonGeometry geoJsonPoint = point.AsGeoJson(isLongitudeFirst: true);

// Serialize to JSON string
string json = geoJsonPoint.Serialize(indented: true);

// Convert other geometry types
var lineString = new Geometry<Point>(
    new List<Point> { new Point(30, 10), new Point(40, 20) },
    GeometryType.LineString,
    srid: 4326
);
IGeoJsonGeometry geoJsonLineString = lineString.AsGeoJson();

// Convert with 3D coordinates
var pointZ = new Geometry<PointZ>(
    new List<PointZ> { new PointZ { X = 30, Y = 10, Z = 100 } },
    GeometryType.Point,
    srid: 4326
);
IGeoJsonGeometry geoJsonPointZ = pointZ.AsGeoJson();
```

### Complete Example: Round-Trip Conversion

```csharp
// 1. Start with GeoJSON string
var geoJsonString = "{\"type\": \"Polygon\", \"coordinates\": [[[30, 10], [40, 40], [20, 40], [10, 20], [30, 10]]]}";

// 2. Deserialize to IGeoJsonGeometry
IGeoJsonGeometry geoJson = GeoJson.Deserialize(geoJsonString);

// 3. Validate ring orientations (RFC 7946 compliance)
if (geoJson is GeoJsonPolygon polygon)
{
    var (isValid, errors) = polygon.ValidateRingOrientations();
    if (!isValid)
    {
        Console.WriteLine("Ring orientation validation failed:");
        foreach (var error in errors)
            Console.WriteLine($"  - {error}");
    }
}

// 4. Convert to IGeometry
IGeometry geometry = geoJson.Parse(isLongitudeFirst: true, srid: 4326);

// 5. Convert back to GeoJSON
IGeoJsonGeometry geoJsonRoundTrip = geometry.AsGeoJson(isLongitudeFirst: true);

// 6. Serialize back to string
string roundTripJson = geoJsonRoundTrip.Serialize(indented: true);
Console.WriteLine(roundTripJson);
```

## 📚 Documentation
- [GeoJSON RFC 7946 Specification](https://datatracker.ietf.org/doc/html/rfc7946)
 
