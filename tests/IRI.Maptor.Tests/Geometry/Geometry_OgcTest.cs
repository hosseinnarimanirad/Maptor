using System.Data.SqlTypes; // For SqlBytes
using System.Collections.Generic;

using Microsoft.SqlServer.Types;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.IO.OgcSFA; 

namespace IRI.Maptor.Tests.TheGeometry;

public class Geometry_OgcTest
{
    public static IEnumerable<object[]> OgcGeometryTestData =>
    [
        // Points
        [ "POINT (1 2)"],
        [ "POINT (0 0)"],
        [ "POINT (-10.5 20.75)" ],
        [ "POINT (100 200)" ],
            
        // MultiPoints
        [ "MULTIPOINT ((0 0), (0 3), (3 3), (3 0), (1 1), (9 9), (9 10), (10 9))" ],
        [ "MULTIPOINT ((2 3), (7 8))" ],
        [ "MULTIPOINT ((1 1), (2 2), (3 3))" ],
        [ "MULTIPOINT ((10 20), (30 40), (50 60), (70 80))" ],
            
        // LineStrings
        [ "LINESTRING (1 1, 2 0, 2 4, 3 3)" ],
        [ "LINESTRING (4 4, 9 0)" ],
        [ "LINESTRING (0 0, 10 10)" ],
        [ "LINESTRING (0 0, 5 5, 10 0, 15 5, 20 0)" ],
        [ "LINESTRING (-5 -5, 0 0, 5 5, 10 10)" ],
            
        // MultiLineStrings
        [ "MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))" ],
        [ "MULTILINESTRING ((0 0, 10 10), (20 20, 30 30))" ],
        [ "MULTILINESTRING ((1 1, 2 2), (3 3, 4 4), (5 5, 6 6))" ],
            
        // Polygons
        [ "POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))" ],
        [ "POLYGON ((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0))" ],
        [ "POLYGON ((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0), (-10 0, -15 0, -10 10, -10 0))" ],
        [ "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))" ],
        [ "POLYGON ((0 0, 20 0, 20 20, 0 20, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))" ],
            
        // MultiPolygons
        [ "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0), (2 1, 1 1, 1 2, 2 1)), ((9 9, 10 9, 9 10, 9 9)))" ],
        [ "MULTIPOLYGON (((0 0, 6 0, 6 6, 0 6, 0 0), (1 5, 5 5, 5 1, 1 1, 1 5)), ((4 4, 2 4, 2 2, 4 2, 4 4), (3.5 3.5, 3.5 2.5, 2.5 2.5, 2.5 3.5, 3.5 3.5)))" ],
        [ "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 20 30, 20 20)))" ],
        [ "MULTIPOLYGON (((0 0, 5 0, 5 5, 0 5, 0 0)), ((10 10, 15 10, 15 15, 10 15, 10 10)), ((20 20, 25 20, 25 25, 20 25, 20 20)))" ],
            
        // Z Variants
        [ "POINT Z (1 2 3)" ],
        [ "POINT Z (0 0 10)" ],
        [ "MULTIPOINT Z ((0 0 0), (1 1 1), (2 2 2))" ],
        [ "LINESTRING Z (0 0 0, 1 1 1, 2 2 2)" ],
        [ "LINESTRING Z (0 0 5, 10 10 15, 20 20 25)" ],
        [ "MULTILINESTRING Z ((0 0 0, 1 1 1), (2 2 2, 3 3 3))" ],
        [ "POLYGON Z ((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0))" ],
        [ "POLYGON Z ((0 0 0, 20 0 0, 20 20 0, 0 20 0, 0 0 0), (5 5 0, 5 15 0, 15 15 0, 15 5 0, 5 5 0))" ],
        [ "MULTIPOLYGON Z (((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0)), ((20 20 0, 30 20 0, 30 30 0, 20 30 0, 20 20 0)))" ],
            
        // M Variants
        [ "POINT M (1 2 100)" ],
        [ "POINT M (0 0 200)" ],
        [ "MULTIPOINT M ((0 0 0), (1 1 10), (2 2 20))" ],
        [ "LINESTRING M (0 0 0, 1 1 10, 2 2 20)" ],
        [ "LINESTRING M (0 0 100, 10 10 200, 20 20 300)" ],
        [ "MULTILINESTRING M ((0 0 0, 1 1 10), (2 2 20, 3 3 30))" ],
        [ "POLYGON M ((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0))" ],
        [ "POLYGON M ((0 0 0, 20 0 0, 20 20 0, 0 20 0, 0 0 0), (5 5 0, 5 15 0, 15 15 0, 15 5 0, 5 5 0))" ],
        [ "MULTIPOLYGON M (((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0)), ((20 20 0, 30 20 0, 30 30 0, 20 30 0, 20 20 0)))" ],
            
        // ZM Variants
        [ "POINT ZM (1 2 3 100)" ],
        [ "POINT ZM (0 0 10 200)" ],
        [ "MULTIPOINT ZM ((0 0 0 0), (1 1 1 10), (2 2 2 20))" ],
        [ "LINESTRING ZM (0 0 0 0, 1 1 1 10, 2 2 2 20)" ],
        [ "LINESTRING ZM (0 0 5 100, 10 10 15 200, 20 20 25 300)" ],
        [ "MULTILINESTRING ZM ((0 0 0 0, 1 1 1 10), (2 2 2 20, 3 3 3 30))" ],
        [ "POLYGON ZM ((0 0 0 0, 10 0 0 0, 10 10 0 0, 0 10 0 0, 0 0 0 0))" ],
        [ "POLYGON ZM ((0 0 0 0, 20 0 0 0, 20 20 0 0, 0 20 0 0, 0 0 0 0), (5 5 0 0, 5 15 0 0, 15 15 0 0, 15 5 0 0, 5 5 0 0))" ],
        [ "MULTIPOLYGON ZM (((0 0 0 0, 10 0 0 0, 10 10 0 0, 0 10 0 0, 0 0 0 0)), ((20 20 0 0, 30 20 0 0, 30 30 0 0, 20 30 0 0, 20 20 0 0)))" ]
     ];

    public static IEnumerable<object[]> SqlServerGeometryTestData =>
    [
        // Points
        [ "POINT (1 2)"],
        [ "POINT (0 0)"],
        [ "POINT (-10.5 20.75)" ],
        [ "POINT (100 200)" ],
            
        // MultiPoints
        [ "MULTIPOINT ((0 0), (0 3), (3 3), (3 0), (1 1), (9 9), (9 10), (10 9))" ],
        [ "MULTIPOINT ((2 3), (7 8))" ],
        [ "MULTIPOINT ((1 1), (2 2), (3 3))" ],
        [ "MULTIPOINT ((10 20), (30 40), (50 60), (70 80))" ],
            
        // LineStrings
        [ "LINESTRING (1 1, 2 0, 2 4, 3 3)" ],
        [ "LINESTRING (4 4, 9 0)" ],
        [ "LINESTRING (0 0, 10 10)" ],
        [ "LINESTRING (0 0, 5 5, 10 0, 15 5, 20 0)" ],
        [ "LINESTRING (-5 -5, 0 0, 5 5, 10 10)" ],
            
        // MultiLineStrings
        [ "MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))" ],
        [ "MULTILINESTRING ((0 0, 10 10), (20 20, 30 30))" ],
        [ "MULTILINESTRING ((1 1, 2 2), (3 3, 4 4), (5 5, 6 6))" ],
            
        // Polygons
        [ "POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))" ],
        [ "POLYGON ((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0))" ],
        [ "POLYGON ((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0), (-10 0, -15 0, -10 10, -10 0))" ],
        [ "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))" ],
        [ "POLYGON ((0 0, 20 0, 20 20, 0 20, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))" ],
            
        // MultiPolygons
        [ "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0), (2 1, 1 1, 1 2, 2 1)), ((9 9, 10 9, 9 10, 9 9)))" ],
        [ "MULTIPOLYGON (((0 0, 6 0, 6 6, 0 6, 0 0), (1 5, 5 5, 5 1, 1 1, 1 5)), ((4 4, 2 4, 2 2, 4 2, 4 4), (3.5 3.5, 3.5 2.5, 2.5 2.5, 2.5 3.5, 3.5 3.5)))" ],
        [ "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 20 30, 20 20)))" ],
        [ "MULTIPOLYGON (((0 0, 5 0, 5 5, 0 5, 0 0)), ((10 10, 15 10, 15 15, 10 15, 10 10)), ((20 20, 25 20, 25 25, 20 25, 20 20)))" ],
            
        // Z Variants(using non-zero Z values)
        [ "POINT (1 2 3)" ],
        [ "POINT (0 0 10)" ],
        [ "MULTIPOINT ((0 0 1), (1 1 2), (2 2 3))" ],
        [ "LINESTRING (0 0 1, 1 1 2, 2 2 3)" ],
        [ "LINESTRING (0 0 5, 10 10 15, 20 20 25)" ],
        [ "MULTILINESTRING ((0 0 1, 1 1 2), (2 2 3, 3 3 5))" ],
        [ "POLYGON ((0 0 10, 10 0 10, 10 10 10, 0 10 10, 0 0 10))" ],
        [ "POLYGON ((0 0 10, 20 0 10, 20 20 10, 0 20 10, 0 0 10), (5 5 10, 5 15 10, 15 15 10, 15 5 10, 5 5 10))" ],
        [ "MULTIPOLYGON (((0 0 10, 10 0 10, 10 10 10, 0 10 10, 0 0 10)), ((20 20 10, 30 20 10, 30 30 10, 20 30 10, 20 20 10)))" ],
            
        // M Variants(using non-zero M values)
        [ "POINT (1 2 100)" ],
        [ "POINT (0 0 200)" ],
        [ "MULTIPOINT ((0 0 10), (1 1 20), (2 2 30))" ],
        [ "LINESTRING (0 0 10, 1 1 20, 2 2 30)" ],
        [ "LINESTRING (0 0 100, 10 10 200, 20 20 300)" ],
        [ "MULTILINESTRING ((0 0 10, 1 1 20), (2 2 30, 3 3 40))" ],
        [ "POLYGON ((0 0 10, 10 0 10, 10 10 10, 0 10 10, 0 0 10))" ],
        [ "POLYGON ((0 0 10, 20 0 10, 20 20 10, 0 20 10, 0 0 10), (5 5 10, 5 15 10, 15 15 10, 15 5 10, 5 5 10))" ],
        [ "MULTIPOLYGON (((0 0 10, 10 0 10, 10 10 10, 0 10 10, 0 0 10)), ((20 20 10, 30 20 10, 30 30 10, 20 30 10, 20 20 10)))" ],
            
        // ZM Variants(using non-zero Z and M values)
        [ "POINT (1 2 3 100)" ],
        [ "POINT (0 0 10 200)" ],
        [ "MULTIPOINT ((0 0 1 10), (1 1 2 20), (2 2 3 30))" ],
        [ "LINESTRING (0 0 1 10, 1 1 2 20, 2 2 3 30)" ],
        [ "LINESTRING (0 0 5 100, 10 10 15 200, 20 20 25 300)" ],
        [ "MULTILINESTRING ((0 0 1 10, 1 1 2 20), (2 2 3 30, 3 3 5 40))" ],
        [ "POLYGON ((0 0 10 100, 10 0 10 100, 10 10 10 100, 0 10 10 100, 0 0 10 100))" ],
        [ "POLYGON ((0 0 10 100, 20 0 10 100, 20 20 10 100, 0 20 10 100, 0 0 10 100), (5 5 10 100, 5 15 10 100, 15 15 10 100, 15 5 10 100, 5 5 10 100))" ],
        [ "MULTIPOLYGON (((0 0 10 100, 10 0 10 100, 10 10 10 100, 0 10 10 100, 0 0 10 100)), ((20 20 10 100, 30 20 10 100, 30 30 10 100, 20 30 10 100, 20 20 10 100)))" ]
     ];

    [Theory]
    [MemberData(nameof(OgcGeometryTestData))]
    public void TestGeometry_WkbAndWkt(string expectedWkt)
    {
        // Arrange
        var initialGeometry = WktReader.Parse(expectedWkt);

        // Act
        var actualWkt = WkbReader.Parse(initialGeometry.AsWkb()!, 0)!.AsWkt();

        // Assert
        Assert.Equal(expectedWkt, initialGeometry.AsWkt());
        Assert.Equal(expectedWkt, actualWkt);
    }

    [Theory]
    [MemberData(nameof(SqlServerGeometryTestData))]
    public void TestGeometry_WkbAndWkt_UsingSqlGeometry(string expectedSqlServerWkt)
    {
        // Arrange
        var initialGeometry = SqlServerWktReader.Parse(expectedSqlServerWkt);

        // Act
        var actualWkt = new string(SqlGeometry.STGeomFromWKB(new SqlBytes(initialGeometry.AsWkb()), 0).AsTextZM ().Buffer);

        // Assert
        // Verifies that WKT -> Geometry -> WKB -> Geometry(Sql) -> WKT results in the original WKT(or its canonical form)
        Assert.Equal(expectedSqlServerWkt, actualWkt);
    }

    [Theory]
    [MemberData(nameof(SqlServerGeometryTestData))]
    public void TestWkbWrite_CompareWithSqlGeometry(string originalWkt)
    {
        // Arrange
        const int srid = 0;
        var geometry = SqlServerWktReader.Parse(originalWkt, srid);
        var sqlGeometry = SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(originalWkt));
        sqlGeometry.STSrid = srid;

        // Act
        var geometryWkb = geometry.AsWkb();
        var sqlGeometryWkb = sqlGeometry.AsWkbZm();

        // Assert
        Assert.NotNull(geometryWkb);
        Assert.NotNull(sqlGeometryWkb);
        Assert.Equal(sqlGeometryWkb, geometryWkb);
    }

    [Theory]
    [MemberData(nameof(SqlServerGeometryTestData))]
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