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
        var choice = new SnapLayerChoice();
        var snapper = new MapSnapper(choice, () => View, pixels => pixels, () => layers);

        snapper.BeginSession();

        if (layers.Length > 0)
        {
            // the first request starts the build; wait until the cache holds the first geometry
            snapper.Snap(new Point(-1, -1));

            Assert.True(SpinWait.SpinUntil(() => snapper.SourceOf(0) is not null, 5000), "the snap index was not built");
        }

        return (new MapDrawingAids(snapper, guidesEnabled), choice);
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

    #region Drawing

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
