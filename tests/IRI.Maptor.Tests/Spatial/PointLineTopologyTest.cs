using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

public class PointLineTopologyTest
{
    // Projected data (SRID 0): coordinates are metres.
    private static Geometry<Point> Line(string wkt) => Geometry<Point>.FromWkt(wkt, 0);

    private static PointLineTopology<Point> Build(
        IReadOnlyList<Geometry<Point>?> lines,
        IReadOnlyList<Point?> points,
        IReadOnlyList<long?>? sequences = null,
        PointLineTopologyOptions? options = null)
        => new PointLineTopologyBuilder<Point>(options).Build(lines, points, sequences);

    private static List<PointLineIssue<Point>> IssuesOf(PointLineTopology<Point> topology, PointLineIssueCode code)
        => topology.Issues.Where(i => i.Code == code).ToList();

    private static long?[] Seq(params long?[] values) => values;

    #region Membership

    [Fact]
    public void PointWithinTolerance_IsOnTheLine_WithMeasureAndOffset()
    {
        var topology = Build(new[] { Line("LINESTRING(0 0, 1000 0)") }, new Point?[] { new Point(250, 0.3) });

        var station = Assert.Single(topology.Lines[0].Stations);

        Assert.Equal(250, station.Measure, 6);
        Assert.Equal(0.3, station.Offset, 6);
        Assert.Equal(new[] { 0 }, topology.GetLineIndices(0));
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.OrphanPoint));
    }

    [Fact]
    public void PointBeyondTolerance_IsAnOrphan_NotGivenToTheNearestLine()
    {
        // 0.7 m off the only line: the old "nearest line wins" rule would have taken it.
        var topology = Build(new[] { Line("LINESTRING(0 0, 1000 0)") }, new Point?[] { new Point(250, 0.7) });

        Assert.Empty(topology.Lines[0].Stations);
        Assert.Empty(topology.GetLineIndices(0));

        var orphan = Assert.Single(IssuesOf(topology, PointLineIssueCode.OrphanPoint));
        Assert.Equal(0, orphan.PointIndex);
        Assert.Equal(PointLineIssueSeverity.Error, orphan.Severity);
    }

    [Fact]
    public void PointOnTwoLines_BelongsToBoth_AndIsReportedAsShared()
    {
        // Two segments meeting at (1000, 0): the junction point is on both.
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 1000 0)"), Line("LINESTRING(1000 0, 2000 0)") },
            new Point?[] { new Point(1000, 0) });

        Assert.Equal(new[] { 0, 1 }, topology.GetLineIndices(0));
        Assert.True(topology.Lines[0].Contains(0));
        Assert.True(topology.Lines[1].Contains(0));

        var shared = Assert.Single(IssuesOf(topology, PointLineIssueCode.SharedPoint));
        Assert.Equal(PointLineIssueSeverity.Info, shared.Severity);
        Assert.Equal(2, shared.Value);
    }

    [Fact]
    public void PointNearTwoVerticesOfOneLine_GetsOneStation_TheClosest()
    {
        var topology = Build(new[] { Line("LINESTRING(0 0, 500 0, 500 0.4, 1000 0.4)") }, new Point?[] { new Point(500, 0.2) });

        var station = Assert.Single(topology.Lines[0].Stations);
        Assert.Equal(0, station.Offset, 6);
    }

    #endregion

    #region Order

    [Fact]
    public void Points_AreOrderedAlongTheLine_NotByInputOrder()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 1000 0)") },
            new Point?[] { new Point(700, 0), new Point(100, 0), new Point(400, 0) });

        Assert.Equal(new[] { 1, 2, 0 }, topology.Lines[0].Stations.Select(s => s.PointIndex));
        Assert.Equal(1, topology.Lines[0].IndexOf(2));
        Assert.Equal(-1, topology.Lines[0].IndexOf(99));
    }

    [Fact]
    public void Sequences_FallingAlongTheDrawing_ReverseTheLine()
    {
        // Drawn west to east, numbered east to west.
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 1000 0)") },
            new Point?[] { new Point(0, 0), new Point(500, 0), new Point(1000, 0) },
            Seq(3, 2, 1));

        var line = topology.Lines[0];

        Assert.True(line.IsReversed);
        Assert.Equal(new long?[] { 1, 2, 3 }, line.Stations.Select(s => s.Sequence));
        Assert.Empty(topology.Issues);
    }

    [Fact]
    public void Sequences_DoNotDecideTheOrder_GeometryDoes()
    {
        // Numbering says 1 3 2, but the points stand at 0, 500, 1000: the order stays geometric.
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 1000 0)") },
            new Point?[] { new Point(0, 0), new Point(1000, 0), new Point(500, 0) },
            Seq(1, 2, 3));

        Assert.Equal(new[] { 0, 2, 1 }, topology.Lines[0].Stations.Select(s => s.PointIndex));
    }

    [Fact]
    public void SharedPoint_DoesNotVoteOnTheDirection()
    {
        // Line B starts at the junction point, which carries line A's number (3); B's own points
        // are 1 and 2. With the junction voting, 3 1 2 would tie and leave B undecided.
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 200 0)"), Line("LINESTRING(200 0, 400 0)") },
            new Point?[] { new Point(0, 0), new Point(100, 0), new Point(200, 0), new Point(300, 0), new Point(400, 0) },
            Seq(1, 2, 3, 1, 2));

        var b = topology.Lines[1];

        Assert.True(b.IsDirectionDecided);
        Assert.False(b.IsReversed);
        Assert.Equal(new[] { 2, 3, 4 }, b.Stations.Select(s => s.PointIndex));
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.DirectionUndecided));

        // Nor is its foreign number reported as out of order or as a gap on either line.
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.SequenceOutOfOrder));
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.SequenceGap));
    }

    [Fact]
    public void OrientBySequenceOff_KeepsTheDrawnDirection()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 1000 0)") },
            new Point?[] { new Point(0, 0), new Point(1000, 0) },
            Seq(2, 1),
            new PointLineTopologyOptions { OrientBySequence = false });

        Assert.False(topology.Lines[0].IsReversed);
        Assert.Equal(new[] { 0, 1 }, topology.Lines[0].Stations.Select(s => s.PointIndex));
    }

    [Fact]
    public void MultiPartLine_IsOrderedByPartThenAlongEachPart()
    {
        var topology = Build(
            new[] { Line("MULTILINESTRING((0 0, 100 0), (0 50, 100 50))") },
            new Point?[] { new Point(100, 50), new Point(0, 50), new Point(100, 0), new Point(0, 0) });

        var stations = topology.Lines[0].Stations;

        Assert.Equal(new[] { 3, 2, 1, 0 }, stations.Select(s => s.PointIndex));
        Assert.Equal(new[] { 0, 0, 1, 1 }, stations.Select(s => s.PartIndex));
        Assert.Equal(new[] { 100.0, 100.0 }, topology.Lines[0].PartLengths);
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.OpenLineEnd));
    }

    #endregion

    #region Sequence checks

    [Fact]
    public void SequenceCheck_ReportsTheOneWrongNumber_NotEverythingAfterIt()
    {
        // 1 2 3 99 5 6 along the line: only 99 is out of order.
        var points = Enumerable.Range(0, 6).Select(i => (Point?)new Point(i * 100, 0)).ToList();

        var topology = Build(new[] { Line("LINESTRING(0 0, 500 0)") }, points, Seq(1, 2, 3, 99, 5, 6));

        var outOfOrder = Assert.Single(IssuesOf(topology, PointLineIssueCode.SequenceOutOfOrder));
        Assert.Equal(3, outOfOrder.PointIndex);
        Assert.Equal(99, outOfOrder.Value);

        // 99 was probably meant to be 4, so 3 → 5 is a gap of one.
        var gap = Assert.Single(IssuesOf(topology, PointLineIssueCode.SequenceGap));
        Assert.Equal(4, gap.PointIndex);
        Assert.Equal(2, gap.OtherPointIndex);
        Assert.Equal(1, gap.Value);
    }

    [Fact]
    public void SequenceGap_CountsTheMissingNumbers()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 300 0)") },
            new Point?[] { new Point(0, 0), new Point(100, 0), new Point(200, 0), new Point(300, 0) },
            Seq(20, 21, 24, 25));

        var gap = Assert.Single(IssuesOf(topology, PointLineIssueCode.SequenceGap));
        Assert.Equal(2, gap.PointIndex);
        Assert.Equal(1, gap.OtherPointIndex);
        Assert.Equal(2, gap.Value);
        Assert.Equal(PointLineIssueSeverity.Warning, gap.Severity);
    }

    [Fact]
    public void DuplicateSequence_IsReportedOnTheRepeat()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 300 0)") },
            new Point?[] { new Point(0, 0), new Point(100, 0), new Point(200, 0), new Point(300, 0) },
            Seq(1, 2, 2, 3));

        var duplicate = Assert.Single(IssuesOf(topology, PointLineIssueCode.DuplicateSequence));
        Assert.Equal(2, duplicate.PointIndex);
        Assert.Equal(1, duplicate.OtherPointIndex);
        Assert.Equal(2, duplicate.Value);
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.SequenceOutOfOrder));
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.SequenceGap));
    }

    [Fact]
    public void MissingSequence_IsReported_AndThePointKeepsItsPlace()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 200 0)") },
            new Point?[] { new Point(0, 0), new Point(100, 0), new Point(200, 0) },
            Seq(1, null, 3));

        Assert.Equal(new[] { 0, 1, 2 }, topology.Lines[0].Stations.Select(s => s.PointIndex));
        Assert.Equal(1, Assert.Single(IssuesOf(topology, PointLineIssueCode.MissingSequence)).PointIndex);
    }

    [Fact]
    public void SequencesRisingInNeitherDirection_LeaveTheDirectionUndecided()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 300 0)") },
            new Point?[] { new Point(0, 0), new Point(100, 0), new Point(200, 0), new Point(300, 0) },
            Seq(5, 5, 5, 5));

        Assert.False(topology.Lines[0].IsDirectionDecided);
        Assert.False(topology.Lines[0].IsReversed);
        Assert.Single(IssuesOf(topology, PointLineIssueCode.DirectionUndecided));
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.SequenceOutOfOrder));
    }

    #endregion

    #region Line checks

    [Fact]
    public void CoincidentPoints_AreAnError()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 100 0)") },
            new Point?[] { new Point(0, 0), new Point(50, 0), new Point(50.001, 0.2), new Point(100, 0) });

        var coincident = Assert.Single(IssuesOf(topology, PointLineIssueCode.CoincidentPoints));
        Assert.Equal(PointLineIssueSeverity.Error, coincident.Severity);
        Assert.Equal(new[] { 1, 2 }, new[] { coincident.OtherPointIndex!.Value, coincident.PointIndex!.Value }.OrderBy(i => i));
    }

    [Fact]
    public void LineEndsWithoutPoints_AreReportedWithTheirVertex()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 1000 0)") },
            new Point?[] { new Point(200, 0), new Point(600, 0) });

        var ends = IssuesOf(topology, PointLineIssueCode.OpenLineEnd);

        Assert.Equal(2, ends.Count);
        Assert.Contains(ends, e => e.Value == 0 && e.Location!.X == 0);
        Assert.Contains(ends, e => e.Value == 1000 && e.Location!.X == 1000);
    }

    [Fact]
    public void LineWithFewerThanTwoPoints_IsReported()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 1000 0)"), Line("LINESTRING(0 500, 1000 500)") },
            new Point?[] { new Point(0, 0) });

        var empty = IssuesOf(topology, PointLineIssueCode.LineWithoutPoints).OrderBy(i => i.LineIndex).ToList();

        Assert.Equal(new int?[] { 0, 1 }, empty.Select(i => i.LineIndex));
        Assert.Equal(new double?[] { 1, 0 }, empty.Select(i => i.Value));
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.OpenLineEnd));
    }

    [Fact]
    public void InvalidLinesAndPoints_AreReported_AndIndicesStayAligned()
    {
        var topology = Build(
            new Geometry<Point>?[] { null, Line("LINESTRING(5 5, 5 5)"), Line("LINESTRING(0 0, 100 0)") },
            new Point?[] { null, new Point(double.NaN, 0), new Point(0, 0), new Point(100, 0) });

        Assert.Equal(3, topology.Lines.Count);
        Assert.Equal(new int?[] { 0, 1 }, IssuesOf(topology, PointLineIssueCode.InvalidLine).Select(i => i.LineIndex));
        Assert.Equal(new int?[] { 0, 1 }, IssuesOf(topology, PointLineIssueCode.InvalidPoint).Select(i => i.PointIndex));
        Assert.Equal(new[] { 2, 3 }, topology.Lines[2].Stations.Select(s => s.PointIndex));
        Assert.False(topology.Lines[0].IsValid);
    }

    [Fact]
    public void IssuesOfLine_IncludeItsPoints_ButNotOtherLines()
    {
        var topology = Build(
            new[] { Line("LINESTRING(0 0, 100 0)"), Line("LINESTRING(100 0, 200 0)") },
            new Point?[] { new Point(0, 0), new Point(100, 0), new Point(200, 0), new Point(150, 30) });

        var first = topology.GetIssuesOfLine(0).ToList();

        Assert.Contains(first, i => i.Code == PointLineIssueCode.SharedPoint && i.PointIndex == 1);
        Assert.DoesNotContain(first, i => i.Code == PointLineIssueCode.OrphanPoint);
        Assert.Single(topology.GetIssuesOfPoint(3), i => i.Code == PointLineIssueCode.OrphanPoint);
    }

    #endregion

    #region Real-world shapes

    [Fact]
    public void ScatteredPointsWithTheSameNumbers_DoNotJoinTheLine()
    {
        // A route with towers 20..24 on it, and towers of other lines (no route of their own
        // loaded) scattered nearby with the same numbers. Nearest-line matching put them all on
        // the route and tower 22's "neighbours" became other 22s; here they are orphans.
        var points = new List<Point?>();
        var sequences = new List<long?>();

        for (int i = 0; i < 5; i++)
        {
            points.Add(new Point(i * 300, 0));
            sequences.Add(20 + i);
        }

        foreach (var (x, y, n) in new[] { (100.0, 400.0, 22L), (500.0, -800.0, 22L), (900.0, 250.0, 21L), (50.0, 3.0, 22L) })
        {
            points.Add(new Point(x, y));
            sequences.Add(n);
        }

        var topology = Build(new[] { Line("LINESTRING(0 0, 1200 0)") }, points, sequences);

        var line = topology.Lines[0];
        var selected = line.IndexOf(2); // tower 22 on the route

        Assert.Equal(5, line.Stations.Count);
        Assert.Equal(21, line.Stations[selected - 1].Sequence);
        Assert.Equal(23, line.Stations[selected + 1].Sequence);
        Assert.Equal(4, IssuesOf(topology, PointLineIssueCode.OrphanPoint).Count);
        Assert.Empty(IssuesOf(topology, PointLineIssueCode.DuplicateSequence));
    }

    [Fact]
    public void GeodeticData_IsMeasuredInMetres()
    {
        // An east-west line at 35.7° N; 0.000003° of latitude is ~0.33 m, 0.000007° ~0.78 m.
        var line = Geometry<Point>.FromWkt("LINESTRING(51.30 35.70, 51.32 35.70)", SridHelper.GeodeticWGS84);

        var on = new Point(51.31, 35.700003);
        var off = new Point(51.31, 35.700007);

        var topology = Build(new[] { line }, new Point?[] { new Point(51.30, 35.70), on, off, new Point(51.32, 35.70) });

        Assert.True(topology.IsGeodetic);
        Assert.Equal(new[] { 0, 1, 3 }, topology.Lines[0].Stations.Select(s => s.PointIndex));
        Assert.InRange(topology.Lines[0].Stations[1].Offset, 0.30, 0.36);
        Assert.Equal(2, Assert.Single(IssuesOf(topology, PointLineIssueCode.OrphanPoint)).PointIndex);

        var expected = SpatialUtility.GetEllipsoidalLength(new Point(51.30, 35.70), new Point(51.31, 35.70));
        Assert.Equal(expected, topology.Lines[0].Stations[1].Measure, 1);
        Assert.Equal(SpatialUtility.GetEllipsoidalLength(new Point(51.30, 35.70), new Point(51.32, 35.70)), topology.Lines[0].Length, 3);
    }

    [Fact]
    public void GridCellSize_DoesNotChangeTheResult()
    {
        var random = new Random(7);

        var lines = Enumerable.Range(0, 20)
            .Select(i => (Geometry<Point>?)Line($"LINESTRING(0 {i * 100}, 500 {i * 100 + 50}, 1000 {i * 100})"))
            .ToList();

        var points = Enumerable.Range(0, 400)
            .Select(_ => (Point?)new Point(random.NextDouble() * 1000, random.NextDouble() * 2000))
            .Concat(Enumerable.Range(0, 20).Select(i => (Point?)new Point(500, i * 100 + 50.2)))
            .ToList();

        string Signature(double cell)
        {
            var topology = Build(lines, points, options: new PointLineTopologyOptions { GridCellSize = cell });

            return string.Join(";", topology.Lines.Select(l => string.Join(",", l.Stations.Select(s => s.PointIndex))));
        }

        var reference = Signature(1_000_000);

        Assert.Equal(reference, Signature(1));
        Assert.Equal(reference, Signature(37));
        // Sanity: every line found at least its planted point.
        Assert.DoesNotContain(";;", ";" + reference + ";");
    }

    #endregion
}
