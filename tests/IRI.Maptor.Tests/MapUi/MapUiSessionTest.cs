using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Shapes;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Core.Common.Services;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Tests.Common;

using WpfPoint = System.Windows.Point;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.MapUi;

/// <summary>
/// What the mouse does in a drawing or an edit, on a real map with the real mouse (see <see cref="MapUiHost"/>):
/// they run only when <c>MAPTOR_UI_TEST</c> is 1. Each check is one thing a person would otherwise try by hand
/// after a change to drawing or editing.
/// </summary>
[Collection(WpfCollection.Name)]
public class MapUiSessionTest
{
    private static void Plain(MapSettings settings)
    {
        settings.Snapping_ShowGuides = false;
        settings.Snapping_ShowCursorBox = false;
    }

    private static Geometry<Point> Line(MapUiHost host, params WpfPoint[] onMap)
        => Geometry<Point>.Create(onMap.Select(host.ToMap).ToList(), GeometryType.LineString, SridHelper.WebMercator);

    private static void AssertNear(MapUiHost host, Point expected, Point actual, double pixels = 1.5)
    {
        var tolerance = host.MapLength(pixels);

        Assert.True(
            Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.Y - actual.Y) <= tolerance,
            $"expected ({expected.X:0.00} {expected.Y:0.00}), was ({actual.X:0.00} {actual.Y:0.00}), tolerance {tolerance:0.00}");
    }

    private static Point Copy(Point point) => new(point.X, point.Y);

    private static void AssertSameView(MapUiHost host, Point centreBefore)
        => AssertNear(host, centreBefore, host.Map.CurrentExtent.Center, pixels: 0.5);

    [Fact]
    public void AClickOnAVertexOfASketchThatIsNotValidYet_KeepsTheSketch()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var a = new WpfPoint(250, 250);
            var b = new WpfPoint(450, 300);

            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polyline);

            host.Pump(200);

            host.Click(a);

            // a line of one point cannot be finished: the click on its point does nothing
            host.Click(a);

            Assert.False(drawing.IsCompleted, "a click on the only point ended the drawing");

            host.Click(b);
            host.Click(b);

            var line = host.Await(drawing, "the line was not finished").Result;

            Assert.Equal(2, line.Points.Count);
            AssertNear(host, host.ToMap(a), line.Points[0]);
            AssertNear(host, host.ToMap(b), line.Points[1]);
        }, Plain);
    }

    [Fact]
    public void AVertexBesideASnapPoint_StaysOnAClick_AndSnapsOnADrag()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var a = new WpfPoint(200, 300);
            var b = new WpfPoint(400, 300);
            var c = new WpfPoint(600, 300);

            // another feature with a vertex 6 px right and 4 px below b: inside the snap distance of 10 px
            var target = new WpfPoint(406, 304);

            host.ViewModel.SketchBar.Snapping.IsEnabled = true;
            host.ViewModel.SketchBar.Snapping.IsDrawingsChecked = true;

            host.ViewModel.AddDrawingItem(Line(host, target, new WpfPoint(406, 500)), "target");

            host.Pump(300);

            var editing = host.ViewModel.EditAsync(Line(host, a, b, c), null);

            host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the edit did not start");

            host.Pump(300);

            var layer = host.Map.CurrentEditingLayer!;

            Point Vertex(int index) => Copy(layer.GetLastGeometry().Points[index]);

            // a first drag, of another vertex, so that what can be snapped to has been read
            host.Drag(c, new WpfPoint(620, 360), steps: 12);

            host.Pump(500);

            var before = Vertex(1);

            host.Click(b);

            Assert.Equal((before.X, before.Y), (Vertex(1).X, Vertex(1).Y));

            // dragged away and back to 2 px from the other vertex: it lands on that vertex
            host.MoveTo(b);
            host.Down();
            host.MoveTo(new WpfPoint(330, 220));
            host.MoveTo(new WpfPoint(380, 280));
            host.Pump(300);
            host.MoveTo(new WpfPoint(404, 302));
            host.Pump(200);
            host.MoveTo(new WpfPoint(404, 303));

            host.Snapshot("snap-while-dragging");

            host.Up();
            host.Pump(600);

            AssertNear(host, host.ToMap(target), Vertex(1), pixels: 0.2);

            layer.CancelEditingCommand.Execute(null);

            host.Await(editing, "the edit was not cancelled");
        }, Plain);
    }

    [Fact]
    public void ALabel_IsDragged_AndTheMapStays()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var options = EditableFeatureLayerOptions.CreateDefaultForEditing(true, true);

            options.IsMeasureVisible = true;
            options.IsEdgeLabelVisible = true;

            var editing = host.ViewModel.EditAsync(Line(host, new WpfPoint(200, 400), new WpfPoint(600, 400), new WpfPoint(600, 150)), options);

            host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the edit did not start");

            host.Pump(400);

            var layer = host.Map.CurrentEditingLayer!;

            // the label of the first edge; it stands on its point, so it is grabbed a little above it
            var label = layer.GetEdgeLengths().Items[0];

            host.Pump(300);

            var centre = Copy(host.Map.CurrentExtent.Center);

            var place = host.OnMap(new Point(label.X, label.Y));

            var grab = new WpfPoint(place.X, place.Y - 8);

            host.Drag(grab, new WpfPoint(grab.X + 40, grab.Y - 70));

            var moved = host.OnMap(new Point(label.X, label.Y));

            Assert.True(Math.Abs(moved.X - (place.X + 40)) <= 3 && Math.Abs(moved.Y - (place.Y - 70)) <= 3,
                $"the label is at ({moved.X:0} {moved.Y:0}), expected about ({place.X + 40:0} {place.Y - 70:0})");

            AssertSameView(host, centre);

            // and a vertex of its edge takes it along, at the same distance
            var middleBefore = host.OnMap(new Point((layer.GetVertices().Items[0].X + layer.GetVertices().Items[1].X) / 2, (layer.GetVertices().Items[0].Y + layer.GetVertices().Items[1].Y) / 2));

            host.Drag(new WpfPoint(200, 400), new WpfPoint(200, 500));

            var middleAfter = host.OnMap(new Point((layer.GetVertices().Items[0].X + layer.GetVertices().Items[1].X) / 2, (layer.GetVertices().Items[0].Y + layer.GetVertices().Items[1].Y) / 2));

            var after = host.OnMap(new Point(label.X, label.Y));

            Assert.True(
                Math.Abs((after.X - moved.X) - (middleAfter.X - middleBefore.X)) <= 2 &&
                Math.Abs((after.Y - moved.Y) - (middleAfter.Y - middleBefore.Y)) <= 2,
                "the dragged label did not follow the middle of its edge");

            host.Snapshot("labels-after-drag");

            layer.CancelEditingCommand.Execute(null);

            host.Await(editing, "the edit was not cancelled");
        }, Plain);
    }

    private static Geometry<Point> StartEdit(MapUiHost host, out Task<Response<Geometry<Point>>> editing, params WpfPoint[] onMap)
    {
        var line = Line(host, onMap);

        editing = host.ViewModel.EditAsync(line, null);

        host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the edit did not start");

        host.Pump(300);

        return line;
    }

    private static void CancelEdit(MapUiHost host, Task<Response<Geometry<Point>>> editing)
    {
        host.Map.CurrentEditingLayer?.CancelEditingCommand.Execute(null);

        host.Await(editing, "the edit was not cancelled");
    }

    private static Point[] Points(MapUiHost host) => host.Map.CurrentEditingLayer!.GetLastGeometry().Points.Select(Copy).ToArray();

    [Fact]
    public void AClickOnAnEdge_AddsAVertexThere_AndTheMapStays()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(600, 300));

            var centre = Copy(host.Map.CurrentExtent.Center);

            // 4 px beside the edge, far from its middle: the vertex goes on the edge, under the pointer
            host.MoveTo(new WpfPoint(280, 296));

            host.Snapshot("edge-handle");

            host.Click(new WpfPoint(280, 296));

            var points = Points(host);

            Assert.Equal(3, points.Length);
            AssertNear(host, host.ToMap(new WpfPoint(280, 300)), points[1]);

            AssertSameView(host, centre);

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void AClickNearTheMiddleOfAnEdge_AddsAVertexOnTheMiddle_WhenMidpointsSnap()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            host.ViewModel.SketchBar.Snapping.IsEnabled = true;

            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(600, 300));

            // 7 px from the middle along the edge, 3 px beside it: within the snap distance (10 px) of the middle
            host.MoveTo(new WpfPoint(407, 303));

            host.Snapshot("edge-handle-middle");

            host.Click(new WpfPoint(407, 303));

            var points = Points(host);

            Assert.Equal(3, points.Length);
            AssertNear(host, host.ToMap(new WpfPoint(400, 300)), points[1], pixels: 0.2);

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void AClickNearTheMiddle_StaysUnderThePointer_WhenSnappingIsOff()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            host.ViewModel.SketchBar.Snapping.IsEnabled = false;

            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(600, 300));

            host.Click(new WpfPoint(407, 303));

            AssertNear(host, host.ToMap(new WpfPoint(407, 300)), Points(host)[1]);

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void APressOnAnEdgeAndADrag_AddsAVertexWhereItIsReleased()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(600, 300));

            var centre = Copy(host.Map.CurrentExtent.Center);

            host.Drag(new WpfPoint(300, 302), new WpfPoint(340, 180));

            var points = Points(host);

            // the vertex starts on the edge, 2 px above the press, and keeps that distance from the pointer (as a
            // vertex grabbed off its centre does): it never jumps
            Assert.Equal(3, points.Length);
            AssertNear(host, host.ToMap(new WpfPoint(340, 178)), points[1]);

            // the ends did not move, and neither did the map
            AssertNear(host, host.ToMap(new WpfPoint(200, 300)), points[0]);
            AssertNear(host, host.ToMap(new WpfPoint(600, 300)), points[2]);
            AssertSameView(host, centre);

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void APressBesideTheReachOfAnEdge_PansTheMap_AndAddsNothing()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(600, 300));

            var centre = Copy(host.Map.CurrentExtent.Center);

            // 10 px from the edge: the reach is 6 px
            host.Drag(new WpfPoint(400, 310), new WpfPoint(400, 410), steps: 10);

            host.Pump(300);

            Assert.Equal(2, Points(host).Length);
            Assert.True(Math.Abs(host.Map.CurrentExtent.Center.Y - centre.Y) > host.MapLength(50), "the map did not pan");

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void APressNearAVertex_MovesTheVertex_AndAddsNothing()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(400, 300), new WpfPoint(600, 300));

            // on the edge, 5 px from the middle vertex: the vertex is meant
            host.Drag(new WpfPoint(405, 300), new WpfPoint(405, 220));

            var points = Points(host);

            Assert.Equal(3, points.Length);
            AssertNear(host, host.ToMap(new WpfPoint(400, 220)), points[1]);

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void ARightClickBesideAnEdge_OffersAddPoint_WhichAddsAVertexThere()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(600, 300));

            host.MoveTo(new WpfPoint(260, 304));
            host.RightClick();

            var options = host.FindInMap<IRI.Maptor.Presentation.Wpf.Controls.MapOptions.MapThreeOptions>();

            Assert.NotNull(options);

            var menu = (IRI.Maptor.Presentation.Wpf.ViewModels.MapOptionsViewModel)options!.DataContext;

            Assert.Equal(IRI.Maptor.Presentation.Core.Properties.Resources.map_draw_addPoint, menu.MiddleToolTip);

            menu.MiddleCommand.Execute(null);

            host.Pump(300);

            var points = Points(host);

            Assert.Equal(3, points.Length);
            AssertNear(host, host.ToMap(new WpfPoint(260, 300)), points[1]);

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void AboveTheLimitInView_NoHandles_AndAPressPans_ZoomedInTheyAreBack()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            // 2,500 vertices across the window, all in view: over the limit of 2,000
            var count = 2_500;

            var onMap = Enumerable.Range(0, count).Select(i => new WpfPoint(100 + i * 700.0 / count, 300 + (i % 2))).ToArray();

            StartEdit(host, out var editing, onMap);

            int Handles() => host.CountInMap<IRI.Maptor.Presentation.Wpf.Controls.MapMarkers.VertexHandle>(_ => true);

            Assert.Equal(0, Handles());

            // no edge handle either: a press on the line pans the map and adds nothing
            var centre = Copy(host.Map.CurrentExtent.Center);

            host.Drag(new WpfPoint(400, 300), new WpfPoint(400, 380), steps: 8);

            host.Pump(500);

            Assert.Equal(count, Points(host).Length);
            Assert.True(Math.Abs(host.Map.CurrentExtent.Center.Y - centre.Y) > host.MapLength(30), "the map did not pan");

            // zoomed in on a tenth of the line: its vertices get their handles
            var first = Points(host)[1_000];
            var last = Points(host)[1_200];

            host.Map.ZoomToExtent(new IRI.Maptor.Core.Common.Primitives.BoundingBox(first.X, first.Y - 50, last.X, last.Y + 50));

            host.WaitFor(() => Handles() > 0, "no handles after zooming in");

            Assert.True(Handles() <= IRI.Maptor.Presentation.Wpf.Layers.EditableFeatureLayer.MaxHandlesInView);

            host.Snapshot("handles-after-zoom-in");

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void TheLook_OfASketchAndAnEdit()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            // a polygon half drawn: two placed segments solid, the pointer's segment and the closing one dashed
            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polygon);

            host.Pump(200);

            host.Click(new WpfPoint(200, 380));
            host.Click(new WpfPoint(420, 360));
            host.Click(new WpfPoint(480, 200));

            host.MoveTo(new WpfPoint(300, 170));
            host.Pump(200);

            host.Snapshot("look-sketch");

            Assert.Equal(2, host.CountInMap<System.Windows.Shapes.Path>(p => p.StrokeDashArray is { Count: > 0 } && !p.IsHitTestVisible && p.Fill is null));

            host.Click(new WpfPoint(300, 170));
            host.Click(new WpfPoint(300, 170));

            var polygon = host.Await(drawing, "the polygon was not finished").Result;

            // the edit: square handles, the first one larger, one selected, the edge handle under the pointer
            var editing = host.ViewModel.EditAsync(polygon, null);

            host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the edit did not start");

            host.Pump(300);

            host.Click(new WpfPoint(480, 200));

            host.MoveTo(new WpfPoint(330, 372));
            host.Pump(200);

            host.Snapshot("look-edit");

            var handles = host.Map.CurrentEditingLayer!.GetVertices().Items.Select(v => v.CreatedElement).OfType<IRI.Maptor.Presentation.Wpf.Controls.MapMarkers.VertexHandle>().ToList();

            Assert.Equal(4, handles.Count);
            Assert.Single(handles, h => h.IsFirst);
            Assert.Single(handles, h => h.IsSelected);

            // the add-vertex cursor over the edge, the move cursor on the handles
            Assert.NotEqual(System.Windows.Input.Cursors.Hand, host.Map.Cursor);
            Assert.All(handles, h => Assert.NotNull(h.Cursor));

            CancelEdit(host, editing);
        }, Plain);
    }

    // ---- step 7: the feel ----

    private static string NotEnoughPoints(int needed)
        => string.Format(IRI.Maptor.Presentation.Core.Localization.LocalizationManager.Instance["map_draw_notEnoughPoints"],
                         IRI.Maptor.Presentation.Core.Localization.LocalizationManager.GetLocalizedNumberString(needed));

    private static bool HintShown(MapUiHost host, string text)
        => host.CountInMap<System.Windows.Controls.TextBlock>(t => t.Text == text && t.IsVisible) > 0;

    [Fact]
    public void Backspace_RemovesTheLastPoint_AndEnter_Finishes()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polyline);

            host.Pump(200);

            host.Click(new WpfPoint(200, 300));
            host.Click(new WpfPoint(350, 250));
            host.Click(new WpfPoint(500, 320));

            host.MoveTo(new WpfPoint(560, 360));

            // the last point goes, the drawing goes on
            host.Press(System.Windows.Input.Key.Back);

            Assert.False(drawing.IsCompleted);

            host.Press(System.Windows.Input.Key.Enter);

            var line = host.Await(drawing, "Enter did not finish the line").Result;

            Assert.Equal(2, line.Points.Count);
            AssertNear(host, host.ToMap(new WpfPoint(350, 250)), line.Points[1]);
        }, Plain);
    }

    [Fact]
    public void Enter_OnASketchNotValidYet_SaysWhy_AndBackspaceOnTheOnlyPointCancels()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polygon);

            host.Pump(200);

            host.Click(new WpfPoint(200, 300));
            host.Click(new WpfPoint(350, 250));

            host.MoveTo(new WpfPoint(400, 380));

            host.Press(System.Windows.Input.Key.Enter);

            Assert.False(drawing.IsCompleted, "a polygon of two points was finished");
            Assert.True(HintShown(host, NotEnoughPoints(3)), "no note beside the pointer");

            host.Snapshot("why-not");

            host.Press(System.Windows.Input.Key.Back);
            host.Press(System.Windows.Input.Key.Back);

            var result = host.Await(drawing, "Backspace on the only point did not end the sketch");

            Assert.True(result.IsCanceled);
        }, Plain);
    }

    [Fact]
    public void ADoubleClick_FinishesALine_WithoutAnExtraPoint_AndDoesNotZoom()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polyline);

            host.Pump(200);

            var scale = host.Map.MapScale;

            host.Click(new WpfPoint(200, 300));
            host.Click(new WpfPoint(350, 250));

            host.DoubleClick(new WpfPoint(500, 320));

            var line = host.Await(drawing, "the double click did not finish the line").Result;

            Assert.Equal(3, line.Points.Count);
            AssertNear(host, host.ToMap(new WpfPoint(500, 320)), line.Points[2]);
            Assert.Equal(scale, host.Map.MapScale);
        }, Plain);
    }

    [Fact]
    public void InAnEdit_DeleteRemovesTheSelectedVertex_AndEnterFinishes()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(350, 250), new WpfPoint(500, 320));

            host.Click(new WpfPoint(350, 250));

            host.Press(System.Windows.Input.Key.Delete);

            Assert.Equal(2, Points(host).Length);

            host.Press(System.Windows.Input.Key.Enter);

            var edited = host.Await(editing, "Enter did not finish the edit");

            Assert.False(edited.IsCanceled);
            Assert.Equal(2, edited.Result.Points.Count);
        }, Plain);
    }

    [Fact]
    public void InAnEdit_ARightClickOnAnotherFeaturesEdge_OffersGuides()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            host.ViewModel.SketchBar.Snapping.IsEnabled = true;
            host.ViewModel.SketchBar.Snapping.IsDrawingsChecked = true;

            // another feature: a long horizontal edge across the map
            host.ViewModel.AddDrawingItem(Line(host, new WpfPoint(100, 450), new WpfPoint(800, 450)), "road");

            host.Pump(300);

            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(400, 250), new WpfPoint(600, 300));

            // a first drag, so the snap candidates are read
            host.Drag(new WpfPoint(600, 300), new WpfPoint(620, 310));
            host.Pump(500);

            host.MoveTo(new WpfPoint(500, 451));
            host.RightClick();

            var options = host.FindInMap<IRI.Maptor.Presentation.Wpf.Controls.MapOptions.MapThreeOptions>();

            Assert.NotNull(options);

            var menu = (IRI.Maptor.Presentation.Wpf.ViewModels.MapOptionsViewModel)options!.DataContext;

            Assert.Equal(IRI.Maptor.Presentation.Core.Properties.Resources.map_draw_guidesFromEdge, menu.MiddleToolTip);
            Assert.Equal(IRI.Maptor.Presentation.Core.Properties.Resources.mapPanel_edit_finish, menu.RightToolTip);

            menu.MiddleCommand.Execute(null);

            host.Pump(200);

            // with the guide edge set, a right click on empty map offers to remove it
            host.MoveTo(new WpfPoint(400, 120));
            host.RightClick();

            var again = host.FindInMap<IRI.Maptor.Presentation.Wpf.Controls.MapOptions.MapThreeOptions>();

            Assert.Equal(IRI.Maptor.Presentation.Core.Properties.Resources.map_draw_removeGuideEdge,
                ((IRI.Maptor.Presentation.Wpf.ViewModels.MapOptionsViewModel)again!.DataContext).MiddleToolTip);

            ((IRI.Maptor.Presentation.Wpf.ViewModels.MapOptionsViewModel)again.DataContext).LeftCommand.Execute(null);

            Assert.True(host.Await(editing, "Cancel from the menu did not end the edit").IsCanceled);
        }, Plain);
    }

    [Fact]
    public void NoHandles_StandBetweenTheVertices()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            StartEdit(host, out var editing, new WpfPoint(200, 300), new WpfPoint(400, 300), new WpfPoint(600, 250));

            host.MoveTo(new WpfPoint(400, 500));

            // one handle per vertex, nothing between them
            Assert.Equal(3, host.CountInMap<IRI.Maptor.Presentation.Wpf.Controls.MapMarkers.VertexHandle>(_ => true));

            host.Snapshot("edit-without-mid-vertices");

            CancelEdit(host, editing);
        }, Plain);
    }

    [Fact]
    public void APanWhileDrawing_ReleasedOutsideTheMap_LeavesTheDrawingWorking()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polyline);

            host.Pump(200);

            var a = new WpfPoint(300, 200);

            host.Click(a);

            var first = host.ToMap(a);

            var centre = Copy(host.Map.CurrentExtent.Center);

            // pan with the left button, and let go below the window
            host.Drag(new WpfPoint(500, 400), new WpfPoint(500, host.Map.ActualHeight + 120), steps: 10);

            host.Pump(400);

            var panned = host.Map.CurrentExtent.Center;

            Assert.True(Math.Abs(panned.Y - centre.Y) > host.MapLength(100), "the map did not pan");

            // the next click is a point of the line, where the pointer is on the panned map
            var b = new WpfPoint(450, 320);

            host.Click(b);

            var second = host.ToMap(b);

            host.Click(b);

            var line = host.Await(drawing, "the line was not finished after the pan").Result;

            Assert.Equal(2, line.Points.Count);
            AssertNear(host, first, line.Points[0]);
            AssertNear(host, second, line.Points[1]);
        }, Plain);
    }

    [Fact]
    public void ARectangleDragThatLosesTheMouse_LeavesNothingBehind_AndTheNextDragDraws()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            int Dashed() => host.CountInMap<Rectangle>(r => r.StrokeDashArray is { Count: > 0 });

            var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Rectangle);

            host.Pump(200);

            var before = Dashed();

            host.MoveTo(new WpfPoint(200, 200));
            host.Down();
            host.MoveTo(new WpfPoint(300, 280));
            host.MoveTo(new WpfPoint(380, 340));

            Assert.Equal(before + 1, Dashed());

            // Alt+Tab, or a dialog: the capture goes and the release never reaches the map
            host.LoseCapture();
            host.Up();
            host.Pump(300);

            Assert.Equal(before, Dashed());
            Assert.False(drawing.IsCompleted, "a rectangle was made from a drag that lost the mouse");

            var from = new WpfPoint(250, 220);
            var to = new WpfPoint(520, 430);

            host.Drag(from, to);

            var rectangle = host.Await(drawing, "the next drag did not draw a rectangle").Result;

            var box = rectangle.GetBoundingBox();

            AssertNear(host, host.ToMap(new WpfPoint(from.X, to.Y)), new Point(box.XMin, box.YMin));
            AssertNear(host, host.ToMap(new WpfPoint(to.X, from.Y)), new Point(box.XMax, box.YMax));
        }, Plain);
    }

    [Fact]
    public void DrawingsAndEdits_OneAfterAnother_EachEndClean()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            void AssertAtRest(string after)
            {
                host.Pump(200);

                Assert.True(host.Map.CurrentEditingLayer is null, $"an edit is still open after {after}");
                Assert.True(host.ViewModel.MapStatus == MapStatus.Idle, $"the map is {host.ViewModel.MapStatus} after {after}");
                Assert.True(host.ViewModel.MapAction == MapAction.Pan, $"the tool is {host.ViewModel.MapAction} after {after}");
            }

            Geometry<Point> Draw(WpfPoint from, WpfPoint to)
            {
                var drawing = host.ViewModel.GetDrawingAsync(DrawMode.Polyline);

                host.Pump(200);

                host.Click(from);
                host.Click(to);
                host.Click(to);

                return host.Await(drawing, "a line was not finished").Result;
            }

            // two drawings in a row
            var first = Draw(new WpfPoint(150, 150), new WpfPoint(350, 180));

            AssertAtRest("the first drawing");

            var second = Draw(new WpfPoint(200, 450), new WpfPoint(500, 480));

            AssertAtRest("the second drawing");

            // edit one and finish, then edit the other and cancel
            var editing = host.ViewModel.EditAsync(first, null);

            host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the first edit did not start");

            host.Pump(300);

            host.Drag(new WpfPoint(350, 180), new WpfPoint(420, 260));

            host.Map.FinishEditing();

            var edited = host.Await(editing, "the first edit did not finish");

            Assert.False(edited.IsCanceled);
            AssertNear(host, host.ToMap(new WpfPoint(420, 260)), edited.Result.Points[1]);

            AssertAtRest("the first edit");

            editing = host.ViewModel.EditAsync(second, null);

            host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the second edit did not start");

            host.Pump(300);

            host.Drag(new WpfPoint(500, 480), new WpfPoint(560, 400));

            host.Map.CurrentEditingLayer!.CancelEditingCommand.Execute(null);

            var cancelled = host.Await(editing, "the second edit was not cancelled");

            Assert.True(cancelled.IsCanceled);

            AssertAtRest("the cancelled edit");

            // and the map still pans with the left button
            var centre = Copy(host.Map.CurrentExtent.Center);

            host.Drag(new WpfPoint(400, 300), new WpfPoint(300, 380));

            host.Pump(300);

            Assert.True(Math.Abs(host.Map.CurrentExtent.Center.X - centre.X) > host.MapLength(50), "the map does not pan after the edits");
        }, Plain);
    }

    [Fact]
    public void ATypedLength_MovesAPressedVertex_WithoutAMouseMove()
    {
        if (!MapUiHost.IsEnabled)
            return;

        MapUiHost.Run(host =>
        {
            var a = new WpfPoint(200, 300);
            var b = new WpfPoint(400, 300);

            var editing = host.ViewModel.EditAsync(Line(host, a, b), null);

            host.WaitFor(() => host.Map.CurrentEditingLayer is not null, "the edit did not start");

            host.Pump(300);

            var layer = host.Map.CurrentEditingLayer!;

            var before = Copy(layer.GetLastGeometry().Points[1]);

            // press the second vertex, do not move, type L 5 0: 50 m from the first vertex, in the same direction
            host.MoveTo(b);
            host.Down();

            host.Press(System.Windows.Input.Key.L);

            Assert.Equal((before.X, before.Y), (layer.GetLastGeometry().Points[1].X, layer.GetLastGeometry().Points[1].Y));

            host.Press(System.Windows.Input.Key.D5);
            host.Press(System.Windows.Input.Key.D0);

            host.Snapshot("typed-length");

            host.Up();
            host.Pump(600);

            var first = layer.GetLastGeometry().Points[0];
            var moved = layer.GetLastGeometry().Points[1];

            var ground = WebMercatorGroundLength(first, moved);

            Assert.True(Math.Abs(ground - 50) < 0.5, $"the segment is {ground:0.00} m long, expected 50");

            layer.CancelEditingCommand.Execute(null);

            host.Await(editing, "the edit was not cancelled");
        }, settings => settings.Snapping_ShowGuides = false);
    }

    // the length on the ground of a short Web Mercator segment: its map length times the cosine of its latitude
    private static double WebMercatorGroundLength(Point first, Point second)
    {
        var latitude = MapProjects.WebMercatorToGeodeticWgs84(new Point((first.X + second.X) / 2, (first.Y + second.Y) / 2)).Y;

        var length = Math.Sqrt(Math.Pow(second.X - first.X, 2) + Math.Pow(second.Y - first.Y, 2));

        return length * Math.Cos(latitude * Math.PI / 180);
    }
}
