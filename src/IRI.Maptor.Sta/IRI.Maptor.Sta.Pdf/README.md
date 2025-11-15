# PDF Vector Format Support in Maptor

PDF vector file writing support for spatial geometries with styling and metadata preservation.

## Features

- **PDF Vector Writing**
  - Write Geometry and Feature types to PDF format
  - Support for all geometry types: Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, GeometryCollection
  - Vector graphics rendering (not rasterized)
  
- **Geometry Mapping**
  - Point → Circle element
  - LineString → Polyline path
  - Polygon → Filled polygon with optional holes
  - Multi geometries → Multiple geometry elements
  - GeometryCollection → Multiple geometry groups
  
- **Page Size Options**
  - Automatic page size calculation from geometry bounding box
  - Standard page sizes (A4, Letter)
  - Custom page size support
  - Portrait and Landscape orientations
  
- **Styling Support**
  - Stroke color and width
  - Fill color for polygons
  - Opacity/transparency
  - Configurable point circle radius
  
- **Optional Features**
  - Feature attribute preservation as PDF metadata (Title, Author, Creator, Subject, Keywords)
  - Bounding box calculation with configurable padding
  - Coordinate precision control

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Pdf
```

The package includes PdfSharpCore dependency for PDF generation and requires IRI.Maptor.Sta.Spatial.

## Usage

### Basic Geometry to PDF

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

// Create a point geometry
var point = Geometry<Point>.Create(100.5, 200.3, srid: 4326);

// Convert to PDF bytes
byte[] pdfBytes = point.ToPdf();

// Save to file
point.SaveAsPdf("point.pdf");
```

### Geometry with Styling

```csharp
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.Spatial.IO.Dxf;

// Create options with styling
var options = new PdfOptions(
    strokeColor: new RgbColor(255, 0, 0),      // Red stroke
    fillColor: new RgbColor(255, 255, 0),      // Yellow fill
    strokeWidth: 2.0
);

// Convert with options
byte[] pdfBytes = geometry.ToPdf(options);
geometry.SaveAsPdf("styled.pdf", options);
```

### Feature to PDF

```csharp
using IRI.Maptor.Sta.Spatial.Primitives;

// Create feature with attributes
var geometry = Geometry<Point>.Create(100, 200, srid: 4326);
var feature = new Feature<Point>(geometry, new Dictionary<string, object>
{
    { "Title", "Sample Point" },
    { "Author", "John Doe" },
    { "Creator", "My Application" }
});

// Convert to PDF (attributes preserved as PDF metadata)
byte[] pdfBytes = feature.ToPdf();
feature.SaveAsPdf("feature.pdf");
```

### Using PdfOptions

```csharp
var options = new PdfOptions
{
    StrokeColor = new RgbColor(0, 0, 255),      // Blue stroke
    FillColor = new RgbColor(255, 255, 255),    // White fill
    StrokeWidth = 1.5,
    Opacity = 0.8,                               // 80% opacity
    BoundingBoxPadding = 0.1,                    // 10% padding
    PageSize = PdfPageSize.A4,                   // A4 page size
    PageOrientation = PdfPageOrientation.Portrait,
    PreserveFeatureAttributes = true,
    Creator = "My Application",
    Title = "My Document",
    Author = "John Doe",
    PointCircleRadius = 5.0                      // Larger point circles
};

geometry.ToPdf(options);
```

### Custom Page Size

```csharp
var options = new PdfOptions
{
    PageSize = PdfPageSize.Custom,
    CustomPageWidth = 800,   // 800 points (11.11 inches)
    CustomPageHeight = 600,  // 600 points (8.33 inches)
    BoundingBoxPadding = 0.05
};

geometry.SaveAsPdf("custom_size.pdf", options);
```

### Auto Page Size (Default)

```csharp
var options = new PdfOptions
{
    PageSize = PdfPageSize.Auto,  // Automatically calculate from bounding box
    BoundingBoxPadding = 0.05      // 5% padding around geometry
};

geometry.SaveAsPdf("auto_size.pdf", options);
```

## PDF File Format Structure

### Document Structure

- PDF document with vector graphics content
- Single page (multi-page support may be added in future)
- PDF metadata (Title, Author, Creator, Subject, Keywords)

### Vector Graphics

- Points rendered as circles
- LineStrings rendered as polylines
- Polygons rendered as filled shapes with optional holes
- All geometries use PDF vector paths (not rasterized)

## Geometry Type Mapping

| Geometry Type | PDF Representation |
|---------------|-------------------|
| Point | Circle element with configurable radius |
| LineString | Polyline path with stroke |
| Polygon | Filled polygon with stroke outline, supports holes |
| MultiPoint | Multiple circle elements |
| MultiLineString | Multiple polyline paths |
| MultiPolygon | Multiple filled polygons |
| GeometryCollection | Multiple geometry groups |

## Coordinate System

- PDF uses bottom-left origin coordinate system (similar to EPS)
- PDF coordinates are in points (1/72 inch)
- Geometry coordinates are automatically transformed to PDF page coordinates
- Page size can be calculated automatically from geometry bounding box
- Aspect ratio is maintained when scaling to fit page

## Extension Methods

### Geometry Extensions

```csharp
// Convert to PDF bytes
byte[] pdfBytes = geometry.ToPdf();
byte[] pdfBytes = geometry.ToPdf(options);

// Save to file
geometry.SaveAsPdf("output.pdf");
geometry.SaveAsPdf("output.pdf", options);
```

### Feature Extensions

```csharp
// Convert to PDF bytes
byte[] pdfBytes = feature.ToPdf();
byte[] pdfBytes = feature.ToPdf(options);

// Save to file
feature.SaveAsPdf("output.pdf");
feature.SaveAsPdf("output.pdf", options);
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

var options = new PdfOptions(
    strokeColor: new RgbColor(0, 100, 0),      // Dark green
    fillColor: new RgbColor(144, 238, 144),    // Light green
    strokeWidth: 2.0
);

polygon.SaveAsPdf("polygon.pdf", options);
```

### Export with Feature Attributes

```csharp
var geometry = Geometry<Point>.Create(100, 200, srid: 4326);
var feature = new Feature<Point>(geometry, new Dictionary<string, object>
{
    { "Title", "Important Location" },
    { "Author", "GIS Team" },
    { "Subject", "Geographic Features" },
    { "Keywords", "location, point, gis" }
});

var options = new PdfOptions
{
    PreserveFeatureAttributes = true,
    StrokeColor = new RgbColor(255, 0, 0),
    StrokeWidth = 3.0,
    PointCircleRadius = 8.0
};

feature.SaveAsPdf("feature.pdf", options);
// PDF metadata will contain: Title="Important Location", Author="GIS Team", etc.
```

### Export Multi-Polygon with Holes

```csharp
var exteriorRing = new Geometry<Point>(
    new List<Point>
    {
        new Point(0, 0),
        new Point(100, 0),
        new Point(100, 100),
        new Point(0, 100),
        new Point(0, 0)
    },
    GeometryType.LineString,
    srid: 4326
);

var holeRing = new Geometry<Point>(
    new List<Point>
    {
        new Point(25, 25),
        new Point(75, 25),
        new Point(75, 75),
        new Point(25, 75),
        new Point(25, 25)
    },
    GeometryType.LineString,
    srid: 4326
);

var polygon = new Geometry<Point>(
    new List<Geometry<Point>> { exteriorRing, holeRing },
    GeometryType.Polygon,
    srid: 4326
);

var options = new PdfOptions(
    strokeColor: new RgbColor(0, 0, 0),
    fillColor: new RgbColor(200, 200, 200),
    strokeWidth: 1.0
);

polygon.SaveAsPdf("polygon_with_hole.pdf", options);
```

## PdfOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| StrokeColor | RgbColor? | null | Stroke (outline) color |
| FillColor | RgbColor? | null | Fill color for polygons |
| StrokeWidth | double | 1.0 | Stroke width/thickness |
| Opacity | double | 1.0 | Opacity (0.0 to 1.0) |
| CoordinatePrecision | int | 6 | Coordinate precision (decimal places) |
| PageSize | PdfPageSize | Auto | Page size preset |
| CustomPageWidth | double? | null | Custom page width (points) |
| CustomPageHeight | double? | null | Custom page height (points) |
| PageOrientation | PdfPageOrientation | Portrait | Page orientation |
| BoundingBoxPadding | double | 0.05 | Padding around bounding box (percentage) |
| PreserveFeatureAttributes | bool | true | Preserve Feature attributes as PDF metadata |
| Title | string? | null | Document title |
| Author | string? | null | Document author |
| Creator | string | "IRI.Maptor.Sta.Pdf" | Application name |
| Subject | string? | null | Document subject |
| Keywords | string? | null | Document keywords |
| PointCircleRadius | double | 3.0 | Radius for point circles |

## Notes

- Empty geometries result in PDF files with default page size (A4)
- Page size is calculated from geometry bounding box with configurable padding when PageSize is Auto
- PDF uses bottom-left origin coordinate system
- All geometries are rendered as vector graphics (not rasterized)
- Feature attributes are stored as PDF document metadata when PreserveFeatureAttributes is true
- PDF coordinates are in points (1/72 inch)
- Aspect ratio is maintained when scaling geometry to fit page

## Compatibility

Generated PDF files are compatible with:
- Adobe Acrobat Reader
- PDF viewers (Chrome, Firefox, Edge)
- PDF editors (Adobe Illustrator, Inkscape)
- PDF processing libraries

## Related Documentation

- [IRI.Maptor.Sta.Spatial](../IRI.Maptor.Sta.Spatial/README.md) - Core spatial library
- [EPS Format Support](../IRI.Maptor.Sta.Spatial/IO/Eps/README.md) - Similar vector format implementation
- [SVG Format Support](../IRI.Maptor.Sta.Spatial/IO/Svg/README.md) - Similar vector format implementation



