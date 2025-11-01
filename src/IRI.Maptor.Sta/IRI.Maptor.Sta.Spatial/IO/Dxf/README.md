# DXF Export with Color Support

This module provides DXF (Drawing Exchange Format) export functionality with full color support for geometric features.

## Architecture

### Core Components

1. **`DxfWriter`** (IRI.Maptor.Sta.Spatial) - Core DXF writing logic
2. **`DxfColorInfo`** (IRI.Maptor.Sta.Spatial) - Color and styling information
3. **`GeometryExtensions`** (IRI.Maptor.Jab.Common) - Extension methods for WPF integration

### Design Philosophy

All DXF writing logic is centralized in the `DxfWriter` class to avoid code duplication. The extension methods in `GeometryExtensions` simply convert WPF visual parameters to `DxfColorInfo` and delegate to `DxfWriter`.

## Usage

### From IRI.Maptor.Sta.Spatial (Core)

```csharp
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

// Create geometry
var geometry = new Geometry<Point>(GeometryType.LineString);
// ... add points

// Simple export without color
DxfWriter.WriteToFile(geometry, @"C:\output\geometry.dxf");

// Export with color information
var colorInfo = new DxfColorInfo(
    strokeColor: new RgbColor(255, 0, 0),      // Red stroke
    fillColor: new RgbColor(255, 255, 0, 128), // Yellow fill with alpha
    strokeThickness: 2.0,
    opacity: 0.8
);
DxfWriter.WriteToFile(geometry, @"C:\output\colored.dxf", colorInfo);
```

### From IRI.Maptor.Jab.Common (WPF Integration)

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using System.Windows.Media;

// Using VisualParameters
var visualParams = new VisualParameters(
    fill: new SolidColorBrush(Colors.LightBlue),
    stroke: new SolidColorBrush(Colors.DarkBlue),
    strokeThickness: 2.0,
    opacity: 0.8
);
geometry.WriteToDxfFile(@"C:\output\myGeometry.dxf", visualParams);

// Using individual parameters
geometry.WriteToDxfFile(
    @"C:\output\geometry.dxf",
    stroke: Brushes.Red,
    fill: Brushes.Yellow,
    strokeThickness: 1.5,
    opacity: 1.0
);

// Get DXF string without saving
string dxfContent = geometry.AsDxf(visualParams);
```

## Color Support

### DxfColorInfo Class

```csharp
public class DxfColorInfo
{
    public RgbColor? StrokeColor { get; set; }      // Outline color
    public RgbColor? FillColor { get; set; }        // Fill color (polygons)
    public double StrokeThickness { get; set; }     // Line width
    public double Opacity { get; set; }             // 0.0 to 1.0
}
```

### RgbColor Struct

```csharp
public struct RgbColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }  // Alpha channel
    
    public RgbColor(byte r, byte g, byte b, byte a = 255)
}
```

## DXF Color Format

The module converts colors to DXF format as follows:

- **Group Code 420**: True Color (24-bit RGB) - `(R << 16) | (G << 8) | B`
- **Group Code 440**: Transparency (0-255) - Alpha channel adjusted by opacity
- **Group Code 43**: Line Width - Stroke thickness for polylines

## Supported Geometry Types

| Geometry Type      | DXF Entity         | Stroke | Fill | Notes                           |
|--------------------|--------------------|--------|------|---------------------------------|
| Point              | POINT              | ✓      | -    | Uses stroke color               |
| LineString         | LWPOLYLINE         | ✓      | -    | Open polyline                   |
| Polygon            | LWPOLYLINE + HATCH | ✓      | ✓    | Outline + solid fill            |
| MultiPoint         | Multiple POINTs    | ✓      | -    | Each point separately           |
| MultiLineString    | Multiple LWPOLYLINEs | ✓    | -    | Each line separately            |
| MultiPolygon       | Multiple entities  | ✓      | ✓    | Each polygon with outline/fill  |
| GeometryCollection | Mixed              | ✓      | ✓    | Recursive processing            |

## Features

### Polygon Fill Support

Polygons are exported with both:
- **LWPOLYLINE**: Closed polyline for the outline (stroke color)
- **HATCH**: Solid fill pattern (fill color)

Interior rings (holes) are properly handled in both entities.

### Opacity/Transparency

- Opacity is applied to the alpha channel
- DXF Group Code 440 stores transparency (0-255)
- Note: Not all DXF viewers support transparency

### Line Thickness

- Applied to LWPOLYLINE entities via Group Code 43
- Measured in drawing units

## DXF Format Details

- **Version**: AC1015 (AutoCAD 2000)
- **Coordinate Precision**: 6 decimal places
- **Handle Generation**: Hexadecimal sequential IDs

## CAD Application Compatibility

Generated DXF files can be opened in:
- AutoCAD (all modern versions)
- DraftSight
- LibreCAD
- QCAD
- FreeCAD
- BricsCAD

Colors, line thicknesses, and fills are preserved in most modern DXF-compatible applications.

## Extension Methods

The `GeometryExtensions` class provides convenient extension methods:

```csharp
// Write to file with VisualParameters
geometry.WriteToDxfFile(filePath, visualParameters);

// Write to file with individual parameters
geometry.WriteToDxfFile(filePath, stroke, fill, strokeThickness, opacity);

// Get DXF string
string dxf = geometry.AsDxf(visualParameters);
string dxf = geometry.AsDxf(stroke, fill, strokeThickness, opacity);
```

These methods automatically convert WPF brushes to `DxfColorInfo` and call the core `DxfWriter`.

## Examples

### Export a Colored Polygon

```csharp
var polygon = new Geometry<Point>(GeometryType.Polygon);
// ... define polygon

var colorInfo = new DxfColorInfo(
    strokeColor: new RgbColor(0, 100, 0),      // Dark green
    fillColor: new RgbColor(144, 238, 144),    // Light green
    strokeThickness: 1.5,
    opacity: 0.7
);

DxfWriter.WriteToFile(polygon, @"C:\output\polygon.dxf", colorInfo);
```

### Export with WPF VisualParameters

```csharp
var visualParams = VisualParameters.Get(
    hexFill: "#FFFF00",
    hexStroke: "#FF0000",
    strokeThickness: 2.0,
    fillOpacity: 0.5,
    strokeOpacity: 1.0
);

geometry.WriteToDxfFile(@"C:\output\geometry.dxf", visualParams);
```

### Batch Export Multiple Geometries

```csharp
// Note: For multiple geometries, use GeometryCollection or export separately
var geometries = new List<Geometry<Point>>();
// ... populate geometries

foreach (var (geo, index) in geometries.Select((g, i) => (g, i)))
{
    var colorInfo = GetColorForIndex(index);
    DxfWriter.WriteToFile(geo, $@"C:\output\geometry_{index}.dxf", colorInfo);
}
```

## Notes

- Empty geometries or null color info results in no color codes in DXF (uses CAD defaults)
- Stroke thickness of 0 means no width specification (CAD default)
- Alpha values are clamped between 0-255
- Opacity is clamped between 0.0-1.0

