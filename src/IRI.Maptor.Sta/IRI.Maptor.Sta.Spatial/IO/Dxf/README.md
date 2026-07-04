# DXF Read & Write

This module provides bidirectional **DXF (Drawing Exchange Format)** support for Maptor. It converts DXF drawings into strongly-typed `Geometry<Point>` values and writes geometries back out to clean, CAD-compatible DXF files — with full color/fill styling and coordinate-reference-system awareness.

- **Import:** parse a DXF file into `List<Geometry<Point>>` (points, lines, polylines, polygons, arcs, circles).
- **Export:** write one geometry or a collection to DXF, with per-geometry stroke/fill colors and transparency.
- **CRS-aware:** the coordinate system is embedded as ESRI WKT on export and auto-detected on import.
- **Output format:** AutoCAD 2000 (`AC1015`), compatible with all modern CAD applications.

## Architecture

### Core Components

1. **`DxfReader`** (IRI.Maptor.Sta.Spatial) — DXF import: DXF → `Geometry<Point>`
2. **`DxfWriter`** (IRI.Maptor.Sta.Spatial) — DXF export: `Geometry<Point>` → DXF
3. **`DxfColorInfo` / `RgbColor`** (IRI.Maptor.Sta.Spatial) — color and styling information
4. **`GeometryExtensions`** (IRI.Maptor.Jab.Common) — WPF integration (brushes → `DxfColorInfo`)
5. **`DxfOpenDialogView`** (IRI.Maptor.Jab.Common) — ready-made WPF import dialog

### Design Philosophy

All DXF writing logic is centralized in `DxfWriter`, and all reading logic in `DxfReader`, to avoid duplication. The WPF extension methods in `GeometryExtensions` simply convert visual parameters to `DxfColorInfo` and delegate to `DxfWriter`.

## Features

**Reading (import)**
- Entities: `POINT`, `LINE`, `LWPOLYLINE`, `POLYLINE`/`VERTEX`, `CIRCLE`, `ARC`.
- Closed polylines (≥ 3 points) become polygons; closed rings are reassembled into polygons-with-holes / multipolygons via `Geometry<Point>.CreatePolygonOrMultiPolygon`.
- SRID auto-detection from embedded WKT, with a caller override.
- Lightweight preview (detected SRID + sample coordinates) for import UIs.

**Writing (export)**
- `Point → POINT`, `LineString → LWPOLYLINE` (open), `Polygon → LWPOLYLINE + HATCH` (outline + solid fill).
- `Multi*` and `GeometryCollection` are decomposed recursively.
- True-color strokes/fills, transparency, and line width.
- CRS embedded as ESRI WKT (readable by ArcMap and re-detected by `DxfReader`).

## Reading (Import)

```csharp
using IRI.Maptor.Sta.Spatial.IO.Dxf;

// Read from file. Pass a defaultSrid, or null to let the reader decide.
List<Geometry<Point>> geometries = await DxfReader.ReadFromFile(@"C:\input\plan.dxf", defaultSrid: null);

// Read from an in-memory DXF string.
List<Geometry<Point>> fromString = DxfReader.Read(dxfContent, defaultSrid: 4326);
```

### SRID precedence

When resolving the coordinate system, `DxfReader` uses, in order:

1. The caller-supplied `defaultSrid` (when non-null and non-zero).
2. Otherwise, an SRID auto-detected from an embedded `GEOGCS`/`PROJCS` WKT string in the file.
3. Otherwise, `SridHelper.GeodeticWGS84` (EPSG:4326) as a fallback.

### Preview

`GetPreviewAsync` extracts the detected SRID and a small sample of coordinates — handy for import dialogs that let the user confirm the projection before loading the whole file.

```csharp
DxfPreviewResult preview = await DxfReader.GetPreviewAsync(@"C:\input\plan.dxf", maxSamplePoints: 50);

int detectedSrid = preview.DetectedSrid;          // 0 when none embedded
IReadOnlyList<Point> samples = preview.SamplePoints;
```

### Supported Entities (Reading)

| DXF Entity            | Result (`GeometryType`)          | Notes                                             |
|-----------------------|----------------------------------|---------------------------------------------------|
| `POINT`               | Point                            |                                                   |
| `LINE`                | LineString                       | Two points (start/end)                            |
| `LWPOLYLINE`          | LineString or Polygon            | Polygon when closed (flag 70 = 1) and ≥ 3 points  |
| `POLYLINE` / `VERTEX` | LineString or Polygon            | Polygon when the closed flag (bit 0) is set       |
| `CIRCLE`              | Polygon                          | Approximated with 32 segments                     |
| `ARC`                 | LineString                       | Approximated with 32 segments                     |
| *(other)*             | —                                | Unknown entities are skipped                      |

## Writing (Export)

### From IRI.Maptor.Sta.Spatial (Core)

```csharp
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Extensions; // ToDxf / SaveAsDxfAsync

// Create geometry
var geometry = new Geometry<Point>(GeometryType.LineString);
// ... add points

// Simplest: extension methods
string dxf = geometry.ToDxf();                       // get DXF text
await geometry.SaveAsDxfAsync(@"C:\output\line.dxf"); // write to file

// Write a single geometry (optionally with color)
await DxfWriter.WriteToFileAsync(geometry, @"C:\output\geometry.dxf");

var colorInfo = new DxfColorInfo(
    strokeColor: new RgbColor(255, 0, 0),      // Red stroke
    fillColor: new RgbColor(255, 255, 0, 128), // Yellow fill with alpha
    strokeThickness: 2.0,
    opacity: 0.8
);
await DxfWriter.WriteToFileAsync(geometry, @"C:\output\colored.dxf", colorInfo);
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

### Writing Multiple Geometries

A whole collection can be written into a single DXF file. All geometries share the same `ENTITIES` section.

```csharp
var geometries = new List<Geometry<Point>> { polygon1, line1, point1 };

// Uniform styling for all geometries
var colorInfo = new DxfColorInfo(
    strokeColor: new RgbColor(0, 0, 0),
    fillColor: new RgbColor(255, 0, 0));
await DxfWriter.WriteToFileAsync(geometries, @"C:\output\all.dxf", colorInfo);

// Per-geometry styling (returns the DXF text as well)
DxfWriter.WriteToFile(geometries, @"C:\output\styled.dxf", geom => GetColorForGeometry(geom));

// In-memory DXF strings (no file written)
string oneDxf  = DxfWriter.Write(polygon1, colorInfo);
string manyDxf = DxfWriter.Write(geometries, colorInfo);
```

### Supported Geometry Types (Writing)

| Geometry Type      | DXF Entity           | Stroke | Fill | Notes                          |
|--------------------|----------------------|--------|------|--------------------------------|
| Point              | POINT                | ✓      | -    | Uses stroke color              |
| LineString         | LWPOLYLINE           | ✓      | -    | Open polyline                  |
| Polygon            | LWPOLYLINE + HATCH   | ✓      | ✓    | Outline + solid fill           |
| MultiPoint         | Multiple POINTs      | ✓      | -    | Each point separately          |
| MultiLineString    | Multiple LWPOLYLINEs | ✓      | -    | Each line separately           |
| MultiPolygon       | Multiple entities    | ✓      | ✓    | Each polygon with outline/fill |
| GeometryCollection | Mixed                | ✓      | ✓    | Recursive processing           |

## Color Support

### DxfColorInfo Class

```csharp
public class DxfColorInfo
{
    public RgbColor? StrokeColor { get; set; }      // Outline color
    public RgbColor? FillColor { get; set; }        // Fill color (polygons)
    public double StrokeThickness { get; set; }     // Line width (default 1.0)
    public double Opacity { get; set; }             // 0.0 to 1.0 (default 1.0)
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

### DXF Color Format

Colors are emitted using DXF group codes:

- **Group Code 420** — True Color (24-bit RGB): `(R << 16) | (G << 8) | B`
- **Group Code 440** — Transparency (0–255): alpha channel adjusted by opacity
- **Group Code 43**  — Line Width: stroke thickness for polylines

Polygons are exported with both an **LWPOLYLINE** (closed outline, stroke color) and a **HATCH** (solid fill, fill color); interior rings (holes) are handled in both. Opacity is applied to the alpha channel — note that not all DXF viewers honor transparency. A stroke thickness of `0` means "no width specification" (CAD default).

## Coordinate Reference System (CRS)

- **Export:** the geometry's SRID is embedded as an ESRI WKT string inside an `XRECORD` (under an `ESRI_PRJ` entry), so files open with the correct projection in ArcMap and other GIS tools.
- **Import:** `DxfReader` scans for embedded `GEOGCS`/`PROJCS` WKT and resolves it back to an SRID (see [SRID precedence](#srid-precedence)).

## WPF Import Dialog

`IRI.Maptor.Jab.Common` ships a ready-made import dialog, `DxfOpenDialogView`, exposed through the dialog service. It gives end users a polished import experience without any custom UI code.

```csharp
DxfOpenDialogResult? result = await DialogService.ShowDxfOpenDialogAsync();
if (result is null)
    return; // user cancelled

List<Geometry<Point>> geometries =
    await DxfReader.ReadFromFile(result.FilePath, result.SelectedSrid);
```

`DxfOpenDialogResult` is a simple record: `record DxfOpenDialogResult(string FilePath, int SelectedSrid)`.

Dialog features:

- **File picker** with a DXF filter.
- **Live coordinate preview** — a sample of the file's X/Y coordinates so users can sanity-check them before importing.
- **Smart SRID auto-detection** — if the file carries a coordinate system, the dialog detects it, pre-selects the matching option (including UTM zone and hemisphere) and locks the controls so the projection can't be overridden by accident.
- **Coordinate-system chooser** — WGS84 (EPSG:4326), Web Mercator (EPSG:3857), or user-defined **UTM** with a zone (1–60) selector and a North/South hemisphere toggle.

## DXF Format Details

- **Version:** `AC1015` (AutoCAD 2000)
- **Coordinate Precision:** 6 decimal places
- **Handle Generation:** hexadecimal sequential IDs

### CAD Application Compatibility

Generated DXF files can be opened in:

- AutoCAD (all modern versions)
- DraftSight
- LibreCAD
- QCAD
- FreeCAD
- BricsCAD

Colors, line thicknesses, and fills are preserved in most modern DXF-compatible applications.

## Limitations

- **Single layer.** The writer places all entities on the default layer `"0"`; there is no multi-layer authoring, and layer names are not round-tripped on read.
- **Geometry-only.** `TEXT`/`MTEXT`, `SPLINE`, `ELLIPSE`, and `INSERT`/blocks are not parsed or written — supported content is point/line/polygon geometry (plus `ARC`/`CIRCLE` on read).
- **Transparency support varies** across DXF viewers (group code 440).

## Notes

- Empty geometries or null color info produce no color codes (CAD defaults are used).
- Alpha values are clamped between 0–255; opacity is clamped between 0.0–1.0.
- `ReadFromFile`/`GetPreviewAsync` throw `FileNotFoundException` when the path does not exist.
