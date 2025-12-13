using System.Collections.Generic;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Tst.Main.TheGeometry;

/// <summary>
/// Tests for KML read/write round-trip functionality
/// Tests all geometry types (Point, LineString, Polygon and their Multi- versions)
/// with both 2D and 3D (Z values) geometries
/// </summary>
public class Geometry_KmlTest
{
    public static IEnumerable<object[]> GeometryRoundTripTestData()
    {
        // 2D Point
        yield return new object[] { "POINT (51.5074 -0.1278)" };

        // 3D Point
        yield return new object[] { "POINT Z (51.5074 -0.1278 100.5)" };

        // 2D LineString
        yield return new object[] { "LINESTRING (0 0, 10 10, 20 5)" };

        // 3D LineString
        yield return new object[] { "LINESTRING Z (0 0 10, 10 10 20, 20 5 15)" };

        // 2D Polygon (simple, no holes)
        yield return new object[] { "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))" };

        // 2D Polygon with hole
        yield return new object[] { "POLYGON ((0 0, 20 0, 20 20, 0 20, 0 0), (5 5, 15 5, 15 15, 5 15, 5 5))" };

        // 3D Polygon
        yield return new object[] { "POLYGON Z ((0 0 100, 10 0 100, 10 10 100, 0 10 100, 0 0 100))" };

        // 2D MultiPoint
        yield return new object[] { "MULTIPOINT ((0 0), (10 10), (20 5))" };

        // 3D MultiPoint
        yield return new object[] { "MULTIPOINT Z ((0 0 10), (10 10 20), (20 5 15))" };

        // 2D MultiLineString
        yield return new object[] { "MULTILINESTRING ((0 0, 10 10), (20 20, 30 30, 40 40))" };

        // 3D MultiLineString
        yield return new object[] { "MULTILINESTRING Z ((0 0 10, 10 10 20), (20 20 30, 30 30 40, 40 40 50))" };

        // 2D MultiPolygon
        yield return new object[] { "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 20 30, 20 20)))" };

        // 3D MultiPolygon
        yield return new object[] { "MULTIPOLYGON Z (((0 0 100, 10 0 100, 10 10 100, 0 10 100, 0 0 100)), ((20 20 200, 30 20 200, 30 30 200, 20 30 200, 20 20 200)))" };
    }

    [Theory]
    [MemberData(nameof(GeometryRoundTripTestData))]
    public void TestGeometry_KmlRoundTrip(string originalWkt)
    {
        const int srid = 4326;

        // Arrange - Parse WKT to geometry
        var originalGeometry = WktReader.Parse(originalWkt, srid);

        // Act - Write to KML
        var kmlString = KmlWriter.ToKml(originalGeometry);

        // Act - Read from KML
        var readGeometries = KmlReader.Parse(kmlString, srid);
        Assert.Single(readGeometries);
        var readGeometry = readGeometries[0];

        // Act - Get round-trip WKT
        var roundTripWkt = readGeometry.AsWkt();

        // Assert
        Assert.Equal(originalWkt, roundTripWkt);
    }

}

