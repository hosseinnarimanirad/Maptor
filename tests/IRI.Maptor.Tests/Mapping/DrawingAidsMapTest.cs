using System.Collections.Generic;
using System.Linq;
using System.Threading;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Snapping;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The drawing aids on the map side, below the mouse: how snapping and guides are weighed for one pointer position,
/// and the overlay that draws the guide. The mouse wiring in MapViewer is checked in the running application.
/// </summary>
[Collection(WpfCollection.Name)]
public class DrawingAidsMapTest
{
    private static readonly BoundingBox View = new(0, 0, 1000, 1000);

    // the sketch so far: drawn east from (100 100) to (200 100)
    private static readonly Point Previous = new(100, 100);
    private static readonly Point Last = new(200, 100);

    private static VectorLayer Layer(string name, params Geometry<Point>[] geometries)
        => new(name, geometries.ToList(), VisualParameters.CreateNew(1), LayerType.VectorLayer, RenderMode.Default, RasterizationMethod.GdiPlus);

    private static Geometry<Point> Line(params (double X, double Y)[] points)
        => Geometry<Point>.Create(points.Select(p => new Point(p.X, p.Y)).ToList(), Core.Common.Enums.GeometryType.LineString, SridHelper.WebMercator);

    private static Geometry<Point> PointAt(double x, double y)
        => Geometry<Point>.CreatePointOrLineString(new List<Point> { new(x, y) }, SridHelper.WebMercator);

    /// <summary>Aids over the given layers, one pixel being one map unit (tolerance 10), with the index already built.</summary>
    private static (MapDrawingAids Aids, SnapLayerChoice Choice) Aids(VectorLayer[] layers, System.Func<bool>? guidesEnabled = null)
    {
        var (snapper, choice) = Snapper(layers);

        return (new MapDrawingAids(snapper, guidesEnabled), choice);
    }

    private static (MapSnapper Snapper, SnapLayerChoice Choice) Snapper(VectorLayer[] layers)
    {
        var choice = new SnapLayerChoice();
        var snapper = new MapSnapper(choice, () => View, pixels => pixels, () => layers);

        snapper.BeginSession();

        if (layers.Length > 0)
        {
            // the first request starts the build; wait until the cache holds the first geometry
            snapper.Snap(new Point(-1, -1));

            Assert.True(SpinWait.SpinUntil(() => snapper.SourceOf(0) is not null, 5000), "the snap index was not built");
        }

        return (snapper, choice);
    }

    #region Which point wins

    [Fact]
    public void NoVertexPlacedYet_OnlySnaps()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            var result = aids.Resolve(new Point(300, 103), previous: null, last: null, lockToSteps: false);

            Assert.Null(result.Guide);
            Assert.Null(result.Snap);
            Assert.Equal((300.0, 103.0), (result.Point.X, result.Point.Y));
        });
    }

    [Fact]
    public void NearTheExtensionOfThePreviousSegment_ThePointGoesOnIt()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            var result = aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: false);

            Assert.Equal(GuideKind.Extension, result.Guide!.Guide.Kind);
            Assert.Equal(GuideHit.OnGuide, result.Guide.Hit);
            Assert.Equal((300.0, 100.0), (result.Point.X, result.Point.Y));
            Assert.Null(result.Snap);
        });
    }

    [Fact]
    public void NearThePerpendicular_ThePointGoesOnIt_AndFarFromBothItIsFree()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            var onPerpendicular = aids.Resolve(new Point(203, 300), Previous, Last, lockToSteps: false);
            Assert.Equal(GuideKind.Perpendicular, onPerpendicular.Guide!.Guide.Kind);
            Assert.Equal((200.0, 300.0), (onPerpendicular.Point.X, onPerpendicular.Point.Y));

            var free = aids.Resolve(new Point(300, 180), Previous, Last, lockToSteps: false);
            Assert.Null(free.Guide);
            Assert.Equal((300.0, 180.0), (free.Point.X, free.Point.Y));
        });
    }

    [Fact]
    public void TheFirstSegmentOfAPart_HasNoPreviousSegmentGuides()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            var result = aids.Resolve(new Point(300, 103), previous: null, last: Last, lockToSteps: false);

            Assert.Null(result.Guide);
            Assert.Equal((300.0, 103.0), (result.Point.X, result.Point.Y));
        });
    }

    [Fact]
    public void AStoredPoint_BeatsTheGuide()
    {
        WpfTestHost.Run(() =>
        {
            // a point feature 4 off the extension, where the pointer is
            var (aids, _) = Aids(new[] { Layer("points", PointAt(300, 104)) });

            var result = aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: false);

            Assert.Equal(SnapKind.Vertex, result.Snap!.Kind);
            Assert.Null(result.Guide);
            Assert.Equal((300.0, 104.0), (result.Point.X, result.Point.Y));
        });
    }

    [Fact]
    public void TheGuide_BeatsAPlainEdge()
    {
        WpfTestHost.Run(() =>
        {
            // a line beside the extension, parallel to it: it never crosses the guide
            var (aids, _) = Aids(new[] { Layer("lines", Line((250, 104), (600, 104))) });

            var result = aids.Resolve(new Point(400, 102), Previous, Last, lockToSteps: false);

            Assert.Equal(GuideHit.OnGuide, result.Guide!.Hit);
            Assert.Equal((400.0, 100.0), (result.Point.X, result.Point.Y));
        });
    }

    [Fact]
    public void WhereTheGuideCrossesAnEdge_ThePointLandsOnTheCrossing()
    {
        WpfTestHost.Run(() =>
        {
            // a line standing across the extension at x = 350
            var (aids, _) = Aids(new[] { Layer("lines", Line((350, 0), (350, 400))) });

            var result = aids.Resolve(new Point(348, 102), Previous, Last, lockToSteps: false);

            Assert.Equal(GuideHit.Crossing, result.Guide!.Hit);
            Assert.Equal(350, result.Point.X, 9);
            Assert.Equal(100, result.Point.Y, 9);
            Assert.Equal(0, result.Guide.GeometryIndex);

            // away from the crossing the edge alone is still a snap target
            var onEdge = aids.Resolve(new Point(352, 250), Previous, Last, lockToSteps: false);
            Assert.Equal(SnapKind.Edge, onEdge.Snap!.Kind);
        });
    }

    #endregion

    #region Shift, off and suspended

    [Fact]
    public void LockedToSteps_ThePointIsOnAStepFromThePreviousSegment_WhereverThePointerIs()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            // far from every guide: about 50° to the left of straight on
            var result = aids.Resolve(new Point(400, 340), Previous, Last, lockToSteps: true);

            Assert.Equal(GuideKind.AngleStep, result.Guide!.Guide.Kind);

            var turn = WebMercatorGround.TurnAngle(Previous, Last, result.Point);

            Assert.Equal(-45, turn, 6);
            Assert.Equal(0, System.Math.Abs(turn) % MapDrawingAids.AngleStepDegrees, 6);
        });
    }

    [Fact]
    public void LockedToSteps_OnTheFirstSegment_CountsFromNorth()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            // a bearing of about 28° from (200 100): the 30° step
            var (dx, dy) = WebMercatorGround.DirectionOf(28);
            var result = aids.Resolve(new Point(200 + 300 * dx, 100 + 300 * dy), previous: null, last: Last, lockToSteps: true);

            Assert.Equal(30, WebMercatorGround.Bearing(Last, result.Point), 6);
        });
    }

    [Fact]
    public void LockedToSteps_StillLandsOnACrossingAlongTheStep()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new[] { Layer("lines", Line((350, 0), (350, 400))) });

            // about 4° off straight on, so the 0° step; a little short of the line at x = 350
            var result = aids.Resolve(new Point(346, 110), Previous, Last, lockToSteps: true);

            Assert.Equal(GuideHit.Crossing, result.Guide!.Hit);
            Assert.Equal(350, result.Point.X, 9);
            Assert.Equal(100, result.Point.Y, 9);
        });
    }

    [Fact]
    public void GuidesOff_OnlySnappingIsLeft_AndSnappingOff_NothingIs()
    {
        WpfTestHost.Run(() =>
        {
            var guidesOn = false;
            var (aids, choice) = Aids(new VectorLayer[0], () => guidesOn);

            var withoutGuides = aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: true);
            Assert.Null(withoutGuides.Guide);
            Assert.Equal((300.0, 103.0), (withoutGuides.Point.X, withoutGuides.Point.Y));

            guidesOn = true;
            Assert.NotNull(aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: false).Guide);

            // the magnet button off: guides are suspended with snapping
            choice.IsEnabled = false;

            var suspended = aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: true);
            Assert.Null(suspended.Guide);
            Assert.Equal((300.0, 103.0), (suspended.Point.X, suspended.Point.Y));
        });
    }

    #endregion

    #region The reference edge

    // an existing line standing at x = 400, and a sketch that is neither along nor across it
    private static readonly ReferenceEdge Standing = new(new Point(400, 0), new Point(400, 400));
    private static readonly Point SlantedPrevious = new(100, 50);

    [Fact]
    public void ReferenceEdge_BeforeAnyVertex_OffersItsExtension()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            // beyond the upper end, a little to the side
            var result = aids.Resolve(new Point(403, 600), previous: null, last: null, lockToSteps: false, reference: Standing);

            Assert.Equal(GuideKind.Extension, result.Guide!.Guide.Kind);
            Assert.True(result.Guide.Guide.HasReference);
            Assert.Equal((400.0, 600.0), (result.Point.X, result.Point.Y));

            // beside the edge itself there is no extension
            var beside = aids.Resolve(new Point(403, 200), previous: null, last: null, lockToSteps: false, reference: Standing);
            Assert.Null(beside.Guide);
        });
    }

    [Fact]
    public void ReferenceEdge_Perpendicular_LandsOnItsFoot_AndParallelGoesThroughTheLastVertex()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new[] { Layer("lines", Line((400, 0), (400, 400))) });

            // near where the perpendicular from (200 100) stands on the edge
            var foot = aids.Resolve(new Point(397, 103), SlantedPrevious, Last, lockToSteps: false, reference: Standing);

            Assert.Equal(GuideHit.PerpendicularFoot, foot.Guide!.Hit);
            Assert.Equal(GuideKind.Perpendicular, foot.Guide.Guide.Kind);
            Assert.Equal(400, foot.Point.X, 9);
            Assert.Equal(100, foot.Point.Y, 9);

            // along the perpendicular, away from the edge
            var across = aids.Resolve(new Point(300, 97), SlantedPrevious, Last, lockToSteps: false, reference: Standing);
            Assert.Equal(GuideKind.Perpendicular, across.Guide!.Guide.Kind);
            Assert.Equal(GuideHit.OnGuide, across.Guide.Hit);
            Assert.Equal((300.0, 100.0), (across.Point.X, across.Point.Y));

            // straight up from the last vertex: parallel to the edge
            var parallel = aids.Resolve(new Point(203, 300), SlantedPrevious, Last, lockToSteps: false, reference: Standing);
            Assert.Equal(GuideKind.Parallel, parallel.Guide!.Guide.Kind);
            Assert.Equal((200.0, 300.0), (parallel.Point.X, parallel.Point.Y));

            // a typed length goes along it
            var typed = aids.Resolve(new Point(203, 300), SlantedPrevious, Last, lockToSteps: false, Typed(length: 30), Standing);
            Assert.True(typed.IsDirectionFixed);
            Assert.Equal(200, typed.Point.X, 9);
            Assert.Equal(30, WebMercatorGround.Length(Last, typed.Point), 3);

            // without the reference none of this
            var none = aids.Resolve(new Point(203, 300), SlantedPrevious, Last, lockToSteps: false);
            Assert.Null(none.Guide);
        });
    }

    [Fact]
    public void Snapper_EdgeAt_GivesTheSegmentUnderThePoint_AndItsLayer()
    {
        WpfTestHost.Run(() =>
        {
            var (snapper, choice) = Snapper(new[] { Layer("lines", Line((400, 0), (400, 400), (700, 400))) });

            var edge = snapper.EdgeAt(new Point(403, 200));

            Assert.True(edge!.SameAs(Standing));
            Assert.Equal("lines", edge.Source!.LayerName);

            Assert.True(snapper.EdgeAt(new Point(550, 396))!.SameAs(new ReferenceEdge(new Point(700, 400), new Point(400, 400))));

            Assert.Null(snapper.EdgeAt(new Point(450, 200)));

            choice.IsEnabled = false;
            Assert.Null(snapper.EdgeAt(new Point(403, 200)));
        });
    }

    [Fact]
    public void ReferenceEdgePicker_HoldsTheEdgeItIsGiven_AndTellsWhenItChanges()
    {
        var picker = new ReferenceEdgePicker();
        var changes = 0;
        picker.Changed += () => changes++;

        var other = new ReferenceEdge(new Point(0, 0), new Point(50, 50));

        Assert.Null(picker.Reference);
        Assert.False(picker.Clear());
        Assert.Equal(0, changes);

        picker.Set(Standing);
        Assert.Same(Standing, picker.Reference);

        picker.Set(other);
        Assert.Same(other, picker.Reference);

        Assert.True(picker.Clear());
        Assert.Null(picker.Reference);
        Assert.False(picker.Clear());
        Assert.Equal(3, changes);

        Assert.Throws<ArgumentNullException>(() => picker.Set(null!));
    }

    [Fact]
    public void AVertexBetweenTwoOthers_HasGuidesFromBothSides()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            // ... (100 100) -> (200 100) -> [the vertex] -> (600 400) -> (600 700) ...
            var next = new Point(600, 400);
            var afterNext = new Point(600, 700);

            // straight below (600 400): on the extension of the far side, running back from it
            var far = aids.Resolve(new Point(603, 250), Previous, Last, lockToSteps: false, next: next, afterNext: afterNext);

            Assert.Equal(GuideKind.Extension, far.Guide!.Guide.Kind);
            Assert.Equal((600.0, 250.0), (far.Point.X, far.Point.Y));

            // level with (600 400): square to the far side
            var square = aids.Resolve(new Point(300, 398), Previous, Last, lockToSteps: false, next: next, afterNext: afterNext);

            Assert.Equal(GuideKind.Perpendicular, square.Guide!.Guide.Kind);
            Assert.Equal((300.0, 400.0), (square.Point.X, square.Point.Y));

            // the near side still gives its own
            var near = aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: false, next: next, afterNext: afterNext);
            Assert.Equal((300.0, 100.0), (near.Point.X, near.Point.Y));

            // a typed length is of the segment from the near neighbour
            var typed = aids.Resolve(new Point(603, 250), Previous, Last, lockToSteps: false, Typed(length: 20), next: next, afterNext: afterNext);
            Assert.Equal(20, WebMercatorGround.Length(Last, typed.Point), 3);

            // one far vertex alone is no segment to guide from
            var alone = aids.Resolve(new Point(603, 250), Previous, Last, lockToSteps: false, next: next);
            Assert.Null(alone.Guide);
        });
    }

    private static List<Point> Points(params (double X, double Y)[] points) => points.Select(p => new Point(p.X, p.Y)).ToList();

    private static (double X, double Y)? XY(Point? point) => point is null ? null : (point.X, point.Y);

    [Fact]
    public void VertexNeighbours_OfALine_StopAtItsEnds()
    {
        var line = Points((0, 0), (10, 0), (20, 0), (30, 0));

        var first = EditableFeatureLayer.NeighboursOf(line, 0, isRing: false);
        Assert.Null(first.BeforePrevious);
        Assert.Null(first.Previous);
        Assert.Equal((10.0, 0.0), XY(first.Next));
        Assert.Equal((20.0, 0.0), XY(first.AfterNext));

        var second = EditableFeatureLayer.NeighboursOf(line, 1, isRing: false);
        Assert.Null(second.BeforePrevious);
        Assert.Equal((0.0, 0.0), XY(second.Previous));
        Assert.Equal((20.0, 0.0), XY(second.Next));
        Assert.Equal((30.0, 0.0), XY(second.AfterNext));

        var last = EditableFeatureLayer.NeighboursOf(line, 3, isRing: false);
        Assert.Equal((10.0, 0.0), XY(last.BeforePrevious));
        Assert.Equal((20.0, 0.0), XY(last.Previous));
        Assert.Null(last.Next);
        Assert.Null(last.AfterNext);

        // not a position of the list
        Assert.Null(EditableFeatureLayer.NeighboursOf(line, 4, isRing: false).Previous);
        Assert.Null(EditableFeatureLayer.NeighboursOf(line, -1, isRing: true).Next);
    }

    [Fact]
    public void VertexNeighbours_OfARing_GoRound_ButNeverBackToTheVertexItself()
    {
        var ring = Points((0, 0), (10, 0), (10, 10), (0, 10));

        var first = EditableFeatureLayer.NeighboursOf(ring, 0, isRing: true);
        Assert.Equal((10.0, 10.0), XY(first.BeforePrevious));
        Assert.Equal((0.0, 10.0), XY(first.Previous));
        Assert.Equal((10.0, 0.0), XY(first.Next));
        Assert.Equal((10.0, 10.0), XY(first.AfterNext));

        // two points: each is the other's neighbour on both sides, and there is nothing beyond
        var pair = EditableFeatureLayer.NeighboursOf(Points((0, 0), (10, 0)), 0, isRing: true);
        Assert.Equal((10.0, 0.0), XY(pair.Previous));
        Assert.Equal((10.0, 0.0), XY(pair.Next));
        Assert.Null(pair.BeforePrevious);
        Assert.Null(pair.AfterNext);

        var alone = EditableFeatureLayer.NeighboursOf(Points((0, 0)), 0, isRing: true);
        Assert.Null(alone.Previous);
        Assert.Null(alone.Next);
    }

    #endregion

    #region Typed values

    private static SegmentConstraint Typed(double? length = null, double? angle = null, bool bearing = false)
        => new() { Length = length, Angle = angle, AngleIsBearing = bearing };

    [Fact]
    public void TypedLength_IsMeasuredTowardsThePointer_OnTheGround()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            // far from every guide
            var result = aids.Resolve(new Point(400, 340), Previous, Last, lockToSteps: false, Typed(length: 25));

            Assert.True(result.IsTyped);
            Assert.False(result.IsDirectionFixed);
            Assert.Null(result.Snap);
            Assert.Null(result.Guide);

            Assert.Equal(25, WebMercatorGround.Length(Last, result.Point), 3);
            Assert.Equal(WebMercatorGround.Bearing(Last, new Point(400, 340)), WebMercatorGround.Bearing(Last, result.Point), 6);
        });
    }

    [Fact]
    public void TypedLength_OnAGuide_GoesAlongTheGuide()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            // near the extension: the length is laid along it (extend the line by a typed distance)
            var result = aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: false, Typed(length: 40));

            Assert.True(result.IsTyped);
            Assert.True(result.IsDirectionFixed);
            Assert.Equal(100, result.Point.Y, 9);
            Assert.True(result.Point.X > Last.X);
            Assert.Equal(40, WebMercatorGround.Length(Last, result.Point), 3);
        });
    }

    [Fact]
    public void TypedLength_NearAStoredPoint_GoesTowardsIt()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new[] { Layer("points", PointAt(300, 200)) });

            var result = aids.Resolve(new Point(298, 203), Previous, Last, lockToSteps: false, Typed(length: 30));

            Assert.True(result.IsTyped);
            Assert.Null(result.Snap);
            Assert.Equal(WebMercatorGround.Bearing(Last, new Point(300, 200)), WebMercatorGround.Bearing(Last, result.Point), 6);
            Assert.Equal(30, WebMercatorGround.Length(Last, result.Point), 3);
        });
    }

    [Fact]
    public void TypedAngle_FixesTheDirection_AndThePointerGivesTheLength()
    {
        WpfTestHost.Run(() =>
        {
            // a point feature under the pointer: a typed angle is not pulled off its direction
            var (aids, _) = Aids(new[] { Layer("points", PointAt(300, 160)) });

            // 90° to the right of east is south
            var result = aids.Resolve(new Point(260, 40), Previous, Last, lockToSteps: true, Typed(angle: 90));

            Assert.True(result.IsTyped);
            Assert.True(result.IsDirectionFixed);
            Assert.Equal(200, result.Point.X, 9);
            Assert.Equal(40, result.Point.Y, 9);

            // a bearing counts from north, whatever the previous segment
            var north = aids.Resolve(new Point(260, 300), Previous, Last, lockToSteps: false, Typed(angle: 0, bearing: true));
            Assert.Equal(200, north.Point.X, 9);
            Assert.Equal(300, north.Point.Y, 9);
        });
    }

    [Fact]
    public void TypedLengthAndAngle_GiveThePoint_WhereverThePointerIs_AlsoWithSnappingOff()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, choice) = Aids(new VectorLayer[0]);

            var typed = Typed(length: 50, angle: -90);

            var here = aids.Resolve(new Point(700, 20), Previous, Last, lockToSteps: false, typed);
            var there = aids.Resolve(new Point(10, 900), Previous, Last, lockToSteps: false, typed);

            Assert.Equal(here.Point.X, there.Point.X, 9);
            Assert.Equal(here.Point.Y, there.Point.Y, 9);

            // 90° to the left of east is north
            Assert.Equal(200, here.Point.X, 6);
            Assert.True(here.Point.Y > Last.Y);
            Assert.Equal(50, WebMercatorGround.Length(Last, here.Point), 3);

            // typed values are not snapping
            choice.IsEnabled = false;

            var off = aids.Resolve(new Point(700, 20), Previous, Last, lockToSteps: false, typed);
            Assert.True(off.IsTyped);
            Assert.Equal(here.Point.Y, off.Point.Y, 9);
        });
    }

    [Fact]
    public void TypedValues_WithoutAStartPoint_OrEmpty_ChangeNothing()
    {
        WpfTestHost.Run(() =>
        {
            var (aids, _) = Aids(new VectorLayer[0]);

            var noStart = aids.Resolve(new Point(300, 103), previous: null, last: null, lockToSteps: false, Typed(length: 25));
            Assert.False(noStart.IsTyped);
            Assert.Equal((300.0, 103.0), (noStart.Point.X, noStart.Point.Y));

            var empty = aids.Resolve(new Point(300, 103), Previous, Last, lockToSteps: false, new SegmentConstraint());
            Assert.False(empty.IsTyped);
            Assert.Equal(GuideKind.Extension, empty.Guide!.Guide.Kind);
        });
    }

    [Fact]
    public void SegmentInput_TurnsTypedCharactersIntoValues()
    {
        var input = new SegmentInput();

        Assert.False(input.IsActive);
        Assert.False(input.Type('5'));

        input.Begin(SegmentField.Length);
        Assert.True(input.IsActive);
        Assert.Null(input.Constraint.Length);

        Assert.True(input.Type('2'));
        Assert.True(input.Type('۵'));           // a Persian digit
        Assert.True(input.Type('.'));
        Assert.False(input.Type('.'));
        Assert.False(input.Type('-'));           // a length has no sign
        Assert.False(input.Type('x'));
        Assert.True(input.Type('5'));

        Assert.Equal("25.5", input.TextOf(SegmentField.Length));
        Assert.Equal(25.5, input.Constraint.Length);

        Assert.True(input.Backspace());
        Assert.True(input.Backspace());
        Assert.Equal(25, input.Constraint.Length);

        // Tab: on to the turn angle, the length stays
        input.Next(turnAvailable: true);
        Assert.Equal(SegmentField.Angle, input.Editing);

        Assert.True(input.Type('-'));
        Assert.Null(input.Constraint.Angle);
        Assert.True(input.Type('3'));
        Assert.True(input.Type('0'));

        Assert.Equal(-30, input.Constraint.Angle);
        Assert.False(input.Constraint.AngleIsBearing);
        Assert.Equal(25, input.Constraint.Length);
        Assert.Equal(string.Empty, input.TextOf(SegmentField.Bearing));

        // and back, with what was typed kept
        input.Next(turnAvailable: true);
        Assert.Equal(SegmentField.Length, input.Editing);
        Assert.Equal("25", input.TextOf(SegmentField.Length));

        // a bearing replaces the turn angle: they are two ways to give one direction
        input.Begin(SegmentField.Bearing);
        Assert.Null(input.Constraint.Angle);
        Assert.False(input.Type('-'));
        Assert.True(input.Type('9'));
        Assert.True(input.Type('0'));
        Assert.True(input.Constraint.AngleIsBearing);
        Assert.Equal(90, input.Constraint.Angle);
        Assert.Equal(string.Empty, input.TextOf(SegmentField.Angle));

        // starting a field again drops its text only
        input.Begin(SegmentField.Length);
        Assert.Null(input.Constraint.Length);
        Assert.Equal(90, input.Constraint.Angle);

        input.Clear();
        Assert.False(input.IsActive);
        Assert.Null(input.Editing);
        Assert.False(input.Constraint.AngleIsBearing);
    }

    [Fact]
    public void SegmentInput_OnTheFirstSegment_TabGoesToTheBearing_AndZeroLengthIsNoLength()
    {
        var input = new SegmentInput();

        input.Begin(SegmentField.Length);
        input.Type('0');
        Assert.Null(input.Constraint.Length);

        input.Next(turnAvailable: false);
        Assert.Equal(SegmentField.Bearing, input.Editing);
        Assert.True(input.Constraint.AngleIsBearing);

        Assert.False(input.Backspace() && input.Constraint.IsActive);
    }

    #endregion

    #region Settings

    [Fact]
    public void GuidesAndTheCursorBox_AreOnByDefault_AndTheSettingsModelPassesThemOn()
    {
        var settings = new IRI.Maptor.Presentation.Core.Data.MapSettings();

        Assert.True(settings.Snapping_ShowGuides);
        Assert.True(settings.Snapping_ShowCursorBox);

        var model = new IRI.Maptor.Presentation.Wpf.Models.Settings.MapSettingsModel(settings);

        var changed = new List<string?>();
        model.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

        model.Snapping_ShowGuides = false;
        model.Snapping_ShowCursorBox = false;

        Assert.False(settings.Snapping_ShowGuides);
        Assert.False(settings.Snapping_ShowCursorBox);
        Assert.Equal(new[] { nameof(settings.Snapping_ShowGuides), nameof(settings.Snapping_ShowCursorBox) }, changed);
    }

    #endregion

    #region Drawing

    [Fact]
    public void CursorBox_ShowsTheValues_TheHeading_AndTheFieldBeingTyped()
    {
        WpfTestHost.Run(() =>
        {
            var canvas = new System.Windows.Controls.Canvas { Width = 400, Height = 300 };
            canvas.Measure(new System.Windows.Size(400, 300));
            canvas.Arrange(new System.Windows.Rect(0, 0, 400, 300));

            var box = new CursorBox(canvas);

            Assert.False(box.IsShown);

            box.Show(new System.Windows.Point(100, 100), "Extension", new CursorBoxValue("25.00 m"), new CursorBoxValue("0.0°"), new CursorBoxValue("90.0°"));

            Assert.True(box.IsShown);
            Assert.Equal("Extension", box.VisibleHeader);
            Assert.Equal("25.00 m", box.VisibleLength);
            Assert.Equal("0.0°", box.VisibleAngle);
            Assert.Equal("90.0°", box.VisibleBearing);
            Assert.True(box.Location.X > 100 && box.Location.Y > 100);

            // the first segment of a part: no heading, no turn angle; the length being typed carries a caret
            box.Show(new System.Windows.Point(100, 100), null, new CursorBoxValue("12", CursorBoxState.Editing), null, new CursorBoxValue("45.0°"));

            Assert.Null(box.VisibleHeader);
            Assert.Null(box.VisibleAngle);
            Assert.StartsWith("12", box.VisibleLength);
            Assert.NotEqual("12", box.VisibleLength);

            // near the lower right corner it flips to the other side of the point
            box.Show(new System.Windows.Point(395, 295), null, new CursorBoxValue("25.00 m"), null, null);

            Assert.True(box.Location.X < 395 && box.Location.Y < 295);
            Assert.Null(box.VisibleBearing);

            box.Hide();
            Assert.False(box.IsShown);
        });
    }

    [Fact]
    public void GuideOverlay_DrawsTheLineAndTheMark_AndHides()
    {
        WpfTestHost.Run(() =>
        {
            var canvas = new System.Windows.Controls.Canvas { Width = 400, Height = 300 };
            var overlay = new GuideOverlay(canvas);

            Assert.False(overlay.IsShown);

            overlay.Show(new System.Windows.Point(10, 20), new System.Windows.Point(900, 20), new System.Windows.Point(120, 20), showMark: true);

            Assert.True(overlay.IsShown);
            Assert.True(overlay.IsMarkShown);
            Assert.Equal((new System.Windows.Point(10, 20), new System.Windows.Point(900, 20)), overlay.Ends);

            // a crossing has the snap marker: the line without the diamond
            overlay.Show(new System.Windows.Point(10, 20), new System.Windows.Point(900, 20), new System.Windows.Point(120, 20), showMark: false);
            Assert.True(overlay.IsShown);
            Assert.False(overlay.IsMarkShown);

            overlay.Hide();
            Assert.False(overlay.IsShown);
            Assert.False(overlay.IsMarkShown);

            // the reference edge is apart from the guide: hiding the guide leaves it
            overlay.ShowReference(new System.Windows.Point(50, 50), new System.Windows.Point(50, 200));
            overlay.Show(new System.Windows.Point(10, 20), new System.Windows.Point(900, 20), new System.Windows.Point(120, 20), showMark: true);
            overlay.Hide();
            Assert.True(overlay.IsReferenceShown);

            overlay.HideReference();
            Assert.False(overlay.IsReferenceShown);
        });
    }

    [Fact]
    public void SnapMarker_CanShowTheLabelAlone()
    {
        WpfTestHost.Run(() =>
        {
            var canvas = new System.Windows.Controls.Canvas { Width = 400, Height = 300 };
            var marker = new SnapMarker(canvas);

            marker.Show(new System.Windows.Point(100, 100), null, "Extension");

            Assert.Equal("Extension", marker.VisibleLabel);
            Assert.All(new[] { SnapKind.End, SnapKind.Vertex, SnapKind.Intersection, SnapKind.Midpoint, SnapKind.Edge },
                       k => Assert.Equal(System.Windows.Visibility.Collapsed, marker.ShapeOf(k)!.Visibility));
        });
    }

    #endregion
}
