# Edit geometry

Draw and edit lines and polygons with everything that helps: snapping to layers and to other drawings, guide lines,
the cursor box with typed lengths and angles, length labels you can move, and the measure panel of the sketch bar.
A block of parcels and a road are built in code, so there is something to snap to without a data file.

## What it shows

- Draw, measure and pan commands: `DrawPolylineCommand`, `DrawPolygonCommand`, `DrawRectangleCommand`,
  `MeasureLengthCommand`, `MeasureAreaCommand`, `PanCommand`.
- `MapLegendView` for the layers that are snapped to, and `MapDrawingLegendView` for the drawings: each drawing's
  menu has **Edit**.
- `SketchBarView`: finish and cancel, the snapping switch and its layers, guides, the cursor box and the measures.
- In an edit: drag a vertex (it snaps). Point at the line: a handle shows under the pointer (a triangle at the middle
  of an edge when midpoint snapping is on); click to add a vertex there, or press and drag to add it and move it at
  once; a right click beside the line offers **Add point**. Drag a length label where it reads best.
  While a point is placed or dragged, type `L` and a length in metres, `A` and a turn angle, or `B` and a bearing.
- **Add a long line** adds a drawing of 2,500 vertices, to see how editing behaves on a big geometry. With all of it
  in view no handles are drawn (more than 2,000 vertices in view) and the map asks to zoom in.

## The essential code

```xml
<Grid>
    <maptor:MapViewer x:Name="map" MapAction="{Binding MapAction, Mode=OneWay}" />
    <maptor:SketchBarView DataContext="{Binding}" />
</Grid>

<maptor:MapLegendView LegendViewModel="{Binding LegendViewModel}" />
<maptor:MapDrawingLegendView />
```

```csharp
// layers in memory are snap targets like any other
presenter.AddLayer(new VectorLayer("Parcels", new MemoryDataSource(features), visual,
                                   LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.DrawingVisual,
                                   ScaleInterval.All));

// a geometry added as a drawing can be edited from the drawings list
presenter.AddDrawingItem(line, "Long line");
```

## How to run

```bash
dotnet run --project samples/IRI.Maptor.Samples.Wpf.Gallery
```

then pick **Edit geometry** in the list. Source: [`EditGeometrySample.xaml`](EditGeometrySample.xaml),
[`EditGeometrySample.xaml.cs`](EditGeometrySample.xaml.cs).

---
[Back to the gallery](../../README.md) · [Samples index](../../../README.md)
