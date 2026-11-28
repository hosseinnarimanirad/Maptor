using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
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
    public void MovingAVertex_ChangesTheGeometryOnce_AndMovesItsMidVertices() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (10, 0), (20, 0)));

        var vertex = layer.GetVertices().Items[1];

        vertex.MoveTo(10, 10);

        var points = layer.GetLastGeometry().Points;

        Assert.Equal((10d, 10d), (points[1].X, points[1].Y));
        Assert.True(layer.HasChanges);

        var mids = layer.GetMidVertices().Items;

        Assert.Equal((5d, 5d), (mids[0].X, mids[0].Y));
        Assert.Equal((15d, 5d), (mids[1].X, mids[1].Y));
    });

    [Fact]
    public void MovingAVertexOfASketch_WhichHasNoMidVertices_Works() => RunWithOwnStyles(() =>
    {
        using var layer = new EditableFeatureLayer(
            "draw", new List<Point> { new(0, 0) }, Transform.Identity, d => d, GeometryType.LineString, (_, _) => { },
            EditableFeatureLayerOptions.CreateDefaultForDrawing(true, true));

        layer.AddVertex(new Point(10, 0));
        layer.AddVertex(new Point(20, 0));

        Assert.Empty(layer.GetMidVertices().Items);

        // the sketch bar's typed coordinates move the selected vertex this way
        layer.GetVertices().Items[2].MoveTo(20, 5);

        var points = layer.GetLastGeometry().Points;

        Assert.Equal((20d, 5d), (points[2].X, points[2].Y));
    });

    // ---- which items are dragged ----

    [Fact]
    public void VerticesAndLabels_AreMovable_InAnEdit_MidVerticesAreNot() => RunWithOwnStyles(() =>
    {
        using var layer = Edit(Line((0, 0), (10, 0), (20, 0)));

        Assert.True(layer.GetVertices().IsMovable);
        Assert.False(layer.GetMidVertices().IsMovable);

        // the user lays the labels out by dragging them
        Assert.True(layer.GetEdgeLengths().IsMovable);
        Assert.True(layer.GetPrimaryVerticesLabels().IsMovable);
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
