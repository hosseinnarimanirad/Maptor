using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Tst.Spatial;

public class ConvexHullTests
{
    private static List<Point> Points(params (double x, double y)[] coordinates) =>
        coordinates.Select(c => new Point(c.x, c.y)).ToList();

    private static void AssertHull(List<Point> hull, params (double x, double y)[] expected)
    {
        Assert.Equal(expected.Length, hull.Count);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].x, hull[i].X);
            Assert.Equal(expected[i].y, hull[i].Y);
        }
    }

    [Fact]
    public void CreateConvexHull_SquareWithInteriorPoints_ReturnsCornersCcwFromLowest()
    {
        var points = Points((0, 0), (10, 0), (10, 10), (0, 10), (5, 5), (2, 7), (8, 3));

        var hull = ComputationalGeometry.CreateConvexHull(points);

        AssertHull(hull, (0, 0), (10, 0), (10, 10), (0, 10));
    }

    [Fact]
    public void CreateConvexHull_LastAngleGroupTie_KeepsFarthestPoint()
    {
        // (-1, 1) and (-2, 2) are collinear with the pivot at the largest angle
        var points = Points((0, 0), (2, 0), (2, 2), (-1, 1), (-2, 2));

        var hull = ComputationalGeometry.CreateConvexHull(points);

        AssertHull(hull, (0, 0), (2, 0), (2, 2), (-2, 2));
    }

    [Fact]
    public void CreateConvexHull_FirstAngleGroupTie_ExcludesEdgeInteriorPoint()
    {
        // (1, 0) lies on the hull edge between (0, 0) and (3, 0)
        var points = Points((0, 0), (1, 0), (3, 0), (1, 2));

        var hull = ComputationalGeometry.CreateConvexHull(points);

        AssertHull(hull, (0, 0), (3, 0), (1, 2));
    }

    [Fact]
    public void CreateConvexHull_CollinearPointOnEdgeNotThroughPivot_IsExcluded()
    {
        // (2, 4) lies on the top edge between (4, 4) and (0, 4)
        var points = Points((0, 0), (4, 0), (4, 4), (2, 4), (0, 4));

        var hull = ComputationalGeometry.CreateConvexHull(points);

        AssertHull(hull, (0, 0), (4, 0), (4, 4), (0, 4));
    }

    [Fact]
    public void CreateConvexHull_AllCollinear_ReturnsTwoExtremes()
    {
        var points = Points((0, 0), (1, 1), (2, 2), (3, 3));

        var hull = ComputationalGeometry.CreateConvexHull(points);

        AssertHull(hull, (0, 0), (3, 3));
    }

    [Fact]
    public void CreateConvexHull_EmptyInput_ReturnsEmpty()
    {
        var hull = ComputationalGeometry.CreateConvexHull(new List<Point>());

        Assert.Empty(hull);
    }

    [Fact]
    public void CreateConvexHull_SinglePoint_ReturnsThatPoint()
    {
        var hull = ComputationalGeometry.CreateConvexHull(Points((3, 4)));

        AssertHull(hull, (3, 4));
    }

    [Fact]
    public void CreateConvexHull_TwoPoints_ReturnsBoth()
    {
        var hull = ComputationalGeometry.CreateConvexHull(Points((3, 4), (1, 2)));

        Assert.Equal(2, hull.Count);
    }

    [Fact]
    public void CreateConvexHull_DuplicatePoints_AreIgnored()
    {
        var points = Points((0, 0), (0, 0), (10, 0), (10, 0), (10, 10), (0, 10), (5, 5), (5, 5));

        var hull = ComputationalGeometry.CreateConvexHull(points);

        AssertHull(hull, (0, 0), (10, 0), (10, 10), (0, 10));
    }

    [Fact]
    public void CreateConvexHull_ResultPointsAreCopies()
    {
        var points = Points((0, 0), (10, 0), (5, 8));

        var hull = ComputationalGeometry.CreateConvexHull(points);

        hull[0].X = 999;

        Assert.Equal(0, points[0].X);
    }

    [Fact]
    public void GetConvexHull_OnGeometry_ReturnsPolygon()
    {
        var geometry = Geometry<Point>.Create(
            Points((0, 0), (10, 0), (10, 10), (0, 10), (5, 5)), GeometryType.MultiPoint, 0);

        var hull = geometry.GetConvexHull();

        Assert.NotNull(hull);
        Assert.Equal(GeometryType.Polygon, hull!.Type);
        Assert.Equal(4, hull.GetAllPoints().Count);
    }
}
