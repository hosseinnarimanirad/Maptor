# IRI.Maptor.Sta.Pdf

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Pdf?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Sta.Pdf/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Vector PDF export for the Maptor stack, built on PdfSharpCore. It offers two paths: simple export of a single `Geometry<Point>` or `Feature<Point>` to a one-page vector PDF, and decorated map export that composes a print-ready page from raster basemap tiles and vector layers with title, scale bar, graticule, neat line, logos, and toggleable PDF layers.

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Pdf
```

## Features

- Vector rendering (not rasterized) of `Point`, `LineString`, `Polygon` (with holes), and their `Multi*`/`GeometryCollection` forms
- Stroke/fill color, stroke width, and opacity; configurable point radius or custom point markers
- Standard (A0–A5, B2–B4, Letter, Legal, Tabloid), custom, or auto (fit-to-bounds) page sizes in portrait/landscape
- Feature attributes stored as PDF document metadata (Title, Author, Creator, Subject, Keywords) when `PdfOptions.PreserveFeatureAttributes` is `true`
- Decorated map export (`PdfWriter.WriteLayers`): raster basemap plus stacked vector layers, title band, four-segment scale bar with representative fraction, dashed degree graticule with WGS84 labels, double-border neat line, powered-by and company logos, and optional-content (toggleable) PDF layers
- `PdfWriter.ComputeDecoratedMapExtent` returns the map extent adjusted for decoration margins

## Usage

Simple geometry/feature export via the extension methods in `IRI.Maptor.Extensions`:

```csharp
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.Spatial.IO.Dxf;   // RgbColor
using IRI.Maptor.Sta.Spatial.Primitives;

var polygon = Geometry<Point>.Create(points, GeometryType.Polygon, srid: 4326);

polygon.SaveAsPdf("plain.pdf");        // default styling

var options = new PdfOptions(
    strokeColor: new RgbColor(0, 100, 0),
    fillColor: new RgbColor(144, 238, 144),
    strokeWidth: 2.0)
{
    PageSize = PdfPageSize.A4,
    PageOrientation = PdfPageOrientation.Landscape,
    BoundingBoxPadding = 0.05,          // 5% around the geometry
};

byte[] bytes = polygon.ToPdf(options);
polygon.SaveAsPdf("styled.pdf", options);
```

Decorated map export — input coordinates for this path are expected in Web Mercator (EPSG:3857):

```csharp
using IRI.Maptor.Sta.Pdf;

var parcels = new PdfWriter.LayerPdfData
{
    LayerName = "Parcels",
    ZIndex = 1,
    Features = parcelFeatures,          // List<Feature<Point>> in Web Mercator
    Options = new PdfOptions(
        strokeColor: new RgbColor(0, 0, 0),
        fillColor: new RgbColor(255, 255, 0, 60),
        strokeWidth: 1.0),
};

var decorations = new PdfMapDecorations
{
    TitleText = "Site Plan",            // Latin text; use TitlePngBytes for RTL/Persian
    ShowScaleBar = true,
    ShowGraticule = true,
    GraticuleIntervalDegrees = 0.5,
};

byte[] pdf = PdfWriter.WriteLayers(
    layers: new List<PdfWriter.LayerPdfData> { parcels },
    mapExtent: extent,                  // BoundingBox in Web Mercator
    mapScale: 0,                        // advisory; derived from the page transform
    baseOptions: new PdfOptions { PageSize = PdfPageSize.A4 },
    rasterLayers: rasterLayers,         // List<PdfWriter.RasterLayerPdfData>
    supportPdfLayers: true,             // emit toggleable optional content groups
    decorations: decorations);

File.WriteAllBytes("map.pdf", pdf);
```

## Dependencies

- `PdfSharpCore`
- `IRI.Maptor.Sta.Spatial`

## Limitations

- Text is Latin-only — PdfSharpCore has no bidi/RTL shaping. For Persian/Arabic titles, supply a pre-rendered `TitlePngBytes` image.
- The north arrow decoration is not implemented.
- The legend column is fixed-width; content flows one column, then two, then shrinks (to a minimum) and is finally clipped with a "+N" indicator. Legend symbols are drawn as vector art (`PdfLegendSwatch`) and stay crisp at any zoom; a raster swatch is only used as a fallback for symbology the vector path can't express.
- Raster/tile opacity is not applied to drawn images (a PdfSharpCore `DrawImage` limitation); vector opacity works.
- `PdfOptions.PointMarker` only takes effect on the `WriteLayers` map-export path; the simple `ToPdf`/`Write` path always draws a circle of `PointCircleRadius`.
- `PdfOptions.CoordinatePrecision` is currently unused (no coordinate rounding is applied).
- Output is a single page; units are PDF points (1/72 inch) with no DPI setting. Set `PreserveMapScale` plus `PreservedWebMercatorScale` to size the page so the printed map keeps a given scale.
- Embedded decoration fonts are registered process-globally (first registration wins).

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Sta.Pdf/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Sta](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/README.md)
