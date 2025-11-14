# EPS Format Support in Maptor

EPS (Encapsulated PostScript) read/write support for spatial geometries with exact coordinate preservation and optional styling.

## Features

- **Full EPS Support**
  - Read/write EPS format (PS-Adobe-3.0 EPSF-3.0)
  - Preserve exact coordinate values (round-trip conversion)
  - Support for all geometry types: Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, GeometryCollection
  
- **Geometry Mapping**
  - Point → `moveto` + `lineto` + `stroke` commands
  - LineString → `moveto` + `lineto` commands + `stroke`
  - Polygon → `moveto` + `lineto` commands + `closepath` + `fill` + `stroke`
  - Multi geometries → Multiple PostScript path groups
  - GeometryCollection → Multiple geometry groups
  
- **Coordinate Preservation**
  - Exact coordinate values stored in PostScript commands
  - Bounding box calculated from geometry with configurable padding
  - Round-trip conversion maintains exact precision

- **Optional Features**
  - Feature attribute preservation (Title, Creator as EPS comments)
  - Configurable styling (stroke, fill, stroke-width)
  - Bounding box calculation with padding
  - Coordinate precision control

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Spatial
```

## Usage

### Basic Geometry to EPS

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

// Create a point geometry
var point = Geometry<Point>.Create(100.5, 200.3, srid: 4326);

// Convert to EPS string
string eps = point.ToEps();

// Save to file
point.SaveAsEps("point.eps");
```

### Geometry with Styling

```csharp
using IRI.Maptor.Sta.Spatial.IO.Eps;
using IRI.Maptor.Sta.Spatial.IO.Dxf;

// Create options with styling
var options = new EpsOptions(
    strokeColor: new RgbColor(255, 0, 0),      // Red stroke
    fillColor: new RgbColor(255, 255, 0),      // Yellow fill
    strokeWidth: 2.0
);

// Convert with options
string eps = geometry.ToEps(options);
geometry.SaveAsEps("styled.eps", options);
```

### Feature to EPS

```csharp
using IRI.Maptor.Sta.Spatial.Primitives;

// Create feature with attributes
var geometry = Geometry<Point>.Create(100, 200, srid: 4326);
var feature = new Feature<Point>(geometry, new Dictionary<string, object>
{
    { "Title", "Sample Point" },
    { "Creator", "My Application" }
});

// Convert to EPS (attributes preserved as comments)
string eps = feature.ToEps();
feature.SaveAsEps("feature.eps");
```

### Reading EPS to Geometry

```csharp
using IRI.Maptor.Extensions;

// Read from string
string epsContent = File.ReadAllText("geometry.eps");
var geometry = epsContent.ToGeometry(srid: 4326);

// Read from file
var fileInfo = new FileInfo("geometry.eps");
var geometry2 = fileInfo.ReadEps(srid: 4326);
```

### Reading EPS to Feature

```csharp
using IRI.Maptor.Extensions;

// Read with attributes
string epsContent = File.ReadAllText("feature.eps");
var feature = epsContent.ToFeature(srid: 4326, preserveAttributes: true);

// Access attributes
if (feature.Attributes.ContainsKey("Title"))
{
    Console.WriteLine($"Feature Title: {feature.Attributes["Title"]}");
}
```

### Using EpsOptions

```csharp
var options = new EpsOptions
{
    StrokeColor = new RgbColor(0, 0, 255),      // Blue stroke
    FillColor = new RgbColor(255, 255, 255),  // White fill
    StrokeWidth = 1.5,
    BoundingBoxPadding = 0.1,                  // 10% padding
    CoordinatePrecision = 8,                    // 8 decimal places
    PreserveFeatureAttributes = true,
    Creator = "My Application",
    Title = "My Document"
};

geometry.ToEps(options);
```

## EPS File Format Structure

### Header Section (Required)

```
%!PS-Adobe-3.0 EPSF-3.0
%%Creator: {Creator}
%%Title: {Title}
%%BoundingBox: {llx} {lly} {urx} {ury}
%%EndComments
```

### Body Section

- PostScript drawing commands
- Path definitions using `moveto`, `lineto`, `curveto`, `closepath`
- Styling commands (`setrgbcolor`, `setlinewidth`)

### Footer Section

```
%%EOF
```

## Geometry Type Mapping

| Geometry Type | EPS Representation |
|---------------|-------------------|
| Point | `newpath {x} {y} moveto {x} {y} lineto stroke` |
| LineString | `newpath moveto` + `lineto` commands + `stroke` |
| Polygon | `newpath moveto` + `lineto` commands + `closepath fill stroke` |
| MultiPoint | Multiple point commands |
| MultiLineString | Multiple path groups |
| MultiPolygon | Multiple polygon paths |
| GeometryCollection | Multiple geometry groups |

## PostScript Commands Supported

### Reading (Parsed)
- **moveto (M)**: Start new path or move current point
- **lineto (L)**: Draw line to point
- **curveto (C)**: Cubic Bezier curve (approximated as line segments)
- **closepath (Z)**: Close current path
- **stroke**: Draw current path
- **fill**: Fill current path

### Writing (Generated)
- **newpath**: Start new path
- **moveto**: Move to point
- **lineto**: Line to point
- **closepath**: Close path
- **setrgbcolor**: Set RGB color (0.0-1.0 range)
- **setlinewidth**: Set line width
- **stroke**: Draw path outline
- **fill**: Fill path

## Coordinate Preservation

The EPS format preserves exact coordinate values:

- **Writing**: Coordinates are stored directly in PostScript commands
- **Bounding Box**: Calculated from geometry bounding box with padding
- **Reading**: Coordinates extracted directly from PostScript commands
- **Result**: Round-trip conversion (Geometry → EPS → Geometry) maintains exact coordinate precision

Example:
```csharp
// Original geometry
var original = Geometry<Point>.Create(123.456789012, 987.654321098, srid: 4326);

// Convert to EPS and back
string eps = original.ToEps();
var restored = eps.ToGeometry(srid: 4326);

// Coordinates are exactly the same
Assert.Equal(original.Points[0].X, restored.Points[0].X);
Assert.Equal(original.Points[0].Y, restored.Points[0].Y);
```

## Feature Attributes

Feature attributes can be preserved in EPS:

- **Title**: Mapped to EPS `%%Title:` comment
- **Creator**: Mapped to EPS `%%Creator:` comment
- **Other attributes**: Stored as `%%{key}: {value}` comments

Example:
```csharp
var feature = new Feature<Point>(geometry, new Dictionary<string, object>
{
    { "Title", "Sample Feature" },
    { "Creator", "My App" }
});

string eps = feature.ToEps();
// EPS will contain: %%Title: Sample Feature
//                    %%Creator: My App
```

## Extension Methods

### Geometry Extensions

```csharp
// Convert to EPS string
string eps = geometry.ToEps();
string eps = geometry.ToEps(options);

// Save to file
geometry.SaveAsEps("output.eps");
geometry.SaveAsEps("output.eps", options);
```

### Feature Extensions

```csharp
// Convert to EPS string
string eps = feature.ToEps();
string eps = feature.ToEps(options);

// Save to file
feature.SaveAsEps("output.eps");
feature.SaveAsEps("output.eps", options);
```

### String Extensions

```csharp
// Parse EPS string to Geometry
var geometry = epsContent.ToGeometry(srid: 4326);

// Parse EPS string to Feature
var feature = epsContent.ToFeature(srid: 4326, preserveAttributes: true);
```

### FileInfo Extensions

```csharp
var fileInfo = new FileInfo("geometry.eps");

// Read as Geometry
var geometry = fileInfo.ReadEps(srid: 4326);

// Read as Feature
var feature = fileInfo.ReadEpsFeature(srid: 4326, preserveAttributes: true);
```

## Examples

### Export a Colored Polygon

```csharp
var polygon = new Geometry<Point>(
    new List<Geometry<Point>>
    {
        new Geometry<Point>(
            new List<Point>
            {
                new Point(0, 0),
                new Point(100, 0),
                new Point(100, 100),
                new Point(0, 100)
            },
            GeometryType.LineString,
            srid: 4326
        )
    },
    GeometryType.Polygon,
    srid: 4326
);

var options = new EpsOptions(
    strokeColor: new RgbColor(0, 100, 0),      // Dark green
    fillColor: new RgbColor(144, 238, 144),    // Light green
    strokeWidth: 2.0
);

polygon.SaveAsEps("polygon.eps", options);
```

### Read EPS with Multiple Geometries

```csharp
string epsContent = @"
%!PS-Adobe-3.0 EPSF-3.0
%%BoundingBox: 0 0 200 200
%%EndComments
newpath
50 50 moveto
50 50 lineto
stroke
newpath
100 50 moveto
150 100 lineto
200 50 lineto
stroke
newpath
50 150 moveto
100 200 lineto
150 150 lineto
closepath
fill
stroke
%%EOF";

var geometry = epsContent.ToGeometry(srid: 4326);
// Returns GeometryCollection containing Point, LineString, and Polygon
```

### Round-Trip Conversion

```csharp
// Create original geometry
var original = Geometry<Point>.Create(123.456789, 987.654321, srid: 4326);

// Convert to EPS
string eps = original.ToEps();

// Convert back to geometry
var restored = eps.ToGeometry(srid: 4326);

// Verify exact coordinate preservation
Console.WriteLine($"Original: {original.Points[0].X}, {original.Points[0].Y}");
Console.WriteLine($"Restored: {restored.Points[0].X}, {restored.Points[0].Y}");
// Output: Both show identical coordinates
```

## Notes

- Empty geometries result in EPS files with default bounding box (0 0 100 100)
- Bounding box is calculated from geometry with configurable padding
- Complex curves (Bezier, arcs) are approximated as line segments when reading
- EPS uses bottom-left origin coordinate system
- Coordinate precision defaults to 6 decimal places (configurable via EpsOptions)
- PostScript color values are in 0.0-1.0 range (automatically converted from RGB 0-255)

## Compatibility

Generated EPS files are compatible with:
- Adobe Illustrator
- Adobe InDesign
- Ghostscript
- PostScript printers
- EPS viewers and editors

## Related Documentation

- [EPS Specification](https://www.adobe.com/content/dam/acom/en/devnet/actionscript/articles/PLRM.pdf)
- [Geometry Types](../../Primitives/Geometry/Geometry.cs)
- [Feature Types](../../Primitives/FeatureSets/FeatureOfT.cs)
- [SVG Format Support](../Svg/README.md) - Similar vector format implementation
