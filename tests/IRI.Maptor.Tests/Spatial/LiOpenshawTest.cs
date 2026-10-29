using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.Spatial;

// Li and Openshaw (1992), vector and raster modes. The expected values are worked out by hand from the
// definition (no reference implementation was available), so these are definition tests, not a benchmark.
public class LiOpenshawTest
{
    private static List<Point> Line(params (double x, double y)[] points) => points.Select(p => new Point(p.x, p.y)).ToList();

    private static SimplificationParameters Threshold(double f) => new() { DistanceThreshold = f };

    [Fact]
    public void VectorMode_StraightLine_MidpointsOfSuccessiveCircles()
    {
        // unit-spaced points on the x axis, SVO diameter 4 (radius 2): the line leaves the circle centred at the
        // entry point exactly at the vertex 2 units further; the midpoints lie at 1, 3, 5, 7; from (8, 0) no
        // vertex is outside the circle (|10 − 8| = 2 is not > 2), so the end point closes the line
        var points = Enumerable.Range(0, 11).Select(i => new Point(i, 0)).ToList();

        var result = Simplifications.SimplifyByLiOpenshaw(points, Threshold(4));

        Assert.Equal(Line((0, 0), (1, 0), (3, 0), (5, 0), (7, 0), (10, 0)), result);
    }

    [Fact]
    public void VectorMode_ExitIsInterpolatedOnTheLeavingSegment()
    {
        // from (0, 0) with radius 1 the line (0,0)-(3,0) leaves the circle at (1, 0): midpoint (0.5, 0);
        // then from (1, 0) it leaves at (2, 0): midpoint (1.5, 0); from (2, 0) the vertex (3, 0) lies on the
        // boundary (not outside), the line leaves on the next segment exactly at (3, 0): midpoint (2.5, 0);
        // from (3, 0) the end point is inside
        var result = Simplifications.SimplifyByLiOpenshaw(Line((0, 0), (3, 0), (3, 0.5)), Threshold(2));

        Assert.Equal(5, result.Count);
        Assert.Equal(new Point(0.5, 0), result[1]);
        Assert.Equal(new Point(1.5, 0), result[2]);
        Assert.Equal(new Point(2.5, 0), result[3]);
        Assert.Equal(new Point(3, 0.5), result[4]);
    }

    [Fact]
    public void VectorMode_WigglesInsideTheSvoDisappear()
    {
        var random = new Random(1);
        var points = Enumerable.Range(0, 200).Select(i => new Point(i * 0.1, (random.NextDouble() - 0.5) * 0.4)).ToList();

        var result = Simplifications.SimplifyByLiOpenshaw(points, Threshold(4));

        Assert.True(result.Count < 15, $"{result.Count} points");
        Assert.All(result, p => Assert.InRange(p.Y, -0.3, 0.3));
        Assert.Equal(points[0], result[0]);
        Assert.Equal(points[^1], result[^1]);

        // successive output points are at most one SVO apart
        for (int i = 1; i < result.Count; i++)
            Assert.True(SpatialUtility.GetEuclideanLength(result[i - 1], result[i]) <= 4 + 1e-9);
    }

    [Fact]
    public void VectorMode_SvoLargerThanTheLine_KeepsOnlyTheEnds()
    {
        var points = Line((0, 0), (1, 1), (2, 0), (3, 1));

        Assert.Equal(Line((0, 0), (3, 1)), Simplifications.SimplifyByLiOpenshaw(points, Threshold(100)));

        var retained = Simplifications.SimplifyByLiOpenshaw(points, new SimplificationParameters() { DistanceThreshold = 100, Retain3Points = true });

        Assert.Equal(Line((0, 0), (2, 0), (3, 1)), retained);
    }

    [Fact]
    public void RasterMode_RunsInOneCellBecomeTheirCentroid()
    {
        // cell size 1: (0.1,0.1) (0.2,0.3) (0.9,0.8) share cell (0,0) with the first point, which is kept as is;
        // (1.5,1.5) (1.7,1.2) share cell (1,1) → centroid (1.6, 1.35); (2.5,0.5) is the last point
        var points = Line((0.1, 0.1), (0.2, 0.3), (0.9, 0.8), (1.5, 1.5), (1.7, 1.2), (2.5, 0.5));

        var result = Simplifications.SimplifyByLiOpenshawRaster(points, Threshold(1));

        Assert.Equal(3, result.Count);
        Assert.Equal(new Point(0.1, 0.1), result[0]);
        Assert.Equal(1.6, result[1].X, 12);
        Assert.Equal(1.35, result[1].Y, 12);
        Assert.Equal(new Point(2.5, 0.5), result[2]);
    }

    [Fact]
    public void RasterMode_PointsInDistinctCellsAreAllKept()
    {
        var points = Line((0.5, 0.5), (1.5, 0.5), (2.5, 0.5), (3.5, 0.5));

        Assert.Equal(points, Simplifications.SimplifyByLiOpenshawRaster(points, Threshold(1)));
    }

    [Fact]
    public void RasterMode_RunReachingTheLastPointIsTheLastPoint()
    {
        var points = Line((0.5, 0.5), (5.5, 5.5), (9.1, 9.1), (9.2, 9.3), (9.9, 9.9));

        Assert.Equal(Line((0.5, 0.5), (5.5, 5.5), (9.9, 9.9)), Simplifications.SimplifyByLiOpenshawRaster(points, Threshold(1)));
    }

    [Theory]
    [InlineData(SimplificationType.LiOpenshaw)]
    [InlineData(SimplificationType.LiOpenshawRaster)]
    public void Geometry_DispatchesBothModes(SimplificationType type)
    {
        var random = new Random(2);
        var points = Enumerable.Range(0, 100).Select(i => new Point(i, Math.Sin(i / 5.0) * 3 + (random.NextDouble() - 0.5) * 0.2)).ToList();
        var line = Geometry<Point>.Create(points, GeometryType.LineString);

        // SVO of 6 units on unit-spaced vertices: well below the 100 input points, well above the two ends
        var simplified = line.Simplify(type, Threshold(6));

        Assert.Equal(GeometryType.LineString, simplified.Type);
        Assert.InRange(simplified.Points.Count, 5, 60);
    }
}
