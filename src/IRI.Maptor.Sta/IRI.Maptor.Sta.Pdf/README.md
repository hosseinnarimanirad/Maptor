# 📄 PDF Support in Maptor

![PDF](https://img.shields.io/badge/PDF-vector-blue)
![Engine](https://img.shields.io/badge/engine-PdfSharpCore-orange)
![.NET](https://img.shields.io/badge/.NET-Standard_2.1-green)

`IRI.Maptor.Sta.Pdf` renders spatial data to vector PDF using **PdfSharpCore**. It offers two paths:

1. **Geometry / feature export** — write a single `Geometry<Point>` or `Feature<Point>` to a one-page vector PDF, with styling and document metadata.
2. **Decorated map export** — compose a print-ready map page from raster basemap tiles and vector layers, with decorations (title, scale bar, graticule, neat line, logos) and toggleable PDF layers.

## ✨ Features

- Vector rendering (not rasterized) of `Point`, `LineString`, `Polygon` (with holes), and their `Multi*` / `GeometryCollection` forms
- Stroke/fill color, stroke width, and opacity; configurable point radius or custom point markers
- Standard (A4, A3, Letter), custom, or auto (fit-to-bounds) page sizes in portrait/landscape
- Feature attributes stored as PDF document metadata (Title, Author, Creator, Subject, Keywords)
- Decorated map export: raster basemap + stacked vector layers, title band, scale bar, degree graticule, neat line, "powered-by" and company logos, and optional-content (toggleable) PDF layers

## ⚙️ Installation

```bash
dotnet add package IRI.Maptor.Sta.Pdf
```

Depends on `PdfSharpCore` and `IRI.Maptor.Sta.Spatial`.

## 🚀 Geometry / Feature Export

The friendliest API is the extension methods in `IRI.Maptor.Extensions`.

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.Spatial.IO.Dxf;   // RgbColor
using IRI.Maptor.Sta.Spatial.Primitives;

var polygon = Geometry<Point>.Create(points, GeometryType.Polygon, srid: 4326);

// Default styling
polygon.SaveAsPdf("plain.pdf");

// With options → bytes or file
var options = new PdfOptions(
    strokeColor: new RgbColor(0, 100, 0),
    fillColor: new RgbColor(144, 238, 144),
    strokeWidth: 2.0)
{
    PageSize = PdfPageSize.A4,
    PageOrientation = PdfPageOrientation.Landscape,
    BoundingBoxPadding = 0.05,   // 5% around the geometry
};

byte[] bytes = polygon.ToPdf(options);
polygon.SaveAsPdf("styled.pdf", options);
```

Features carry metadata into the PDF when `PreserveFeatureAttributes` is `true`:

```csharp
var feature = new Feature<Point>(polygon, new Dictionary<string, object>
{
    ["Title"] = "Sample Area",
    ["Author"] = "GIS Team",
});

feature.SaveAsPdf("feature.pdf");   // Title/Author become PDF metadata
```

The extension methods delegate to `PdfWriter.Write(...)` / `PdfWriter.WriteToFile(...)`, which you can also call directly.

## 🗺️ Decorated Map Export

`PdfWriter.WriteLayers(...)` composes a full map page. Input coordinates for this path are expected in **Web Mercator (EPSG:3857)** — the scale bar and graticule labels rely on it.

```csharp
using IRI.Maptor.Sta.Pdf;

var basemap = new PdfWriter.RasterLayerPdfData
{
    LayerName = "Basemap",
    ZIndex = 0,
    Tiles = tiles,   // List<PdfWriter.RasterTileData> { ImageBytes, WebMercatorExtent }
};

var parcels = new PdfWriter.LayerPdfData
{
    LayerName = "Parcels",
    ZIndex = 1,
    Features = parcelFeatures,   // List<Feature<Point>> in Web Mercator
    Options = new PdfOptions(
        strokeColor: new RgbColor(0, 0, 0),
        fillColor: new RgbColor(255, 255, 0, 60),
        strokeWidth: 1.0),
};

var decorations = new PdfMapDecorations
{
    TitleText = "Site Plan",              // Latin text; use TitlePngBytes for RTL/Persian
    ShowScaleBar = true,
    ShowGraticule = true,
    GraticuleIntervalDegrees = 0.5,
    PrimaryLogoPngBytes = poweredByLogoBytes,
};

byte[] pdf = PdfWriter.WriteLayers(
    layers: new List<PdfWriter.LayerPdfData> { parcels },
    mapExtent: extent,                    // BoundingBox in Web Mercator
    mapScale: 0,                          // currently derived from the page transform
    baseOptions: new PdfOptions { PageSize = PdfPageSize.A4, PageOrientation = PdfPageOrientation.Landscape },
    rasterLayers: new List<PdfWriter.RasterLayerPdfData> { basemap },
    supportPdfLayers: true,               // emit toggleable Optional Content Groups
    decorations: decorations);

File.WriteAllBytes("map.pdf", pdf);
```

`PdfWriter.ComputeDecoratedMapExtent(mapExtent, options, decorations)` returns the extent adjusted for the decoration margins, if you need to align data to the drawn frame.

### What decorations render

| Decoration | Source | Notes |
|------------|--------|-------|
| Title | `TitleText` (Latin) or `TitlePngBytes` (image) | Image is the RTL/Persian workaround |
| Scale bar | `ShowScaleBar` | 4-segment bar + representative fraction (`1 : N`) |
| Graticule | `ShowGraticule`, `GraticuleIntervalDegrees` | Dashed grid + WGS84 degree labels |
| Neat line | always (decorated export) | Double border around the map frame |
| Powered-by logo | `PrimaryLogoPngBytes` or `PrimaryVectorLogo` (`PdfVectorLogo`) | Footer band |
| Company logo | `SecondaryLogoPngBytes` | Title band |
| Point markers | `PdfOptions.PointMarker` (`PdfPointMarker`) | Vector figures or PNG stamp; map-export path only |

> **Not included:** legend and north arrow are not implemented.

## 🔧 PdfOptions

Constructors: `new PdfOptions()` or `new PdfOptions(strokeColor, fillColor?, strokeWidth?, opacity?)`.

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `StrokeColor` / `FillColor` | `RgbColor?` | `null` | Transparent (alpha ≤ 0) is skipped, not painted |
| `StrokeWidth` | `double` | `1.0` | |
| `Opacity` | `double` | `1.0` | Applied to vector colors |
| `PageSize` | `PdfPageSize` | `Auto` | `Auto, A0–A5, B2–B4, Letter, Legal, Tabloid, Custom` |
| `PageOrientation` | `PdfPageOrientation` | `Portrait` | `Portrait, Landscape` |
| `CustomPageWidth` / `CustomPageHeight` | `double?` | `null` | Points (1/72″), for `Custom` |
| `PreserveMapScale` | `bool` | `false` | Map-export path: size a custom page so the map keeps its on-screen scale |
| `PreservedWebMercatorScale` | `double?` | `null` | Web-mercator scale used when `PreserveMapScale` is on |
| `BoundingBoxPadding` | `double` | `0.05` | Fraction of bounds |
| `PointCircleRadius` | `double` | `3.0` | Fallback point size |
| `PointMarker` | `PdfPointMarker?` | `null` | Map-export path only |
| `PreserveFeatureAttributes` | `bool` | `true` | Feature attrs → PDF metadata |
| `Title` / `Author` / `Subject` / `Keywords` | `string?` | `null` | Document metadata |
| `Creator` | `string` | `"IRI.Maptor.Sta.Pdf"` | |

## 📐 Coordinates, Units & Layout

- Output units are PDF **points** (1/72 inch); there is no DPI setting. Portrait presets (points): A0 2384×3370, A1 1684×2384, A2 1191×1684, A3 842×1191, A4 595×842, A5 420×595, B2 1417×2004, B3 1001×1417, B4 709×1001, Letter 612×792, Legal 612×1008, Tabloid 792×1224. See `PdfPageDimensions`.
- The PDF coordinate origin is bottom-left; geometry is transformed and Y-flipped automatically, preserving aspect ratio when fitting the page.
- **Simple export** fits the geometry's own coordinates to the page (any SRID). **Decorated map export** assumes Web Mercator input; graticule labels are converted to WGS84 degrees.

## 📝 Notes & Limitations

- Text is Latin-only — PdfSharpCore has no bidi/RTL shaping. For Persian/Arabic titles, supply a pre-rendered `TitlePngBytes` image.
- Raster/tile `Opacity` is **not** applied to drawn images (a PdfSharpCore `DrawImage` limitation); vector opacity works.
- `PdfOptions.PointMarker` only takes effect on the `WriteLayers` map-export path; the simple `ToPdf`/`Write` path always draws a circle of `PointCircleRadius`.
- All fixed page presets are honored on both the map-export and single-geometry paths (via `PdfPageDimensions`).
- `PdfOptions.CoordinatePrecision` is currently unused (no coordinate rounding is applied).
- Output is a single page. By default the `mapScale` argument of `WriteLayers` is only advisory (the effective scale is derived from the page transform); set `PreserveMapScale` + `PreservedWebMercatorScale` to instead size the page so the printed map keeps that scale.
- Embedded decoration fonts are registered process-globally (first registration wins).

## ✅ Compatibility

Generated PDFs open in Adobe Acrobat, browser viewers (Chrome/Firefox/Edge), and vector editors (Illustrator, Inkscape).

## 🔗 Related

- [IRI.Maptor.Sta.Spatial](../IRI.Maptor.Sta.Spatial/README.md) — core spatial library
- [DXF](../IRI.Maptor.Sta.Spatial/IO/Dxf/README.md) · [SVG](../IRI.Maptor.Sta.Spatial/IO/Svg/README.md) · [EPS](../IRI.Maptor.Sta.Spatial/IO/Eps/README.md) — related vector formats
