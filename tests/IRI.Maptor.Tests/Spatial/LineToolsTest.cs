using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

public class LineToolsTest
{
    private static MeasuredLine<Point> Line(string wkt, int srid = 0) => MeasuredLine<Point>.FromLine(Geometry<Point>.FromWkt(wkt, srid), srid == 4326);

    #region MeasuredLine

    [Fact]
    public void MeasuredLine_MeasuresPartsAndFindsPoints()
    {
        var line = Line("MULTILINESTRING((0 0, 10 0, 10 10), (20 0, 30 0))");

        Assert.Equal(2, line.Parts.Count);
        Assert.Equal(30, line.Length, 9);

        var point = line.Parts[0].PointAt(15);
        Assert.Equal(10, point.X, 9);
        Assert.Equal(5, point.Y, 9);

        var stretch = line.Parts[0].Extract(5, 15, 0);
        Assert.Equal(new[] { (5.0, 0.0), (10.0, 0.0), (10.0, 5.0) }, stretch.Points!.Select(p => (p.X, p.Y)).ToArray());
    }

    [Fact]
    public void MeasuredLine_FromRings_ClosesEachRing()
    {
        var rings = MeasuredLine<Point>.FromRings(Geometry<Point>.FromWkt("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 8, 8 8, 8 2, 2 2))", 0), false);

        Assert.Equal(2, rings.Parts.Count);
        Assert.Equal(40, rings.Parts[0].Length, 9);
        Assert.Equal(24, rings.Parts[1].Length, 9);
        Assert.True(rings.Parts.All(p => p.IsRing));
    }

    #endregion

    #region LineCoverage

    [Fact]
    public void Coverage_OfAParallelLineWithinTheDistance_IsTheWholeStretchBesideIt()
    {
        var line = Line("LINESTRING(0 0, 100 0)");
        var other = Line("LINESTRING(20 0.3, 60 0.3)");

        var stretch = Assert.Single(LineCoverage.Compute(line, new[] { other }, 0.5));

        // From 0.4 m before 20 to 0.4 m after 60: points within 0.5 m of the other line's ends too.
        Assert.Equal(20 - 0.4, stretch.From, 6);
        Assert.Equal(60 + 0.4, stretch.To, 6);
    }

    [Fact]
    public void Coverage_FartherThanTheDistance_IsNothing()
    {
        Assert.Empty(LineCoverage.Compute(Line("LINESTRING(0 0, 100 0)"), new[] { Line("LINESTRING(0 0.6, 100 0.6)") }, 0.5));
    }

    [Fact]
    public void Coverage_WithAnAngleLimit_IgnoresACrossingLine()
    {
        var line = Line("LINESTRING(0 0, 100 0)");
        var crossing = Line("LINESTRING(50 -50, 50 50)");

        var anyAngle = Assert.Single(LineCoverage.Compute(line, new[] { crossing }, 0.5));
        Assert.Equal(1.0, anyAngle.Length, 6);

        Assert.Empty(LineCoverage.Compute(line, new[] { crossing }, 0.5, maxAngleDegrees: 30));
    }

    [Fact]
    public void Coverage_OppositeDirection_CountsWithAnAngleLimit()
    {
        var stretch = Assert.Single(LineCoverage.Compute(Line("LINESTRING(0 0, 100 0)"), new[] { Line("LINESTRING(100 0.2, 0 0.2)") }, 0.5, 30));

        Assert.Equal(100, stretch.Length, 6);
    }

    [Fact]
    public void Coverage_OnGeodeticData_IsInMetres()
    {
        // 0.000003° of latitude is about 0.33 m.
        var line = Line("LINESTRING(51.0 35.7, 51.001 35.7)", 4326);
        var near = Line("LINESTRING(51.0 35.700003, 51.001 35.700003)", 4326);
        var far = Line("LINESTRING(51.0 35.70001, 51.001 35.70001)", 4326);

        Assert.Single(LineCoverage.Compute(line, new[] { near }, 0.5));
        Assert.Empty(LineCoverage.Compute(line, new[] { far }, 0.5));
    }

    [Fact]
    public void Intervals_MergeAndSubtract()
    {
        var merged = LineCoverage.MergeIntervals(new[] { (0, 0.0, 5.0), (0, 4.0, 8.0), (0, 10.0, 12.0), (1, 0.0, 1.0) });
        Assert.Equal(new[] { (0, 0.0, 8.0), (0, 10.0, 12.0), (1, 0.0, 1.0) }, merged.ToArray());

        var left = LineCoverage.Subtract(new[] { (0, 0.0, 10.0) }, new[] { (0, 2.0, 3.0), (0, 8.0, 12.0) });
        Assert.Equal(new[] { (0, 0.0, 2.0), (0, 3.0, 8.0) }, left.ToArray());
    }

    #endregion

    #region SegmentCrossings

    [Fact]
    public void Crossings_GiveTheMeasureOnBothLines()
    {
        var crossing = Assert.Single(SegmentCrossings.Find(new[] { Line("LINESTRING(0 0, 100 0)") }, new[] { Line("LINESTRING(30 -10, 30 10)") }));

        Assert.Equal(30, crossing.Measure, 9);
        Assert.Equal(10, crossing.OtherMeasure, 9);
        Assert.Equal(30, crossing.Location.X, 9);
    }

    [Fact]
    public void Crossings_AtASharedVertex_AreFoundOnce()
    {
        var crossings = SegmentCrossings.Find(new[] { Line("LINESTRING(0 0, 50 0, 100 0)") }, new[] { Line("LINESTRING(50 -10, 50 0, 50 10)") });

        Assert.Single(crossings);
    }

    [Fact]
    public void Crossings_OfLinesLyingOnEachOther_AreNotReported()
    {
        Assert.Empty(SegmentCrossings.Find(new[] { Line("LINESTRING(0 0, 100 0)") }, new[] { Line("LINESTRING(20 0, 60 0)") }));
    }

    [Fact]
    public void Crossings_WithinOneSet_TakeEachPairOnce()
    {
        var lines = new[] { Line("LINESTRING(0 0, 100 0)"), Line("LINESTRING(50 -10, 50 10)") };

        var crossing = Assert.Single(SegmentCrossings.Find(lines));

        Assert.Equal((0, 1), (crossing.LineIndex, crossing.OtherLineIndex));
    }

    #endregion

    [Fact]
    public void Proximity_FindsTheNearestLineWithinTheDistance()
    {
        var proximity = new LineProximity<Point>(new[] { Line("LINESTRING(0 0, 100 0)"), Line("LINESTRING(0 3, 100 3)") });

        var nearest = proximity.Nearest(new Point(40, 2), 5);

        Assert.NotNull(nearest);
        Assert.Equal(1, nearest!.LineIndex);
        Assert.Equal(1, nearest.Distance, 9);
        Assert.Equal(40, nearest.Measure, 9);

        Assert.Null(proximity.Nearest(new Point(40, 20), 5));
        Assert.Equal(0, proximity.Nearest(new Point(40, 2), 5, l => l == 0)!.LineIndex);
    }
}
