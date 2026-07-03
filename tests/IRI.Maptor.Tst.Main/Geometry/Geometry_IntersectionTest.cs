using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Tst.Main.TheGeometry;

/// <summary>
/// Tests for Geometry.Intersection / STIntersection and the STIntersects predicate
/// </summary>
public class Geometry_IntersectionTest
{
    private const int TestSrid = SridHelper.WebMercator;

    private const string Rectangle = "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))";

    private static Geometry<Point> FromWkt(string wkt) => Geometry<Point>.FromWkt(wkt, TestSrid);

    private static void AssertContainsPoint(Geometry<Point> geometry, double x, double y)
    {
        var points = GetAllPoints(geometry);

        Assert.Contains(points, p => System.Math.Abs(p.X - x) < 1e-6 && System.Math.Abs(p.Y - y) < 1e-6);
    }

    private static List<Point> GetAllPoints(Geometry<Point> geometry)
    {
        var result = new List<Point>();

        if (geometry.Points != null)
            result.AddRange(geometry.Points);

        if (geometry.Geometries != null)
            foreach (var child in geometry.Geometries)
                result.AddRange(GetAllPoints(child));

        return result;
    }

    #region LineString - LineString

    [Fact]
    public void Intersection_CrossingLineStrings_ReturnsIntersectionPoint()
    {
        var line1 = FromWkt("LINESTRING(0 0, 10 10)");
        var line2 = FromWkt("LINESTRING(0 10, 10 0)");

        var result = line1.Intersection(line2);

        Assert.Equal(GeometryType.Point, result.Type);
        Assert.Equal(5, result.Points[0].X, 6);
        Assert.Equal(5, result.Points[0].Y, 6);
    }

    [Fact]
    public void Intersection_CollinearOverlappingLineStrings_ReturnsOverlappingSegment()
    {
        var line1 = FromWkt("LINESTRING(0 0, 10 0)");
        var line2 = FromWkt("LINESTRING(5 0, 15 0)");

        var result = line1.Intersection(line2);

        Assert.Equal(GeometryType.LineString, result.Type);
        AssertContainsPoint(result, 5, 0);
        AssertContainsPoint(result, 10, 0);
    }

    [Fact]
    public void Intersection_DisjointLineStrings_ReturnsEmpty()
    {
        var line1 = FromWkt("LINESTRING(0 0, 10 10)");
        var line2 = FromWkt("LINESTRING(20 20, 30 30)");

        var result = line1.Intersection(line2);

        Assert.True(result.IsNullOrEmpty());
        Assert.Equal(TestSrid, result.Srid);
    }

    [Fact]
    public void Intersection_LineStringWithMultiLineString_ReturnsAllIntersectionPoints()
    {
        // the old implementation returned only the first intersection
        var line = FromWkt("LINESTRING(0 0, 20 0)");
        var multiLine = FromWkt("MULTILINESTRING((5 -5, 5 5), (15 -5, 15 5))");

        var result = line.Intersection(multiLine);

        Assert.Equal(GeometryType.MultiPoint, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
        AssertContainsPoint(result, 5, 0);
        AssertContainsPoint(result, 15, 0);
    }

    #endregion

    #region LineString - Polygon

    [Fact]
    public void Intersection_LineStringCrossingPolygon_ReturnsClippedLineString()
    {
        var line = FromWkt("LINESTRING(5 5, 15 15)");
        var polygon = FromWkt(Rectangle);

        var result = line.Intersection(polygon);

        Assert.Equal(GeometryType.LineString, result.Type);
        AssertContainsPoint(result, 5, 5);
        AssertContainsPoint(result, 10, 10);
        Assert.Equal(2, result.NumberOfPoints);
    }

    [Fact]
    public void Intersection_LineStringInsidePolygon_ReturnsWholeLineString()
    {
        var line = FromWkt("LINESTRING(2 2, 5 5, 8 2)");
        var polygon = FromWkt(Rectangle);

        var result = line.Intersection(polygon);

        // multi-vertex line fully inside must come back as a single stitched line string
        Assert.Equal(GeometryType.LineString, result.Type);
        Assert.Equal(3, result.NumberOfPoints);
    }

    [Fact]
    public void Intersection_LineStringThroughPolygon_ReturnsInnerSegment()
    {
        var line = FromWkt("LINESTRING(-5 5, 15 5)");
        var polygon = FromWkt(Rectangle);

        var result = line.Intersection(polygon);

        Assert.Equal(GeometryType.LineString, result.Type);
        AssertContainsPoint(result, 0, 5);
        AssertContainsPoint(result, 10, 5);
    }

    [Fact]
    public void Intersection_LineStringTouchingPolygonBoundary_ReturnsTouchPoint()
    {
        // the line pokes the boundary at (0 5) and goes back
        var line = FromWkt("LINESTRING(-5 0, 0 5, -5 10)");
        var polygon = FromWkt(Rectangle);

        var result = line.Intersection(polygon);

        Assert.Equal(GeometryType.Point, result.Type);
        AssertContainsPoint(result, 0, 5);
    }

    [Fact]
    public void Intersection_DisjointLineStringAndPolygon_ReturnsEmpty()
    {
        var line = FromWkt("LINESTRING(20 20, 30 30)");
        var polygon = FromWkt(Rectangle);

        Assert.True(line.Intersection(polygon).IsNullOrEmpty());
        Assert.True(polygon.Intersection(line).IsNullOrEmpty());
    }

    #endregion

    #region Point / MultiPoint

    [Fact]
    public void Intersection_PointOnPolygonBoundary_ReturnsPoint()
    {
        var point = FromWkt("POINT(0 5)");
        var polygon = FromWkt(Rectangle);

        // OGC/SQL Server semantics: boundary points intersect
        Assert.True(point.STIntersects(polygon));
        Assert.True(polygon.STIntersects(point));

        var result = point.Intersection(polygon);

        Assert.Equal(GeometryType.Point, result.Type);
        AssertContainsPoint(result, 0, 5);
    }

    [Fact]
    public void Intersection_NearCoincidentPoints_IsConsistentWithIntersects()
    {
        var point1 = Geometry<Point>.Create(10, 20, TestSrid);
        var point2 = Geometry<Point>.Create(10.00000001, 20, TestSrid);

        Assert.True(point1.Intersects(point2));
        Assert.False(point1.Intersection(point2).IsNullOrEmpty());
    }

    [Fact]
    public void Intersection_MultiPointWithPolygon_ReturnsContainedPoints()
    {
        // the old implementation returned empty for MultiPoint ∩ Polygon
        var multiPoint = FromWkt("MULTIPOINT((5 5), (20 20), (2 2))");
        var polygon = FromWkt(Rectangle);

        var result1 = multiPoint.Intersection(polygon);
        var result2 = polygon.Intersection(multiPoint);

        Assert.Equal(GeometryType.MultiPoint, result1.Type);
        Assert.Equal(2, result1.NumberOfGeometries);
        AssertContainsPoint(result1, 5, 5);
        AssertContainsPoint(result1, 2, 2);

        // commutativity
        Assert.Equal(result1.Type, result2.Type);
        Assert.Equal(result1.NumberOfGeometries, result2.NumberOfGeometries);
    }

    [Fact]
    public void Intersection_MultiPointWithLineString_ReturnsPointsOnLine()
    {
        var multiPoint = FromWkt("MULTIPOINT((5 5), (20 20))");
        var line = FromWkt("LINESTRING(0 0, 10 10)");

        var result = multiPoint.Intersection(line);

        Assert.Equal(GeometryType.Point, result.Type);
        AssertContainsPoint(result, 5, 5);
    }

    [Fact]
    public void Intersection_MultiPolygonWithPointInOverlappingMembers_ReturnsSinglePoint()
    {
        // the old implementation produced a GeometryCollection with the point duplicated
        var multiPolygon = FromWkt("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((5 5, 5 15, 15 15, 15 5, 5 5)))");
        var point = FromWkt("POINT(7 7)");

        var result = multiPolygon.Intersection(point);

        Assert.Equal(GeometryType.Point, result.Type);
        AssertContainsPoint(result, 7, 7);
    }

    #endregion

    #region Polygon - Polygon

    [Fact]
    public void Intersection_OverlappingPolygons_ReturnsIntersectionPolygon()
    {
        var polygon1 = FromWkt(Rectangle);
        var polygon2 = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");

        var result = polygon1.Intersection(polygon2);

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.Equal(4, result.TotalNumberOfPoints);
        AssertContainsPoint(result, 5, 5);
        AssertContainsPoint(result, 5, 10);
        AssertContainsPoint(result, 10, 10);
        AssertContainsPoint(result, 10, 5);
    }

    [Fact]
    public void Intersection_ContainedPolygon_ReturnsInnerPolygon()
    {
        var outer = FromWkt(Rectangle);
        var inner = FromWkt("POLYGON((2 2, 2 8, 8 8, 8 2, 2 2))");

        var result1 = outer.Intersection(inner);
        var result2 = inner.Intersection(outer);

        Assert.Equal(GeometryType.Polygon, result1.Type);
        Assert.Equal(4, result1.TotalNumberOfPoints);
        AssertContainsPoint(result1, 2, 2);
        AssertContainsPoint(result1, 8, 8);

        Assert.Equal(GeometryType.Polygon, result2.Type);
        Assert.Equal(4, result2.TotalNumberOfPoints);
    }

    [Fact]
    public void Intersection_DisjointPolygons_ReturnsEmpty()
    {
        var polygon1 = FromWkt(Rectangle);
        var polygon2 = FromWkt("POLYGON((20 20, 20 30, 30 30, 30 20, 20 20))");

        Assert.True(polygon1.Intersection(polygon2).IsNullOrEmpty());
    }

    [Fact]
    public void Intersection_PolygonWithMultiPolygon_CombinesMemberResults()
    {
        var polygon = FromWkt("POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))");
        var multiPolygon = FromWkt("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((20 20, 20 30, 30 30, 30 20, 20 20)))");

        var result = polygon.Intersection(multiPolygon);

        Assert.Equal(GeometryType.Polygon, result.Type);
        AssertContainsPoint(result, 5, 5);
        AssertContainsPoint(result, 10, 10);
    }

    [Fact]
    public void Intersection_PolygonsWithDegenerateSharedEdgeOverlap_ThrowsNotImplemented()
    {
        // interiors overlap while a boundary edge lies exactly on the other boundary:
        // honest NotImplementedException instead of a silent wrong result
        var polygon1 = FromWkt(Rectangle);
        var polygon2 = FromWkt("POLYGON((5 0, 5 10, 15 10, 15 0, 5 0))");

        Assert.Throws<NotImplementedException>(() => polygon1.Intersection(polygon2));
    }

    #endregion

    #region GeometryCollection

    [Fact]
    public void Intersection_GeometryCollectionWithPolygon_CombinesMemberResults()
    {
        var collection = Geometry<Point>.Create(
            new List<Geometry<Point>>
            {
                FromWkt("POINT(5 5)"),
                FromWkt("LINESTRING(20 20, 30 30)"),
            },
            GeometryType.GeometryCollection,
            TestSrid);

        var polygon = FromWkt(Rectangle);

        Assert.True(collection.Intersects(polygon));
        Assert.True(polygon.Intersects(collection));

        var result = collection.Intersection(polygon);

        Assert.Equal(GeometryType.Point, result.Type);
        AssertContainsPoint(result, 5, 5);
    }

    #endregion

    #region Commutativity / consistency between Intersects and Intersection

    [Theory]
    [InlineData("POINT(5 5)", Rectangle)]
    [InlineData("POINT(50 50)", Rectangle)]
    [InlineData("MULTIPOINT((5 5), (20 20))", Rectangle)]
    [InlineData("LINESTRING(5 5, 15 15)", Rectangle)]
    [InlineData("LINESTRING(20 20, 30 30)", Rectangle)]
    [InlineData("LINESTRING(0 0, 10 10)", "LINESTRING(0 10, 10 0)")]
    [InlineData("LINESTRING(0 0, 10 10)", "LINESTRING(20 20, 30 30)")]
    [InlineData(Rectangle, "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))")]
    [InlineData(Rectangle, "POLYGON((20 20, 20 30, 30 30, 30 20, 20 20))")]
    public void Intersection_IsCommutativeAndConsistentWithIntersects(string wkt1, string wkt2)
    {
        var geometry1 = FromWkt(wkt1);
        var geometry2 = FromWkt(wkt2);

        var result12 = geometry1.Intersection(geometry2);
        var result21 = geometry2.Intersection(geometry1);

        Assert.Equal(result12.IsNullOrEmpty(), result21.IsNullOrEmpty());

        Assert.Equal(geometry1.Intersects(geometry2), !result12.IsNullOrEmpty());
        Assert.Equal(geometry2.Intersects(geometry1), !result21.IsNullOrEmpty());

        // the SQL Server style aliases must agree
        Assert.Equal(geometry1.Intersects(geometry2), geometry1.STIntersects(geometry2));
        Assert.Equal(result12.IsNullOrEmpty(), geometry1.STIntersection(geometry2).IsNullOrEmpty());
    }

    #endregion
}
