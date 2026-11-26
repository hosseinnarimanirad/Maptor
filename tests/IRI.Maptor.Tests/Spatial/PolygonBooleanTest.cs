using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>Difference and union from boundary pieces (step 5.3), on shared edges and vertices where <c>Geometry.Intersection</c> gives up.</summary>
public class PolygonBooleanTest
{
    private static Geometry<Point> Wkt(string wkt) => Geometry<Point>.FromWkt(wkt, 0);

    private const string Square = "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))";

    private static double Area(Geometry<Point>? geometry) => geometry is null ? 0 : PolygonMeasure.Area(geometry, isGeodetic: false);

    private static Geometry<Point>? Difference(string first, string second) => PolygonBoolean.Difference(Wkt(first), Wkt(second), false, 1e-6);

    private static Geometry<Point>? Union(string first, string second) => PolygonBoolean.Union(Wkt(first), Wkt(second), false, 1e-6);

    [Theory]
    [InlineData("POLYGON((5 0, 15 0, 15 10, 5 10, 5 0))", 50)]                 // overlap along shared edge lines
    [InlineData("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))", 100)]             // shared edge: nothing removed
    [InlineData("POLYGON((10 10, 20 10, 20 20, 10 20, 10 10))", 100)]          // shared vertex
    [InlineData("POLYGON((50 50, 60 50, 60 60, 50 60, 50 50))", 100)]          // apart
    [InlineData("POLYGON((9.99 0, 20 0, 20 10, 9.99 10, 9.99 0))", 99.9)]      // sliver overlap
    public void Difference_HasTheRightArea(string other, double area)
    {
        var result = Difference(Square, other);

        Assert.NotNull(result);
        Assert.Equal(area, Area(result), 6);
        Assert.Equal(GeometryType.Polygon, result!.Type);
    }

    [Fact]
    public void Difference_ProperOverlap_GivesTheRemainingRectangle()
    {
        var result = Difference(Square, "POLYGON((5 0, 15 0, 15 10, 5 10, 5 0))")!;

        var ring = Assert.Single(result.Geometries!).Points!;

        Assert.Equal(4, ring.Count);   // never closed (house convention)
        Assert.All(ring, p => Assert.InRange(p.X, 0, 5));
        Assert.True(PlanarPolygon.SignedArea(ring.Select(p => (p.X, p.Y)).ToArray()) > 0);   // outer counterclockwise
    }

    [Fact]
    public void Difference_OfAPolygonInside_LeavesAHole()
    {
        var result = Difference(Square, "POLYGON((2 2, 4 2, 4 4, 2 4, 2 2))")!;

        Assert.Equal(2, result.Geometries!.Count);
        Assert.Equal(96, Area(result), 6);
        Assert.True(PlanarPolygon.SignedArea(result.Geometries[1].Points!.Select(p => (p.X, p.Y)).ToArray()) < 0);   // hole clockwise
    }

    [Fact]
    public void Difference_OfACoveringPolygon_LeavesNothing()
    {
        Assert.Null(Difference("POLYGON((2 2, 4 2, 4 4, 2 4, 2 2))", Square));
        Assert.Null(Difference(Square, Square));
    }

    [Fact]
    public void Difference_ThatCutsInTwo_IsAMultipolygon()
    {
        var result = Difference(Square, "POLYGON((4 -1, 6 -1, 6 11, 4 11, 4 -1))")!;

        Assert.Equal(GeometryType.MultiPolygon, result.Type);
        Assert.Equal(2, result.Geometries!.Count);
        Assert.Equal(80, Area(result), 6);
    }

    [Theory]
    [InlineData("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))", 200)]             // shared edge: one polygon
    [InlineData("POLYGON((5 0, 15 0, 15 10, 5 10, 5 0))", 150)]                // overlap
    [InlineData("POLYGON((2 2, 4 2, 4 4, 2 4, 2 2))", 100)]                    // inside
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", 100)]                // duplicate drawn the other way
    public void Union_HasTheRightArea_AndOneRing(string other, double area)
    {
        var result = Union(Square, other)!;

        Assert.Equal(area, Area(result), 6);
        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.Single(result.Geometries!);
    }

    [Fact]
    public void Union_OfAPartialNeighbour_FollowsItsEdge()
    {
        // The neighbour shares only part of the edge x = 10.
        var result = Union(Square, "POLYGON((10 2, 14 2, 14 6, 10 6, 10 2))")!;

        Assert.Equal(116, Area(result), 6);
        Assert.Single(result.Geometries!);
    }

    [Fact]
    public void Union_OfApartPolygons_IsAMultipolygon()
    {
        var result = Union(Square, "POLYGON((50 50, 60 50, 60 60, 50 60, 50 50))")!;

        Assert.Equal(GeometryType.MultiPolygon, result.Type);
        Assert.Equal(200, Area(result), 6);
    }

    [Fact]
    public void OnGeodeticData_TheResultStaysInDegrees()
    {
        var a = Geometry<Point>.FromWkt("POLYGON((51 35.7, 51.001 35.7, 51.001 35.701, 51 35.701, 51 35.7))", 4326);
        var b = Geometry<Point>.FromWkt("POLYGON((51.0005 35.7, 51.0015 35.7, 51.0015 35.701, 51.0005 35.701, 51.0005 35.7))", 4326);

        var result = PolygonBoolean.Difference(a, b, true, 1e-6)!;

        Assert.Equal(4326, result.Srid);
        Assert.All(result.Geometries![0].Points!, p => Assert.InRange(p.X, 50.9999999, 51.0005000001));
        Assert.Equal(PolygonMeasure.Area(a, true) / 2, PolygonMeasure.Area(result, true), 0);
    }
}
