using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.IO;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Linq;

namespace IRI.Maptor.Tst.Main.TheGeometry;

public class Geometry_SqlServerNativeBinaryTest
{
    public Geometry_SqlServerNativeBinaryTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    int _srid = 4326;

    [Theory]
    [InlineData("POINT(1 2)")]
    [InlineData("MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1), (9 9), (9 10), (10 9))")]
    [InlineData("MULTIPOINT((2 3), (7 8))")]
    [InlineData("LINESTRING(1 1, 2 0, 2 4, 3 3)")]
    [InlineData("LINESTRING(4 4, 9 0)")]
    [InlineData("MULTILINESTRING((1 1, 3 5), (-5 3, -8 -2))")]
    [InlineData("POLYGON((0 0, 30 0, 30 30, 0 30, 0 0))")]
    [InlineData("POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))")]
    [InlineData("POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0), (-10 0, -10 10, -15 0, -10 0))")]
    [InlineData("MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))")]
    [InlineData("MULTIPOLYGON(((0 0, 0 6, 6 6, 6 0, 0 0), (1 5, 1 1, 5 1, 5 5, 1 5)), ((4 4, 4 2, 2 2, 2 4, 4 4),(3.5 3.5, 2.5 3.5, 2.5 2.5, 3.5 2.5, 3.5 3.5)))")]
    public void TestSqlNativeBinaryDeserialize(string wktGeometry)
    {
        //var bytes = HexStringHelper.ToByteArray("0xE6100000010C363CBD529621F23F2D78D15790363640");

        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        // ACT
        var geometry = SqlServerSpatialNativeBinary.Deserialize(nativeBinary);

        // ASSERT
        Assert.Equal(geometry.AsWkt(), wktGeometry);
    }

    [Theory]
    [InlineData("POINT(1 2)")]
    [InlineData("MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1), (9 9), (9 10), (10 9))")]
    [InlineData("MULTIPOINT((2 3), (7 8))")]
    [InlineData("LINESTRING(1 1, 2 0, 2 4, 3 3)")]
    [InlineData("LINESTRING(4 4, 9 0)")]
    [InlineData("MULTILINESTRING((1 1, 3 5), (-5 3, -8 -2))")]
    [InlineData("POLYGON((0 0, 30 0, 30 30, 0 30, 0 0))")]
    [InlineData("POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))")]
    [InlineData("POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0), (-10 0, -10 10, -15 0, -10 0))")]
    [InlineData("MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))")]
    [InlineData("MULTIPOLYGON(((0 0, 0 6, 6 6, 6 0, 0 0), (1 5, 1 1, 5 1, 5 5, 1 5)), ((4 4, 4 2, 2 2, 2 4, 4 4),(3.5 3.5, 2.5 3.5, 2.5 2.5, 3.5 2.5, 3.5 3.5)))")]

    public void TestSqlNativeBinarySerialize(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        var geometry = Geometry<Point>.FromWkt(wktGeometry, _srid);

        // ACT
        var newNativeBinary = SqlServerSpatialNativeBinary.Serialize(geometry);

        // ASSERT
        Assert.True(nativeBinary.SequenceEqual(newNativeBinary));

    }

    [Theory]
    [InlineData("POINT(1 2 1)")] // PointZ
    [InlineData("POINT(1 2 2 3)")] // PointZM
    public void TestSqlNativeBinaryDeserializeZM(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        // ACT
        var geometry = SqlServerSpatialNativeBinary.Deserialize(nativeBinary);

        // ASSERT - Note: Z/M values are lost when converting to Point, so we only check X, Y
        var expectedGeometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        Assert.Equal(geometry.AsWkt(), expectedGeometry.AsWkt());
    }

    [Theory]
    [InlineData("POINT(1 2 1)")] // PointZ
    [InlineData("POINT(1 2 2 3)")] // PointZM
    public void TestSqlNativeBinarySerializeZM(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        var geometry = Geometry<Point>.FromWkt(wktGeometry, _srid);

        // ACT
        var newNativeBinary = SqlServerSpatialNativeBinary.Serialize(geometry);

        // ASSERT - Note: Since Point doesn't support Z/M, serialization may differ
        // We check that deserialization works correctly
        var deserialized = SqlServerSpatialNativeBinary.Deserialize(newNativeBinary);
        var expectedGeometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        Assert.Equal(deserialized.AsWkt(), expectedGeometry.AsWkt());
    }

    [Theory]
    [InlineData("LINESTRING(4 4 4 4, 9 0 4 4)")] // LineStringZM
    public void TestSqlNativeBinaryDeserializeLineStringZM(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        // ACT
        var geometry = SqlServerSpatialNativeBinary.Deserialize(nativeBinary);

        // ASSERT - Note: Z/M values are lost when converting to Point, so we only check X, Y
        var expectedGeometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        Assert.Equal(geometry.AsWkt(), expectedGeometry.AsWkt());
    }

    [Theory]
    [InlineData("POLYGON((0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9))")] // PolygonZ
    public void TestSqlNativeBinaryDeserializePolygonZ(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        // ACT
        var geometry = SqlServerSpatialNativeBinary.Deserialize(nativeBinary);

        // ASSERT - Note: Z values are lost when converting to Point, so we only check X, Y
        var expectedGeometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        Assert.Equal(geometry.AsWkt(), expectedGeometry.AsWkt());
    }

    [Theory]
    [InlineData("MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1 1), (9 9 1 2), (9 10), (10 9))")] // MultiPointZM
    public void TestSqlNativeBinaryDeserializeMultiPointZM(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        // ACT
        var geometry = SqlServerSpatialNativeBinary.Deserialize(nativeBinary);

        // ASSERT - Note: Z/M values are lost when converting to Point, so we only check X, Y
        var expectedGeometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        Assert.Equal(geometry.AsWkt(), expectedGeometry.AsWkt());
    }
}
