# KML Support for Maptor

Complete implementation of KML 2.2 (Keyhole Markup Language) import/export capabilities for the Maptor GIS library.

## Overview

This module provides comprehensive KML support including:
- **KmlReader**: Parse and read KML files
- **KmlWriter**: Export geometries to KML format
- **KmlStyleBuilder**: Build complex KML styles with fluent API
- **KmlValidator**: Validate KML content against KML 2.2 specification
- **KmlDecorator**: Decorate placemarks with extended data and styles
- **Extension Methods**: Easy-to-use extensions for `Geometry<Point>`

## Features

✅ **Full KML 2.2 Support**: Complete implementation of OGC KML 2.2 specification  
✅ **Import & Export**: Read from and write to KML files  
✅ **All Geometry Types**: Point, LineString, Polygon, MultiGeometry  
✅ **Extended Data**: Support for custom attributes  
✅ **Styling**: Icon, Line, Polygon, Label, and Balloon styles  
✅ **Validation**: Comprehensive KML validation with detailed error reporting  
✅ **Async Support**: Async file I/O operations  
✅ **Coordinate Projection**: Built-in support for coordinate transformation  

## Quick Start

### Reading KML

```csharp
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Read KML file
var geometries = KmlReader.ReadFromFile("map.kml", targetSrid: 4326);

// Read with features and attributes
var features = KmlReader.ReadFeaturesFromFile("map.kml");
foreach (var feature in features)
{
    Console.WriteLine($"Name: {feature.Name}");
    Console.WriteLine($"Description: {feature.Description}");
    Console.WriteLine($"Geometry Type: {feature.Geometry.Type}");
    
    foreach (var attr in feature.Attributes)
    {
        Console.WriteLine($"  {attr.Key}: {attr.Value}");
    }
}

// Parse KML string
string kmlString = "<?xml version=\"1.0\"?>...";
var parsed = KmlReader.Parse(kmlString);
```

### Writing KML

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

// Write to file
KmlWriter.WriteToFile(
    geometry, 
    "london.kml", 
    name: "London", 
    description: "Capital of England");

// Convert to KML string
string kml = KmlWriter.ToKml(geometry, "London", "Capital of England");

// Write multiple geometries
var geometries = new List<Geometry<Point>> { /* ... */ };
KmlWriter.WriteToFile(geometries, "places.kml", "My Places");
```

### Using Extension Methods

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

// Convert geometry to KML
var geometry = Geometry<Point>.Create(51.5074, -0.1278, srid: 4326);
string kml = geometry.AsKml(name: "London", description: "Capital of England");

// Save directly to file
geometry.SaveAsKml("london.kml", "London", "Capital of England");

// Async save
await geometry.SaveAsKmlAsync("london.kml", "London");

// Multiple geometries
var geometries = new List<Geometry<Point>> { /* ... */ };
string multiKml = geometries.AsKml(documentName: "My Places");
geometries.SaveAsKml("places.kml", "My Places");
```

### Styling with KmlStyleBuilder

```csharp
using IRI.Maptor.Ket.KmlFormat;

// Create a point style
var pointStyle = new KmlStyleBuilder()
    .WithIconStyle(
        iconHref: "http://maps.google.com/mapfiles/kml/pushpin/red-pushpin.png",
        scale: 1.2)
    .WithLabelStyle(
        red: 255, green: 0, blue: 0, 
        scale: 1.1)
    .Build();

// Create a line style
var lineStyle = new KmlStyleBuilder()
    .WithLineStyle(
        red: 0, green: 0, blue: 255, 
        alpha: 255, 
        width: 3.0)
    .Build();

// Create a polygon style
var polygonStyle = new KmlStyleBuilder()
    .WithPolyStyle(
        red: 0, green: 255, blue: 0, 
        alpha: 128, // Semi-transparent
        fill: true, 
        outline: true)
    .WithLineStyle(
        red: 0, green: 128, blue: 0, 
        width: 2.0)
    .Build();

// Use predefined styles
var defaultPoint = KmlStyleBuilder.CreateDefaultPointStyle();
var defaultLine = KmlStyleBuilder.CreateDefaultLineStyle();
var defaultPolygon = KmlStyleBuilder.CreateDefaultPolygonStyle();

// Create color from hex
var color = KmlStyleBuilder.CreateKmlColorFromHex("#FF0000"); // Red
var colorWithAlpha = KmlStyleBuilder.CreateKmlColorFromHex("#80FF0000"); // Semi-transparent red
```

### Decorating Placemarks

```csharp
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Ket.KmlFormat.Primitives;

// Create placemarks
var placemarks = new List<PlacemarkType> { /* ... */ };

// Decorate with icon style
string styledKml = KmlDecorator.DecorateWithIconStyle(
    placemarks,
    iconHref: "http://maps.google.com/mapfiles/kml/pushpin/ylw-pushpin.png",
    scale: 1.2);

// Decorate with line style
var lineColor = KmlStyleBuilder.CreateKmlColor(255, 0, 0, 255); // Red
string lineKml = KmlDecorator.DecorateWithLineStyle(placemarks, lineColor, width: 3.0);

// Decorate with extended data
var attributes = new List<MyDataType> { /* ... */ };
var attributeNames = new List<string> { "Name", "Population", "Area" };
var extractFuncs = new List<Func<MyDataType, string>> 
{
    data => data.Name,
    data => data.Population.ToString(),
    data => data.Area.ToString()
};

string kmlWithData = KmlDecorator.DecorateWithExtendedData(
    placemarks, 
    attributes, 
    attributeNames, 
    extractFuncs);

// Combine data and style
var styleBuilder = new KmlStyleBuilder().WithIconStyle("...", 1.0);
string combined = KmlDecorator.DecorateWithDataAndStyle(
    placemarks, 
    attributes, 
    attributeNames, 
    extractFuncs, 
    styleBuilder);
```

### Validating KML

```csharp
using IRI.Maptor.Ket.KmlFormat;

// Validate KML file
bool isValid = KmlValidator.ValidateFile("map.kml", out var errors, out var warnings);

if (!isValid)
{
    Console.WriteLine("Validation Errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error}");
    }
}

if (warnings.Count > 0)
{
    Console.WriteLine("Validation Warnings:");
    foreach (var warning in warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}

// Validate KML string
string kmlContent = File.ReadAllText("map.kml");
bool valid = KmlValidator.Validate(kmlContent, out errors, out warnings);

// Quick validation
bool isValidQuick = KmlValidator.IsValid(kmlContent);

// Generate validation report
string report = KmlValidator.GenerateValidationReport(kmlContent);
Console.WriteLine(report);

// Validate coordinates
bool coordsValid = KmlValidator.ValidateCoordinates(longitude: 51.5074, latitude: -0.1278);
bool coordStringValid = KmlValidator.ValidateCoordinateString("51.5074,-0.1278", out string error);
```

### Working with Features

```csharp
using IRI.Maptor.Ket.KmlFormat;

// Create a feature with attributes
var feature = new KmlFeature
{
    Geometry = geometry,
    Name = "London",
    Description = "Capital of England",
    Attributes = new Dictionary<string, string>
    {
        ["Population"] = "9000000",
        ["Country"] = "UK",
        ["Founded"] = "43 AD"
    }
};

// Convert to KML
string kml = feature.AsKml();

// Write multiple features
var features = new List<KmlFeature> { feature1, feature2, feature3 };
KmlWriter.WriteToFile(features, "cities.kml", "World Cities");
```

### Coordinate Projection

```csharp
using IRI.Maptor.Sta.SpatialReferenceSystem;

// Define projection function to convert to WGS84
Func<Point, Point> projectToWgs84 = (point) =>
{
    // Your projection logic here
    // For example, using MapProjects:
    return MapProjects.MapToGeodetic(point, sourceSrid);
};

// Use projection when writing
var geometry = new Geometry<Point>(points, GeometryType.LineString, srid: 3857);
KmlWriter.WriteToFile(
    geometry, 
    "path.kml", 
    name: "My Path",
    projectToGeodeticFunc: projectToWgs84);

// Or with extension methods
geometry.SaveAsKml("path.kml", "My Path", projectToGeodeticFunc: projectToWgs84);
```

### Advanced: Folders and Organization

```csharp
using IRI.Maptor.Ket.KmlFormat;

// Organize geometries into folders
var folders = new Dictionary<string, List<Geometry<Point>>>
{
    ["Cities"] = cityGeometries,
    ["Rivers"] = riverGeometries,
    ["Boundaries"] = boundaryGeometries
};

string kmlWithFolders = KmlWriter.ToKmlWithFolders(folders, "Geographic Data");
```

### Advanced: Shared Styles

```csharp
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Ket.KmlFormat.Primitives;

// Create shared styles
var sharedStyles = new Dictionary<string, StyleType>
{
    ["cityStyle"] = new KmlStyleBuilder()
        .WithIconStyle("http://maps.google.com/mapfiles/kml/pushpin/red-pushpin.png", 1.0)
        .WithId("cityStyle")
        .Build(),
    
    ["riverStyle"] = new KmlStyleBuilder()
        .WithLineStyle(red: 0, green: 0, blue: 255, width: 2.0)
        .WithId("riverStyle")
        .Build()
};

// Assign styles to placemarks
string kml = KmlDecorator.DecorateWithSharedStyles(
    placemarks,
    sharedStyles,
    (placemark, index) => placemark.name.Contains("City") ? "cityStyle" : "riverStyle"
);
```

## KML Color Format

KML uses a unique color format: `aabbggrr` (alpha, blue, green, red) in hexadecimal.

```csharp
// Create colors
var red = KmlStyleBuilder.CreateKmlColor(255, 0, 0, 255);
var semiTransparentGreen = KmlStyleBuilder.CreateKmlColor(0, 255, 0, 128);
var blue = KmlStyleBuilder.CreateKmlColorFromHex("#0000FF");
var semiTransparentYellow = KmlStyleBuilder.CreateKmlColorFromHex("#80FFFF00");
```

## Geometry Type Support

| Geometry Type | Import | Export | Styling |
|---------------|--------|--------|---------|
| Point | ✅ | ✅ | Icon, Label |
| LineString | ✅ | ✅ | Line |
| Polygon | ✅ | ✅ | Poly, Line |
| MultiPoint | ✅ | ✅ | Icon, Label |
| MultiLineString | ✅ | ✅ | Line |
| MultiPolygon | ✅ | ✅ | Poly, Line |
| GeometryCollection | ✅ | ✅ | Mixed |

## Best Practices

1. **Always use WGS84 (SRID 4326)** for KML files - it's the KML standard
2. **Validate before exporting** to ensure compliance
3. **Use shared styles** for better performance with many features
4. **Add meaningful names and descriptions** for better user experience
5. **Use async methods** for large files
6. **Project coordinates** if your data is not in WGS84

## Error Handling

```csharp
try
{
    var geometries = KmlReader.ReadFromFile("map.kml");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"File not found: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Invalid KML: {ex.Message}");
}
```

## Performance Tips

- Use `ReadFromFileAsync` for large files
- Use shared styles instead of inline styles for better file size
- Validate KML during development, not in production
- Use `WriteToFileAsync` for better responsiveness

## Standards Compliance

This implementation follows:
- OGC KML 2.2 Specification
- KML namespace: `http://www.opengis.net/kml/2.2`
- WGS84 coordinate system (EPSG:4326)

## Related Classes

- `IRI.Maptor.Sta.Spatial.Primitives.Geometry<T>` - Core geometry class
- `IRI.Maptor.Sta.Common.Primitives.Point` - Point primitive
- `IRI.Maptor.Ket.KmlFormat.Primitives.*` - Auto-generated KML 2.2 types

## License

MIT License - See LICENSE.txt in the repository root

