using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Tests.Common;

using WpfPoint = System.Windows.Point;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.MapUi;

/// <summary>
/// Snapping to a vector layer, built as the gallery's Edit geometry page builds its parcels: features in memory
/// (MemoryDataSource), added with AddLayer. Runs only when <c>MAPTOR_UI_TEST</c> is 1 (see <see cref="MapUiHost"/>).
/// </summary>
[Collection(WpfCollection.Name)]
public class MapUiSnapLayerTest
{
    [Fact]
    public void ADrawnPoint_SnapsToAVertexOfAVectorLayerInMemory()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            // a square parcel whose corner is at (400 300) on the map control
            var corners = new[] { new WpfPoint(400, 300), new WpfPoint(550, 300), new WpfPoint(550, 420), new WpfPoint(400, 420) };

            var polygon = Geometry<Point>.CreatePolygon(corners.Select(host.ToMap).ToList(), SridHelper.WebMercator);

            var layer = new VectorLayer(
                layerName: "Parcels",
                dataSource: new MemoryDataSource(new List<Feature<Point>> { new(polygon, new Dictionary<string, object> { ["Parcel"] = "1" }) }),
                parameters: new VisualParameters(fill: Colors.Khaki, stroke: Colors.SaddleBrown, strokeThickness: 1, opacity: 0.6),
                type: LayerType.VectorLayer,
                renderMode: RenderMode.Default,
                rasterizationMethod: RasterizationMethod.DrawingVisual,
                visibleRange: ScaleInterval.All);

            host.ViewModel.AddLayer(layer);

            host.Pump(800);

            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polyline);

            host.Pump(200);

            // the first request of a session starts reading the layers: a move, then time
            host.MoveTo(new WpfPoint(200, 200));
            host.Pump(800);

            // 4 px and 3 px from the corner: within the snap distance of 10 px
            host.Click(new WpfPoint(404, 303));
            host.Click(new WpfPoint(250, 150));
            host.Click(new WpfPoint(250, 150));

            var line = host.Await(drawing, "the line was not finished").Result;

            var snapped = line.Points[0];
            var corner = host.ToMap(corners[0]);

            Assert.True(Math.Abs(snapped.X - corner.X) < host.MapLength(0.2) && Math.Abs(snapped.Y - corner.Y) < host.MapLength(0.2),
                $"the point did not snap to the parcel's corner: {Math.Abs(snapped.X - corner.X) / host.MapLength(1):0.0} px, {Math.Abs(snapped.Y - corner.Y) / host.MapLength(1):0.0} px away; " +
                $"snap layers listed: {host.ViewModel.SketchBar.Snapping.Layers.Count}, checked: {host.ViewModel.SketchBar.Snapping.CheckedLayers.Count}, enabled: {host.ViewModel.SketchBar.Snapping.IsEnabled}");
        }, settings =>
        {
            settings.Snapping_ShowGuides = false;
            settings.Snapping_ShowCursorBox = false;
        });
    }
}
