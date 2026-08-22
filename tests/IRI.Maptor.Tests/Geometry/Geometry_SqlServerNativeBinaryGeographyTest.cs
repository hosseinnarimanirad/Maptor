using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

using Microsoft.SqlServer.Types;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.IO.SqlServerNativeBinary;

namespace IRI.Maptor.Tests.TheGeometry;

/// <summary>
/// Tests for SQL Server <c>geography</c> support in the MS-SSCLRT (native binary) serializer/deserializer.
/// Geography stores each point as (Latitude, Longitude) — the opposite order of geometry — so the essential
/// property under test is that coordinates round-trip with X = Longitude and Y = Latitude.
/// Fixtures are generated with Microsoft.SqlServer.Types (fully managed in 160.x, no native assemblies needed).
/// </summary>
public class Geometry_SqlServerNativeBinaryGeographyTest
{
    const int _srid = 4326;

    // Longitude/latitude WKT (SQL Server geography WKT is longitude-latitude order),
    // exterior rings counter-clockwise, holes clockwise, all within a hemisphere.
    [Theory]
    [InlineData("POINT (51.4 35.7)")]
    [InlineData("LINESTRING (0 0, 10 10)")]                       // L-flag optimization (2 points)
    [InlineData("LINESTRING (0 0, 10 10, 20 0, 30 10)")]
    [InlineData("MULTIPOINT ((0 0), (10 10), (20 20))")]
    [InlineData("MULTILINESTRING ((0 0, 10 10), (20 20, 30 5))")]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))")]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0), (10 10, 10 20, 20 20, 20 10, 10 10))")] // CW hole
    [InlineData("MULTIPOLYGON (((0 0, 30 0, 30 30, 0 30, 0 0)), ((40 40, 50 40, 50 50, 40 50, 40 40)))")]
    // GeometryCollection is covered by TestGeographySerializeRoundTrip (SqlServerWktWriter does not support it).
    public void TestGeographyDeserialize(string wktGeography)
    {
        // ARRANGE — build a geography instance and grab its native binary
        var sqlGeography = SqlGeography.STGeomFromText(new SqlChars(new SqlString(wktGeography)), _srid);
        var nativeBinary = sqlGeography.Serialize().Buffer;

        // ACT
        var geometry = SqlServerSpatialNativeBinary.Deserialize(nativeBinary, isGeography: true);

        // ASSERT — the deserialized geometry must render to the same WKT as the source geography
        Assert.Equal(new string(sqlGeography.AsTextZM().Value), geometry.AsSqlServerWkt());
    }

    [Fact]
    public void TestGeographyCoordinateOrder_XisLongitude_YisLatitude()
    {
        // ARRANGE — SqlGeography.Point takes (latitude, longitude); asymmetric values catch a swap bug.
        var sqlGeography = SqlGeography.Point(35.7, 51.4, _srid);
        var nativeBinary = sqlGeography.Serialize().Buffer;

        // ACT
        var geometry = SqlServerSpatialNativeBinary.DeserializeGeometryPoint(nativeBinary, isGeography: true);

        // ASSERT — X carries longitude, Y carries latitude
        Assert.NotNull(geometry);
        Assert.Equal(51.4, geometry!.Points[0].X, 6);
        Assert.Equal(35.7, geometry.Points[0].Y, 6);
    }

    [Theory]
    [InlineData("POINT (51.4 35.7)")]
    [InlineData("LINESTRING (0 0, 10 10)")]
    [InlineData("LINESTRING (0 0, 10 10, 20 0, 30 10)")]
    [InlineData("MULTIPOINT ((0 0), (10 10), (20 20))")]
    [InlineData("MULTILINESTRING ((0 0, 10 10), (20 20, 30 5))")]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))")]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0), (10 10, 10 20, 20 20, 20 10, 10 10))")]
    [InlineData("MULTIPOLYGON (((0 0, 30 0, 30 30, 0 30, 0 0)), ((40 40, 50 40, 50 50, 40 50, 40 40)))")]
    [InlineData("GEOMETRYCOLLECTION (POINT (4 0), LINESTRING (4 2, 5 3), POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0)))")]
    public void TestGeographySerializeRoundTrip(string wktGeography)
    {
        // ARRANGE — deserialize the source geography into our model...
        var sqlGeography = SqlGeography.STGeomFromText(new SqlChars(new SqlString(wktGeography)), _srid);
        var geometry = (Geometry<Point>)SqlServerSpatialNativeBinary.Deserialize(sqlGeography.Serialize().Buffer, isGeography: true);

        // ACT — ...then serialize it back out and let SQL Server parse our bytes
        var roundTripBinary = SqlServerSpatialNativeBinary.Serialize(geometry, isGeography: true);
        var roundTrip = SqlGeography.Deserialize(new SqlBytes(roundTripBinary));

        // ASSERT — SQL Server accepts our output and it is spatially equal to the original
        Assert.True(roundTrip.STEquals(sqlGeography).Value);
    }

    [Fact]
    public void TestGeographyRingOrientation_IsCorrectedOnWrite()
    {
        // ARRANGE — a polygon whose exterior ring is CLOCKWISE (invalid for geography as-is).
        // Points wind clockwise: (0,0) -> (0,30) -> (30,30) -> (30,0).
        var exterior = Geometry<Point>.CreatePolygonRing(
            new List<Point> { new Point(0, 0), new Point(0, 30), new Point(30, 30), new Point(30, 0) }, _srid);
        var polygon = Geometry<Point>.Create(new List<Geometry<Point>> { exterior }, GeometryType.Polygon, _srid);

        // ACT — geography serialization must reorient the exterior to counter-clockwise so SQL Server accepts it.
        var binary = SqlServerSpatialNativeBinary.Serialize(polygon, isGeography: true);
        var roundTrip = SqlGeography.Deserialize(new SqlBytes(binary));

        // ASSERT — a valid, small polygon (not the whole-globe complement that a mis-oriented ring would produce)
        Assert.False(roundTrip.IsNull);
        // A correctly-oriented 30x30 degree polygon covers far less than a hemisphere.
        Assert.True(roundTrip.STArea().Value < SqlGeography.Point(0, 0, _srid).STBuffer(10_000_000).STArea().Value);
    }

    [Fact]
    public void TestNullInstance_ReturnsNull()
    {
        // SRID -1 marks a null instance ([MS-SSCLRT] §2.1.1)
        var nullBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Null(SqlServerSpatialNativeBinary.Deserialize(nullBytes, isGeography: true));
        Assert.Null(SqlServerSpatialNativeBinary.DeserializeGeometryPoint(nullBytes, isGeography: true));
    }

    [Fact]
    public void TestVersion2_Throws()
    {
        // Header: SRID (4326) + Version byte = 2 (unsupported: curves / FullGlobe / large geographies)
        var bytes = System.BitConverter.GetBytes(_srid).Concat(new byte[] { 0x02 }).ToArray();

        Assert.Throws<System.NotSupportedException>(
            () => SqlServerSpatialNativeBinary.Deserialize(bytes, isGeography: true));
    }

    [Fact]
    public void TestZValueGeography_DeserializeGeometryPointThrows()
    {
        // ARRANGE — a geography point carrying a Z value (materializes as Geometry<PointZ>, not Geometry<Point>)
        var pointZ = Geometry<PointZ>.Create(
            new List<PointZ> { new PointZ { X = 51.4, Y = 35.7, Z = 100 } }, GeometryType.Point, _srid);
        var binary = SqlServerSpatialNativeBinary.Serialize(pointZ, isGeography: true);

        // ACT + ASSERT — the 2D entry point rejects Z/M data with a clear error
        Assert.Throws<System.NotSupportedException>(
            () => SqlServerSpatialNativeBinary.DeserializeGeometryPoint(binary, isGeography: true));

        // ...but the general Deserialize still reads it as a Geometry<PointZ>
        var asZ = SqlServerSpatialNativeBinary.Deserialize(binary, isGeography: true);
        Assert.IsType<Geometry<PointZ>>(asZ);
    }
}
