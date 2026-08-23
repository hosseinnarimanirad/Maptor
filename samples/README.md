# Maptor samples

Small, self-contained programs that each show one thing you can do with Maptor. Every sample
has its own folder with a README next to the code, so a sample can be linked to directly —
from an issue, a blog post or a LinkedIn post — and read top to bottom in a few minutes.

## Projects

| Project | Kind | What it is | Run |
|---|---|---|---|
| [IRI.Maptor.Samples.Core](IRI.Maptor.Samples.Core/README.md) | .NET 8 console | A cookbook for the UI-free libraries: one file per sample, a runner that lists and executes them | `dotnet run --project samples/IRI.Maptor.Samples.Core -- <id>` |
| [IRI.Maptor.Samples.Wpf.HelloMap](IRI.Maptor.Samples.Wpf.HelloMap/README.md) | WPF | The smallest complete map application: a `MapViewer`, a base map, a scale bar, a coordinate panel | `dotnet run --project samples/IRI.Maptor.Samples.Wpf.HelloMap` |
| [IRI.Maptor.Samples.Wpf.Gallery](IRI.Maptor.Samples.Wpf.Gallery/README.md) | WPF | One window, one page per UI feature: legend, go to, identify, drawing, markers, Delaunay/Voronoi, localization, ... | `dotnet run --project samples/IRI.Maptor.Samples.Wpf.Gallery` |
| IRI.Maptor.SampleMauiApp | .NET MAUI | Mobile/desktop sample (separate track, not covered by this index) | — |

Prerequisites: .NET 8 SDK; Windows for the WPF projects. Tiles come from the selected online
base-map provider, so the WPF samples need internet access to show a background map.

## Core samples

| Id | Sample | Shows |
|---|---|---|
| `geodesy/precision` | [Coordinate precision](IRI.Maptor.Samples.Core/Geodesy/README.md) | How many metres one decimal place of latitude/longitude is worth, at several latitudes |
| `geodesy/web-mercator-resolution` | [Web Mercator resolution](IRI.Maptor.Samples.Core/Geodesy/README.md#web-mercator-ground-resolution-and-scale) | Metres per pixel and map scale for every tile zoom level |
| `graph/algorithms` | [Graph algorithms](IRI.Maptor.Samples.Core/Graph/README.md) | BFS, DFS, topological sort, strongly connected components, Kruskal and Prim |
| `formats/geojson-vs-shapefile` | [GeoJSON vs Shapefile](IRI.Maptor.Samples.Core/SpatialFormats/README.md) | The same polygons written in both formats and the resulting file sizes |

## WPF samples

| Sample | Shows |
|---|---|
| [Hello map](IRI.Maptor.Samples.Wpf.HelloMap/README.md) | `App.xaml` resources, `MapViewModelBase`, `MapInitializationHelper`, a base map — the complete startup path |
| [Basic map](IRI.Maptor.Samples.Wpf.Gallery/Samples/BasicMap/README.md) | Navigation commands, base-map picker, `Scalebar`, `CoordinatePanelView` |
| [Go to](IRI.Maptor.Samples.Wpf.Gallery/Samples/GoTo/README.md) | `GoToCommand` dialog and `GoToView` hosted inline |
| [Map legend](IRI.Maptor.Samples.Wpf.Gallery/Samples/MapLegend/README.md) | `MapLegendView`, in-memory `VectorLayer`, add shapefile / GeoJSON |
| [Attribute table](IRI.Maptor.Samples.Wpf.Gallery/Samples/AttributeTable/README.md) | `FeatureTabControl`, `SelectedLayer`, opening a layer's table from code |
| [Map markers](IRI.Maptor.Samples.Wpf.Gallery/Samples/MapMarkers/README.md) | `SpecialPointLayer`, `Locateable`, built-in markers, extending `MapViewModelBase` with a command |
| [Drawing and drawing legend](IRI.Maptor.Samples.Wpf.Gallery/Samples/DrawingLegend/README.md) | Draw commands, `MapDrawingLegendView`, `SketchBarView` |
| [Measurement](IRI.Maptor.Samples.Wpf.Gallery/Samples/Measurement/README.md) | `MeasureLengthCommand`, `MeasureAreaCommand` |
| [Identify](IRI.Maptor.Samples.Wpf.Gallery/Samples/Identify/README.md) | `IdentifyModeCommand`, the Identify results window, overlapping layers |
| [Delaunay and Voronoi](IRI.Maptor.Samples.Wpf.Gallery/Samples/DelaunayVoronoi/README.md) | `DelaunayTriangulation`, `VoronoiDiagram`, clipping unbounded cells, toggling layer visibility |
| [Localization and right-to-left](IRI.Maptor.Samples.Wpf.Gallery/Samples/Localization/README.md) | `LanguageCombo`, `LocalizationManager` bindings, `FlowDirection` |

## Conventions

- One folder per sample. The folder holds the code and a `README.md` with: what it shows, the
  essential code, how to run.
- A sample depends on the Maptor libraries only. Gallery samples may additionally use
  `Shell/GalleryMapViewModel.cs`, an empty `MapViewModelBase` subclass — copy it along with the
  sample folder.
- No absolute paths and no external data files: generate data in code, or ship a small asset
  inside the sample folder.
- Core samples: one public static method with `[Sample("<area>/<name>", "<title>")]`; the runner
  finds it by reflection.
- Gallery samples: a `UserControl` in `Samples/<Name>/` plus one line in `Shell/SampleCatalog.cs`.

## Adding a sample

1. Pick the project: UI-free code goes in `IRI.Maptor.Samples.Core`, anything with a `MapViewer`
   goes in `IRI.Maptor.Samples.Wpf.Gallery`.
2. Create the folder and the code following the conventions above.
3. Write the folder's `README.md` (copy a neighbouring one as the template).
4. Add a row to the table in this file.

---
[Back to the solution README](../README.md)
