using System;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Tests.Common;

using WpfPoint = System.Windows.Point;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.MapUi;

/// <summary>
/// Drawing and editing on a real map with the real mouse (see <see cref="MapUiHost"/>): they run only when
/// <c>MAPTOR_UI_TEST</c> is 1. Guides and the cursor box are off unless a check is about them, so a clicked point
/// is exactly where the pointer was; there are no layers, so nothing is snapped to.
/// </summary>
[Collection(WpfCollection.Name)]
public class MapUiDrawEditTest
{
    private static void Plain(MapSettings settings)
    {
        settings.Snapping_ShowGuides = false;
        settings.Snapping_ShowCursorBox = false;
    }

    private static void AssertNear(MapUiHost host, Point expected, Point actual, double pixels = 1.5)
    {
        var tolerance = host.MapLength(pixels);

        Assert.True(
            Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.Y - actual.Y) <= tolerance,
            $"expected ({expected.X:0.00} {expected.Y:0.00}), was ({actual.X:0.00} {actual.Y:0.00}), tolerance {tolerance:0.00}");
    }

    [Fact]
    public void DrawALine_ThenEditIt_ClickAndDragAVertex()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var a = new WpfPoint(200, 200);
            var b = new WpfPoint(400, 200);
            var c = new WpfPoint(400, 380);

            // ---- draw: three clicks, then a click on the last vertex finishes
            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polyline);

            host.Pump(200);

            host.Click(a);
            host.Click(b);
            host.Click(c);

            Assert.False(drawing.IsCompleted, "the drawing ended before it was finished");

            host.Click(c);

            var line = host.Await(drawing, "a click on the last vertex did not finish the line").Result;

            Assert.NotNull(line);
            Assert.Equal(3, line.Points.Count);

            AssertNear(host, host.ToMap(a), line.Points[0]);
            AssertNear(host, host.ToMap(b), line.Points[1]);
            AssertNear(host, host.ToMap(c), line.Points[2]);

            // ---- edit it
            var editing = host.ViewModel.EditAsync(line, null);

            host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the edit did not start");

            host.Pump(300);

            var layer = host.Map.CurrentEditingLayer!;

            Point Vertex(int index)
            {
                var point = layer.GetLastGeometry().Points[index];

                return new Point(point.X, point.Y);
            }

            // a click on a vertex selects it and does not move it
            var before = Vertex(1);

            host.Click(host.OnMap(before));

            Assert.Equal((before.X, before.Y), (Vertex(1).X, Vertex(1).Y));
            Assert.Equal(1, layer.GetVertices().FindSelectedLocatableIndex());

            // a drag moves it to where the pointer is released
            var from = host.OnMap(before);
            var to = new WpfPoint(from.X + 60, from.Y - 90);

            host.Drag(from, to);

            AssertNear(host, host.ToMap(to), Vertex(1));

            // the other vertices did not move
            AssertNear(host, host.ToMap(a), Vertex(0));
            AssertNear(host, host.ToMap(c), Vertex(2));

            // ---- finish
            host.Map.FinishEditing();

            var edited = host.Await(editing, "the edit did not finish").Result;

            Assert.NotNull(edited);
            AssertNear(host, host.ToMap(to), edited.Points[1]);
            Assert.Null(host.Map.CurrentEditingLayer);
        }, Plain);
    }
}
