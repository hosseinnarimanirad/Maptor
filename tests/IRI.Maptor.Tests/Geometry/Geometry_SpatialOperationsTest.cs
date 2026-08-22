using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using Microsoft.SqlServer.Types;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Tests.TheGeometry;

/// <summary>
/// Tests for spatial operations: Union, Buffer, Intersection, and Difference
/// </summary>
public class Geometry_SpatialOperationsTest
{
    private const int TestSrid = SridHelper.WebMercator;

    /// <summary>
    /// Normalizes WKT strings for comparison by removing extra spaces
    /// </summary>
    private static string NormalizeWkt(string wkt)
    {
        if (string.IsNullOrEmpty(wkt))
            return wkt;
        
        // Remove spaces after geometry type names (e.g., "POINT (" -> "POINT(")
        // and normalize multiple spaces to single space
        return System.Text.RegularExpressions.Regex.Replace(
            wkt.Trim(),
            @"\s+",
            " ")
            .Replace("POINT (", "POINT(")
            .Replace("LINESTRING (", "LINESTRING(")
            .Replace("POLYGON (", "POLYGON(")
            .Replace("MULTIPOINT (", "MULTIPOINT(")
            .Replace("MULTILINESTRING (", "MULTILINESTRING(")
            .Replace("MULTIPOLYGON (", "MULTIPOLYGON(")
            .Replace("GEOMETRYCOLLECTION (", "GEOMETRYCOLLECTION(");
    }

    #region Union Tests

    [Theory]
    [InlineData("POINT(10 20)", "POINT(10 20)", GeometryType.Point, 1)] // Same point - returns point
    [InlineData("POINT(10 20)", "POINT(30 40)", GeometryType.MultiPoint, 2)] // Different points - returns MultiPoint
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))", GeometryType.Polygon, -1)] // Overlapping polygons - returns merged polygon
    public void Union_VariousGeometries_ReturnsExpectedResult(string wkt1, string wkt2, GeometryType expectedType, int expectedCount)
    {
        // Arrange
        var geo1 = Geometry<Point>.FromWkt(wkt1, TestSrid);
        var geo2 = Geometry<Point>.FromWkt(wkt2, TestSrid);

        // Act
        var result = geo1.Union(geo2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedType, result.Type);
        if (expectedCount >= 0)
        {
            if (result.Type == GeometryType.MultiPoint || result.Type == GeometryType.MultiLineString || result.Type == GeometryType.MultiPolygon)
            {
                Assert.Equal(expectedCount, result.NumberOfGeometries);
            }
            else
            {
                Assert.Equal(expectedCount, result.NumberOfPoints);
            }
        }
        Assert.True(result.HasAnyPoint());
    }

    [Fact]
    public void Union_EmptyGeometry_ReturnsOther()
    {
        // Arrange
        var empty = Geometry<Point>.Empty;
        var point = Geometry<Point>.Create(10, 20, TestSrid);

        // Act
        var result = empty.Union(point);

        // Assert
        Assert.Equal(point, result);
    }

    [Fact]
    public void Union_DifferentSrids_ThrowsException()
    {
        // Arrange
        var point1 = Geometry<Point>.Create(10, 20, TestSrid);
        var point2 = Geometry<Point>.Create(30, 40, SridHelper.GeodeticWGS84);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => point1.Union(point2));
    }

    #endregion

    #region Buffer Tests
     

    [Theory]
    [InlineData("POINT(10 20)", -5.0)]
    [InlineData("LINESTRING(0 0, 10 10)", -2.0)]
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", -1.0)]
    public void Buffer_NegativeDistance_ThrowsException(string wkt, double distance)
    {
        // Arrange
        var geo = Geometry<Point>.FromWkt(wkt, TestSrid);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => geo.Buffer(distance));
    }

    [Fact]
    public void Buffer_EmptyGeometry_ReturnsEmpty()
    {
        // Arrange
        var empty = Geometry<Point>.Empty;
        double distance = 5.0;

        // Act
        var result = empty.Buffer(distance);

        // Assert
        Assert.True(result.IsNullOrEmpty());
    }

    #endregion

    #region Intersection Tests

    [Theory]
    [InlineData("POINT(10 20)", "POINT(10 20)", GeometryType.Point, 1, false)] // Same point - returns point
    [InlineData("POINT(10 20)", "POINT(30 40)", GeometryType.Point, -1, true)] // Different points - returns empty
    [InlineData("POINT(5 5)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", GeometryType.Point, 1, false)] // Point in polygon - returns point
    [InlineData("POINT(50 50)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", GeometryType.Point, -1, true)] // Point outside polygon - returns empty
    [InlineData("MULTIPOINT((0 0), (10 10), (20 20))", "MULTIPOINT((10 10), (20 20), (30 30))", GeometryType.MultiPoint, -1, false)] // MultiPoint with common points
    public void Intersection_VariousGeometries_ReturnsExpectedResult(string wkt1, string wkt2, GeometryType expectedType, int expectedCount, bool shouldBeEmpty)
    {
        // Arrange
        var geo1 = Geometry<Point>.FromWkt(wkt1, TestSrid);
        var geo2 = Geometry<Point>.FromWkt(wkt2, TestSrid);

        // Act
        var result = geo1.Intersection(geo2);

        // Assert
        if (shouldBeEmpty)
        {
            Assert.True(result.IsNullOrEmpty());
        }
        else
        {
            Assert.False(result.IsNullOrEmpty());
            Assert.Equal(expectedType, result.Type);
            if (expectedCount >= 0)
            {
                Assert.Equal(expectedCount, result.NumberOfPoints);
            }
            Assert.True(result.HasAnyPoint());
        }
    }

    [Fact]
    public void Intersection_EmptyGeometry_ReturnsEmpty()
    {
        // Arrange
        var empty = Geometry<Point>.Empty;
        var point = Geometry<Point>.Create(10, 20, TestSrid);

        // Act
        var result = empty.Intersection(point);

        // Assert
        Assert.True(result.IsNullOrEmpty());
    }

    [Fact]
    public void Intersection_DifferentSrids_ThrowsException()
    {
        // Arrange
        var point1 = Geometry<Point>.Create(10, 20, TestSrid);
        var point2 = Geometry<Point>.Create(10, 20, SridHelper.GeodeticWGS84);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => point1.Intersection(point2));
    }

    #endregion

    #region Difference Tests

    [Theory]
    [InlineData("POINT(10 20)", "POINT(10 20)", GeometryType.Point, -1, true)] // Same point - returns empty
    [InlineData("POINT(10 20)", "POINT(30 40)", GeometryType.Point, 1, false)] // Different points - returns first point
    [InlineData("POINT(5 5)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", GeometryType.Point, -1, true)] // Point in polygon - returns empty
    [InlineData("POINT(50 50)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", GeometryType.Point, 1, false)] // Point outside polygon - returns point
    [InlineData("MULTIPOINT((0 0), (10 10), (20 20))", "MULTIPOINT((10 10), (30 30))", GeometryType.MultiPoint, -1, false)] // MultiPoint - removes common points
    public void Difference_VariousGeometries_ReturnsExpectedResult(string wkt1, string wkt2, GeometryType expectedType, int expectedCount, bool shouldBeEmpty)
    {
        // Arrange
        var geo1 = Geometry<Point>.FromWkt(wkt1, TestSrid);
        var geo2 = Geometry<Point>.FromWkt(wkt2, TestSrid);

        // Act
        var result = geo1.Difference(geo2);

        // Assert
        if (shouldBeEmpty)
        {
            Assert.True(result.IsNullOrEmpty());
        }
        else
        {
            Assert.False(result.IsNullOrEmpty());
            Assert.Equal(expectedType, result.Type);
            if (expectedCount >= 0)
            {
                Assert.Equal(expectedCount, result.NumberOfPoints);
            }
            Assert.True(result.HasAnyPoint());
            
            // For point difference, verify coordinates match
            if (geo1.Type == GeometryType.Point && geo2.Type == GeometryType.Point && !shouldBeEmpty)
            {
                Assert.Equal(geo1.Points[0].X, result.Points[0].X);
                Assert.Equal(geo1.Points[0].Y, result.Points[0].Y);
            }
        }
    }

    [Fact]
    public void Difference_EmptyGeometry_ReturnsEmpty()
    {
        // Arrange
        var empty = Geometry<Point>.Empty;
        var point = Geometry<Point>.Create(10, 20, TestSrid);

        // Act
        var result = empty.Difference(point);

        // Assert
        Assert.True(result.IsNullOrEmpty());
    }

    [Fact]
    public void Difference_GeometryMinusEmpty_ReturnsOriginal()
    {
        // Arrange
        var point = Geometry<Point>.Create(10, 20, TestSrid);
        var empty = Geometry<Point>.Empty;

        // Act
        var result = point.Difference(empty);

        // Assert
        Assert.Equal(point, result);
    }

    [Fact]
    public void Difference_DifferentSrids_ThrowsException()
    {
        // Arrange
        var point1 = Geometry<Point>.Create(10, 20, TestSrid);
        var point2 = Geometry<Point>.Create(10, 20, SridHelper.GeodeticWGS84);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => point1.Difference(point2));
    }

    #endregion

    #region Intersection Comparison Tests

    [Theory]
    [InlineData("POINT(10 20)", "POINT(10 20)")] // Same point - should return point
    [InlineData("POINT(10 20)", "POINT(30 40)")] // Different points - should return empty
    [InlineData("POINT(5 5)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // Point in polygon - should return point
    [InlineData("POINT(50 50)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // Point outside polygon - should return empty
    [InlineData("MULTIPOINT((0 0), (10 10), (20 20))", "MULTIPOINT((10 10), (20 20), (30 30))")] // MultiPoint with common points
    [InlineData("MULTIPOINT((0 0), (10 10))", "MULTIPOINT((20 20), (30 30))")] // MultiPoint with no common points
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))")] // Overlapping polygons
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "POLYGON((20 20, 20 30, 30 30, 30 20, 20 20))")] // Non-overlapping polygons
    [InlineData("LINESTRING(5 5, 15 15)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // LineString intersecting polygon
    [InlineData("LINESTRING(20 20, 30 30)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // LineString not intersecting polygon
    [InlineData("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((20 20, 20 30, 30 30, 30 20, 20 20)))", "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))")] // MultiPolygon with overlapping polygon
    [InlineData("POINT(5 5)", "LINESTRING(0 0, 10 10)")] // Point on LineString
    [InlineData("POINT(50 50)", "LINESTRING(0 0, 10 10)")] // Point not on LineString
    [InlineData("POINT(5 5)", "MULTILINESTRING((0 0, 10 10), (20 20, 30 30))")] // Point on MultiLineString
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "MULTIPOLYGON(((5 5, 5 15, 15 15, 15 5, 5 5)), ((25 25, 25 35, 35 35, 35 25, 25 25)))")] // Polygon vs MultiPolygon
    public void Intersection_CompareWithSqlGeometry_VariousGeometries(string wkt1, string wkt2)
    {
        // Arrange
        var geo1 = Geometry<Point>.FromWkt(wkt1, TestSrid);
        var geo2 = Geometry<Point>.FromWkt(wkt2, TestSrid);

        // Act - Geometry<Point> intersection
        var geometryResult = geo1.Intersection(geo2);

        // Act - SqlGeometry intersection
        var sqlGeo1 = geo1.AsSqlGeometry();
        var sqlGeo2 = geo2.AsSqlGeometry();
        var sqlResult = sqlGeo1.STIntersection(sqlGeo2);

        // Assert - intersection detection must agree with SqlGeometry
        bool geometryDetectsIntersection = geo1.Intersects(geo2);
        bool sqlDetectsIntersection = sqlGeo1.STIntersects(sqlGeo2).IsTrue;
        Assert.Equal(sqlDetectsIntersection, geometryDetectsIntersection);

        // Assert - both must agree on whether the intersection is empty
        bool geometryHasResult = !geometryResult.IsNullOrEmpty();
        bool sqlHasResult = !sqlResult.IsNullOrEmpty() && !sqlResult.STIsEmpty().IsTrue;
        Assert.Equal(sqlHasResult, geometryHasResult);

        // Assert - the results must be spatially equal (point order / ring orientation independent);
        // SqlGeometry results carry ~1e-13 floating point fuzz, so fall back to a tolerance-based
        // mutual containment check when the exact STEquals fails
        if (geometryHasResult && sqlHasResult)
        {
            var geometryResultAsSql = geometryResult.AsSqlGeometry();

            const double tolerance = 1E-6;

            bool spatiallyEqual = sqlResult.STEquals(geometryResultAsSql).IsTrue ||
                                    (sqlResult.STBuffer(tolerance).STContains(geometryResultAsSql).IsTrue &&
                                     geometryResultAsSql.STBuffer(tolerance).STContains(sqlResult).IsTrue);

            Assert.True(
                spatiallyEqual,
                $"Expected intersection {sqlResult.AsWkt()} but got {geometryResult.AsWkt()}");
        }
    }

    #endregion

    #region Union Comparison Tests

    [Theory]
    [InlineData("POINT(10 20)", "POINT(10 20)")] // Same point
    [InlineData("POINT(10 20)", "POINT(30 40)")] // Different points
    [InlineData("POINT(5 5)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // Point in polygon
    [InlineData("POINT(50 50)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // Point outside polygon
    [InlineData("MULTIPOINT((10 20), (30 40))", "POINT(10 20)")] // MultiPoint with duplicate point
    [InlineData("MULTIPOINT((10 20), (30 40))", "POINT(50 60)")] // MultiPoint with new point
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))")] // Overlapping polygons
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "POLYGON((20 20, 20 30, 30 30, 30 20, 20 20))")] // Non-overlapping polygons
    [InlineData("LINESTRING(0 0, 10 10)", "LINESTRING(5 5, 15 15)")] // Overlapping linestrings
    [InlineData("LINESTRING(0 0, 10 10)", "LINESTRING(20 20, 30 30)")] // Non-overlapping linestrings
    [InlineData("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((20 20, 20 30, 30 30, 30 20, 20 20)))", "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))")] // MultiPolygon with overlapping polygon
    public void Union_CompareWithSqlGeometry_VariousGeometries(string wkt1, string wkt2)
    {
        // Arrange
        var geo1 = Geometry<Point>.FromWkt(wkt1, TestSrid);
        var geo2 = Geometry<Point>.FromWkt(wkt2, TestSrid);

        // Act - Geometry<Point> union
        var geometryResult = geo1.Union(geo2);

        // Act - SqlGeometry union
        var sqlGeo1 = geo1.AsSqlGeometry();
        var sqlGeo2 = geo2.AsSqlGeometry();
        var sqlResult = sqlGeo1.STUnion(sqlGeo2);

        // Assert - Both should return results (or both empty)
        bool geometryHasResult = !geometryResult.IsNullOrEmpty();
        bool sqlHasResult = !sqlResult.IsNullOrEmpty() && !sqlResult.STIsEmpty().IsTrue;

        Assert.Equal(sqlHasResult, geometryHasResult);

        if (geometryHasResult && sqlHasResult)
        {
            Microsoft.SqlServer.Types.SqlGeometry geometryResultAsSql;

            try
            {
                geometryResultAsSql = geometryResult.AsSqlGeometry();
            }
            catch (NotImplementedException)
            {
                // GeometryCollection conversion may be unsupported; compare member counts instead
                var sqlResultGeometry = sqlResult.AsGeometry();
                Assert.Equal(sqlResultGeometry.Type, geometryResult.Type);
                Assert.Equal(sqlResultGeometry.NumberOfGeometries, geometryResult.NumberOfGeometries);
                return;
            }

            // spatial equality; the mutual buffer-containment fallback tolerates the documented
            // representation difference for partially overlapping collinear linework
            const double tolerance = 1E-6;

            bool spatiallyEqual = sqlResult.STEquals(geometryResultAsSql).IsTrue ||
                                    (sqlResult.STBuffer(tolerance).STContains(geometryResultAsSql).IsTrue &&
                                     geometryResultAsSql.STBuffer(tolerance).STContains(sqlResult).IsTrue);

            if (!spatiallyEqual)
            {
                // AsWkt does not support GeometryCollection, so build the message defensively
                string geometryWkt;

                try { geometryWkt = geometryResult.AsWkt(); }
                catch (NotImplementedException) { geometryWkt = geometryResultAsSql.ToString(); }

                Assert.True(spatiallyEqual, $"Expected union {sqlResult.AsWkt()} but got {geometryWkt}");
            }
        }
    }

    #endregion

    #region Difference Comparison Tests

    [Theory]
    [InlineData("POINT(10 20)", "POINT(10 20)")] // Same point - should return empty
    [InlineData("POINT(10 20)", "POINT(30 40)")] // Different points - should return first point
    [InlineData("POINT(5 5)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // Point in polygon - should return empty
    [InlineData("POINT(50 50)", "POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))")] // Point outside polygon - should return point
    [InlineData("MULTIPOINT((10 20), (30 40))", "POINT(10 20)")] // MultiPoint minus point - should remove point
    [InlineData("MULTIPOINT((10 20), (30 40))", "POINT(50 60)")] // MultiPoint minus non-existent point - should return original
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))")] // Overlapping polygons
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", "POLYGON((20 20, 20 30, 30 30, 30 20, 20 20))")] // Non-overlapping polygons - should return original
    [InlineData("LINESTRING(0 0, 10 10)", "LINESTRING(5 5, 15 15)")] // Overlapping linestrings
    [InlineData("LINESTRING(0 0, 10 10)", "LINESTRING(20 20, 30 30)")] // Non-overlapping linestrings - should return original
    [InlineData("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((20 20, 20 30, 30 30, 30 20, 20 20)))", "POLYGON((5 5, 5 15, 15 15, 15 5, 5 5))")] // MultiPolygon minus overlapping polygon
    public void Difference_CompareWithSqlGeometry_VariousGeometries(string wkt1, string wkt2)
    {
        // Arrange
        var geo1 = Geometry<Point>.FromWkt(wkt1, TestSrid);
        var geo2 = Geometry<Point>.FromWkt(wkt2, TestSrid);

        // Act - Geometry<Point> difference
        var geometryResult = geo1.Difference(geo2);

        // Act - SqlGeometry difference
        var sqlGeo1 = geo1.AsSqlGeometry();
        var sqlGeo2 = geo2.AsSqlGeometry();
        var sqlResult = sqlGeo1.STDifference(sqlGeo2);

        // Assert - Both should return results (or both empty)
        bool geometryHasResult = !geometryResult.IsNullOrEmpty();
        bool sqlHasResult = !sqlResult.IsNullOrEmpty() && !sqlResult.STIsEmpty().IsTrue;

        Assert.Equal(sqlHasResult, geometryHasResult);

        if (geometryHasResult && sqlHasResult)
        {
            var geometryResultAsSql = geometryResult.AsSqlGeometry();

            // spatial equality (point order / ring orientation independent, tolerant of fuzz)
            const double tolerance = 1E-6;

            bool spatiallyEqual = sqlResult.STEquals(geometryResultAsSql).IsTrue ||
                                    (sqlResult.STBuffer(tolerance).STContains(geometryResultAsSql).IsTrue &&
                                     geometryResultAsSql.STBuffer(tolerance).STContains(sqlResult).IsTrue);

            if (!spatiallyEqual)
            {
                string geometryWkt;

                try { geometryWkt = geometryResult.AsWkt(); }
                catch (NotImplementedException) { geometryWkt = geometryResultAsSql.ToString(); }

                Assert.True(spatiallyEqual, $"Expected difference {sqlResult.AsWkt()} but got {geometryWkt}");
            }
        }
    }

    #endregion

    #region Buffer Comparison Tests

    [Theory]
    [InlineData("POINT(10 20)", 5.0)] // Point buffer
    [InlineData("POINT(0 0)", 10.0)] // Point at origin
    [InlineData("LINESTRING(0 0, 10 10)", 2.0)] // Simple linestring
    [InlineData("LINESTRING(0 0, 10 0, 10 10, 0 10, 0 0)", 1.0)] // Closed linestring (rectangle)
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", 2.0)] // Simple polygon
    [InlineData("POLYGON((0 0, 0 20, 20 20, 20 0, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))", 1.0)] // Polygon with hole - small buffer
    [InlineData("POLYGON((0 0, 0 20, 20 20, 20 0, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))", 10.0)] // Polygon with hole - large buffer (should eliminate hole)
    [InlineData("MULTIPOINT((10 20), (30 40))", 5.0)] // MultiPoint buffer
    [InlineData("MULTILINESTRING((0 0, 10 10), (20 20, 30 30))", 3.0)] // MultiLineString buffer
    [InlineData("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((20 20, 20 30, 30 30, 30 20, 20 20)))", 2.0)] // MultiPolygon buffer - non-overlapping
    [InlineData("MULTIPOLYGON(((0 0, 0 10, 10 10, 10 0, 0 0)), ((5 5, 5 15, 15 15, 15 5, 5 5)))", 3.0)] // MultiPolygon buffer - overlapping (should merge)
    public void Buffer_CompareWithSqlGeometry_VariousGeometries(string wkt, double distance)
    {
        // Arrange
        var geo = Geometry<Point>.FromWkt(wkt, TestSrid);

        // Act - Geometry<Point> buffer
        var geometryResult = geo.Buffer(distance);

        // Act - SqlGeometry buffer
        var sqlGeo = geo.AsSqlGeometry();
        var sqlResult = sqlGeo.STBuffer(distance);

        // Assert - Both should return results (or both empty)
        bool geometryHasResult = !geometryResult.IsNullOrEmpty();
        bool sqlHasResult = !sqlResult.IsNullOrEmpty() && !sqlResult.STIsEmpty().IsTrue;

        Assert.Equal(sqlHasResult, geometryHasResult);

        if (geometryHasResult && sqlHasResult)
        {
            Assert.True(geometryResult.IsPolygonOrMultiPolygon());
            Assert.True(sqlResult.AsGeometry().IsPolygonOrMultiPolygon());

            // area parity with SQL Server STBuffer (both approximate circular arcs)
            double sqlArea = sqlResult.STArea().Value;
            double geometryArea = geometryResult.EuclideanArea;

            Assert.True(
                Math.Abs(geometryArea - sqlArea) / sqlArea < 0.02,
                $"Buffer area {geometryArea} differs from SqlGeometry buffer area {sqlArea} by more than 2% for {wkt} (d={distance})");

            // the buffer must contain the original geometry
            Assert.True(
                geometryResult.AsSqlGeometry().STBuffer(1E-6).STContains(sqlGeo).IsTrue,
                $"Buffer does not contain the original geometry for {wkt} (d={distance})");
        }
    }

    #endregion
}

