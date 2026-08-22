# SVG

SVG (Scalable Vector Graphics) read/write support for `Geometry<Point>` and `Feature<Point>`.
Coordinates are written directly into element attributes (`points`, `d`, `cx`/`cy`) — the viewBox
is calculated for display only and never transforms them — so geometries round-trip through SVG at
the configured coordinate precision. Ships in the
[IRI.Maptor.Core.Spatial](../../README.md) package; the EPS support in
[`../Eps`](../Eps/README.md) mirrors this API for PostScript output.

## Supported capabilities

| Capability | Supported | Implemented in |
|---|---|---|
| Read (SVG → geometry/feature) | Yes | `SvgReader`, `SvgReaderExtensions` |
| Write (geometry/feature → SVG) | Yes | `SvgWriter`, `ToSvg`/`SaveAsSvg` extensions |
| Styling on write (stroke, fill, width, opacity) | Yes | `SvgOptions` |
| Feature attributes (`id`, `class`, `data-*`) | Yes | `SvgWriter`/`SvgReader` (`PreserveFeatureAttributes`, `preserveAttributes`) |

All geometry types are handled: Point, LineString, Polygon (including holes), MultiPoint,
MultiLineString, MultiPolygon, and GeometryCollection.

## Usage

### Writing

`ToSvg`/`SaveAsSvg` live in the `IRI.Maptor.Extensions` namespace; `SvgOptions` in
`IRI.Maptor.Core.Spatial.IO.Svg`; `RgbColor` in `IRI.Maptor.Core.Spatial.IO.Dxf`.

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.IO.Svg;
using IRI.Maptor.Core.Spatial.IO.Dxf;      // RgbColor
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;

var point = Geometry<Point>.Create(100.5, 200.3, srid: 4326);

string svg = point.ToSvg();        // SVG text
point.SaveAsSvg("point.svg");      // write to file

// With styling
var styled = new SvgOptions(
    strokeColor: new RgbColor(255, 0, 0),
    fillColor: new RgbColor(255, 255, 0, 200),
    strokeWidth: 2.0,
    opacity: 0.8);
point.SaveAsSvg("styled.svg", styled);
```

Features carry their attributes into the output:

```csharp
var feature = new Feature<Point>(point, new Dictionary<string, object>
{
    { "id", "point-1" },
    { "class", "marker" },
    { "name", "Sample" }
});

string svg = feature.ToSvg();
// <circle id="point-1" class="marker" data-name="Sample" ... />
feature.SaveAsSvg("feature.svg");
```

### Reading

Reader extensions live in `IRI.Maptor.Core.Spatial.IO.Svg` (`SvgReaderExtensions`); the underlying
static methods are `SvgReader.Read`, `SvgReader.ReadFromFile`, `SvgReader.ReadFeature`, and
`SvgReader.ReadFeatureFromFile`.

```csharp
using IRI.Maptor.Core.Spatial.IO.Svg;

// From string
string svgContent = File.ReadAllText("geometry.svg");
var geometry = svgContent.FromSvg(srid: 4326);

// From file
var fileInfo = new FileInfo("geometry.svg");
var geometry2 = fileInfo.ReadSvg(srid: 4326);

// With attributes
var feature = svgContent.FromSvgFeature(srid: 4326, preserveAttributes: true);
var feature2 = fileInfo.ReadSvgFeature(srid: 4326, preserveAttributes: true);

if (feature.Attributes.ContainsKey("id"))
    Console.WriteLine($"Feature ID: {feature.Attributes["id"]}");
```

A document with several top-level elements parses to a GeometryCollection:

```csharp
string svgContent = @"
<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 200'>
  <circle cx='50' cy='50' r='3'/>
  <polyline points='100,50 150,100 200,50'/>
  <polygon points='50,150 100,200 150,150'/>
</svg>";

var collection = svgContent.FromSvg(srid: 4326);
// GeometryCollection containing Point, LineString, and Polygon
```

### Options

```csharp
var options = new SvgOptions
{
    StrokeColor = new RgbColor(0, 0, 255),
    FillColor = new RgbColor(255, 255, 255),
    StrokeWidth = 1.5,                 // default 1.0
    Opacity = 0.9,                     // default 1.0
    IncludeViewBox = true,             // default true
    ViewBoxPadding = 0.1,              // fraction of bbox, default 0.05
    CoordinatePrecision = 8,           // decimal places, default 6
    PreserveFeatureAttributes = true,  // default true
    PointCircleRadius = 5.0            // default 3.0
};
```

## Element mapping

| Geometry type | SVG element | Notes |
|---|---|---|
| Point | `<circle>` | `cx`, `cy`, `r` attributes |
| LineString | `<polyline>` | `points` attribute |
| Polygon (single ring) | `<polygon>` | `points` attribute |
| Polygon (with holes) | `<path>` | `d` attribute with M/L/Z commands |
| MultiPoint | `<g>` + circles | group of `<circle>` elements |
| MultiLineString | `<g>` + polylines | group of `<polyline>` elements |
| MultiPolygon | `<g>` + polygons | group of `<polygon>`/`<path>` elements |
| GeometryCollection | `<g>` + mixed | group of the above |

Feature attributes map to SVG attributes: `id` → `id`, `class` → `class`, existing `data-*` keys
are kept as-is, and every other attribute is stored as `data-{key}`.

## Limitations

- Only `circle`, `polyline`, `polygon`, `path`, and `g` elements are parsed; `rect`, `ellipse`,
  `line`, `text`, and transforms are ignored.
- The path parser handles `M`/`L`/`Z` commands; curve and arc commands are approximated as line
  segments.
- Round-trip fidelity is bounded by `CoordinatePrecision` (default 6 decimal places).
- Styling applies on write only; stroke/fill attributes are not read back into options.
- Empty geometries produce an empty SVG document.

---
[Back to IRI.Maptor.Core.Spatial](../../README.md)
