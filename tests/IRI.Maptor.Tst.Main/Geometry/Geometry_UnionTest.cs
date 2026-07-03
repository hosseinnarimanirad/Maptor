using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Collections.Generic;

namespace IRI.Maptor.Tst.Main.TheGeometry;

/// <summary>
/// Tests for Geometry.Union / STUnion correctness (polygon merging, absorption, dedup, commutativity)
/// </summary>
public class Geometry_UnionTest
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
    public void Union_ContainedPolygon_ReturnsOuterPolygonWithoutHole()
    {
        // regression: ring concatenation turned the contained polygon into a HOLE (area 300)
        var outer = FromWkt("POLYGON((0 0, 0 20, 20 20, 20 0, 0 0))");
        var inner = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        var result1 = outer.Union(inner);
        var result2 = inner.Union(outer);

        Assert.Equal(GeometryType.Polygon, result1.Type);
        Assert.Equal(1, result1.NumberOfGeometries); // single ring, no hole
        AssertAreaEquals(400, result1.EuclideanArea);

        Assert.Equal(GeometryType.Polygon, result2.Type);
        AssertAreaEquals(400, result2.EuclideanArea);
    }

    [Fact]
    public void Union_OverlappingPolygons_ReturnsMergedPolygon()
    {
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        var result1 = polygon1.Union(polygon2);
        var result2 = polygon2.Union(polygon1);

        Assert.Equal(GeometryType.Polygon, result1.Type);
        AssertAreaEquals(175, result1.EuclideanArea); // 100 + 100 − 25
        AssertAreaEquals(175, result2.EuclideanArea);
    }

    [Fact]
    public void Union_IdenticalPolygons_ReturnsSinglePolygon()
    {
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        var result = polygon1.Union(polygon2);

        Assert.Equal(GeometryType.Polygon, result.Type);
        AssertAreaEquals(100, result.EuclideanArea);
    }

    [Fact]
    public void Union_DisjointPolygons_ReturnsMultiPolygon()
    {
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((20 20, 20 30, 30 30, 30 20, 20 20))");

        var result = polygon1.Union(polygon2);

        Assert.Equal(GeometryType.MultiPolygon, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
        AssertAreaEquals(200, result.EuclideanArea);
    }

    [Fact]
    public void Union_TouchingPolygons_KeepsBothWithFullCoverage()
    {
        // adjacent parcels sharing an edge must not throw; coverage stays complete
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((10 0, 10 10, 20 10, 20 0, 10 0))");

        var result = polygon1.Union(polygon2);

        Assert.False(result.IsNullOrEmpty());
        AssertAreaEquals(200, result.EuclideanArea);
    }

    [Fact]
    public void Union_DegenerateOverlappingPolygons_ThrowsNotImplemented()
    {
        // interiors overlap while boundary edges are collinear: honest failure instead of
        // an invalid double-covered result
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((5 0, 5 10, 15 10, 15 0, 5 0))");

        Assert.Throws<NotImplementedException>(() => polygon1.Union(polygon2));
    }

    [Fact]
    public void Union_PolygonWithMultiPolygon_MergesOverlappingMembers()
    {
        var polygon = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");
        var multiPolygon = FromWkt("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((30 30, 30 40, 40 40, 40 30, 30 30)))");

        var result = polygon.Union(multiPolygon);

        Assert.Equal(GeometryType.MultiPolygon, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
        AssertAreaEquals(175 + 100, result.EuclideanArea);
    }

    #endregion

    #region Absorption of covered lower-dimensional operands

    [Fact]
    public void Union_PointOnLineString_ReturnsJustTheLine()
    {
        var point = FromWkt("POINT(5 5)");
        var line = FromWkt("LINESTRING(0 0, 10 10)");

        var result1 = point.Union(line);
        var result2 = line.Union(point);

        Assert.Equal(GeometryType.LineString, result1.Type);
        Assert.Equal(GeometryType.LineString, result2.Type);
    }

    [Fact]
    public void Union_PointInsidePolygon_ReturnsJustThePolygon()
    {
        var point = FromWkt("POINT(5 5)");
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        var result1 = point.Union(polygon);
        var result2 = polygon.Union(point);

        Assert.Equal(GeometryType.Polygon, result1.Type);
        AssertAreaEquals(100, result1.EuclideanArea);
        Assert.Equal(GeometryType.Polygon, result2.Type);
    }

    [Fact]
    public void Union_LineInsidePolygon_ReturnsJustThePolygon()
    {
        var line = FromWkt("LINESTRING(2 2, 8 8)");
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        var result = line.Union(polygon);

        Assert.Equal(GeometryType.Polygon, result.Type);
        AssertAreaEquals(100, result.EuclideanArea);
    }

    [Fact]
    public void Union_DisjointPointAndLine_ReturnsGeometryCollection()
    {
        var point = FromWkt("POINT(50 50)");
        var line = FromWkt("LINESTRING(0 0, 10 10)");

        var result = point.Union(line);

        Assert.Equal(GeometryType.GeometryCollection, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
    }

    #endregion

    #region Point / line dedup and commutativity

    [Fact]
    public void Union_NearCoincidentPoints_ReturnsSinglePoint()
    {
        var point1 = Geometry<Point>.Create(10, 20, TestSrid);
        var point2 = Geometry<Point>.Create(10.00000001, 20, TestSrid);

        var result = point1.Union(point2);

        Assert.Equal(GeometryType.Point, result.Type);
    }

    [Fact]
    public void Union_PointWithMultiPoint_IsCommutativeAndDeduped()
    {
        // regression: Point ∪ MultiPoint did not dedupe while MultiPoint ∪ MultiPoint did
        var point = FromWkt("POINT(10 20)");
        var multiPoint = FromWkt("MULTIPOINT((10 20), (30 40))");

        var result1 = point.Union(multiPoint);
        var result2 = multiPoint.Union(point);

        Assert.Equal(GeometryType.MultiPoint, result1.Type);
        Assert.Equal(2, result1.NumberOfGeometries);
        Assert.Equal(GeometryType.MultiPoint, result2.Type);
        Assert.Equal(2, result2.NumberOfGeometries);
    }

    [Fact]
    public void Union_IdenticalLineStrings_ReturnsSingleLineString()
    {
        var line1 = FromWkt("LINESTRING(0 0, 10 10)");
        var line2 = FromWkt("LINESTRING(0 0, 10 10)");
        var reversed = FromWkt("LINESTRING(10 10, 0 0)");

        Assert.Equal(GeometryType.LineString, line1.Union(line2).Type);
        Assert.Equal(GeometryType.LineString, line1.Union(reversed).Type);
    }

    [Fact]
    public void Union_DisjointLineStrings_ReturnsMultiLineString()
    {
        var line1 = FromWkt("LINESTRING(0 0, 10 10)");
        var line2 = FromWkt("LINESTRING(20 20, 30 30)");

        var result = line1.Union(line2);

        Assert.Equal(GeometryType.MultiLineString, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
    }

    #endregion

    #region GeometryCollection / aliases

    [Fact]
    public void Union_GeometryCollectionOperand_FlattensAndCombines()
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

        // point (5 5) is absorbed by the polygon; the line stays
        var result = collection.Union(polygon);

        Assert.Equal(GeometryType.GeometryCollection, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
        Assert.Contains(result.Geometries, g => g.Type == GeometryType.LineString);
        Assert.Contains(result.Geometries, g => g.Type == GeometryType.Polygon);
    }

    [Fact]
    public void Union_STUnionAlias_AgreesWithUnion()
    {
        var polygon1 = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");
        var polygon2 = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        AssertAreaEquals(polygon1.Union(polygon2).EuclideanArea, polygon1.STUnion(polygon2).EuclideanArea);
    }

    [Fact]
    public void Union_DoesNotAliasInputs()
    {
        // regression: results used to reference the input sub-geometries directly
        var multiPoint = FromWkt("MULTIPOINT((10 20), (30 40))");
        var point = FromWkt("POINT(50 60)");

        var result = multiPoint.Union(point);

        result.Geometries[0].Points[0].X = 999;

        Assert.Equal(10, multiPoint.Geometries[0].Points[0].X);
    }

    #endregion
}
