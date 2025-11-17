using System.Data.SqlTypes; // For SqlBytes
using System.Collections.Generic;

using Microsoft.SqlServer.Types;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using Xunit;

namespace IRI.Maptor.Tst.Main.TheGeometry;

public class Geometry_OgcTest
{ 
    public static IEnumerable<object[]> OgcGeometryTestData =>
        new List<object[]>
        {
            // Points
            new object[] { "POINT(1 2)" },
            new object[] { "POINT(0 0)" },
            new object[] { "POINT(-10.5 20.75)" },
            new object[] { "POINT(100 200)" },
            
            // MultiPoints
            new object[] { "MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1), (9 9), (9 10), (10 9))" },
            new object[] { "MULTIPOINT((2 3), (7 8))" },
            new object[] { "MULTIPOINT((1 1), (2 2), (3 3))" },
            new object[] { "MULTIPOINT((10 20), (30 40), (50 60), (70 80))" },
            
            // LineStrings
            new object[] { "LINESTRING(1 1, 2 0, 2 4, 3 3)" },
            new object[] { "LINESTRING(4 4, 9 0)" },
            new object[] { "LINESTRING(0 0, 10 10)" },
            new object[] { "LINESTRING(0 0, 5 5, 10 0, 15 5, 20 0)" },
            new object[] { "LINESTRING(-5 -5, 0 0, 5 5, 10 10)" },
            
            // MultiLineStrings
            new object[] { "MULTILINESTRING((1 1, 3 5), (-5 3, -8 -2))" },
            new object[] { "MULTILINESTRING((0 0, 10 10), (20 20, 30 30))" },
            new object[] { "MULTILINESTRING((1 1, 2 2), (3 3, 4 4), (5 5, 6 6))" },
            
            // Polygons
            new object[] { "POLYGON((0 0, 30 0, 30 30, 0 30, 0 0))" },
            new object[] { "POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))" },
            new object[] { "POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0), (-10 0, -10 10, -15 0, -10 0))" },
            new object[] { "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))" },
            new object[] { "POLYGON((0 0, 20 0, 20 20, 0 20, 0 0), (5 5, 15 5, 15 15, 5 15, 5 5))" },
            
            // MultiPolygons
            new object[] { "MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))" },
            new object[] { "MULTIPOLYGON(((0 0, 0 6, 6 6, 6 0, 0 0), (1 5, 1 1, 5 1, 5 5, 1 5)), ((4 4, 4 2, 2 2, 2 4, 4 4),(3.5 3.5, 2.5 3.5, 2.5 2.5, 3.5 2.5, 3.5 3.5)))" },
            new object[] { "MULTIPOLYGON(((0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 20 30, 20 20)))" },
            new object[] { "MULTIPOLYGON(((0 0, 5 0, 5 5, 0 5, 0 0)), ((10 10, 15 10, 15 15, 10 15, 10 10)), ((20 20, 25 20, 25 25, 20 25, 20 20)))" },
            
            // Z Variants
            new object[] { "POINT Z (1 2 3)" },
            new object[] { "POINT Z (0 0 10)" },
            new object[] { "MULTIPOINT Z ((0 0 0), (1 1 1), (2 2 2))" },
            new object[] { "LINESTRING Z (0 0 0, 1 1 1, 2 2 2)" },
            new object[] { "LINESTRING Z (0 0 5, 10 10 15, 20 20 25)" },
            new object[] { "MULTILINESTRING Z ((0 0 0, 1 1 1), (2 2 2, 3 3 3))" },
            new object[] { "POLYGON Z ((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0))" },
            new object[] { "POLYGON Z ((0 0 0, 20 0 0, 20 20 0, 0 20 0, 0 0 0), (5 5 0, 15 5 0, 15 15 0, 5 15 0, 5 5 0))" },
            new object[] { "MULTIPOLYGON Z (((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0)), ((20 20 0, 30 20 0, 30 30 0, 20 30 0, 20 20 0)))" },
            
            // M Variants
            new object[] { "POINT M (1 2 100)" },
            new object[] { "POINT M (0 0 200)" },
            new object[] { "MULTIPOINT M ((0 0 0), (1 1 10), (2 2 20))" },
            new object[] { "LINESTRING M (0 0 0, 1 1 10, 2 2 20)" },
            new object[] { "LINESTRING M (0 0 100, 10 10 200, 20 20 300)" },
            new object[] { "MULTILINESTRING M ((0 0 0, 1 1 10), (2 2 20, 3 3 30))" },
            new object[] { "POLYGON M ((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0))" },
            new object[] { "POLYGON M ((0 0 0, 20 0 0, 20 20 0, 0 20 0, 0 0 0), (5 5 0, 15 5 0, 15 15 0, 5 15 0, 5 5 0))" },
            new object[] { "MULTIPOLYGON M (((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0)), ((20 20 0, 30 20 0, 30 30 0, 20 30 0, 20 20 0)))" },
            
            // ZM Variants
            new object[] { "POINT ZM (1 2 3 100)" },
            new object[] { "POINT ZM (0 0 10 200)" },
            new object[] { "MULTIPOINT ZM ((0 0 0 0), (1 1 1 10), (2 2 2 20))" },
            new object[] { "LINESTRING ZM (0 0 0 0, 1 1 1 10, 2 2 2 20)" },
            new object[] { "LINESTRING ZM (0 0 5 100, 10 10 15 200, 20 20 25 300)" },
            new object[] { "MULTILINESTRING ZM ((0 0 0 0, 1 1 1 10), (2 2 2 20, 3 3 3 30))" },
            new object[] { "POLYGON ZM ((0 0 0 0, 10 0 0 0, 10 10 0 0, 0 10 0 0, 0 0 0 0))" },
            new object[] { "POLYGON ZM ((0 0 0 0, 20 0 0 0, 20 20 0 0, 0 20 0 0, 0 0 0 0), (5 5 0 0, 15 5 0 0, 15 15 0 0, 5 15 0 0, 5 5 0 0))" },
            new object[] { "MULTIPOLYGON ZM (((0 0 0 0, 10 0 0 0, 10 10 0 0, 0 10 0 0, 0 0 0 0)), ((20 20 0 0, 30 20 0 0, 30 30 0 0, 20 30 0 0, 20 20 0 0)))" }
     };


    [Theory]
    [MemberData(nameof(OgcGeometryTestData))]
    public void TestGeometry_WkbAndWkt(string originalWkt)
    {
        // Arrange
        var initialGeometry = WktParser.Parse(originalWkt);

        // Act
        var finalWkt = Geometry<Point>.FromWkb(initialGeometry.AsWkb(), 0).AsWkt();

        // Assert
        Assert.Equal(initialGeometry.AsWkt(), finalWkt);
    }

    [Theory]
    [MemberData(nameof(OgcGeometryTestData))]
    public void TestGeometry_WkbAndWkt_UsingSqlGeometry(string originalWkt)
    {
        // Arrange
        var initialGeometry = WktParser.Parse(originalWkt);

        // Act
        var finalWkt = SqlGeometry.STGeomFromWKB(new SqlBytes(initialGeometry.AsWkb()), 0).AsGeometry().AsWkt();  
         
        // Assert
        // Verifies that WKT -> Geometry -> WKB -> Geometry (Sql) -> WKT results in the original WKT (or its canonical form)
        Assert.Equal(initialGeometry.AsWkt(), finalWkt);
    }

    [Theory]
    [MemberData(nameof(OgcGeometryTestData))]
    public void TestWkbWrite_CompareWithSqlGeometry(string originalWkt)
    {
        // Arrange
        const int srid = 0;
        var geometry = WktParser.Parse(originalWkt, srid);
        var sqlGeometry = SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(originalWkt));
        sqlGeometry.STSrid = srid;

        // Act
        var geometryWkb = geometry.AsWkb();
        var sqlGeometryWkb = sqlGeometry.AsWkb();

        // Assert
        Assert.NotNull(geometryWkb);
        Assert.NotNull(sqlGeometryWkb);
        Assert.Equal(sqlGeometryWkb, geometryWkb);
    }

    [Theory]
    [MemberData(nameof(OgcGeometryTestData))]
    public void TestWkbRead_CompareWithSqlGeometry(string originalWkt)
    {
        // Arrange
        const int srid = 0;
        var sqlGeometry = SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(originalWkt));
        sqlGeometry.STSrid = srid;
        var initialWkb = sqlGeometry.AsWkb();

        // Act
        var geometry = WkbReader.Parse(initialWkb, srid);
        Assert.NotNull(geometry);
        
        var geometryWkb = geometry.AsWkb();

        // Assert
        Assert.NotNull(initialWkb);
        Assert.NotNull(geometryWkb);
        Assert.Equal(initialWkb, geometryWkb);
    }
      
}