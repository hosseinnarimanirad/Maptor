using System.Linq;

using IRI.Maptor.Sta.Spatial.IO;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

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

        // ASSERT - Compare byte arrays using Assert.Equal
        Assert.Equal(nativeBinary, newNativeBinary);

    }

    [Theory]
    [InlineData("POINT(1 2 1)")] // PointZ
    [InlineData("POINT(1 2 2 3)")] // PointZM
    [InlineData("LINESTRING(4 4 4, 9 0 4)")] // LineStringZ
    [InlineData("LINESTRING(4 4 4 4, 9 0 4 4)")] // LineStringZM
    [InlineData("POLYGON((0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9))")] // PolygonZ
    [InlineData("POLYGON((0 0 9 8, 30 0 9 8, 30 30 9 8, 0 30 9 8, 0 0 9 8))")] // PolygonZM
    [InlineData("MULTILINESTRING((1 1 1, 3 5 1), (-5 3 1, -8 -2 1))")] // MultiLineStringZ
    [InlineData("MULTILINESTRING((1 1 1 2, 3 5 1 2), (-5 3 1 2, -8 -2 1 2))")] // MultiLineStringZM
    [InlineData("MULTIPOLYGON(((0 0 9, 0 3 9, 3 3 9, 3 0 9, 0 0 9), (2 1 9, 1 2 9, 1 1 9, 2 1 9)), ((9 9 9, 9 10 9, 10 9 9, 9 9 9)))")] // MultiPolygonZ
    [InlineData("MULTIPOLYGON(((0 0 9 8, 0 3 9 8, 3 3 9 8, 3 0 9 8, 0 0 9 8), (2 1 9 8, 1 2 9 8, 1 1 9 8, 2 1 9 8)), ((9 9 9 8, 9 10 9 8, 10 9 9 8, 9 9 9 8)))")] // MultiPolygonZM
    [InlineData("MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1 1), (9 9 1 2), (9 10), (10 9))")] // MultiPointZM
    public void TestSqlNativeBinaryDeserialize_new(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var geometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;
         
        var expectedNativeBinary = sqlGeometry.Serialize().Buffer;

        // ACT
        var actualNativeBinary = SqlServerSpatialNativeBinary.Serialize(geometry);

        // ASSERT - Compare byte arrays
        Assert.Equal(expectedNativeBinary, actualNativeBinary);

    }

    [Theory]
    [InlineData("POINT(1 2)")]
    [InlineData("POINT(1 2 1)")] // PointZ
    [InlineData("POINT(1 2 2 3)")] // PointZM
    [InlineData("MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1), (9 9), (9 10), (10 9))")]
    [InlineData("MULTIPOINT((2 3), (7 8))")]
    [InlineData("LINESTRING(1 1, 2 0, 2 4, 3 3)")]
    [InlineData("LINESTRING(4 4, 9 0)")]
    [InlineData("LINESTRING(4 4 4, 9 0 4)")] // LineStringZ
    [InlineData("LINESTRING(4 4 4 4, 9 0 4 4)")] // LineStringZM
    [InlineData("MULTILINESTRING((1 1, 3 5), (-5 3, -8 -2))")]
    [InlineData("MULTILINESTRING((1 1 1, 3 5 1), (-5 3 1, -8 -2 1))")] // MultiLineStringZ
    [InlineData("MULTILINESTRING((1 1 1 2, 3 5 1 2), (-5 3 1 2, -8 -2 1 2))")] // MultiLineStringZM
    [InlineData("POLYGON((0 0, 30 0, 30 30, 0 30, 0 0))")]
    [InlineData("POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))")]
    [InlineData("POLYGON((0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9))")] // PolygonZ
    [InlineData("POLYGON((0 0 9 8, 30 0 9 8, 30 30 9 8, 0 30 9 8, 0 0 9 8))")] // PolygonZM
    [InlineData("MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))")]
    [InlineData("MULTIPOLYGON(((0 0 9, 0 3 9, 3 3 9, 3 0 9, 0 0 9), (2 1 9, 1 2 9, 1 1 9, 2 1 9)), ((9 9 9, 9 10 9, 10 9 9, 9 9 9)))")] // MultiPolygonZ
    [InlineData("MULTIPOLYGON(((0 0 9 8, 0 3 9 8, 3 3 9 8, 3 0 9 8, 0 0 9 8), (2 1 9 8, 1 2 9 8, 1 1 9 8, 2 1 9 8)), ((9 9 9 8, 9 10 9 8, 10 9 9 8, 9 9 9 8)))")] // MultiPolygonZM
    public void TestSqlNativeBinarySerialize_new(string wktGeometry)
    {
        // ARRANGE 
        wktGeometry = wktGeometry.Replace(", ", ",");
        var geometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        // ACT
        var geometryNativeBinary = SqlServerSpatialNativeBinary.Serialize(geometry);
        var sqlGeometryNativeBinary = sqlGeometry.Serialize().Buffer;

        // ASSERT - Compare byte arrays
        Assert.Equal(sqlGeometryNativeBinary, geometryNativeBinary);
    }

    [Theory]
    [InlineData("POINT(1 2)")]
    [InlineData("POINT EMPTY")]
    public void TestSqlNativeBinaryDeserialize_PointOnly(string wktGeometry)
    {
        wktGeometry = wktGeometry.Replace(", ", ",");
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;

        var nativeBinary = sqlGeometry.Serialize().Buffer;

        var geometry = SqlServerSpatialNativeBinary.Deserialize(nativeBinary);
        var serializedBinary = SqlServerSpatialNativeBinary.Serialize(geometry);

        // Compare byte arrays instead of WKT
        Assert.Equal(nativeBinary, serializedBinary);
    }

    [Theory]
    [InlineData("POINT(1 2)")]
    [InlineData("POINT EMPTY")]
    public void TestSqlNativeBinarySerialize_PointOnly(string wktGeometry)
    {
        wktGeometry = wktGeometry.Replace(", ", ",");
        var geometry = Geometry<Point>.FromWkt(wktGeometry, _srid);
        
        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wktGeometry));
        sqlGeometry.STSrid = _srid;
        var expectedNativeBinary = sqlGeometry.Serialize().Buffer;
        
        var nativeBinary = SqlServerSpatialNativeBinary.Serialize(geometry);

        // Compare byte arrays instead of WKT
        Assert.Equal(expectedNativeBinary, nativeBinary);
    }
}
