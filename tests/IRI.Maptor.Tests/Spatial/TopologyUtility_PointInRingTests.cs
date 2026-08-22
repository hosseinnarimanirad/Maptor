using System.Collections.Generic;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.Spatial;

public class TopologyUtility_PointInRingTests
{
    private static Geometry<Point> RingFrom(params Point[] points) =>
        Geometry<Point>.Create(new List<Point>(points), GeometryType.LineString, 0);

    [Fact]
    public void IsPointInRing_RayCast_InsideCcwSquare()
    {
        var ring = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10));

        Assert.True(TopologyUtility.IsPointInRing(ring, new Point(5, 5)));
    }

    [Fact]
    public void IsPointInRing_RayCast_OutsideCcwSquare()
    {
        var ring = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10));

        Assert.False(TopologyUtility.IsPointInRing(ring, new Point(50, 50)));
    }

    [Fact]
    public void IsPointInRing_OnEdgeOfSquare_NotInside_StrictInterior()
    {
        var ring = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10));

        Assert.False(TopologyUtility.IsPointInRing(ring, new Point(5, 0)));
    }

    [Fact]
    public void IsPointInRing_OnVertexOfSquare_NotInside_StrictInterior()
    {
        var ring = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10));

        Assert.False(TopologyUtility.IsPointInRing(ring, new Point(10, 10)));
    }

    /// <summary>
    /// Outside point to the left; horizontal ray at y=0 passes through vertex (0,0). Naive parity toggled once; must be outside.
    /// </summary>
    [Fact]
    public void IsPointInRing_OutsideLeftOfTriangle_RayThroughVertexOnHorizon_IsOutside()
    {
        var ring = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(5, 10));

        Assert.False(TopologyUtility.IsPointInRing(ring, new Point(-1, 0)));
    }

    [Fact]
    public void IsPointInRing_PrecomputedBoundingBox_SkipsVertexWalkWhenOutside()
    {
        var ring = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10));

        var env = new BoundingBox(0, 0, 10, 10);

        Assert.False(TopologyUtility.IsPointInRing(ring, new Point(100, 100), env));
    }

    [Fact]
    public void IsPointInRingUsingSignedAngles_LegacyInsideCcwSquare()
    {
        var ring = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10));

        Assert.True(TopologyUtility.IsPointInRingUsingSignedAngles(ring, new Point(5, 5)));
    }

    [Fact]
    public void CreatePolygonOrMultiPolygon_TwoDisjointParts()
    {
        var part1 = RingFrom(
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10));

        var part2 = RingFrom(
            new Point(100, 0),
            new Point(110, 0),
            new Point(110, 10),
            new Point(100, 10));

        var g = Geometry<Point>.CreatePolygonOrMultiPolygon(new List<Geometry<Point>> { part1, part2 }, 0);

        Assert.Equal(GeometryType.MultiPolygon, g.Type);
        Assert.Equal(2, g.NumberOfGeometries);
        Assert.Single(g.Geometries![0].Geometries!);
        Assert.Single(g.Geometries[1].Geometries!);
    }

    [Fact]
    public void CreatePolygonOrMultiPolygon_OuterAndHoleByAreaOrder()
    {
        var outer = RingFrom(
            new Point(0, 0),
            new Point(20, 0),
            new Point(20, 20),
            new Point(0, 20));

        var hole = RingFrom(
            new Point(5, 5),
            new Point(15, 5),
            new Point(15, 15),
            new Point(5, 15));

        var g = Geometry<Point>.CreatePolygonOrMultiPolygon(new List<Geometry<Point>> { outer, hole }, 0);

        Assert.Equal(GeometryType.Polygon, g.Type);
        Assert.Equal(2, g.NumberOfGeometries);
    }
}
