# SVG Format Support in Maptor

SVG (Scalable Vector Graphics) read/write support for spatial geometries with exact coordinate preservation and optional styling.

## Features

- **Full SVG Support**
  - Read/write SVG format
  - Preserve exact coordinate values (round-trip conversion)
  - Support for all geometry types: Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, GeometryCollection
  
- **Geometry Mapping**
  - Point → `<circle>` element
  - LineString → `<polyline>` element
  - Polygon → `<polygon>` or `<path>` element (for polygons with holes)
  - Multi geometries → `<g>` groups with nested elements
  
- **Coordinate Preservation**
  - Exact coordinate values stored in SVG element attributes
  - ViewBox calculated for proper display but does not transform coordinates
  - Round-trip conversion maintains exact precision

- **Optional Features**
  - Feature attribute preservation (id, class, data-*)
  - Configurable styling (stroke, fill, stroke-width, opacity)
  - ViewBox calculation with padding
  - Coordinate precision control

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Spatial
```

## Usage

### Basic Geometry to SVG

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

// Create a point geometry
var point = Geometry<Point>.Create(100.5, 200.3, srid: 4326);

// Convert to SVG string
string svg = point.ToSvg();

// Save to file
point.SaveAsSvg("point.svg");
```

### Geometry with Styling

```csharp
using IRI.Maptor.Sta.Spatial.IO.Svg;
using IRI.Maptor.Sta.Spatial.IO.Dxf;

// Create options with styling
var options = new SvgOptions(
    strokeColor: new RgbColor(255, 0, 0),      // Red stroke
    fillColor: new RgbColor(255, 255, 0, 200), // Yellow fill with alpha
    strokeWidth: 2.0,
    opacity: 0.8
);

// Convert with options
string svg = geometry.ToSvg(options);
geometry.SaveAsSvg("styled.svg", options);
```

### Feature to SVG

```csharp
using IRI.Maptor.Sta.Spatial.Primitives;

// Create feature with attributes
var geometry = Geometry<Point>.Create(100, 200, srid: 4326);
var feature = new Feature<Point>(geometry, new Dictionary<string, object>
{
    { "id", "feature-1" },
    { "name", "Sample Point" },
    { "class", "marker" }
});

// Convert to SVG (attributes preserved)
string svg = feature.ToSvg();
feature.SaveAsSvg("feature.svg");
```

### Reading SVG to Geometry

```csharp
using IRI.Maptor.Extensions;

// Read from string
string svgContent = File.ReadAllText("geometry.svg");
var geometry = svgContent.ToGeometry(srid: 4326);

// Read from file
var fileInfo = new FileInfo("geometry.svg");
var geometry2 = fileInfo.ReadSvg(srid: 4326);
```

### Reading SVG to Feature

```csharp
using IRI.Maptor.Extensions;

// Read with attributes
string svgContent = File.ReadAllText("feature.svg");
var feature = svgContent.ToFeature(srid: 4326, preserveAttributes: true);

// Access attributes
if (feature.Attributes.ContainsKey("id"))
{
    Console.WriteLine($"Feature ID: {feature.Attributes["id"]}");
}
```

### Using SvgOptions

```csharp
var options = new SvgOptions
{
    StrokeColor = new RgbColor(0, 0, 255),      // Blue stroke
    FillColor = new RgbColor(255, 255, 255),    // White fill
    StrokeWidth = 1.5,
    Opacity = 0.9,
    IncludeViewBox = true,
    ViewBoxPadding = 0.1,                       // 10% padding
    CoordinatePrecision = 8,                    // 8 decimal places
    PreserveFeatureAttributes = true,
    PointCircleRadius = 5.0                     // Radius for point circles
};

geometry.ToSvg(options);
```

## SVG Element Mapping

| Geometry Type      | SVG Element    | Notes                                    |
|--------------------|----------------|------------------------------------------|
| Point              | `<circle>`     | Uses cx, cy, r attributes                |
| LineString         | `<polyline>`   | Uses points attribute                    |
| Polygon            | `<polygon>`    | Uses points attribute (single ring)      |
| Polygon (with holes)| `<path>`      | Uses d attribute with M/L/Z commands    |
| MultiPoint         | `<g>` + circles| Group containing multiple circles        |
| MultiLineString    | `<g>` + polylines| Group containing multiple polylines   |
| MultiPolygon       | `<g>` + polygons| Group containing multiple polygons    |
| GeometryCollection | `<g>` + mixed | Group containing various element types   |

## Coordinate Preservation

The SVG format preserves exact coordinate values:

- **Writing**: Coordinates are stored directly in SVG element attributes (points, d, cx/cy)
- **ViewBox**: Calculated from bounding box for proper display, but does not transform coordinates
- **Reading**: Coordinates extracted directly from SVG attributes, preserving exact values
- **Result**: Round-trip conversion (Geometry → SVG → Geometry) maintains exact coordinate precision

Example:
```csharp
// Original geometry
var original = Geometry<Point>.Create(123.456789012, 987.654321098, srid: 4326);

// Convert to SVG and back
string svg = original.ToSvg();
var restored = svg.ToGeometry(srid: 4326);

// Coordinates are exactly the same
Assert.Equal(original.Points[0].X, restored.Points[0].X);
Assert.Equal(original.Points[0].Y, restored.Points[0].Y);
```

## Feature Attributes

Feature attributes can be preserved in SVG:

- **id**: Mapped to SVG `id` attribute
- **class**: Mapped to SVG `class` attribute
- **data-***: Preserved as SVG data attributes
- **Other attributes**: Stored as `data-{key}` attributes

Example:
```csharp
var feature = new Feature<Point>(geometry, new Dictionary<string, object>
{
    { "id", "point-1" },
    { "class", "marker" },
    { "name", "Sample" }
});

string svg = feature.ToSvg();
// SVG will contain: <circle id="point-1" class="marker" data-name="Sample" ... />
```

## Extension Methods

### Geometry Extensions

```csharp
// Convert to SVG string
string svg = geometry.ToSvg();
string svg = geometry.ToSvg(options);

// Save to file
geometry.SaveAsSvg("output.svg");
geometry.SaveAsSvg("output.svg", options);
```

### Feature Extensions

```csharp
// Convert to SVG string
string svg = feature.ToSvg();
string svg = feature.ToSvg(options);

// Save to file
feature.SaveAsSvg("output.svg");
feature.SaveAsSvg("output.svg", options);
```

### String Extensions

```csharp
// Parse SVG string to Geometry
var geometry = svgContent.ToGeometry(srid: 4326);

// Parse SVG string to Feature
var feature = svgContent.ToFeature(srid: 4326, preserveAttributes: true);
```

### FileInfo Extensions

```csharp
var fileInfo = new FileInfo("geometry.svg");

// Read as Geometry
var geometry = fileInfo.ReadSvg(srid: 4326);

// Read as Feature
var feature = fileInfo.ReadSvgFeature(srid: 4326, preserveAttributes: true);
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

var options = new SvgOptions(
    strokeColor: new RgbColor(0, 100, 0),      // Dark green
    fillColor: new RgbColor(144, 238, 144),    // Light green
    strokeWidth: 2.0,
    opacity: 0.7
);

polygon.SaveAsSvg("polygon.svg", options);
```

### Read SVG with Multiple Geometries

```csharp
string svgContent = @"
<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 200'>
  <circle cx='50' cy='50' r='3'/>
  <polyline points='100,50 150,100 200,50'/>
  <polygon points='50,150 100,200 150,150'/>
</svg>";

var geometry = svgContent.ToGeometry(srid: 4326);
// Returns GeometryCollection containing Point, LineString, and Polygon
```

### Round-Trip Conversion

```csharp
// Create original geometry
var original = Geometry<Point>.Create(123.456789, 987.654321, srid: 4326);

// Convert to SVG
string svg = original.ToSvg();

// Convert back to geometry
var restored = svg.ToGeometry(srid: 4326);

// Verify exact coordinate preservation
Console.WriteLine($"Original: {original.Points[0].X}, {original.Points[0].Y}");
Console.WriteLine($"Restored: {restored.Points[0].X}, {restored.Points[0].Y}");
// Output: Both show identical coordinates
```

## Notes

- Empty geometries result in empty SVG files
- ViewBox is calculated from bounding box but coordinates remain unchanged
- Path elements are used for polygons with interior rings (holes)
- Complex path commands (curves, arcs) are approximated as line segments when reading
- SVG namespace is automatically added to root element
- Coordinate precision defaults to 6 decimal places (configurable via SvgOptions)

## Browser Compatibility

Generated SVG files are compatible with:
- Modern web browsers (Chrome, Firefox, Safari, Edge)
- SVG viewers and editors (Inkscape, Adobe Illustrator)
- Web mapping libraries (Leaflet, OpenLayers, Mapbox GL)

## Related Documentation

- [SVG Specification](https://www.w3.org/TR/SVG2/)
- [Geometry Types](../Primitives/Geometry/Geometry.cs)
- [Feature Types](../Primitives/FeatureSets/FeatureOfT.cs)







