# IRI.Maptor.Presentation.Wpf

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Presentation.Wpf?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Presentation.Wpf/)
[![Target](https://img.shields.io/badge/net8.0--windows-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

The WPF UI tier of the Maptor stack. It hosts the central `MapViewer` control together with the
MVVM infrastructure, layer model, cartography primitives, map-marker controls, and WPF resource
dictionaries that Maptor-based desktop applications build on. Targets .NET 8.0 on Windows with
both WPF and Windows Forms enabled.

## Installation

```bash
dotnet add package IRI.Maptor.Presentation.Wpf
```

Requires Windows (WPF). Alternatively, add `IRI.Maptor.Presentation.Wpf.csproj` to your solution and
reference it from your WPF app.

## Features

- `MapViewer` control (`IRI.Maptor.Presentation.Wpf.Controls` XAML namespace) with companion views: sketch bar,
  geometry editor, coordinate panel, go-to (geodetic/projected), map extent panel, legends, and
  scalebar.
- MVVM building blocks: `ViewModelBase`, `RelayCommand`, and the abstract `MapViewModelBase` that
  drives the map, plus the `IDialogService` dialog abstraction.
- Layer model: `BaseLayer` and concrete layers such as `FeatureLayer`, `VectorLayer`,
  `RasterLayer`, `TileServiceLayer`, `GridLayer`, `GroupLayer`, `DrawingLayer`,
  `EditableFeatureLayer`, and `ClusteredPointLayer`.
- Ready-made time-based overlays:
  - `DayNightLayers.Create(source)`: the night side with its twilight bands.
  - `SatelliteLayers.Create(source, style?)`: satellite positions, names, and for the selected one its track, the area that sees it and its imaging swath. It also draws the source's region for passes, and a highlighted pass in bold. `SatelliteLayerStyle` sets the colours and sizes. `SatelliteLayers.HitTest(map, source, mouseEvent)` finds the satellite under the mouse, within 10 pixels.
  - `MapClockBinding` keeps either layer in step with a `MapClock`. On each tick it sets the source's time and redraws the layer in place, so the legend row is kept. A burst of ticks gives one redraw.
- Snapping while drawing, measuring and editing: a new point, or a dragged vertex of the geometry being edited, jumps onto a nearby point of the map's vector layers or of the user's drawings. A marker shows the target and a small label beside it says what it is and which layer it belongs to (for example "Vertex — Towers"). It works with any `MapViewer` and needs no set-up:
  - Five kinds, tried in this order, the first one in range winning: line end (filled square), vertex (open square), intersection (X), midpoint (triangle), edge (circle).
  - The sketch bar has a magnet toggle (on by default) and a list to choose the kinds, the drawings (one entry for all of them) and the layers. Holding Ctrl places a point freely.
  - The snap distance and the kinds are map settings: `Snapping_TolerancePixels` (10; `MapSettingsModel` keeps it within 5 to 25) and `Snapping_ToEnds`, `Snapping_ToVertices`, `Snapping_ToIntersections`, `Snapping_ToMidpoints`, `Snapping_ToEdges`. An application saves them with its other map settings and can show the distance in its own settings window.
  - The layers left out are kept in the project file (`MapProject.SnapExcludedLayerNames`, by layer name), and so is the drawings entry (`SnapExcludesDrawings`).
  - The classes are in `Snapping/`: `SnapLayerChoice` (on/off, the drawings entry and the layers, at `SketchBarViewModel.Snapping`), `MapSnapCache` (the index for the current extent; nothing is indexed above 50,000 vertices in view), `MapSnapper` (the point to use for a map point, and where a snap came from) and `SnapMarker`. The engine itself is UI-free: `SnapIndex` and `SnapEngine` in IRI.Maptor.Core.Spatial.
  - Not snapped: rectangle drawing, identify, and markers such as text labels (which are not targets either).
- Guides while drawing a line or polygon and while measuring: near the extension of the segment just drawn, or the perpendicular at its end, the point goes onto a dashed guide; holding Shift locks the direction to 15° steps from that segment (from north for the first one). A line end or vertex still wins over a guide, and where a guide crosses an existing edge the point lands on the crossing. Guides are suspended together with snapping (the magnet toggle, or Ctrl). `MapDrawingAids` weighs snapping and guides for one pointer position and `GuideOverlay` draws the guide; the guide engine is in IRI.Maptor.Core.Spatial.
- A box beside the point being placed shows the segment's ground length (L), its turn from the previous segment (A) and its bearing (B), under what the point snapped to or the guide it is on. Pressing L, A or B and typing a number fixes that value (Tab moves between length and direction, Enter or a click places the point, Esc drops the typed values); a typed length alone is laid along the snapped direction or the guide, so a line can be extended by an exact distance. `SegmentInput` turns the typed characters into a `SegmentConstraint`, `CursorBox` shows the values, and `MapDrawingAids` puts typed values before snapping and guides.
- Guides from an existing edge: the fourth right-click option while drawing makes the edge under the pointer the reference (the same option removes it; nothing is picked by moving the pointer). It is highlighted, and its extension, the perpendicular to it and the parallel to it (through the last placed vertex) become guides; near the perpendicular's foot the point lands on the edge at 90°. Esc or the same option removes it. `ReferenceEdgePicker` holds and picks the edge, `MapSnapper.EdgeAt` finds the edge under a point, and `MapDrawingAids` adds its guides.
- The same aids apply while a vertex of the geometry being edited is dragged: the box, the typed values and the angle steps are for the segment from the vertex before it, and guides come from the segments on both sides (`EditableFeatureLayer.GetVertexNeighbours`). Releasing the mouse places the vertex.
- Two map settings switch the aids, both on by default and shown as check boxes in the snapping pop-up: `Snapping_ShowGuides` (guides, angle steps, the reference edge) and `Snapping_ShowCursorBox` (the box and typed values). An application that stores map settings by name adds these two names to what it saves.
- Cartography primitives: `VisualParameters`, symbologies, and rendering helpers.
- Map markers: WPF user controls for location, label, photo, and textbox markers. The textbox marker is the text label of the map's Text tool: a hover formatting bar, Box / Plain text / Halo display modes, width and rotate handles, saving with the drawings as a GeoJSON point with a `TextSymbolizer` SLD, and printing in the PDF export as vector shapes.
- Localization: RTL-aware language switching (`LanguageCombo`, WPF binding extensions) on top of
  the `LocalizationManager` in IRI.Maptor.Presentation.Core.
- WPF assets: resource dictionaries for converters, colors, fonts, and animations.
- Office helpers: minimal Excel/Word export utilities built on OpenXML.

## Usage

Place a `MapViewer` in a window and initialize it with a presenter derived from
`MapViewModelBase`:

```xml
<Window xmlns:maptor="clr-namespace:IRI.Maptor.Presentation.Wpf.Controls;assembly=IRI.Maptor.Presentation.Wpf" ...>
    <maptor:MapViewer x:Name="map" />
</Window>
```

```csharp
var presenter = new AppViewModel(); // your class derived from MapViewModelBase

await MapInitializationHelper.InitializeMapAsync(this.map, this, presenter);

this.DataContext = presenter;
```

See the WPF gallery for a window per feature - measurement, drawing, legends, markers, identify,
localization: https://github.com/hosseinnarimanirad/Maptor/blob/master/samples/IRI.Maptor.Samples.Wpf.Gallery/README.md

### Bring the resources into your app

Merge the built-in resource dictionaries to unlock the package's colors, fonts, converters, and
animations:

```xml
<!-- App.xaml -->
<Application x:Class="YourApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/IRI.Maptor.Converters.xaml"/>
                <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/IRI.Maptor.Fonts.xaml"/>
                <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/IRI.Maptor.Colors.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Converters are then available by key, e.g. `boolToVisibilityConverter` and
`stringToColorConverter`. An `IRI.Maptor.Animations.xaml` dictionary is also available.

## Dependencies

- Windows only: .NET 8.0 (`net8.0-windows`) with WPF and Windows Forms.
- Builds on the Maptor Sta packages (Spatial, SpatialReferenceSystem, ShapefileFormat, Ogc, ...)
  and IRI.Maptor.Presentation.Core (tile providers, localization store).
- Third-party: MahApps.Metro (+ icon packs), WriteableBitmapEx, Microsoft.Xaml.Behaviors.Wpf,
  DocumentFormat.OpenXml, Stateless.

## See also

- [SLD symbology editor](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Presentation/IRI.Maptor.Presentation.Wpf/Views/Symbology/Sld/README.md)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Presentation.Wpf/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Presentation](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Presentation/README.md)
