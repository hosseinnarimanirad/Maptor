using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using System.Collections.Generic;

namespace IRI.Maptor.Tests.TheGeometry;

/// <summary>
/// Tests for Geometry.Buffer correctness (areas, containment, hole handling, multi-part union)
/// </summary>
public class Geometry_BufferTest
{
    private const int TestSrid = SridHelper.WebMercator;

    private static Geometry<Point> FromWkt(string wkt) => Geometry<Point>.FromWkt(wkt, TestSrid);

    private static void AssertAreaEquals(double expected, double actual, double relativeTolerance)
    {
        Assert.True(
            Math.Abs(actual - expected) / expected < relativeTolerance,
            $"Expected area ≈ {expected} but got {actual}");
    }

    #region Point

    [Fact]
    public void Buffer_Point_ReturnsCircleOfCorrectArea()
    {
        var point = FromWkt("POINT(10 20)");

        var result = point.Buffer(10);

        Assert.Equal(GeometryType.Polygon, result.Type);
        AssertAreaEquals(Math.PI * 100, result.EuclideanArea, 0.01);
        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(10, 20)));
    }

    #endregion

    #region LineString

    [Fact]
    public void Buffer_LineString_CoversBothSidesOfTheLine()
    {
        // regression: the old implementation only produced the left half
        var line = FromWkt("LINESTRING(0 0, 10 0)");

        var result = line.Buffer(2);

        Assert.Equal(GeometryType.Polygon, result.Type);

        // area = 2·d·L + π·d²
        AssertAreaEquals(2 * 2 * 10 + Math.PI * 4, result.EuclideanArea, 0.02);

        // points strictly on each side of the line must be covered
        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(5, 1)));
        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(5, -1)));

        // and the line itself
        Assert.True(result.Intersects(line));
    }

    [Fact]
    public void Buffer_LineString_IsIndependentOfPointOrder()
    {
        var forward = FromWkt("LINESTRING(0 0, 10 0)").Buffer(2);
        var backward = FromWkt("LINESTRING(10 0, 0 0)").Buffer(2);

        AssertAreaEquals(forward.EuclideanArea, backward.EuclideanArea, 0.001);
    }

    [Fact]
    public void Buffer_MultiVertexLineString_HasCorrectAreaWithRoundJoin()
    {
        // right-angle polyline: area = 2·d·L + π·d² (caps) + π·d²/4 (outer round join) − d² (inner overlap)
        var line = FromWkt("LINESTRING(0 0, 10 0, 10 10)");

        var result = line.Buffer(2);

        double d = 2, length = 20;
        double expected = 2 * d * length + Math.PI * d * d + Math.PI * d * d / 4 - d * d;

        Assert.Equal(GeometryType.Polygon, result.Type);
        AssertAreaEquals(expected, result.EuclideanArea, 0.02);
    }

    [Fact]
    public void Buffer_ZeroLengthLineString_BuffersLikeAPoint()
    {
        var degenerate = Geometry<Point>.Create(
            new List<Point> { new(5, 5), new(5, 5) }, GeometryType.LineString, TestSrid);

        var result = degenerate.Buffer(3);

        Assert.Equal(GeometryType.Polygon, result.Type);
        AssertAreaEquals(Math.PI * 9, result.EuclideanArea, 0.01);
    }

    #endregion

    #region Polygon

    [Fact]
    public void Buffer_Polygon_GrowsOutward()
    {
        // regression: the old implementation offset the CCW exterior ring inward (shrank the polygon)
        var polygon = FromWkt("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))");

        var result = polygon.Buffer(2);

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.True(result.EuclideanArea > polygon.EuclideanArea, "Buffered polygon must be larger than the input");

        // area = a² + 4·a·d + π·d² (round corners)
        AssertAreaEquals(100 + 4 * 10 * 2 + Math.PI * 4, result.EuclideanArea, 0.02);

        // all original vertices are strictly inside the buffer
        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(0, 0)));
        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(10, 10)));
    }

    [Fact]
    public void Buffer_PolygonWithHole_ShrinksHole()
    {
        var polygon = FromWkt("POLYGON((0 0, 0 20, 20 20, 20 0, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))");

        var result = polygon.Buffer(1);

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.Equal(2, result.NumberOfGeometries); // hole survives

        // outer: 22² − (4 − π)·1²; hole shrinks to a sharp 8×8 square
        AssertAreaEquals(22 * 22 - (4 - Math.PI) - 64, result.EuclideanArea, 0.02);

        // the hole center must remain uncovered
        Assert.False(TopologyUtility.IsPointInPolygon(result, new Point(10, 10)));
    }

    [Fact]
    public void Buffer_PolygonWithHole_LargeDistanceEliminatesHole()
    {
        var polygon = FromWkt("POLYGON((0 0, 0 20, 20 20, 20 0, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))");

        var result = polygon.Buffer(10);

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.Equal(1, result.NumberOfGeometries); // hole gone

        AssertAreaEquals(40 * 40 - (4 - Math.PI) * 100, result.EuclideanArea, 0.02);
        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(10, 10)));
    }

    [Fact]
    public void Buffer_PolygonWithThinHole_DropsCollapsedHole()
    {
        // hole is 10×2 (half-width 1); a distance of 2 collapses it — the offset ring
        // must not survive as an inverted garbage ring
        var polygon = FromWkt("POLYGON((0 0, 0 30, 30 30, 30 0, 0 0), (10 14, 10 16, 20 16, 20 14, 10 14))");

        var result = polygon.Buffer(2);

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.Equal(1, result.NumberOfGeometries);

        AssertAreaEquals(34 * 34 - (4 - Math.PI) * 4, result.EuclideanArea, 0.02);
    }

    [Fact]
    public void Buffer_PolygonWithDegenerateExterior_DoesNotThrow()
    {
        // regression: bufferedRings[0] used to throw IndexOutOfRangeException when the
        // exterior was degenerate but a hole survived
        var degenerateExterior = Geometry<Point>.Create(
            new List<Point> { new(0, 0), new(10, 0) }, GeometryType.LineString, TestSrid);

        var hole = Geometry<Point>.Create(
            new List<Point> { new(1, 1), new(2, 2), new(3, 1) }, GeometryType.LineString, TestSrid);

        var polygon = Geometry<Point>.Create(
            new List<Geometry<Point>> { degenerateExterior, hole }, GeometryType.Polygon, TestSrid);

        var result = polygon.Buffer(1);

        Assert.True(result.IsNullOrEmpty());
    }

    #endregion

    #region Multi-part union

    [Fact]
    public void Buffer_MultiPointWithOverlappingCircles_ReturnsSinglePolygonWithoutHole()
    {
        // regression: ring-concatenation union turned the second circle into a HOLE of the first
        var multiPoint = FromWkt("MULTIPOINT((0 0), (1 0))");

        var result = multiPoint.Buffer(10);

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.Equal(1, result.NumberOfGeometries); // single ring, no hole

        // union of two r=10 circles with centers 1 apart
        double r = 10, dist = 1;
        double lens = 2 * r * r * Math.Acos(dist / (2 * r)) - (dist / 2.0) * Math.Sqrt(4 * r * r - dist * dist);
        AssertAreaEquals(2 * Math.PI * r * r - lens, result.EuclideanArea, 0.02);

        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(0.5, 0)));
    }

    [Fact]
    public void Buffer_MultiPointWithIdenticalPoints_ReturnsSingleCircle()
    {
        var multiPoint = FromWkt("MULTIPOINT((0 0), (0 0))");

        var result = multiPoint.Buffer(10);

        Assert.Equal(GeometryType.Polygon, result.Type);
        AssertAreaEquals(Math.PI * 100, result.EuclideanArea, 0.01);
    }

    [Fact]
    public void Buffer_MultiPointWithDisjointCircles_ReturnsMultiPolygon()
    {
        var multiPoint = FromWkt("MULTIPOINT((0 0), (100 0))");

        var result = multiPoint.Buffer(5);

        Assert.Equal(GeometryType.MultiPolygon, result.Type);
        Assert.Equal(2, result.NumberOfGeometries);
        AssertAreaEquals(2 * Math.PI * 25, result.EuclideanArea, 0.01);
    }

    #endregion

    #region Geodesic (SRID 4326)

    [Fact]
    public void Buffer_GeodesicLineWithBearingsStraddlingNorth_CoversTheLine()
    {
        // regression: naive averaging of perpendicular bearings across 0/2π flipped the
        // offset to the wrong side
        var line = Geometry<Point>.Create(
            new List<Point> { new(0, 50), new(-1, 50.2), new(-2, 50.1) },
            GeometryType.LineString,
            SridHelper.GeodeticWGS84);

        var result = line.Buffer(10000); // meters

        Assert.Equal(GeometryType.Polygon, result.Type);

        foreach (var vertex in line.Points)
            Assert.True(
                TopologyUtility.IsPointInPolygon(result, vertex),
                $"Buffer does not cover line vertex ({vertex.X}, {vertex.Y})");
    }

    [Fact]
    public void Buffer_GeodesicPolygon_GrowsOutward()
    {
        var polygon = Geometry<Point>.FromWkt(
            "POLYGON((51 35, 51 35.1, 51.1 35.1, 51.1 35, 51 35))", SridHelper.GeodeticWGS84);

        var result = polygon.Buffer(1000); // meters

        Assert.Equal(GeometryType.Polygon, result.Type);
        Assert.True(result.EuclideanArea > polygon.EuclideanArea, "Geodesic buffer must grow the polygon");

        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(51, 35)));
        Assert.True(TopologyUtility.IsPointInPolygon(result, new Point(51.1, 35.1)));
    }

    #endregion
}
