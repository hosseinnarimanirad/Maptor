using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Collections.Generic;

namespace IRI.Maptor.Tst.Main.TheGeometry;

/// <summary>
/// Tests for Geometry.Difference / STDifference correctness (polygon clipping, line clipping, coverage removal)
/// </summary>
public class Geometry_DifferenceTest
{
    private const int TestSrid = SridHelper.WebMercator;

    private static Geometry<Point> FromWkt(string wkt) => Geometry<Point>.FromWkt(wkt, TestSrid);

    private static void AssertAreaEquals(double expected, double actual)
    {
        Assert.True(
            Math.Abs(actual - expected) < 1E-6 * Math.Max(1, Math.Abs(expected)),
            $"Expected area {expected} but got {actual}");
    }

    #region Polygon - Polygon

    [Fact]
    public void Difference_OverlappingPolygons_ReturnsClippedPolygon()
    {
        // regression: the old implementation returned the minuend unchanged (or empty,
        // depending on where its first vertex sat)
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        var result1 = polygon1.Difference(polygon2);
        var result2 = polygon2.Difference(polygon1);

        Assert.Equal(GeometryType.Polygon, result1.Type);
        AssertAreaEquals(75, result1.EuclideanArea); // 100 − 25

        Assert.Equal(GeometryType.Polygon, result2.Type);
        AssertAreaEquals(75, result2.EuclideanArea);
    }

    [Fact]
    public void Difference_ContainedSubtrahend_PunchesHole()
    {
        var outer = FromWkt("POLYGON((0 0, 0 20, 20 20, 20 0, 0 0))");
        var inner = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        var result = outer.Difference(inner);

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.Equal(2, result.NumberOfGeometries); // exterior + hole
        AssertAreaEquals(300, result.EuclideanArea); // 400 − 100
    }

    [Fact]
    public void Difference_ContainedMinuend_ReturnsEmpty()
    {
        var inner = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");
        var outer = FromWkt("POLYGON((0 0, 0 20, 20 20, 20 0, 0 0))");

        Assert.True(inner.Difference(outer).IsNullOrEmpty());
    }

    [Fact]
    public void Difference_IdenticalPolygons_ReturnsEmpty()
    {
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        Assert.True(polygon1.Difference(polygon2).IsNullOrEmpty());
    }

    [Fact]
    public void Difference_DisjointAndTouchingPolygons_ReturnMinuendUnchanged()
    {
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var disjoint = FromWkt("POLYGON((20 20, 20 30, 30 30, 30 20, 20 20))");
        var touching = FromWkt("POLYGON((10 0, 10 10, 20 10, 20 0, 10 0))");

        AssertAreaEquals(100, polygon.Difference(disjoint).EuclideanArea);
        AssertAreaEquals(100, polygon.Difference(touching).EuclideanArea);
    }

    [Fact]
    public void Difference_SubtrahendSplitsMinuend_ReturnsMultiPolygon()
    {
        // a vertical strip cuts the square into two pieces
        var square = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var strip = FromWkt("POLYGON((4 -5, 4 15, 6 15, 6 -5, 4 -5))");

        var result = square.Difference(strip);

        Assert.Equal(GeometryType.MultiPolygon, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
        AssertAreaEquals(80, result.EuclideanArea); // 100 − 2·10
    }

    [Fact]
    public void Difference_DegenerateOverlappingPolygons_ThrowsNotImplemented()
    {
        // interiors overlap while boundary edges are collinear: honest failure
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((5 0, 5 10, 15 10, 15 0, 5 0))");

        Assert.Throws<NotImplementedException>(() => polygon1.Difference(polygon2));
    }

    [Fact]
    public void Difference_MultiPolygonMinusPolygon_ClipsOnlyOverlappingMember()
    {
        var multiPolygon = FromWkt("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((20 20, 20 30, 30 30, 30 20, 20 20)))");
        var polygon = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        var result = multiPolygon.Difference(polygon);

        Assert.Equal(GeometryType.MultiPolygon, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
        AssertAreaEquals(75 + 100, result.EuclideanArea);
    }

    [Fact]
    public void Difference_PolygonMinusPointOrLine_ReturnsPolygonUnchanged()
    {
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        AssertAreaEquals(100, polygon.Difference(FromWkt("POINT(5 5)")).EuclideanArea);
        AssertAreaEquals(100, polygon.Difference(FromWkt("LINESTRING(2 2, 8 8)")).EuclideanArea);
    }

    #endregion

    #region LineString

    [Fact]
    public void Difference_LineStringMinusPolygon_ReturnsOutsideParts()
    {
        // regression: the old implementation returned Empty for any intersecting line
        var line = FromWkt("LINESTRING(-5 5, 15 5)");
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        var result = line.Difference(polygon);

        Assert.Equal(GeometryType.MultiLineString, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
    }

    [Fact]
    public void Difference_LineStringInsidePolygon_ReturnsEmpty()
    {
        var line = FromWkt("LINESTRING(2 2, 8 8)");
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        Assert.True(line.Difference(polygon).IsNullOrEmpty());
    }

    [Fact]
    public void Difference_LineStringMinusCollinearOverlap_RemovesOverlappingPart()
    {
        var line = FromWkt("LINESTRING(0 0, 10 10)");
        var overlapping = FromWkt("LINESTRING(5 5, 15 15)");

        var result = line.Difference(overlapping);

        Assert.Equal(GeometryType.LineString, result.Type);
        Assert.Contains(result.Points, p => Math.Abs(p.X) < 1e-6 && Math.Abs(p.Y) < 1e-6);
        Assert.Contains(result.Points, p => Math.Abs(p.X - 5) < 1e-6 && Math.Abs(p.Y - 5) < 1e-6);
    }

    [Fact]
    public void Difference_LineStringMinusCrossingLine_ReturnsLineUnchanged()
    {
        // a crossing point has zero measure and does not change the line
        var line = FromWkt("LINESTRING(0 0, 10 10)");
        var crossing = FromWkt("LINESTRING(0 10, 10 0)");

        var result = line.Difference(crossing);

        Assert.Equal(GeometryType.LineString, result.Type);
    }

    [Fact]
    public void Difference_DisjointLineStrings_ReturnsLineUnchanged()
    {
        var line = FromWkt("LINESTRING(0 0, 10 10)");
        var other = FromWkt("LINESTRING(20 20, 30 30)");

        Assert.Equal(GeometryType.LineString, line.Difference(other).Type);
    }

    #endregion

    #region Point / MultiPoint

    [Fact]
    public void Difference_NearCoincidentPoints_ReturnsEmpty()
    {
        var point1 = Geometry<Point>.Create(10, 20, TestSrid);
        var point2 = Geometry<Point>.Create(10.00000001, 20, TestSrid);

        Assert.True(point1.Difference(point2).IsNullOrEmpty());
    }

    [Fact]
    public void Difference_PointOnPolygonBoundary_ReturnsEmpty()
    {
        var point = FromWkt("POINT(0 5)");
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        Assert.True(point.Difference(polygon).IsNullOrEmpty());
    }

    [Fact]
    public void Difference_MultiPointMinusPolygon_RemovesCoveredPoints()
    {
        // regression: the old implementation returned all points unchanged
        var multiPoint = FromWkt("MULTIPOINT((5 5), (20 20), (2 2))");
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        var result = multiPoint.Difference(polygon);

        Assert.Equal(GeometryType.Point, result.Type);
        Assert.Equal(20, result.Points[0].X, 6);
    }

    [Fact]
    public void Difference_MultiPointMinusLineString_RemovesPointsOnLine()
    {
        var multiPoint = FromWkt("MULTIPOINT((5 5), (20 20))");
        var line = FromWkt("LINESTRING(0 0, 10 10)");

        var result = multiPoint.Difference(line);

        Assert.Equal(GeometryType.Point, result.Type);
        Assert.Equal(20, result.Points[0].X, 6);
    }

    #endregion

    #region GeometryCollection / aliases

    [Fact]
    public void Difference_GeometryCollectionOperands_FlattenAndSubtract()
    {
        var collection = Geometry<Point>.Create(
            new List<Geometry<Point>>
            {
                FromWkt("POINT(5 5)"),
                FromWkt("LINESTRING(20 20, 30 30)"),
            },
            GeometryType.GeometryCollection,
            TestSrid);

        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        // the point is inside the polygon and disappears; the line remains
        var result = collection.Difference(polygon);

        Assert.Equal(GeometryType.LineString, result.Type);
    }

    [Fact]
    public void Difference_STDifferenceAlias_AgreesWithDifference()
    {
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        AssertAreaEquals(polygon1.Difference(polygon2).EuclideanArea, polygon1.STDifference(polygon2).EuclideanArea);
    }

    [Fact]
    public void Difference_DoesNotAliasInputs()
    {
        var multiPoint = FromWkt("MULTIPOINT((10 20), (30 40))");
        var point = FromWkt("POINT(50 60)");

        var result = multiPoint.Difference(point);

        result.Geometries[0].Points[0].X = 999;

        Assert.Equal(10, multiPoint.Geometries[0].Points[0].X);
    }

    #endregion
}
