# DXF

Bidirectional DXF (Drawing Exchange Format) support: parse DXF drawings into strongly-typed
`Geometry<Point>` values and write geometries back out to CAD-compatible DXF files, with
color/fill styling and coordinate-reference-system awareness. Output is AutoCAD 2000 (`AC1015`)
format. Ships in the [IRI.Maptor.Sta.Spatial](../../README.md) package; WPF integration lives in
`IRI.Maptor.Jab.Wpf`.

## Supported capabilities

| Capability | Supported | Implemented in |
|---|---|---|
| Read (DXF → geometries) | Yes | `DxfReader.Read`/`ReadFromFile`, preview via `GetPreviewAsync` |
| Read with CAD context (layer/entity/color/annotation) | Yes | `DxfReader.ReadFeatures`/`ReadFeaturesFromFile` → `DxfFeature` |
| Write (geometries → DXF) | Yes | `DxfWriter.Write`/`WriteToFile`/`WriteToFileAsync`, `ToDxf`/`SaveAsDxfAsync` extensions |
| Styling (true color, transparency, line width) | Yes (write) | `DxfColorInfo`, `RgbColor` |
| CRS embedding/detection (ESRI WKT) | Yes | `DxfWriter` (XRECORD), `DxfReader` (SRID auto-detection) |
| WPF import dialog / brush-based export | Yes | `DxfOpenDialogView`, `GeometryExtensions` (Jab.Wpf) |

All reading logic is centralized in `DxfReader` and all writing logic in `DxfWriter`
(`IRI.Maptor.Sta.Spatial.IO.Dxf`); the WPF extension methods only convert visual parameters to
`DxfColorInfo` and delegate.

## Reading

```csharp
using IRI.Maptor.Sta.Spatial.IO.Dxf;

// Read from file. Pass a defaultSrid, or null to let the reader decide.
List<Geometry<Point>> geometries = await DxfReader.ReadFromFile(@"C:\input\plan.dxf", defaultSrid: null);

// Read from an in-memory DXF string.
List<Geometry<Point>> fromString = DxfReader.Read(dxfContent, defaultSrid: 4326);

// Read with CAD context: source DXF layer, entity type, resolved color, annotation flag.
List<DxfFeature> features = await DxfReader.ReadFeaturesFromFile(@"C:\input\plan.dxf", defaultSrid: null);
```

### CAD context and annotation separation (`ReadFeatures`)

`Read` returns bare geometries; `ReadFeatures` wraps each one in a `DxfFeature` carrying the CAD
context needed to treat a DXF as GIS data:

- **`DxfLayerName`** — the DXF layer of the source entity (group 8).
- **`EntityType`** — the DXF entity name (`LINE`, `LWPOLYLINE`, `INSERT`, `TEXT`, …).
- **`Color`** — resolved to `#RRGGBB`: entity true color (420) wins, then an explicit ACI color
  (62, converted through the standard AutoCAD palette via `DxfAciColor`), then the color of the
  entity's layer from the LAYER table (ByLayer/ByBlock/absent).
- **`Text`** — the content of `TEXT`/`MTEXT`/`ATTRIB` entities.
- **`IsAnnotation`** — `true` for drawing decoration as opposed to real-world features:
  `TEXT`/`MTEXT`/`ATTRIB` (as Points), `LEADER` (as LineString), `DIMENSION` (its anonymous block
  expanded), `SOLID`/`TRACE` (arrowheads), `3DFACE` (3D visualization facets), `HATCH` (fill
  boundaries), `WIPEOUT` (masking frames), `INSERT` of anonymous (`*`-prefixed) blocks — which
  includes dynamic-block instances (`*U…`) and Civil 3D label blocks — and anything on the
  `DEFPOINTS` layer. Callers use this flag to route annotation into separate layers
  (e.g. `-Polyline-other`, `-Polygon-other`) so the main point/line/polygon layers hold only
  real features.

Polygon-with-hole reassembly pools real and annotation rings **separately** (an arrowhead inside a
parcel never becomes a hole of the parcel), and each reassembled polygon keeps the CAD context of
the entity that contributed its exterior ring.

### SRID precedence

When resolving the coordinate system, `DxfReader` uses, in order:

1. The caller-supplied `defaultSrid` (when non-null and non-zero).
2. Otherwise, an SRID auto-detected from an embedded `GEOGCS`/`PROJCS` WKT string in the file.
3. Otherwise, `SridHelper.GeodeticWGS84` (EPSG:4326) as a fallback.

### Preview

`GetPreviewAsync` extracts the detected SRID and a small sample of coordinates — handy for import
dialogs that let the user confirm the projection before loading the whole file.

```csharp
DxfPreviewResult preview = await DxfReader.GetPreviewAsync(@"C:\input\plan.dxf", maxSamplePoints: 50);

int detectedSrid = preview.DetectedSrid;          // 0 when none embedded
IReadOnlyList<Point> samples = preview.SamplePoints;
```

### Supported entities (reading)

| DXF entity | Result (`GeometryType`) | Notes |
|---|---|---|
| `POINT` | Point | |
| `LINE` | LineString | Two points (start/end) |
| `LWPOLYLINE` | LineString or Polygon | Polygon when the closed flag (70, bit 0) is set and ≥ 3 points |
| `POLYLINE` / `VERTEX` | LineString or Polygon | Polygon when the closed flag (bit 0) is set |
| `CIRCLE` | Polygon | Approximated with 32 segments |
| `ARC` | LineString | Approximated with 32 segments |
| `ELLIPSE` | Polygon or LineString | Polygon for a full ellipse, LineString for an elliptical arc; 32 segments |
| `SPLINE` | LineString or Polygon | Fit points used directly when present, otherwise de Boor sampling of the (rational) B-spline; Polygon when the closed flag is set |
| `SOLID` / `TRACE` | Polygon | Zigzag corner order (1,2,4,3) reassembled into a ring; triangles supported; always annotation |
| `3DFACE` | Polygon | XY projection of the face; always annotation (3D visualization facet, not a mapped feature) |
| `INSERT` | Point + expanded block geometry | See *Block references* below |
| `TEXT` / `MTEXT` / `ATTRIB` | Point | Insertion point; text content in `DxfFeature.Text`; always annotation |
| `LEADER` | LineString | Callout/arrow path from its WCS vertices; always annotation |
| `DIMENSION` | expanded block geometry | The anonymous `*D` block (group 2) is expanded in place; always annotation |
| `HATCH` | Polygon | Boundary paths (polyline paths with bulges flattened; edge paths with line edges and sampled arc edges); always annotation |
| `WIPEOUT` | Polygon | Masking frame from the clip boundary (image space → world via insertion + U/V vectors); always annotation |
| *(other)* | — | Unknown entities (MLEADER, RAY, XLINE, …) are skipped |

The closed flag is tested bitwise: AutoCAD commonly writes `70 = 129` (closed + plinegen), which is
just as closed as `70 = 1`. If a closed polyline repeats its first vertex as its last, the duplicate
is dropped, because `CreatePolygonOrMultiPolygon` expects rings whose closing point is not repeated.

Closed rings are reassembled into polygons-with-holes / multipolygons via
`Geometry<Point>.CreatePolygonOrMultiPolygon`.

### Block references (INSERT)

The BLOCKS section is parsed into block definitions, and every `INSERT` in the ENTITIES section is
expanded: the referenced block's geometry is translated by the block base point, scaled (41/42),
rotated (50), and placed at the insertion point — nested block references are expanded recursively
(depth cap 8, resolution memoized per block name), and MINSERT column/row arrays (70/71/44/45) are
repeated on their grid.

Each `INSERT` contributes:

1. a **Point** feature at its insertion location — matching how ArcMap populates its CAD point
   feature class from block references (symbol positions: lamps, poles, …); and
2. the expanded block geometry, merged into at most one multi-part feature per geometry class
   (MultiPoint / MultiLineString / MultiPolygon). A block reference is one symbol, so its internals
   stay one feature instead of flooding the layer — and a symbol's concentric rings become sibling
   polygon parts rather than being pulled into the file-wide polygon/hole reassembly.

An `INSERT` referencing an undefined block still emits its insertion Point.

### Object Coordinate System (extrusion direction)

Planar DXF entities (`LWPOLYLINE`, 2D `POLYLINE`, `CIRCLE`, `ARC`, `SOLID`, `INSERT`) store their
coordinates in an Object Coordinate System defined by the extrusion direction (group codes
`210`/`220`/`230`, default `(0,0,1)`). The common non-default case is `(0,0,-1)` — a plane mirrored
about the Y axis, which AutoCAD emits for arcs and polylines drawn clockwise or through a mirror
operation. `DxfReader` negates the X of such entities to bring them back into world coordinates;
ignoring this puts them at `-x` instead of `x`, which for projected data (e.g. UTM eastings) throws
the layer extent hundreds of kilometres wide and makes the real features collapse to a sub-pixel
speck when the map zooms to the layer.

True-3D entities (`POINT`, `LINE`, `ELLIPSE`, `SPLINE`, `3DFACE`) store WCS coordinates and are
never mirrored. Entities with an arbitrary (tilted) extrusion axis are read as-is; the full
Arbitrary Axis Algorithm is not implemented.

## Writing

### From IRI.Maptor.Sta.Spatial (core)

```csharp
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Extensions; // ToDxf / SaveAsDxfAsync

// Simplest: extension methods
string dxf = geometry.ToDxf();                        // get DXF text
await geometry.SaveAsDxfAsync(@"C:\output\line.dxf"); // write to file

// Write a single geometry (optionally with color)
await DxfWriter.WriteToFileAsync(geometry, @"C:\output\geometry.dxf");

var colorInfo = new DxfColorInfo(
    strokeColor: new RgbColor(255, 0, 0),      // red stroke
    fillColor: new RgbColor(255, 255, 0, 128), // yellow fill with alpha
    strokeThickness: 2.0,
    opacity: 0.8);
await DxfWriter.WriteToFileAsync(geometry, @"C:\output\colored.dxf", colorInfo);
```

### Writing multiple geometries

A whole collection can be written into a single DXF file; all geometries share the same
`ENTITIES` section.

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

### From IRI.Maptor.Jab.Wpf (WPF)

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Wpf;
using System.Windows.Media;

// Using VisualParameters
var visualParams = new VisualParameters(
    fill: new SolidColorBrush(Colors.LightBlue),
    stroke: new SolidColorBrush(Colors.DarkBlue),
    strokeThickness: 2.0,
    opacity: 0.8);
await geometry.WriteToDxfFileAsync(@"C:\output\myGeometry.dxf", visualParams);

// Using individual brush parameters
await geometry.WriteToDxfFileAsync(
    @"C:\output\geometry.dxf",
    stroke: Brushes.Red,
    fill: Brushes.Yellow,
    strokeThickness: 1.5,
    opacity: 1.0);

// Get DXF string without saving
string dxfContent = geometry.AsDxf(visualParams);
```

### Supported geometry types (writing)

| Geometry type | DXF entity | Stroke | Fill | Notes |
|---|---|---|---|---|
| Point | POINT | Yes | – | Uses stroke color |
| LineString | LWPOLYLINE | Yes | – | Open polyline |
| Polygon | LWPOLYLINE + HATCH | Yes | Yes | Outline + solid fill |
| MultiPoint | Multiple POINTs | Yes | – | Each point separately |
| MultiLineString | Multiple LWPOLYLINEs | Yes | – | Each line separately |
| MultiPolygon | Multiple entities | Yes | Yes | Each polygon with outline/fill |
| GeometryCollection | Mixed | Yes | Yes | Recursive processing |

## Color support

```csharp
public class DxfColorInfo
{
    public RgbColor? StrokeColor { get; set; }      // outline color
    public RgbColor? FillColor { get; set; }        // fill color (polygons)
    public double StrokeThickness { get; set; }     // line width (default 1.0)
    public double Opacity { get; set; }             // 0.0 to 1.0 (default 1.0)
}

public struct RgbColor
{
    public byte R, G, B, A;                         // A = alpha channel
    public RgbColor(byte r, byte g, byte b, byte a = 255)
}
```

Colors are emitted using DXF group codes:

- **Group code 420** — true color (24-bit RGB): `(R << 16) | (G << 8) | B`
- **Group code 440** — transparency (0–255): alpha channel adjusted by opacity
- **Group code 43** — line width: stroke thickness for polylines

Polygons are exported with both an LWPOLYLINE (closed outline, stroke color) and a HATCH (solid
fill, fill color); interior rings (holes) are handled in both. A stroke thickness of `0` means
"no width specification" (CAD default). Alpha values are clamped to 0–255 and opacity to 0.0–1.0.

## Coordinate reference system

- **Export:** the geometry's SRID is embedded as an ESRI WKT string inside an `XRECORD` (under an
  `ESRI_PRJ` entry), so files open with the correct projection in ArcMap and other GIS tools.
- **Import:** `DxfReader` scans for embedded `GEOGCS`/`PROJCS` WKT and resolves it back to an SRID
  (see [SRID precedence](#srid-precedence)).

## WPF import dialog

`IRI.Maptor.Jab.Wpf` ships a ready-made import dialog, `DxfOpenDialogView`, exposed through the
dialog service:

```csharp
DxfOpenDialogResult? result = await DialogService.ShowDxfOpenDialogAsync();
if (result is null)
    return; // user cancelled

List<Geometry<Point>> geometries =
    await DxfReader.ReadFromFile(result.FilePath, result.SelectedSrid);
```

`DxfOpenDialogResult` is a simple record: `record DxfOpenDialogResult(string FilePath, int SelectedSrid)`.
The dialog provides a DXF file picker, a live coordinate preview, SRID auto-detection (with
controls locked when the file carries a coordinate system), and a coordinate-system chooser
(WGS84, Web Mercator, or UTM with zone and hemisphere selection).

## Format details

- **Version:** `AC1015` (AutoCAD 2000), readable by modern CAD applications.
- **Coordinate precision:** 6 decimal places.
- **Handle generation:** hexadecimal sequential IDs.

## Limitations

- **Single layer.** The writer places all entities on the default layer `"0"`; there is no
  multi-layer authoring, and layer names are not round-tripped on read.
- **Geometry-only.** `TEXT`/`MTEXT`, `SPLINE`, `ELLIPSE`, and `INSERT`/blocks are not parsed or
  written — supported content is point/line/polygon geometry (plus `ARC`/`CIRCLE` on read).
- **Transparency support varies** across DXF viewers (group code 440).
- `ReadFromFile`/`GetPreviewAsync` throw `FileNotFoundException` when the path does not exist.

---
[Back to IRI.Maptor.Sta.Spatial](../../README.md)
