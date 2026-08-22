# EPS

EPS (Encapsulated PostScript, `PS-Adobe-3.0 EPSF-3.0`) read/write support for `Geometry<Point>`
and `Feature<Point>`. Coordinates are written directly into PostScript path commands, so
geometries round-trip through EPS at the configured coordinate precision; the `%%BoundingBox`
header is derived from the geometry with configurable padding. Ships in the
[IRI.Maptor.Core.Spatial](../../README.md) package and mirrors the API of the
[SVG support](../Svg/README.md).

## Supported capabilities

| Capability | Supported | Implemented in |
|---|---|---|
| Read (EPS → geometry/feature) | Yes | `EpsReader`, `EpsReaderExtensions` |
| Write (geometry/feature → EPS) | Yes | `EpsWriter`, `ToEps`/`SaveAsEps` extensions |
| Styling on write (stroke, fill, width) | Yes | `EpsOptions` |
| Feature attributes as DSC comments (`%%Title`, `%%Creator`) | Yes | `EpsWriter`/`EpsReader` (`PreserveFeatureAttributes`, `preserveAttributes`) |

All geometry types are handled: Point, LineString, Polygon, MultiPoint, MultiLineString,
MultiPolygon, and GeometryCollection.

## Usage

### Writing

`ToEps`/`SaveAsEps` live in the `IRI.Maptor.Extensions` namespace; `EpsOptions` in
`IRI.Maptor.Core.Spatial.IO.Eps`; `RgbColor` in `IRI.Maptor.Core.Spatial.IO.Dxf`.

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.IO.Eps;
using IRI.Maptor.Core.Spatial.IO.Dxf;      // RgbColor
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;

var point = Geometry<Point>.Create(100.5, 200.3, srid: 4326);

string eps = point.ToEps();        // EPS text
point.SaveAsEps("point.eps");      // write to file

// With styling
var styled = new EpsOptions(
    strokeColor: new RgbColor(255, 0, 0),
    fillColor: new RgbColor(255, 255, 0),
    strokeWidth: 2.0);
point.SaveAsEps("styled.eps", styled);
```

Feature attributes become DSC comments in the header:

```csharp
var feature = new Feature<Point>(point, new Dictionary<string, object>
{
    { "Title", "Sample Feature" },
    { "Creator", "My App" }
});

string eps = feature.ToEps();
// %%Title: Sample Feature
// %%Creator: My App
feature.SaveAsEps("feature.eps");
```

### Reading

Reader extensions live in `IRI.Maptor.Core.Spatial.IO.Eps` (`EpsReaderExtensions`); the underlying
static methods are `EpsReader.Read`, `EpsReader.ReadFromFile`, `EpsReader.ReadFeature`, and
`EpsReader.ReadFeatureFromFile`.

```csharp
using IRI.Maptor.Core.Spatial.IO.Eps;

// From string
string epsContent = File.ReadAllText("geometry.eps");
var geometry = epsContent.FromEps(srid: 4326);

// From file
var fileInfo = new FileInfo("geometry.eps");
var geometry2 = fileInfo.ReadEps(srid: 4326);

// With attributes (%%Title / %%Creator become feature attributes)
var feature = epsContent.FromEpsFeature(srid: 4326, preserveAttributes: true);
var feature2 = fileInfo.ReadEpsFeature(srid: 4326, preserveAttributes: true);
```

A file with several paths parses to a GeometryCollection:

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

var collection = epsContent.FromEps(srid: 4326);
// GeometryCollection containing Point, LineString, and Polygon
```

### Options

```csharp
var options = new EpsOptions
{
    StrokeColor = new RgbColor(0, 0, 255),
    FillColor = new RgbColor(255, 255, 255),
    StrokeWidth = 1.5,                        // default 1.0
    BoundingBoxPadding = 0.1,                 // fraction of bbox, default 0.05
    CoordinatePrecision = 8,                  // decimal places, default 6
    PreserveFeatureAttributes = true,         // default true
    Creator = "My Application",               // default "IRI.Maptor.Core.Spatial"
    Title = "My Document"
};
```

## File structure

Generated files follow the EPSF-3.0 document structuring conventions:

```
%!PS-Adobe-3.0 EPSF-3.0
%%Creator: {Creator}
%%Title: {Title}
%%BoundingBox: {llx} {lly} {urx} {ury}
%%EndComments
... path and styling commands ...
%%EOF
```

## Geometry mapping

| Geometry type | PostScript representation |
|---|---|
| Point | `newpath {x} {y} moveto {x} {y} lineto stroke` (degenerate path) |
| LineString | `newpath` + `moveto`/`lineto` commands + `stroke` |
| Polygon | `newpath` + `moveto`/`lineto` commands + `closepath fill stroke` |
| Multi*/GeometryCollection | one path group per member |

The reader parses `moveto`, `lineto`, `curveto`, `closepath`, `stroke`, and `fill` (long and
single-letter forms). The writer emits `newpath`, `moveto`, `lineto`, `closepath`, `setrgbcolor`,
`setlinewidth`, `stroke`, and `fill`; RGB 0–255 values are converted to PostScript's 0.0–1.0
range.

## Limitations

- Cubic Bezier `curveto` segments are approximated as line segments on read; arcs and other
  operators are not parsed.
- Round-trip fidelity is bounded by `CoordinatePrecision` (default 6 decimal places).
- Styling applies on write only; colors and line widths are not read back.
- All feature attributes are written as `%%{key}: {value}` comments, but only `%%Title` and
  `%%Creator` are read back as attributes.
- EPS uses a bottom-left origin; no axis flip is applied.
- Empty geometries produce a file with the default `%%BoundingBox: 0 0 100 100`.
- `EpsOptions.IncludePreview` exists but TIFF preview generation is not performed.

---
[Back to IRI.Maptor.Core.Spatial](../../README.md)
