using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>
/// The engine under the drawing aids: ground distance in Web Mercator, guides, the search among them, and a typed
/// length or angle. No map and no mouse here.
/// </summary>
public class DrawingAidsTest
{
    private static Point WebMercator(double longitude, double latitude)
        => MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));

    private static SnapIndex<Point> Index(params string[] wkts)
        => new(wkts.Select(w => (Geometry<Point>?)Geometry<Point>.FromWkt(w, 3857)).ToList());

    #region WebMercatorGround

    public static IEnumerable<object[]> PointAtCases()
    {
        foreach (var latitude in new[] { 0.0, 35.0, 60.0 })
        foreach (var metres in new[] { 1.0, 25.0, 1000.0, 50_000.0 })
        foreach (var bearing in new[] { 0.0, 45.0, 90.0, 135.0, 180.0, 225.0, 270.0, 315.0 })
            yield return new object[] { latitude, metres, bearing };
    }

    [Theory]
    [MemberData(nameof(PointAtCases))]
    public void PointAt_GivesAnEdgeOfTheAskedGroundLength(double latitude, double metres, double bearing)
    {
        var start = WebMercator(51.4, latitude);
        var (dx, dy) = WebMercatorGround.DirectionOf(bearing);

        var end = WebMercatorGround.PointAt(start, dx, dy, metres);

        // what the map's edge label computes for this segment
        var label = SpatialUtility.GetEllipsoidalLength(MapProjects.WebMercatorToGeodeticWgs84(start), MapProjects.WebMercatorToGeodeticWgs84(end));

        Assert.Equal(metres, label, 3);                                   // to the millimetre
        Assert.Equal(metres, WebMercatorGround.Length(start, end), 3);
        Assert.Equal(bearing, WebMercatorGround.Bearing(start, end), 6);  // and still in the asked direction
    }

    [Fact]
    public void PointAt_AtTehransLatitude_IsFartherOnTheMapThanOnTheGround()
    {
        var start = WebMercator(51.4, 35.7);

        var end = WebMercatorGround.PointAt(start, 1, 0, 1000);

        // a map unit is cos(latitude) metres, so 1000 m is about 1000 / cos(35.7°) = 1231 map units
        Assert.InRange(end.X - start.X, 1225, 1237);
    }

    [Fact]
    public void PointAt_DirectionNeedNotBeAUnitVector_AndNoLengthStaysPut()
    {
        var start = WebMercator(51.4, 35.7);

        var unit = WebMercatorGround.PointAt(start, 0, 1, 250);
        var scaled = WebMercatorGround.PointAt(start, 0, 40, 250);

        Assert.Equal(unit.X, scaled.X, 6);
        Assert.Equal(unit.Y, scaled.Y, 6);

        foreach (var end in new[] { WebMercatorGround.PointAt(start, 0, 1, 0), WebMercatorGround.PointAt(start, 0, 1, -5), WebMercatorGround.PointAt(start, 0, 0, 25) })
        {
            Assert.Equal(start.X, end.X);
            Assert.Equal(start.Y, end.Y);
        }
    }

    [Fact]
    public void Bearing_IsClockwiseFromNorth()
    {
        var origin = new Point(0, 0);

        Assert.Equal(0, WebMercatorGround.Bearing(origin, new Point(0, 10)), 9);
        Assert.Equal(90, WebMercatorGround.Bearing(origin, new Point(10, 0)), 9);
        Assert.Equal(180, WebMercatorGround.Bearing(origin, new Point(0, -10)), 9);
        Assert.Equal(270, WebMercatorGround.Bearing(origin, new Point(-10, 0)), 9);
        Assert.Equal(45, WebMercatorGround.Bearing(origin, new Point(10, 10)), 9);
        Assert.Equal(0, WebMercatorGround.Bearing(origin, origin));
    }

    [Fact]
    public void TurnAngle_IsPositiveToTheRight()
    {
        var previous = new Point(0, 0);
        var last = new Point(0, 10);          // going north

        Assert.Equal(0, WebMercatorGround.TurnAngle(previous, last, new Point(0, 20)), 9);
        Assert.Equal(90, WebMercatorGround.TurnAngle(previous, last, new Point(10, 10)), 9);     // east: a right turn
        Assert.Equal(-90, WebMercatorGround.TurnAngle(previous, last, new Point(-10, 10)), 9);   // west: a left turn
        Assert.Equal(180, WebMercatorGround.TurnAngle(previous, last, new Point(0, 0)), 9);      // straight back
    }

    #endregion

    #region GuideBuilder

    [Fact]
    public void FromPreviousSegment_GivesItsExtensionAndItsPerpendicular()
    {
        var guides = GuideBuilder.FromPreviousSegment(new Point(0, 0), new Point(10, 0));

        Assert.Equal(2, guides.Count);

        var extension = guides.Single(g => g.Kind == GuideKind.Extension);
        Assert.True(extension.IsRay);
        Assert.Equal(10, extension.Origin.X);
        Assert.Equal((1.0, 0.0), (extension.DirectionX, extension.DirectionY));
        Assert.False(extension.HasReference);

        var perpendicular = guides.Single(g => g.Kind == GuideKind.Perpendicular);
        Assert.False(perpendicular.IsRay);
        Assert.Equal(10, perpendicular.Origin.X);
        Assert.Equal(0, perpendicular.DirectionX * extension.DirectionX + perpendicular.DirectionY * extension.DirectionY, 12);
    }

    [Fact]
    public void FromReferenceSegment_ExtendsBothEnds_AndWithALastPointAddsPerpendicularAndParallel()
    {
        var start = new Point(0, 0);
        var end = new Point(10, 0);

        var withoutLast = GuideBuilder.FromReferenceSegment(start, end);
        Assert.Equal(2, withoutLast.Count);
        Assert.All(withoutLast, g => Assert.Equal(GuideKind.Extension, g.Kind));
        Assert.All(withoutLast, g => Assert.True(g.IsRay && g.HasReference));

        // one ray beyond each end, pointing away from the segment
        Assert.Contains(withoutLast, g => g.Origin.X == 10 && g.DirectionX == 1);
        Assert.Contains(withoutLast, g => g.Origin.X == 0 && g.DirectionX == -1);

        var last = new Point(4, 7);
        var withLast = GuideBuilder.FromReferenceSegment(start, end, last);

        Assert.Equal(4, withLast.Count);

        var perpendicular = withLast.Single(g => g.Kind == GuideKind.Perpendicular);
        Assert.Equal((4.0, 7.0), (perpendicular.Origin.X, perpendicular.Origin.Y));
        Assert.Equal(0, perpendicular.DirectionX, 12);
        Assert.True(perpendicular.HasReference);

        var parallel = withLast.Single(g => g.Kind == GuideKind.Parallel);
        Assert.Equal((4.0, 7.0), (parallel.Origin.X, parallel.Origin.Y));
        Assert.Equal(0, parallel.DirectionY, 12);
        Assert.False(parallel.IsRay);
    }

    [Fact]
    public void AngleSteps_CountFromThePreviousSegment_OrFromNorth()
    {
        // going east: the steps start at east
        var fromSegment = GuideBuilder.AngleSteps(new Point(0, 0), new Point(10, 0), 15);

        Assert.Equal(24, fromSegment.Count);
        Assert.All(fromSegment, g => Assert.True(g.Kind == GuideKind.AngleStep && g.IsRay && g.Origin.X == 10));
        Assert.Equal(90, WebMercatorGround.BearingOf(fromSegment[0].DirectionX, fromSegment[0].DirectionY), 9);
        Assert.Equal(105, WebMercatorGround.BearingOf(fromSegment[1].DirectionX, fromSegment[1].DirectionY), 9);

        // the first segment of a shape: from north
        var fromNorth = GuideBuilder.AngleSteps<Point>(null, new Point(10, 0), 45);

        Assert.Equal(8, fromNorth.Count);
        Assert.Equal(0, WebMercatorGround.BearingOf(fromNorth[0].DirectionX, fromNorth[0].DirectionY), 9);
        Assert.Equal(45, WebMercatorGround.BearingOf(fromNorth[1].DirectionX, fromNorth[1].DirectionY), 9);
    }

    [Fact]
    public void Builders_GiveNothingForASegmentOfNoLength_AndRejectABadStep()
    {
        var point = new Point(5, 5);

        Assert.Empty(GuideBuilder.FromPreviousSegment(point, new Point(5, 5)));
        Assert.Empty(GuideBuilder.FromReferenceSegment(point, new Point(5, 5), new Point(1, 1)));

        // no previous direction: the steps fall back to north
        Assert.Equal(0, WebMercatorGround.BearingOf(GuideBuilder.AngleSteps(point, new Point(5, 5), 90)[0].DirectionX, GuideBuilder.AngleSteps(point, new Point(5, 5), 90)[0].DirectionY), 9);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => GuideBuilder.AngleSteps(point, new Point(9, 5), 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => GuideBuilder.AngleSteps(point, new Point(9, 5), 200));
    }

    #endregion

    #region GuideEngine

    [Fact]
    public void Find_PutsThePointOnTheGuide_WithinTheTolerance()
    {
        // drawn east to (10 0): the extension runs on along y = 0, the perpendicular along x = 10
        var guides = GuideBuilder.FromPreviousSegment(new Point(0, 0), new Point(10, 0));

        var onExtension = GuideEngine.Find(guides, new Point(30, 2), 5);
        Assert.Equal(GuideHit.OnGuide, onExtension!.Hit);
        Assert.Equal(GuideKind.Extension, onExtension.Guide.Kind);
        Assert.Equal((30.0, 0.0), (onExtension.Location.X, onExtension.Location.Y));
        Assert.Equal(2, onExtension.Distance, 9);
        Assert.Equal(-1, onExtension.GeometryIndex);

        var onPerpendicular = GuideEngine.Find(guides, new Point(12, -40), 5);
        Assert.Equal(GuideKind.Perpendicular, onPerpendicular!.Guide.Kind);
        Assert.Equal((10.0, -40.0), (onPerpendicular.Location.X, onPerpendicular.Location.Y));

        Assert.Null(GuideEngine.Find(guides, new Point(30, 20), 5));
    }

    [Fact]
    public void Find_ARayOffersNothingBehindItsOrigin()
    {
        var extension = GuideBuilder.FromPreviousSegment(new Point(0, 0), new Point(10, 0)).Where(g => g.Kind == GuideKind.Extension).ToList();

        // beside the segment itself, behind the ray's origin at (10 0)
        Assert.Null(GuideEngine.Find(extension, new Point(4, 1), 5));
        Assert.NotNull(GuideEngine.Find(extension, new Point(14, 1), 5));
    }

    [Fact]
    public void Find_NearestGuideWins()
    {
        var guides = GuideBuilder.FromPreviousSegment(new Point(0, 0), new Point(10, 0));

        // 3 from the extension (y = 0), 2 from the perpendicular (x = 10)
        var result = GuideEngine.Find(guides, new Point(12, 3), 5);

        Assert.Equal(GuideKind.Perpendicular, result!.Guide.Kind);
    }

    [Fact]
    public void Find_AGuidesCrossingWithAnEdge_BeatsThePlainGuide()
    {
        // extending east along y = 0 towards a line standing at x = 50
        var guides = GuideBuilder.FromPreviousSegment(new Point(0, 0), new Point(10, 0));
        var index = Index("LINESTRING(0 0, 10 0)", "LINESTRING(50 -30, 50 30)");

        var result = GuideEngine.Find(guides, new Point(48, 2), 5, index);

        Assert.Equal(GuideHit.Crossing, result!.Hit);
        Assert.Equal(50, result.Location.X, 9);
        Assert.Equal(0, result.Location.Y, 9);
        Assert.Equal(1, result.GeometryIndex);

        // away from the crossing: just the guide
        Assert.Equal(GuideHit.OnGuide, GuideEngine.Find(guides, new Point(30, 2), 5, index)!.Hit);

        // the crossed geometry filtered out: just the guide again
        Assert.Equal(GuideHit.OnGuide, GuideEngine.Find(guides, new Point(48, 2), 5, index, g => g != 1)!.Hit);
    }

    [Fact]
    public void Find_PerpendicularFoot_OnTheReferenceSegment_ComesFirst()
    {
        // drawing from (40 30) towards a reference edge along y = 0; the foot is (40 0)
        var guides = GuideBuilder.FromReferenceSegment(new Point(0, 0), new Point(100, 0), new Point(40, 30));
        var index = Index("LINESTRING(0 0, 100 0)");

        var result = GuideEngine.Find(guides, new Point(42, 1), 5, index);

        Assert.Equal(GuideHit.PerpendicularFoot, result!.Hit);
        Assert.Equal(GuideKind.Perpendicular, result.Guide.Kind);
        Assert.Equal(40, result.Location.X, 9);
        Assert.Equal(0, result.Location.Y, 9);

        // the new segment (40 30)→(40 0) stands at 90° on the reference
        Assert.Equal(180, WebMercatorGround.Bearing(new Point(40, 30), result.Location), 9);
    }

    [Fact]
    public void Find_PerpendicularFoot_OnlyWithinTheReferenceSegment()
    {
        // the perpendicular through (140 30) meets the reference's line beyond its end at (100 0): no foot
        var guides = GuideBuilder.FromReferenceSegment(new Point(0, 0), new Point(100, 0), new Point(140, 30))
                                 .Where(g => g.Kind == GuideKind.Perpendicular).ToList();

        var result = GuideEngine.Find(guides, new Point(141, 1), 5);

        Assert.Equal(GuideHit.OnGuide, result!.Hit);
    }

    [Fact]
    public void Find_ExtendsAnExistingLineBeyondItsEnd()
    {
        // the reference ends at (100 0); its extension runs on east
        var guides = GuideBuilder.FromReferenceSegment(new Point(0, 0), new Point(100, 0));

        var result = GuideEngine.Find(guides, new Point(130, -2), 5);

        Assert.Equal(GuideKind.Extension, result!.Guide.Kind);
        Assert.Equal((130.0, 0.0), (result.Location.X, result.Location.Y));

        // the other end extends west; on the segment itself there is no guide
        Assert.Equal(-20, GuideEngine.Find(guides, new Point(-20, 1), 5)!.Location.X, 9);
        Assert.Null(GuideEngine.Find(guides, new Point(50, 1), 5));
    }

    [Fact]
    public void Nearest_LocksThePointOntoTheNearestStep_WhateverTheDistance()
    {
        // from (0 0) with no previous segment: steps every 15° from north
        var steps = GuideBuilder.AngleSteps<Point>(null, new Point(0, 0), 15);

        // the pointer at a bearing of about 50°: the 45° step is nearest
        var (dx, dy) = WebMercatorGround.DirectionOf(50);
        var result = GuideEngine.Nearest(steps, new Point(1000 * dx, 1000 * dy));

        Assert.Equal(GuideKind.AngleStep, result!.Guide.Kind);
        Assert.Equal(45, WebMercatorGround.Bearing(new Point(0, 0), result.Location), 6);

        Assert.Null(GuideEngine.Nearest(new List<GuideLine<Point>>(), new Point(1, 1)));
    }

    [Fact]
    public void Find_NoGuides_OrABadTolerance()
    {
        var guides = GuideBuilder.FromPreviousSegment(new Point(0, 0), new Point(10, 0));

        Assert.Null(GuideEngine.Find(new List<GuideLine<Point>>(), new Point(1, 1), 5));
        Assert.Null(GuideEngine.Find(guides, new Point(double.NaN, 1), 5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => GuideEngine.Find(guides, new Point(1, 1), -1));
    }

    #endregion

    #region SegmentConstraint

    [Fact]
    public void Constraint_NothingFixed_ReturnsThePointer()
    {
        var constraint = new SegmentConstraint();
        var pointer = new Point(7, 9);

        Assert.False(constraint.IsActive);
        Assert.Same(pointer, constraint.Resolve(new Point(0, 0), new Point(1, 1), pointer));
    }

    [Fact]
    public void Constraint_LengthOnly_KeepsThePointersDirection()
    {
        var last = WebMercator(51.4, 35.7);
        var previous = new Point(last.X - 100, last.Y);

        var constraint = new SegmentConstraint { Length = 25 };

        // the pointer is somewhere to the north-east
        var point = constraint.Resolve(previous, last, new Point(last.X + 300, last.Y + 300));

        Assert.Equal(25, WebMercatorGround.Length(last, point), 3);
        Assert.Equal(45, WebMercatorGround.Bearing(last, point), 6);
    }

    [Fact]
    public void Constraint_LengthOnly_OnAGuide_KeepsTheGuidesDirection()
    {
        var last = WebMercator(51.4, 35.7);

        var constraint = new SegmentConstraint { Length = 25 };

        // the pointer is a little off the eastward guide; the point goes along the guide
        var point = constraint.Resolve(new Point(last.X - 100, last.Y), last, new Point(last.X + 300, last.Y + 4), direction: (1, 0));

        Assert.Equal(25, WebMercatorGround.Length(last, point), 3);
        Assert.Equal(90, WebMercatorGround.Bearing(last, point), 6);
    }

    [Fact]
    public void Constraint_TurnAngleOnly_ProjectsThePointer_AndNeverGoesBack()
    {
        var previous = new Point(0, 0);
        var last = new Point(100, 0);                     // going east

        var right = new SegmentConstraint { Angle = 90 };  // a right turn from east is south

        var point = right.Resolve(previous, last, new Point(130, -40));
        Assert.Equal(100, point.X, 9);
        Assert.Equal(-40, point.Y, 9);

        // the pointer behind that direction: the point stays at the start
        var behind = right.Resolve(previous, last, new Point(130, 40));
        Assert.Equal((100.0, 0.0), (behind.X, behind.Y));

        var left = new SegmentConstraint { Angle = -90 };
        Assert.Equal(40, left.Resolve(previous, last, new Point(130, 40)).Y, 9);
    }

    [Fact]
    public void Constraint_Bearing_AndTheFirstSegment_CountFromNorth()
    {
        var last = new Point(100, 0);

        var bearing = new SegmentConstraint { Angle = 90, AngleIsBearing = true };

        // previous says "going north", but a bearing ignores it: 90° is east
        var point = bearing.Resolve(new Point(100, -50), last, new Point(160, 20));
        Assert.Equal(160, point.X, 9);
        Assert.Equal(0, point.Y, 9);

        // no previous point: a turn angle is read as a bearing
        var first = new SegmentConstraint { Angle = 180 };
        var south = first.Resolve<Point>(null, last, new Point(110, -30));
        Assert.Equal(100, south.X, 9);
        Assert.Equal(-30, south.Y, 9);
    }

    [Fact]
    public void Constraint_LengthAndAngle_FixThePoint()
    {
        var last = WebMercator(51.4, 35.7);
        var previous = new Point(last.X, last.Y - 100);    // going north

        var constraint = new SegmentConstraint { Length = 40, Angle = 90 };

        // wherever the pointer is
        var a = constraint.Resolve(previous, last, new Point(last.X - 500, last.Y + 80));
        var b = constraint.Resolve(previous, last, new Point(last.X + 3, last.Y - 900));

        Assert.Equal(a.X, b.X, 6);
        Assert.Equal(a.Y, b.Y, 6);
        Assert.Equal(40, WebMercatorGround.Length(last, a), 3);
        Assert.Equal(90, WebMercatorGround.Bearing(last, a), 6);   // a right turn from north is east
    }

    [Fact]
    public void Constraint_LengthOnly_WithThePointerOnTheStart_StaysPut_AndClearFreesBoth()
    {
        var last = new Point(100, 0);

        var constraint = new SegmentConstraint { Length = 25, Angle = 10 };

        constraint.Clear();
        Assert.False(constraint.IsActive);

        constraint.Length = 25;
        var point = constraint.Resolve<Point>(null, last, new Point(100, 0));

        Assert.Equal((100.0, 0.0), (point.X, point.Y));

        // a length of zero or less is no constraint
        Assert.False(new SegmentConstraint { Length = 0 }.IsActive);
    }

    #endregion
}
