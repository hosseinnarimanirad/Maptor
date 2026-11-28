using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The geometry being drawn or edited, below the mouse: one change per vertex move, the sketch bar's "Delete point"
/// in any part, and which of the layer's items can be dragged. Synthetic Web Mercator geometries. The mouse wiring
/// in MapViewer is checked in the running application.
/// </summary>
[Collection(WpfCollection.Name)]
public class EditableFeatureLayerEditTest
{
    private static Geometry<Point> Line(params (double X, double Y)[] points)
        => Geometry<Point>.Create(points.Select(p => new Point(p.X, p.Y)).ToList(), GeometryType.LineString, SridHelper.WebMercator);

    private static Geometry<Point> MultiLine(params Geometry<Point>[] parts)
        => Geometry<Point>.Create(parts.ToList(), GeometryType.MultiLineString, SridHelper.WebMercator);

    private static EditableFeatureLayer Edit(Geometry<Point> geometry)
        => new("edit", geometry, Transform.Identity, d => d, (_, _) => { }, EditableFeatureLayerOptions.CreateDefaultForEditing(true, true));

    /// <summary>
    /// Runs <paramref name="body"/> on its own WPF thread with a SharedResourceDictionary cache of its own, emptied first
    /// and again afterwards: the vertex marker's animations come from that cache, and ones made on another test's
    /// finished thread cannot be used here (the same reason as in IdentifyGeometryStripViewTest).
    /// </summary>
    private static void RunWithOwnStyles(Action body)
    {
        static void ClearCache()
        {
            var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
                .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .GetValue(null) as System.Collections.IDictionary;

            lock (cache!)
                cache.Clear();
        }

        WpfTestHost.Run(() =>
        {
            ClearCache();

            try
            {
                body();
            }
            finally
            {
                ClearCache();
            }
        });
    }

    private static int[] PartSizes(EditableFeatureLayer layer)
    {
        var geometry = layer.GetLastGeometry();

        return geometry.Points is not null
            ? new[] { geometry.Points.Count }
            : geometry.Geometries.Select(g => g.Points.Count).ToArray();
    }

    // ---- Locateable.MoveTo ----

    [Fact]
    public void MoveTo_RaisesOnePositionChange_ForTheNewPlace()
    {
        var locateable = new Locateable();

        locateable.MoveTo(1, 2);

        var changes = new List<(System.Windows.Point Old, System.Windows.Point New)>();
        var properties = new List<string?>();

        locateable.OnPositionChanged += (_, e) => changes.Add((e.OldValue, e.NewValue));
        locateable.PropertyChanged += (_, e) => properties.Add(e.PropertyName);

        locateable.MoveTo(10, 20);

        Assert.Equal(new[] { (new System.Windows.Point(1, 2), new System.Windows.Point(10, 20)) }, changes);
        Assert.Equal(new[] { nameof(Locateable.X), nameof(Locateable.Y) }, properties);
        Assert.Equal(new System.Windows.Point(10, 20), locateable.Location);
    }

    [Fact]
    public void MoveTo_TheSamePlace_RaisesNothing()
    {
        var locateable = new Locateable();

        locateable.MoveTo(1, 2);

        var raised = 0;

        locateable.OnPositionChanged += (_, _) => raised++;
        locateable.PropertyChanged += (_, _) => raised++;

        locateable.MoveTo(1, 2);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void SettingOneCoordinate_StillRaisesItsOwnChange()
    {
        var locateable = new Locateable();

        locateable.MoveTo(1, 2);

        var properties = new List<string?>();
        var changes = 0;

        locateable.PropertyChanged += (_, e) => properties.Add(e.PropertyName);
        locateable.OnPositionChanged += (_, _) => changes++;

        locateable.X = 5;

        Assert.Equal(new[] { nameof(Locateable.X) }, properties);
        Assert.Equal(1, changes);
        Assert.Equal(new System.Windows.Point(5, 2), locateable.Location);
    }

    // ---- moving a vertex ----

    [Fact]
    public void MovingAVertex_ChangesTheGeometryOnce() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (10, 0), (20, 0)));

        var vertex = layer.GetVertices().Items[1];

        vertex.MoveTo(10, 10);

        var points = layer.GetLastGeometry().Points;

        Assert.Equal((10d, 10d), (points[1].X, points[1].Y));
        Assert.True(layer.HasChanges);
    });

    [Fact]
    public void MovingAVertexOfASketch_Works() => RunWithOwnStyles(() =>
    {
        using var layer = new EditableFeatureLayer(
            "draw", new List<Point> { new(0, 0) }, Transform.Identity, d => d, GeometryType.LineString, (_, _) => { },
            EditableFeatureLayerOptions.CreateDefaultForDrawing(true, true));

        layer.AddVertex(new Point(10, 0));
        layer.AddVertex(new Point(20, 0));

        // the sketch bar's typed coordinates move the selected vertex this way
        layer.GetVertices().Items[2].MoveTo(20, 5);

        var points = layer.GetLastGeometry().Points;

        Assert.Equal((20d, 5d), (points[2].X, points[2].Y));
    });

    // ---- which items are dragged ----

    [Fact]
    public void VerticesAndLabels_AreMovable_InAnEdit() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (10, 0), (20, 0)));

        Assert.True(layer.GetVertices().IsMovable);

        // the user lays the labels out by dragging them
        Assert.True(layer.GetEdgeLengths().IsMovable);
        Assert.True(layer.GetPrimaryVerticesLabels().IsMovable);
    });

    // ---- the edge handle: finding an edge, inserting a vertex on it ----

    [Fact]
    public void FindEdge_ReturnsTheNearestEdge_AndThePointOnIt() => RunWithOwnStyles(() =>
    {
        // edges: 0 (0 0 → 100 0), 1 (100 0 → 100 100)
        using var layer = Edit(Line((0, 0), (100, 0), (100, 100)));

        var hit = layer.FindEdge(new Point(30, 4), reach: 6, vertexReach: 8);

        Assert.NotNull(hit);
        Assert.Equal(0, hit!.Value.Part);
        Assert.Equal(0, hit.Value.Edge);
        Assert.Equal((30d, 0d), (hit.Value.Point.X, hit.Value.Point.Y));
        Assert.Equal((50d, 0d), (hit.Value.Middle.X, hit.Value.Middle.Y));

        hit = layer.FindEdge(new Point(97, 60), reach: 6, vertexReach: 8);

        Assert.Equal(1, hit!.Value.Edge);
        Assert.Equal((100d, 60d), (hit.Value.Point.X, hit.Value.Point.Y));
    });

    [Fact]
    public void FindEdge_FindsNothing_TooFarFromAnEdge_OrNearAVertex() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (100, 0), (100, 100)));

        // 7 from the edge, the reach is 6
        Assert.Null(layer.FindEdge(new Point(30, 7), reach: 6, vertexReach: 8));

        // on the edge, but 5 from a vertex: the vertex is meant
        Assert.Null(layer.FindEdge(new Point(95, 0), reach: 6, vertexReach: 8));
    });

    [Fact]
    public void FindEdge_OfARing_FindsTheClosingEdge() => RunWithOwnStyles(() =>
    {
        var square = Geometry<Point>.CreatePolygon(new List<Point> { new(0, 0), new(100, 0), new(100, 100), new(0, 100) }, SridHelper.WebMercator);

        using var layer = Edit(square);

        var hit = layer.FindEdge(new Point(2, 50), reach: 6, vertexReach: 8);

        Assert.NotNull(hit);
        Assert.Equal(3, hit!.Value.Edge);
        Assert.Equal((0d, 100d), (hit.Value.Start.X, hit.Value.Start.Y));
        Assert.Equal((0d, 0d), (hit.Value.End.X, hit.Value.End.Y));
    });

    [Fact]
    public void FindEdge_InASecondPart_SaysWhichPart() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(MultiLine(Line((0, 0), (100, 0)), Line((0, 50), (100, 50))));

        var hit = layer.FindEdge(new Point(40, 48), reach: 6, vertexReach: 8);

        Assert.Equal(1, hit!.Value.Part);
        Assert.Equal(0, hit.Value.Edge);
    });

    [Fact]
    public void ASketchBeingDrawn_HasNoEdgeHandle() => RunWithOwnStyles(() =>
    {
        using var layer = new EditableFeatureLayer(
            "draw", new List<Point> { new(0, 0) }, Transform.Identity, d => d, GeometryType.LineString, (_, _) => { },
            EditableFeatureLayerOptions.CreateDefaultForDrawing(true, true));

        layer.AddVertex(new Point(100, 0));

        Assert.False(layer.CanInsertVertices);
        Assert.Null(layer.FindEdge(new Point(50, 1), reach: 6, vertexReach: 8));
    });

    [Fact]
    public void InsertVertex_PutsItInItsPartAfterTheEdgeStart_AndSelectsIt() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(MultiLine(Line((0, 0), (100, 0)), Line((0, 50), (100, 50), (200, 50))));

        var refreshes = 0;

        layer.RequestRefresh = _ => refreshes++;

        var hit = layer.FindEdge(new Point(150, 52), reach: 6, vertexReach: 8)!.Value;

        var vertex = layer.InsertVertex(hit, hit.Point);

        Assert.NotNull(vertex);
        Assert.Equal((150d, 50d), (vertex!.X, vertex.Y));

        var parts = layer.GetLastGeometry().Geometries;

        Assert.Equal(2, parts[0].Points.Count);
        Assert.Equal(new[] { (0d, 50d), (100d, 50d), (150d, 50d), (200d, 50d) }, parts[1].Points.Select(p => (p.X, p.Y)));

        // the fifth vertex over all parts, and the one selected
        Assert.Same(vertex, layer.GetVertices().Items[4]);
        Assert.Equal(4, layer.GetVertices().FindSelectedLocatableIndex());

        Assert.Equal(1, refreshes);
        Assert.True(layer.HasChanges);
    });

    [Fact]
    public void InsertVertex_OnTheClosingEdgeOfARing_AddsItAtTheEnd() => RunWithOwnStyles(() =>
    {
        var square = Geometry<Point>.CreatePolygon(new List<Point> { new(0, 0), new(100, 0), new(100, 100), new(0, 100) }, SridHelper.WebMercator);

        using var layer = Edit(square);

        var hit = layer.FindEdge(new Point(1, 40), reach: 6, vertexReach: 8)!.Value;

        layer.InsertVertex(hit, hit.Middle);

        var ring = layer.GetLastGeometry().Geometries[0].Points;

        Assert.Equal(new[] { (0d, 0d), (100d, 0d), (100d, 100d), (0d, 100d), (0d, 50d) }, ring.Select(p => (p.X, p.Y)));
    });

    [Fact]
    public void AnInsertedVertex_MovesLikeAnyOther() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (100, 0)));

        var hit = layer.FindEdge(new Point(50, 2), reach: 6, vertexReach: 8)!.Value;

        var vertex = layer.InsertVertex(hit, hit.Point)!;

        vertex.MoveTo(50, 30);

        var path = (PathGeometry)layer.GetPath(Transform.Identity).Data;

        Assert.Equal((50d, 30d), (layer.GetLastGeometry().Points[1].X, layer.GetLastGeometry().Points[1].Y));
        Assert.Equal(new System.Windows.Point(50, 30), ((System.Windows.Media.LineSegment)path.Figures[0].Segments[0]).Point);
    });

    // ---- the sketch bar's "Delete point" ----

    [Fact]
    public void DeleteCurrentPoint_InTheSecondPart_DeletesThatVertex() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(MultiLine(Line((0, 0), (10, 0), (20, 0)), Line((0, 50), (10, 50), (20, 50))));

        // the fifth vertex over all parts: the middle one of the second part
        layer.SelectPoint(4);

        layer.TryDeleteCurrentPoint();

        Assert.Equal(new[] { 3, 2 }, PartSizes(layer));

        var second = layer.GetLastGeometry().Geometries[1].Points;

        Assert.Equal(new[] { (0d, 50d), (20d, 50d) }, second.Select(p => (p.X, p.Y)));

        // the selection goes to the vertex before the deleted one
        Assert.Equal(3, layer.GetVertices().FindSelectedLocatableIndex());
    });

    [Fact]
    public void DeleteCurrentPoint_WhenTheFirstPartCannotLoseAPoint_StillDeletesInTheSecond() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(MultiLine(Line((0, 0), (10, 0)), Line((0, 50), (10, 50), (20, 50))));

        layer.SelectPoint(3);

        layer.TryDeleteCurrentPoint();

        Assert.Equal(new[] { 2, 2 }, PartSizes(layer));
    });

    [Fact]
    public void DeleteCurrentPoint_OfTwoVerticesAtTheSamePlace_DeletesTheSelectedOne() => RunWithOwnStyles(() =>
    {
        // both parts pass through (10 10)
        using var layer = Edit(MultiLine(Line((0, 0), (10, 10), (20, 0)), Line((0, 20), (10, 10), (20, 20))));

        layer.SelectPoint(4);

        layer.TryDeleteCurrentPoint();

        Assert.Equal(new[] { 3, 2 }, PartSizes(layer));
    });

    [Fact]
    public void DeleteCurrentPoint_OfALineOfTwoPoints_DeletesNothing_AndKeepsTheSelection() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (10, 0)));

        layer.SelectPoint(1);

        var refreshes = 0;

        layer.RequestRefresh = _ => refreshes++;

        layer.TryDeleteCurrentPoint();

        Assert.Equal(new[] { 2 }, PartSizes(layer));
        Assert.Equal(1, layer.GetVertices().FindSelectedLocatableIndex());
        Assert.Equal(0, refreshes);
    });

    [Fact]
    public void DeleteCurrentPoint_OfTwoVerticesAtTheSamePlaceInOnePart_DeletesTheSelectedOne() => RunWithOwnStyles(() =>
    {
        // the line passes through (10 10) twice: the second and the fourth vertex
        using var layer = Edit(Line((0, 0), (10, 10), (20, 0), (10, 10), (0, 20)));

        layer.SelectPoint(3);

        layer.TryDeleteCurrentPoint();

        Assert.Equal(
            new[] { (0d, 0d), (10d, 10d), (20d, 0d), (0d, 20d) },
            layer.GetLastGeometry().Points.Select(p => (p.X, p.Y)));
    });

    // ---- what a vertex move touches ----

    private static EditableFeatureLayer EditWithLabels(Geometry<Point> geometry)
    {
        var options = EditableFeatureLayerOptions.CreateDefaultForEditing(true, true);

        options.IsMeasureVisible = true;
        options.IsEdgeLabelVisible = true;

        return new EditableFeatureLayer("edit", geometry, Transform.Identity, d => d, (_, _) => { }, options);
    }

    private static string Text(Locateable label) => ((RectangleLabelMarker)label.Element).LabelValue;

    [Fact]
    public void MovingAVertex_UpdatesTheLabelsOfItsTwoEdges_AndKeepsTheOthers() => RunWithOwnStyles(() =>
    {
        // edges: 0 (0→1), 1 (1→2), 2 (2→3); the last item is the label of the whole length
        using var layer = EditWithLabels(Line((0, 0), (100, 0), (200, 0), (300, 0)));

        var labels = layer.GetEdgeLengths().Items;

        Assert.Equal(4, labels.Count);

        var before = labels.ToList();
        var texts = before.Select(Text).ToList();
        var places = before.Select(l => (l.X, l.Y)).ToList();

        layer.GetVertices().Items[1].MoveTo(100, 80);

        // the same labels, not new ones
        Assert.Equal(before, labels.ToList());

        // the two edges of the moved vertex: new length, at the new middle of the edge
        Assert.NotEqual(texts[0], Text(labels[0]));
        Assert.NotEqual(texts[1], Text(labels[1]));
        Assert.Equal((50d, 40d), (labels[0].X, labels[0].Y));
        Assert.Equal((150d, 40d), (labels[1].X, labels[1].Y));

        // the edge it does not touch
        Assert.Equal(texts[2], Text(labels[2]));
        Assert.Equal(places[2], (labels[2].X, labels[2].Y));

        // the whole length changed; the last point did not move, so its label stays where it was
        Assert.NotEqual(texts[3], Text(labels[3]));
        Assert.Equal(places[3], (labels[3].X, labels[3].Y));
    });

    [Fact]
    public void MovingAVertex_KeepsWhereTheUserPutALabel() => RunWithOwnStyles(() =>
    {
        using var layer = EditWithLabels(Line((0, 0), (100, 0), (200, 0)));

        var labels = layer.GetEdgeLengths().Items;

        // the user drags the label of the first edge 30 up from the middle of its edge
        labels[0].MoveTo(50, 30);

        layer.GetVertices().Items[1].MoveTo(100, 80);

        // the middle of the edge went from (50 0) to (50 40): the label is still 30 above it
        Assert.Equal((50d, 70d), (labels[0].X, labels[0].Y));
    });

    [Fact]
    public void MovingTheFirstVertexOfARing_UpdatesTheClosingEdge() => RunWithOwnStyles(() =>
    {
        // edges: 0 (0→1), 1 (1→2), 2 (2→3), 3 (3→0, the closing edge)
        var square = Geometry<Point>.CreatePolygon(new List<Point> { new(0, 0), new(100, 0), new(100, 100), new(0, 100) }, SridHelper.WebMercator);

        using var layer = EditWithLabels(square);

        var labels = layer.GetEdgeLengths().Items;

        Assert.Equal(5, labels.Count);

        var texts = labels.Select(Text).ToList();

        layer.GetVertices().Items[0].MoveTo(-40, -40);

        Assert.NotEqual(texts[0], Text(labels[0]));
        Assert.Equal(texts[1], Text(labels[1]));
        Assert.Equal(texts[2], Text(labels[2]));
        Assert.NotEqual(texts[3], Text(labels[3]));
        Assert.Equal((-20d, 30d), (labels[3].X, labels[3].Y));

        // the area changed too
        Assert.NotEqual(texts[4], Text(labels[4]));
    });

    [Fact]
    public void MovingAVertex_MovesItsOwnPointOfTheScreenPath_InItsOwnPart() => RunWithOwnStyles(() =>
    {
        // both parts pass through (10 10): the second vertex of each
        using var layer = Edit(MultiLine(Line((0, 0), (10, 10), (20, 0)), Line((0, 20), (10, 10), (20, 20))));

        var path = (PathGeometry)layer.GetPath(Transform.Identity).Data;

        // the fifth vertex over all parts: the one of the second part at (10 10)
        layer.GetVertices().Items[4].MoveTo(10, 15);

        Assert.Equal(new System.Windows.Point(10, 10), ((System.Windows.Media.LineSegment)path.Figures[0].Segments[0]).Point);
        Assert.Equal(new System.Windows.Point(10, 15), ((System.Windows.Media.LineSegment)path.Figures[1].Segments[0]).Point);

        var parts = layer.GetLastGeometry().Geometries;

        Assert.Equal((10d, 10d), (parts[0].Points[1].X, parts[0].Points[1].Y));
        Assert.Equal((10d, 15d), (parts[1].Points[1].X, parts[1].Points[1].Y));
    });

    [Fact]
    public void MovingAVertex_UpdatesTheScreenPath_WithoutRebuildingIt() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (10, 0), (20, 0)));

        var path = (PathGeometry)layer.GetPath(Transform.Identity).Data;

        var figure = path.Figures[0];

        layer.GetVertices().Items[0].MoveTo(-5, -5);
        layer.GetVertices().Items[2].MoveTo(25, 5);

        Assert.Same(figure, path.Figures[0]);
        Assert.Equal(new System.Windows.Point(-5, -5), figure.StartPoint);
        Assert.Equal(new System.Windows.Point(25, 5), ((System.Windows.Media.LineSegment)figure.Segments[1]).Point);
    });

    [Fact]
    public void TheMeasures_FollowAVertexMove() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((5_700_000, 4_200_000), (5_700_100, 4_200_000)));

        var before = (layer.EllipsoidalLength, layer.EuclideanLengthInUtm);

        // read twice: the second read comes from the same transformed geometry
        Assert.Equal(before, (layer.EllipsoidalLength, layer.EuclideanLengthInUtm));

        layer.GetVertices().Items[1].MoveTo(5_700_200, 4_200_000);

        Assert.True(layer.EllipsoidalLength > before.EllipsoidalLength * 1.9);
        Assert.True(layer.EuclideanLengthInUtm > before.EuclideanLengthInUtm * 1.9);
    });

    [Fact]
    public void AVertexAddedToASketch_IsFoundByItsPlaceToo() => RunWithOwnStyles(() =>
    {
        using var layer = new EditableFeatureLayer(
            "draw", new List<Point> { new(0, 0) }, Transform.Identity, d => d, GeometryType.LineString, (_, _) => { },
            EditableFeatureLayerOptions.CreateDefaultForDrawing(true, true));

        layer.AddVertex(new Point(10, 0));
        layer.AddVertex(new Point(20, 0));

        var path = (PathGeometry)layer.GetPath(Transform.Identity).Data;

        layer.GetVertices().Items[1].MoveTo(10, 7);

        Assert.Equal(new System.Windows.Point(10, 7), ((System.Windows.Media.LineSegment)path.Figures[0].Segments[0]).Point);
    });

    // ---- the point layer ----

    [Fact]
    public void RemovingById_WhenTwoItemsShareTheId_RemovesBoth() => RunWithOwnStyles(() =>
    {
        var id = Guid.NewGuid();

        var layer = new SpecialPointLayer("labels", new List<Locateable>());

        layer.Items.Add(new Locateable { Id = id });
        layer.Items.Add(new Locateable { Id = id });
        layer.Items.Add(new Locateable { Id = Guid.NewGuid() });

        layer.Remove(id);

        Assert.Single(layer.Items);
    });
}
