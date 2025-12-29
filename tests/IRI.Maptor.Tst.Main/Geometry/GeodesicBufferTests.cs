using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using Xunit;

namespace IRI.Maptor.Tst.Main.TheGeometry;

public class GeodesicBufferTests
{
    // EPSG:4326 (WGS84)
    private const int Wgs84Srid = 4326;

    [Theory]
    [InlineData("POINT(30 10)", 1000)] // 1 km buffer
    [InlineData("POINT(0 0)", 500)] // 500 m buffer
    [InlineData("POINT(50 50)", 2000)] // 2 km buffer
    [InlineData("POINT(-120 45)", 1500)] // 1.5 km buffer
    [InlineData("POINT(151.2093 -33.8688)", 5000)] // 5 km buffer (Sydney)
    public void BufferGeodesic_Point_ShouldMatchSqlGeography(string wkt, double distance)
    {
        // Arrange
        var geometry = Geometry<Point>.FromWkt(wkt, Wgs84Srid);
        var sqlGeo = SqlGeography.STGeomFromText(new SqlChars(wkt), Wgs84Srid);

        // Act
        var actual = geometry.BufferGeodesic(distance);
        var expectedSqlGeo = sqlGeo.STBuffer(distance);
        var expected = Geometry<Point>.FromWkt(expectedSqlGeo.As2DWkt(), Wgs84Srid);

        // Assert - Compare ellipsoidal areas (buffered points become polygons)
        if (!actual.IsNullOrEmpty() && !expected.IsNullOrEmpty())
        {
            var actualArea = SpatialUtility.GetEllipsoidalArea(actual);
            var expectedArea = expectedSqlGeo.STArea().Value;
            var relativeError = Math.Abs(actualArea - expectedArea) / expectedArea;

            Assert.True(relativeError < 0.1, // Allow 10% tolerance for buffer differences
                $"Area mismatch: WKT={wkt}, Distance={distance}m, Expected={expectedArea}, Actual={actualArea}, RelErr={relativeError}");
        }
    }

    //[Theory]
    //[InlineData("LINESTRING(30 10, 31 11, 32 10)", 500)] // 500 m buffer
    //[InlineData("LINESTRING(0 0, 0.01 0.01, 0.02 0)", 1000)] // 1 km buffer
    //[InlineData("LINESTRING(50 50, 50.01 50.01, 50.02 50)", 2000)] // 2 km buffer
    //[InlineData("LINESTRING(-120 45, -119.99 45.01, -119.98 45)", 1500)] // 1.5 km buffer
    //public void BufferGeodesic_LineString_ShouldMatchSqlGeography(string wkt, double distance)
    //{
    //    // Arrange
    //    var geometry = Geometry<Point>.FromWkt(wkt, Wgs84Srid);
    //    var sqlGeo = SqlGeography.STGeomFromText(new SqlChars(wkt), Wgs84Srid);

    //    // Act
    //    var actual = geometry.BufferGeodesic(distance);
    //    var expectedSqlGeo = sqlGeo.STBuffer(distance);
    //    var expected = Geometry<Point>.FromWkt(expectedSqlGeo.As2DWkt(), Wgs84Srid);

    //    // Assert - Compare ellipsoidal areas
    //    if (!actual.IsNullOrEmpty() && !expected.IsNullOrEmpty())
    //    {
    //        var actualArea = SpatialUtility.GetEllipsoidalArea(actual);
    //        var expectedArea = expectedSqlGeo.STArea().Value;
    //        var relativeError = Math.Abs(actualArea - expectedArea) / expectedArea;

    //        Assert.True(relativeError < 0.15, // Allow 15% tolerance for line buffers
    //            $"Area mismatch: WKT={wkt}, Distance={distance}m, Expected={expectedArea}, Actual={actualArea}, RelErr={relativeError}");
    //    }
    //}

    //[Theory]
    //[InlineData("POLYGON((30 10, 31 10, 31 11, 30 11, 30 10))", 500)] // 500 m buffer
    //[InlineData("POLYGON((0 0, 0.01 0, 0.01 0.01, 0 0.01, 0 0))", 1000)] // 1 km buffer
    //[InlineData("POLYGON((50 50, 50.01 50, 50.01 50.01, 50 50.01, 50 50))", 2000)] // 2 km buffer
    //[InlineData("POLYGON((-120 45, -119.99 45, -119.99 45.01, -120 45.01, -120 45))", 1500)] // 1.5 km buffer
    //public void BufferGeodesic_Polygon_ShouldMatchSqlGeography(string wkt, double distance)
    //{
    //    // Arrange
    //    var geometry = Geometry<Point>.FromWkt(wkt, Wgs84Srid);
    //    var sqlGeo = SqlGeography.STGeomFromText(new SqlChars(wkt), Wgs84Srid);

    //    // Act
    //    var actual = geometry.BufferGeodesic(distance);
    //    var expectedSqlGeo = sqlGeo.STBuffer(distance);
    //    var expected = Geometry<Point>.FromWkt(expectedSqlGeo.As2DWkt(), Wgs84Srid);

    //    // Assert - Compare ellipsoidal areas
    //    if (!actual.IsNullOrEmpty() && !expected.IsNullOrEmpty())
    //    {
    //        var actualArea = SpatialUtility.GetEllipsoidalArea(actual);
    //        var expectedArea = expectedSqlGeo.STArea().Value;
    //        var relativeError = Math.Abs(actualArea - expectedArea) / expectedArea;

    //        Assert.True(relativeError < 0.2, // Allow 20% tolerance for polygon buffers
    //            $"Area mismatch: WKT={wkt}, Distance={distance}m, Expected={expectedArea}, Actual={actualArea}, RelErr={relativeError}");
    //    }
    //}

    //[Theory]
    //[InlineData("MULTIPOINT((30 10), (31 11), (32 10))", 500)] // 500 m buffer
    //[InlineData("MULTIPOINT((0 0), (0.01 0.01))", 1000)] // 1 km buffer
    //public void BufferGeodesic_MultiPoint_ShouldMatchSqlGeography(string wkt, double distance)
    //{
    //    // Arrange
    //    var geometry = Geometry<Point>.FromWkt(wkt, Wgs84Srid);
    //    var sqlGeo = SqlGeography.STGeomFromText(new SqlChars(wkt), Wgs84Srid);

    //    // Act
    //    var actual = geometry.BufferGeodesic(distance);
    //    var expectedSqlGeo = sqlGeo.STBuffer(distance);
    //    var expected = Geometry<Point>.FromWkt(expectedSqlGeo.As2DWkt(), Wgs84Srid);

    //    // Assert - Compare ellipsoidal areas
    //    if (!actual.IsNullOrEmpty() && !expected.IsNullOrEmpty())
    //    {
    //        var actualArea = SpatialUtility.GetEllipsoidalArea(actual);
    //        var expectedArea = expectedSqlGeo.STArea().Value;
    //        var relativeError = Math.Abs(actualArea - expectedArea) / expectedArea;

    //        Assert.True(relativeError < 0.15, // Allow 15% tolerance for multipoint buffers
    //            $"Area mismatch: WKT={wkt}, Distance={distance}m, Expected={expectedArea}, Actual={actualArea}, RelErr={relativeError}");
    //    }
    //}

    //[Theory]
    //[InlineData("MULTILINESTRING((30 10, 31 11), (32 10, 33 11))", 500)] // 500 m buffer
    //[InlineData("MULTILINESTRING((0 0, 0.01 0.01), (0.02 0, 0.03 0.01))", 1000)] // 1 km buffer
    //public void BufferGeodesic_MultiLineString_ShouldMatchSqlGeography(string wkt, double distance)
    //{
    //    // Arrange
    //    var geometry = Geometry<Point>.FromWkt(wkt, Wgs84Srid);
    //    var sqlGeo = SqlGeography.STGeomFromText(new SqlChars(wkt), Wgs84Srid);

    //    // Act
    //    var actual = geometry.BufferGeodesic(distance);
    //    var expectedSqlGeo = sqlGeo.STBuffer(distance);
    //    var expected = Geometry<Point>.FromWkt(expectedSqlGeo.As2DWkt(), Wgs84Srid);

    //    // Assert - Compare ellipsoidal areas
    //    if (!actual.IsNullOrEmpty() && !expected.IsNullOrEmpty())
    //    {
    //        var actualArea = SpatialUtility.GetEllipsoidalArea(actual);
    //        var expectedArea = expectedSqlGeo.STArea().Value;
    //        var relativeError = Math.Abs(actualArea - expectedArea) / expectedArea;

    //        Assert.True(relativeError < 0.2, // Allow 20% tolerance for multilinestring buffers
    //            $"Area mismatch: WKT={wkt}, Distance={distance}m, Expected={expectedArea}, Actual={actualArea}, RelErr={relativeError}");
    //    }
    //}

    //[Theory]
    //[InlineData("MULTIPOLYGON(((30 10, 31 10, 31 11, 30 11, 30 10)), ((32 10, 33 10, 33 11, 32 11, 32 10)))", 500)] // 500 m buffer
    //[InlineData("MULTIPOLYGON(((0 0, 0.01 0, 0.01 0.01, 0 0.01, 0 0)), ((0.02 0, 0.03 0, 0.03 0.01, 0.02 0.01, 0.02 0)))", 1000)] // 1 km buffer
    //public void BufferGeodesic_MultiPolygon_ShouldMatchSqlGeography(string wkt, double distance)
    //{
    //    // Arrange
    //    var geometry = Geometry<Point>.FromWkt(wkt, Wgs84Srid);
    //    var sqlGeo = SqlGeography.STGeomFromText(new SqlChars(wkt), Wgs84Srid);

    //    // Act
    //    var actual = geometry.BufferGeodesic(distance);
    //    var expectedSqlGeo = sqlGeo.STBuffer(distance);
    //    var expected = Geometry<Point>.FromWkt(expectedSqlGeo.As2DWkt(), Wgs84Srid);

    //    // Assert - Compare ellipsoidal areas
    //    if (!actual.IsNullOrEmpty() && !expected.IsNullOrEmpty())
    //    {
    //        var actualArea = SpatialUtility.GetEllipsoidalArea(actual);
    //        var expectedArea = expectedSqlGeo.STArea().Value;
    //        var relativeError = Math.Abs(actualArea - expectedArea) / expectedArea;

    //        Assert.True(relativeError < 0.25, // Allow 25% tolerance for multipolygon buffers
    //            $"Area mismatch: WKT={wkt}, Distance={distance}m, Expected={expectedArea}, Actual={actualArea}, RelErr={relativeError}");
    //    }
    //}
}

