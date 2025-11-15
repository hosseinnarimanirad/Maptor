# KMZ Support for Maptor

Complete implementation of KMZ (compressed KML) import/export capabilities for the Maptor GIS library.

## Overview

KMZ files are ZIP archives containing KML files (typically named "doc.kml") and optionally embedded resources such as images. This module provides comprehensive KMZ support by leveraging the existing KML functionality and ZIP handling utilities.

## Features

✅ **Full KMZ Support**: Read and write KMZ files (ZIP archives containing KML)  
✅ **KML Integration**: Leverages existing `KmlReader` and `KmlWriter` functionality  
✅ **Resource Management**: Add and extract embedded image resources  
✅ **Async Support**: Async file I/O operations  
✅ **Stream Support**: Read from streams in addition to files  
✅ **Resource Extraction**: Extract individual resources from KMZ archives  

## Quick Start

### Reading KMZ

```csharp
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Read KMZ file
var geometries = KmzReader.ReadFromFile("map.kmz", targetSrid: 4326);

// Read with features and attributes
var features = KmzReader.ReadFeaturesFromFile("map.kmz");
foreach (var feature in features)
{
    Console.WriteLine($"Name: {feature.Name}");
    Console.WriteLine($"Description: {feature.Description}");
    Console.WriteLine($"Geometry Type: {feature.Geometry.Type}");
}

// Read asynchronously
var geometriesAsync = await KmzReader.ReadFromFileAsync("map.kmz");

// Read from stream
using (var stream = File.OpenRead("map.kmz"))
{
    var geometriesFromStream = KmzReader.Parse(stream);
}
```

### Writing KMZ

```csharp
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Create a point
var point = new Point(51.5074, -0.1278); // London
var geometry = new Geometry<Point>(
    new List<Point> { point }, 
    GeometryType.Point, 
    srid: 4326);

// Write to KMZ file
KmzWriter.WriteToFile(
    geometry, 
    "london.kmz", 
    name: "London", 
    description: "Capital of England");

// Write multiple geometries
var geometries = new List<Geometry<Point>> { /* ... */ };
KmzWriter.WriteToFile(geometries, "places.kmz", "My Places");

// Write features
var features = new List<KmlFeature> { /* ... */ };
KmzWriter.WriteToFile(features, "features.kmz", "My Features");

// Write asynchronously
await KmzWriter.WriteToFileAsync(geometries, "places.kmz", "My Places");
```

### Working with Resources

```csharp
using IRI.Maptor.Ket.KmlFormat;

// Add image resource to KMZ
var imageBytes = File.ReadAllBytes("icon.png");
KmzWriter.AddResource("output.kmz", "images/icon.png", imageBytes);

// Add resource from file
KmzWriter.AddResourceFromFile("output.kmz", "images/icon.png", "icon.png");

// Add resource from stream
using (var imageStream = File.OpenRead("icon.png"))
{
    KmzWriter.AddResource("output.kmz", "images/icon.png", imageStream);
}

// Get list of resources in KMZ
var resources = KmzReader.GetResourceFiles("map.kmz");
foreach (var resource in resources)
{
    Console.WriteLine($"Resource: {resource}");
}

// Extract a resource
var iconBytes = KmzReader.ExtractResource("map.kmz", "images/icon.png");
if (iconBytes != null)
{
    File.WriteAllBytes("extracted_icon.png", iconBytes);
}
```

### Using Extension Methods

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Convert geometry to KMZ
var geometry = Geometry<Point>.Create(51.5074, -0.1278, srid: 4326);
geometry.SaveAsKmz("london.kmz", "London", "Capital of England");

// Async save
await geometry.SaveAsKmzAsync("london.kmz", "London");

// Multiple geometries
var geometries = new List<Geometry<Point>> { /* ... */ };
geometries.SaveAsKmz("places.kmz", "My Places");

// Features
var features = new List<KmlFeature> { /* ... */ };
features.SaveAsKmz("features.kmz", "My Features");
```

## KMZ File Structure

KMZ files follow this structure:

```
archive.kmz
├── doc.kml          (main KML file, typically named "doc.kml")
├── images/
│   ├── icon1.png
│   └── icon2.jpg
└── other/
    └── resource.txt
```

- The main KML file is typically named "doc.kml" but can be any `.kml` file
- Resources are stored with relative paths that can be referenced in the KML
- The reader will prefer "doc.kml" if present, otherwise uses the first `.kml` file found

## API Reference

### KmzReader

#### Read Methods

- `ReadFromFile(string kmzFilePath, int targetSrid = 4326)` - Read geometries from KMZ file
- `ReadFromFileAsync(string kmzFilePath, int targetSrid = 4326)` - Read geometries asynchronously
- `ReadFeaturesFromFile(string kmzFilePath, int targetSrid = 4326)` - Read features with attributes
- `ReadFeaturesFromFileAsync(string kmzFilePath, int targetSrid = 4326)` - Read features asynchronously
- `Parse(Stream kmzStream, int targetSrid = 4326)` - Parse KMZ from stream

#### Resource Methods

- `GetResourceFiles(string kmzFilePath)` - Get list of all resource files
- `ExtractResource(string kmzFilePath, string resourcePath)` - Extract a resource as byte array

### KmzWriter

#### Write Methods

- `WriteToFile(Geometry<Point> geometry, string kmzFilePath, ...)` - Write single geometry
- `WriteToFile(List<Geometry<Point>> geometries, string kmzFilePath, ...)` - Write multiple geometries
- `WriteToFile(List<KmlFeature> features, string kmzFilePath, ...)` - Write features
- `WriteToFileAsync(...)` - Async write methods

#### Resource Methods

- `AddResource(string kmzFilePath, string resourcePath, byte[] resourceData)` - Add resource from byte array
- `AddResource(string kmzFilePath, string resourcePath, Stream resourceStream)` - Add resource from stream
- `AddResourceFromFile(string kmzFilePath, string resourcePath, string sourceFilePath)` - Add resource from file

## Best Practices

1. **Always use WGS84 (SRID 4326)** for KMZ files - it's the KML standard
2. **Use "doc.kml" as the KML filename** for better compatibility
3. **Use relative paths for resources** in KML hrefs (e.g., "images/icon.png")
4. **Organize resources in folders** (e.g., "images/", "models/") for better structure
5. **Use async methods** for large files or when UI responsiveness is important
6. **Project coordinates** if your data is not in WGS84

## Resource Handling

### Supported Resources

- ✅ **Images**: PNG, JPG, GIF, and other image formats
- ❌ **3D Models**: Not supported in initial implementation (future enhancement)

### Resource Paths

When adding resources to KMZ files:
- Use forward slashes (`/`) in paths (e.g., "images/icon.png")
- Avoid absolute paths or drive letters
- Keep paths relative to the archive root
- In KML, reference resources using relative paths (e.g., `<href>images/icon.png</href>`)

## Error Handling

```csharp
try
{
    var geometries = KmzReader.ReadFromFile("map.kmz");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"File not found: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Invalid KMZ: {ex.Message}");
}
catch (InvalidDataException ex)
{
    Console.WriteLine($"Invalid ZIP format: {ex.Message}");
}
```

## Performance Tips

- Use `ReadFromFileAsync` for large files
- Use `WriteToFileAsync` for better responsiveness
- Extract resources only when needed (don't extract all resources upfront)
- Use streams when working with large resources

## Standards Compliance

This implementation follows:
- OGC KML 2.2 Specification (via KmlReader/KmlWriter)
- ZIP file format (RFC 1950, RFC 1951)
- WGS84 coordinate system (EPSG:4326)

## Relationship to KML

KMZ support builds on top of the existing KML functionality:

- **KmzReader** uses `KmlReader.Parse()` to parse extracted KML content
- **KmzWriter** uses `KmlWriter.ToKml()` to generate KML content before archiving
- All KML features (geometries, features, styles, etc.) are fully supported in KMZ

## Examples

### Complete Example: Create KMZ with Image

```csharp
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Create geometry
var point = new Point(51.5074, -0.1278);
var geometry = new Geometry<Point>(
    new List<Point> { point },
    GeometryType.Point,
    srid: 4326);

// Write to KMZ
KmzWriter.WriteToFile(geometry, "london.kmz", "London", "Capital of England");

// Add icon image
var iconBytes = File.ReadAllBytes("london_icon.png");
KmzWriter.AddResource("london.kmz", "images/london_icon.png", iconBytes);

// Read back
var features = KmzReader.ReadFeaturesFromFile("london.kmz");
var icon = KmzReader.ExtractResource("london.kmz", "images/london_icon.png");
```

### Round-trip Example

```csharp
// Write KMZ
var geometries = new List<Geometry<Point>> { /* ... */ };
KmzWriter.WriteToFile(geometries, "output.kmz", "My Places");

// Read KMZ
var readGeometries = KmzReader.ReadFromFile("output.kmz");

// Verify
Console.WriteLine($"Original: {geometries.Count} geometries");
Console.WriteLine($"Read: {readGeometries.Count} geometries");
```

## Related Classes

- `IRI.Maptor.Ket.KmlFormat.KmlReader` - KML file reading
- `IRI.Maptor.Ket.KmlFormat.KmlWriter` - KML file writing
- `IRI.Maptor.Sta.Common.Helpers.ZipFileHelper` - ZIP archive utilities
- `IRI.Maptor.Sta.Spatial.Primitives.Geometry<T>` - Core geometry class
- `IRI.Maptor.Sta.Common.Primitives.Point` - Point primitive

## License

MIT License - See LICENSE.txt in the repository root





