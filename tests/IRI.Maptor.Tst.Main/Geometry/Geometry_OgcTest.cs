using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using Microsoft.SqlServer.Types;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using System.Collections.Generic;
using System.Data.SqlTypes; // For SqlBytes
using Xunit;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tst.Main.TheGeometry;

public class Geometry_OgcTest
{
    public Geometry_OgcTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    public static IEnumerable<object[]> OgcGeometryTestData =>
        new List<object[]>
        {
            new object[] { "POINT(1 2)" },
            new object[] { "MULTIPOINT((0 0), (0 3), (3 3), (3 0), (1 1), (9 9), (9 10), (10 9))" },
            new object[] { "MULTIPOINT((2 3), (7 8))" },
            new object[] { "LINESTRING(1 1, 2 0, 2 4, 3 3)" },
            new object[] { "LINESTRING(4 4, 9 0)" },
            new object[] { "MULTILINESTRING((1 1, 3 5), (-5 3, -8 -2))" },
            new object[] { "POLYGON((0 0, 30 0, 30 30, 0 30, 0 0))" },
            new object[] { "POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))" },
            new object[] { "POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0), (-10 0, -10 10, -15 0, -10 0))" },
            new object[] { "MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))" },
            new object[] { "MULTIPOLYGON(((0 0, 0 6, 6 6, 6 0, 0 0), (1 5, 1 1, 5 1, 5 5, 1 5)), ((4 4, 4 2, 2 2, 2 4, 4 4),(3.5 3.5, 2.5 3.5, 2.5 2.5, 3.5 2.5, 3.5 3.5)))" },
            //new object[] { "MULTIPOLYGON(((0 0, 0 6, 6 6, 6 0, 0 0), (5 5, 5 1, 1 1, 1 5, 5 5)), ((2 4, 2 2, 4 2, 4 4, 2 4),(3.5 3.5, 2.5 3.5, 2.5 2.5, 3.5 2.5, 3.5 3.5)))" }
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
      
}