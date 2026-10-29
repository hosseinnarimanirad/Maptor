using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.Topology;
using IRI.Maptor.Core.SpatialReferenceSystem;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Tests.TheGeometry;

/// <summary>
/// Tests for Geometry.OffsetCurve: side convention, join styles, collapse removal on the inner
/// side of tight bends, and the distance property (every vertex keeps the offset distance).
/// </summary>
public class Geometry_OffsetCurveTest
{
    private const int TestSrid = SridHelper.WebMercator;

    private static Geometry<Point> FromWkt(string wkt) => Geometry<Point>.FromWkt(wkt, TestSrid);

    private static void AssertPoint(double x, double y, Point actual, int precision = 9)
    {
        Assert.Equal(x, actual.X, precision);
        Assert.Equal(y, actual.Y, precision);
    }

    /// <summary>The vertices of a LineString, or of every part of a MultiLineString.</summary>
    private static List<Point> AllPoints(Geometry<Point> geometry)
        => geometry.Type == GeometryType.MultiLineString
            ? geometry.Geometries.SelectMany(g => g.Points).ToList()
            : geometry.Points;

    private static double DistanceToLine(Point point, List<Point> line)
    {
        double min = double.MaxValue;

        for (int i = 0; i < line.Count - 1; i++)
            min = Math.Min(min, TopologyUtility.GetPointToLineSegmentDistance(point, line[i], line[i + 1]));

        return min;
    }

    /// <summary>
    /// Every vertex at least the distance from the source, and no more than the mitre limit. Round
    /// joins are drawn as chords (π/16 steps, as in Buffer); a crossing point cut between two chords
    /// may sit up to the chord's sagitta, d·(1 − cos π/32) ≈ 0.5 % of d, inside the true arc.
    /// </summary>
    private static void AssertKeepsDistance(Geometry<Point> source, Geometry<Point> offset, double distance, OffsetCurveJoin join = OffsetCurveJoin.Mitre)
    {
        double slack = join == OffsetCurveJoin.Round ? distance * (1 - Math.Cos(Math.PI / 32)) : 0;

        foreach (var point in AllPoints(offset))
        {
            double d = DistanceToLine(point, source.Points);

            Assert.True(d >= distance - slack - 1e-6, $"vertex ({point.X}, {point.Y}) is {d} from the source, closer than {distance}");
            Assert.True(d <= 4 * distance + 1e-9, $"vertex ({point.X}, {point.Y}) is {d} from the source, beyond the mitre limit");
        }
    }

    private static bool CrossesItself(List<Point> line)
    {
        for (int i = 0; i < line.Count - 1; i++)
        {
            for (int j = i + 2; j < line.Count - 1; j++)
            {
                if (TopologyUtility.LineSegmentsIntersects(line[i], line[i + 1], line[j], line[j + 1], out _) == LineLineSegmentRelation.Intersect)
                    return true;
            }
        }

        return false;
    }

    #region Side and joins

    [Fact]
    public void OffsetCurve_PositiveDistanceIsLeftOfTravel()
    {
        var line = FromWkt("LINESTRING(0 0, 10 0)");

        var left = line.OffsetCurve(2);
        var right = line.OffsetCurve(-2);

        Assert.Equal(GeometryType.LineString, left.Type);
        Assert.Equal(2, left.Points.Count);
        AssertPoint(0, 2, left.Points[0]);
        AssertPoint(10, 2, left.Points[1]);

        AssertPoint(0, -2, right.Points[0]);
        AssertPoint(10, -2, right.Points[1]);

        Assert.Equal(TestSrid, left.Srid);
    }

    [Fact]
    public void OffsetCurve_InnerSideOfABendMeetsAtTheCorner()
    {
        // east, then north: the left side is the inside of the bend
        var line = FromWkt("LINESTRING(0 0, 10 0, 10 10)");

        foreach (var join in new[] { OffsetCurveJoin.Round, OffsetCurveJoin.Mitre })
        {
            var offset = line.OffsetCurve(2, join);

            Assert.Equal(3, offset.Points.Count);
            AssertPoint(0, 2, offset.Points[0]);
            AssertPoint(8, 2, offset.Points[1]);
            AssertPoint(8, 10, offset.Points[2]);
        }
    }

    [Fact]
    public void OffsetCurve_MitreJoinMakesASharpOuterCorner()
    {
        var offset = FromWkt("LINESTRING(0 0, 10 0, 10 10)").OffsetCurve(-2, OffsetCurveJoin.Mitre);

        Assert.Equal(3, offset.Points.Count);
        AssertPoint(0, -2, offset.Points[0]);
        AssertPoint(12, -2, offset.Points[1]);
        AssertPoint(12, 10, offset.Points[2]);
    }

    [Fact]
    public void OffsetCurve_RoundJoinArcsAroundTheOuterCorner()
    {
        var line = FromWkt("LINESTRING(0 0, 10 0, 10 10)");

        var offset = line.OffsetCurve(-2, OffsetCurveJoin.Round);

        Assert.True(offset.Points.Count > 3);
        AssertPoint(0, -2, offset.Points[0]);
        AssertPoint(12, 10, offset.Points[offset.Points.Count - 1]);

        // the arc keeps exactly the distance from the corner vertex
        foreach (var point in offset.Points.Where(p => p.X > 10 && p.Y < 0))
            Assert.Equal(2, Math.Sqrt((point.X - 10) * (point.X - 10) + point.Y * point.Y), 9);

        AssertKeepsDistance(line, offset, 2, OffsetCurveJoin.Round);
    }

    [Fact]
    public void OffsetCurve_DefaultJoinIsRoundLikeBuffer()
    {
        var line = FromWkt("LINESTRING(0 0, 10 0, 10 10)");

        Assert.Equal(line.OffsetCurve(-2, OffsetCurveJoin.Round).Points.Count, line.OffsetCurve(-2).Points.Count);
    }

    [Fact]
    public void OffsetCurve_MitreBevelsANearUTurnInsteadOfASpike()
    {
        // a hairpin: the outer mitre would lie ~40 units out
        var line = FromWkt("LINESTRING(0 0, 10 0, 0 0.5)");

        var offset = line.OffsetCurve(-1, OffsetCurveJoin.Mitre);

        AssertKeepsDistance(line, offset, 1);
        Assert.True(AllPoints(offset).Max(p => p.X) < 11.0 + 1e-9);
    }

    #endregion

    #region Collapse on the inner side

    [Fact]
    public void OffsetCurve_CutsTheFoldOverANarrowNotch()
    {
        // a notch 2 units wide and 2 deep below the line; offsetting 3 units up folds over it
        var line = FromWkt("LINESTRING(0 0, 10 0, 11 -2, 12 0, 22 0)");

        foreach (var join in new[] { OffsetCurveJoin.Round, OffsetCurveJoin.Mitre })
        {
            var offset = line.OffsetCurve(3, join);

            Assert.False(CrossesItself(offset.Points), $"{join}: the fold is still there");
            AssertKeepsDistance(line, offset, 3, join);

            AssertPoint(0, 3, offset.Points[0]);
            AssertPoint(22, 3, offset.Points[offset.Points.Count - 1]);

            // over the notch the true offset is the straight line y = 3 (plus the arcs' rise)
            Assert.True(offset.Points.Max(p => p.Y) < 3.5, $"{join}: max y {offset.Points.Max(p => p.Y)}");
        }
    }

    [Fact]
    public void OffsetCurve_DropsAShortEndSegmentAfterAnInnerBend()
    {
        // east 10, then north 1: offsetting 3 to the left, the last segment is inside the bend
        var line = FromWkt("LINESTRING(0 0, 10 0, 10 1)");

        var offset = line.OffsetCurve(3, OffsetCurveJoin.Mitre);

        AssertKeepsDistance(line, offset, 3);

        // the north segment's offset runs backwards and is dropped; y = 3 then ends where it is
        // exactly 3 from the line's end (10, 1): x = 10 − √5
        Assert.Equal(2, offset.Points.Count);
        AssertPoint(0, 3, offset.Points[0]);
        AssertPoint(10 - Math.Sqrt(5), 3, offset.Points[1], 6);
    }

    [Fact]
    public void OffsetCurve_ReturnsEmptyWhenTheWholeSideCollapses()
    {
        // a U whose arms are 2 apart: nothing lies 3 units inside it
        var offset = FromWkt("LINESTRING(0 0, 10 0, 10 2, 0 2)").OffsetCurve(3, OffsetCurveJoin.Mitre);

        Assert.True(offset.IsNullOrEmpty());
    }

    [Fact]
    public void OffsetCurve_CutsTheOffsetWhereItPassesOverTheLineItself()
    {
        // a bow tie crossing itself at (5, 5): the offset of each diagonal passes over the other
        // diagonal, and the stretch within 0.5 of it is not part of the single-sided offset
        var line = FromWkt("LINESTRING(0 0, 10 10, 10 0, 0 10)");

        var offset = line.OffsetCurve(0.5, OffsetCurveJoin.Mitre);

        Assert.Equal(GeometryType.MultiLineString, offset.Type);
        Assert.True(offset.Geometries.Count >= 2);
        AssertKeepsDistance(line, offset, 0.5);

        // only the short stretches around the crossing go: most of the line's length remains
        double kept = offset.Geometries.Sum(g => g.GetEuclideanLength());

        Assert.True(kept > 0.8 * line.GetEuclideanLength(), $"kept {kept}");
    }

    [Fact]
    public void OffsetCurve_ZigzagKeepsTheDistanceOnBothSides()
    {
        var line = FromWkt("LINESTRING(0 0, 5 3, 10 0, 15 3, 20 0, 25 3)");

        foreach (var join in new[] { OffsetCurveJoin.Round, OffsetCurveJoin.Mitre })
        {
            foreach (var distance in new[] { 0.5, -0.5, 1.5, -1.5 })
            {
                var offset = line.OffsetCurve(distance, join);

                Assert.False(offset.IsNullOrEmpty());
                Assert.False(CrossesItself(offset.Points), $"{join} {distance}");
                AssertKeepsDistance(line, offset, Math.Abs(distance), join);
            }
        }
    }

    [Fact]
    public void OffsetCurve_RandomLinesKeepTheDistance()
    {
        var random = new Random(3);

        for (int n = 0; n < 300; n++)
        {
            // a wandering line: steps of 1–6 units, turning up to ±150° per vertex
            var points = new List<Point> { new Point(0, 0) };
            double heading = 0;

            for (int v = 0; v < 12; v++)
            {
                heading += (random.NextDouble() * 2 - 1) * 150 * Math.PI / 180;
                double step = 1 + random.NextDouble() * 5;

                var last = points[points.Count - 1];
                points.Add(new Point(last.X + step * Math.Cos(heading), last.Y + step * Math.Sin(heading)));
            }

            var line = Geometry<Point>.Create(points, GeometryType.LineString, TestSrid);

            foreach (var join in new[] { OffsetCurveJoin.Round, OffsetCurveJoin.Mitre })
            {
                foreach (var distance in new[] { 0.3, -0.3, 2.0, -2.0 })
                {
                    var offset = line.OffsetCurve(distance, join);

                    if (offset.IsNullOrEmpty())
                        continue;

                    AssertKeepsDistance(line, offset, Math.Abs(distance), join);
                }
            }
        }
    }

    [Fact]
    public void OffsetCurve_LongLineIsFastEnough()
    {
        // a 5 000-vertex meander, ~50 km of route at 10 m vertex spacing, offset 10 m both ways
        var points = Enumerable.Range(0, 5000)
            .Select(i => new Point(i * 10.0, 300 * Math.Sin(i / 80.0)))
            .ToList();

        var line = Geometry<Point>.Create(points, GeometryType.LineString, TestSrid);

        var watch = System.Diagnostics.Stopwatch.StartNew();

        var left = line.OffsetCurve(10, OffsetCurveJoin.Mitre);
        var right = line.OffsetCurve(-10, OffsetCurveJoin.Round);

        watch.Stop();

        Assert.Equal(GeometryType.LineString, left.Type);
        Assert.Equal(GeometryType.LineString, right.Type);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
    }

    [Fact]
    public void OffsetCurve_IgnoresRepeatedVertices()
    {
        var offset = FromWkt("LINESTRING(0 0, 5 0, 5 0, 10 0)").OffsetCurve(1, OffsetCurveJoin.Mitre);

        Assert.All(offset.Points, p => Assert.Equal(1, p.Y, 9));
        AssertPoint(10, 1, offset.Points[offset.Points.Count - 1]);
    }

    #endregion

    #region Types and edge cases

    [Fact]
    public void OffsetCurve_OffsetsEachPartOfAMultiLineString()
    {
        var multi = FromWkt("MULTILINESTRING((0 0, 10 0), (0 5, 10 5))");

        var offset = multi.OffsetCurve(1);

        Assert.Equal(GeometryType.MultiLineString, offset.Type);
        Assert.Equal(2, offset.Geometries.Count);
        AssertPoint(0, 1, offset.Geometries[0].Points[0]);
        AssertPoint(0, 6, offset.Geometries[1].Points[0]);
    }

    [Fact]
    public void OffsetCurve_ZeroDistanceReturnsACopy()
    {
        var line = FromWkt("LINESTRING(0 0, 10 0, 10 10)");

        var offset = line.OffsetCurve(0);

        Assert.NotSame(line, offset);
        Assert.Equal(3, offset.Points.Count);
        AssertPoint(10, 10, offset.Points[2]);
    }

    [Fact]
    public void OffsetCurve_EmptyStaysEmpty()
    {
        Assert.True(Geometry<Point>.Empty.OffsetCurve(5).IsNullOrEmpty());
    }

    [Fact]
    public void OffsetCurve_RefusesGeodeticCoordinates()
    {
        var line = Geometry<Point>.FromWkt("LINESTRING(51 35, 51.1 35)", SridHelper.GeodeticWGS84);

        Assert.Throws<InvalidOperationException>(() => line.OffsetCurve(10));
    }

    [Fact]
    public void OffsetCurve_RefusesPolygons()
    {
        var polygon = FromWkt("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))");

        Assert.Throws<NotImplementedException>(() => polygon.OffsetCurve(1));
    }

    #endregion
}
