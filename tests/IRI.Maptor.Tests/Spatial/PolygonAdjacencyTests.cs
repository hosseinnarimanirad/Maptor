using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.Spatial;

public class PolygonAdjacencyTests
{
    private static Geometry<Point> Rectangle(double xMin, double yMin, double xMax, double yMax)
    {
        return Geometry<Point>.Create(
            new List<Point> { new(xMin, yMin), new(xMax, yMin), new(xMax, yMax), new(xMin, yMax) },
            GeometryType.Polygon);
    }

    private static Geometry<Point> Polygon(params (double X, double Y)[] points)
    {
        return Geometry<Point>.Create(points.Select(p => new Point(p.X, p.Y)).ToList(), GeometryType.Polygon);
    }

    [Fact]
    public void Analyze_TwoSquaresSharingAnEdge_AreNeighboursAlongTheWholeEdge()
    {
        var result = PolygonAdjacency.Analyze(new[] { Rectangle(0, 0, 1, 1), Rectangle(1, 0, 2, 1) }, 0.01);

        Assert.Equal(new[] { 1 }, result[0].Neighbours);
        Assert.Equal(new[] { 0 }, result[1].Neighbours);

        Assert.Equal(1, result[0].GetSharedLength(1), 6);
        Assert.Equal(1, result[1].GetSharedLength(0), 6);

        // four edges each: one shared, three open
        Assert.Equal(1, result[0].Pieces.Count(p => p.IsShared));
        Assert.Equal(3, result[0].Pieces.Count(p => !p.IsShared));
    }

    [Fact]
    public void Analyze_SquaresTouchingAtACorner_AreNotNeighbours()
    {
        var result = PolygonAdjacency.Analyze(new[] { Rectangle(0, 0, 1, 1), Rectangle(1, 1, 2, 2) }, 0.01);

        Assert.Empty(result[0].Neighbours);
        Assert.Empty(result[1].Neighbours);
        Assert.All(result[0].Pieces, p => Assert.False(p.IsShared));
    }

    [Fact]
    public void Analyze_PartialOverlap_SplitsTheEdgeIntoOpenAndSharedPieces()
    {
        // B covers the upper half of A's right edge (x = 2, y 1..2)
        var result = PolygonAdjacency.Analyze(new[] { Rectangle(0, 0, 2, 2), Rectangle(2, 1, 3, 3) }, 0.01);

        var rightEdge = result[0].Pieces.Where(p => p.Start.X == 2 && p.End.X == 2).ToList();

        Assert.Equal(2, rightEdge.Count);
        Assert.Null(rightEdge[0].NeighbourIndex);
        Assert.Equal(1, rightEdge[0].Length, 6);
        Assert.Equal(1, rightEdge[1].NeighbourIndex);
        Assert.Equal(1, rightEdge[1].Length, 6);

        // the shared piece runs from (2,1) to (2,2)
        Assert.Equal(1, rightEdge[1].Start.Y, 6);
        Assert.Equal(2, rightEdge[1].End.Y, 6);
    }

    [Fact]
    public void Analyze_SmallGap_DependsOnTheTolerance()
    {
        var polygons = new[] { Rectangle(0, 0, 1, 1), Rectangle(1.02, 0, 2, 1) };

        Assert.Equal(new[] { 1 }, PolygonAdjacency.Analyze(polygons, 0.05)[0].Neighbours);
        Assert.Empty(PolygonAdjacency.Analyze(polygons, 0.01)[0].Neighbours);
    }

    [Fact]
    public void Analyze_DifferentVertexDensity_MergesTheSharedEdgeIntoOnePiece()
    {
        // B's left side has an extra vertex at (1, 0.5); A's right side is one edge
        var a = Rectangle(0, 0, 1, 1);
        var b = Polygon((1, 0), (2, 0), (2, 1), (1, 1), (1, 0.5));

        var result = PolygonAdjacency.Analyze(new[] { a, b }, 0.01);

        Assert.Equal(1, result[0].Pieces.Count(p => p.NeighbourIndex == 1));
        Assert.Equal(2, result[1].Pieces.Count(p => p.NeighbourIndex == 0));
        Assert.Equal(1, result[1].GetSharedLength(0), 6);
    }

    [Fact]
    public void Analyze_SlightlySkewedNeighbour_SharesOnlyThePartWithinTheTolerance()
    {
        // B's left side leans away from A: x = 1 at y = 0, x = 1.1 at y = 1; within 0.05 for y <= 0.5
        var a = Rectangle(0, 0, 1, 1);
        var b = Polygon((1, 0), (2, 0), (2, 1), (1.1, 1));

        var shared = PolygonAdjacency.Analyze(new[] { a, b }, 0.05)[0].GetSharedLength(1);

        Assert.Equal(0.5, shared, 2);
    }

    [Fact]
    public void Analyze_ThreeParcelsAlongOneEdge_GivesEachItsOwnPiece()
    {
        var a = Rectangle(0, 0, 3, 1);
        var b = Rectangle(0, 1, 1, 2);
        var c = Rectangle(1, 1, 3, 2);

        var top = PolygonAdjacency.Analyze(new[] { a, b, c }, 0.01)[0]
            .Pieces.Where(p => p.Start.Y == 1 && p.End.Y == 1)
            .ToList();

        Assert.Equal(2, top.Count);
        Assert.Equal(new int?[] { 2, 1 }, top.Select(p => p.NeighbourIndex).ToArray());
        Assert.Equal(new[] { 2.0, 1.0 }, top.Select(p => System.Math.Round(p.Length, 6)).ToArray());
    }

    [Fact]
    public void Analyze_NonPositiveTolerance_Throws()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PolygonAdjacency.Analyze(new[] { Rectangle(0, 0, 1, 1) }, 0));
    }
}
