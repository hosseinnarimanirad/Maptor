//using System.Collections.Generic;
//using IRI.Maptor.Extensions;
//using IRI.Maptor.Sta.Ogc.Extensions;
//using IRI.Maptor.Sta.Spatial.Primitives;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
//using Xunit;

//namespace IRI.Maptor.Tst.Main.OGC;

///// <summary>
///// Roundtrip tests for GML 2 and GML 3 formats
///// Tests: WKT -> IGeometry -> GML -> IGeometry -> WKT
///// </summary>
//public class GmlRoundtripTest
//{
//    public static IEnumerable<object[]> StandardOgcWktTestData =>
//    [
//        // 2D Geometries - Standard OGC WKT
//        [ "POINT (1 2)" ],
//        [ "POINT (0 0)" ],
//        [ "POINT (-10.5 20.75)" ],
//        [ "MULTIPOINT ((0 0), (0 3), (3 3), (3 0))" ],
//        [ "MULTIPOINT ((2 3), (7 8))" ],
//        [ "LINESTRING (1 1, 2 0, 2 4, 3 3)" ],
//        [ "LINESTRING (0 0, 10 10)" ],
//        [ "LINESTRING (0 0, 5 5, 10 0, 15 5, 20 0)" ],
//        [ "MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))" ],
//        [ "MULTILINESTRING ((0 0, 10 10), (20 20, 30 30))" ],
//        [ "POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))" ],
//        [ "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))" ],
//        [ "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0)), ((9 9, 10 9, 9 10, 9 9)))" ],
//        [ "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 20 30, 20 20)))" ],
        
//        // 3D Geometries (Z values) - Standard OGC WKT format with Z suffix
//        [ "POINT Z (1 2 3)" ],
//        [ "POINT Z (0 0 10)" ],
//        [ "MULTIPOINT Z ((0 0 0), (1 1 1), (2 2 2))" ],
//        [ "LINESTRING Z (0 0 0, 1 1 1, 2 2 2)" ],
//        [ "LINESTRING Z (0 0 5, 10 10 15, 20 20 25)" ],
//        [ "MULTILINESTRING Z ((0 0 0, 1 1 1), (2 2 2, 3 3 3))" ],
//        [ "POLYGON Z ((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0))" ],
//        [ "POLYGON Z ((0 0 0, 20 0 0, 20 20 0, 0 20 0, 0 0 0), (5 5 0, 5 15 0, 15 15 0, 15 5 0, 5 5 0))" ],
//        [ "MULTIPOLYGON Z (((0 0 0, 10 0 0, 10 10 0, 0 10 0, 0 0 0)), ((20 20 0, 30 20 0, 30 30 0, 20 30 0, 20 20 0)))" ],
//    ];

//    [Theory]
//    [MemberData(nameof(StandardOgcWktTestData))]
//    public void Roundtrip_Gml2_ShouldPreserveGeometry(string wktString)
//    {
//        // Arrange
//        const int srid = 4326;
        
//        // Parse WKT to IGeometry using OGC standard WktReader
//        var originalGeometry = WktReader.Parse(wktString, srid);
//        Assert.NotNull(originalGeometry);
//        Assert.False(originalGeometry.IsEmpty(), "Original geometry should not be empty");
        
//        var originalWkt = originalGeometry.AsWkt();
//        var originalType = originalGeometry.Type;
//        var originalSrid = originalGeometry.Srid;
//        var originalPointCount = originalGeometry.TotalNumberOfPoints;

//        // Act - Convert to GML 2 and back
//        var gml2String = originalGeometry.AsGml2(includeSrid: false);
//        Assert.NotNull(gml2String);
//        Assert.NotEmpty(gml2String);
        
//        var restoredGeometry = Sta_GmlExtensions.FromGml2(gml2String, srid);
//        Assert.NotNull(restoredGeometry);
//        Assert.False(restoredGeometry.IsEmpty(), "Restored geometry should not be empty");

//        // Assert - Verify geometry properties
//        Assert.Equal(originalType, restoredGeometry.Type);
//        Assert.Equal(originalSrid, restoredGeometry.Srid);
//        Assert.Equal(originalPointCount, restoredGeometry.TotalNumberOfPoints);
        
//        // Compare WKT strings (normalized)
//        var restoredWkt = restoredGeometry.AsWkt();
//        Assert.Equal(originalWkt, restoredWkt);
//    }

//    [Theory]
//    [MemberData(nameof(StandardOgcWktTestData))]
//    public void Roundtrip_Gml2_WithSrid_ShouldPreserveGeometry(string wktString)
//    {
//        // Arrange
//        const int srid = 4326;
        
//        // Parse WKT to IGeometry using OGC standard WktReader
//        var originalGeometry = WktReader.Parse(wktString, srid);
//        Assert.NotNull(originalGeometry);
//        Assert.False(originalGeometry.IsEmpty(), "Original geometry should not be empty");
        
//        var originalWkt = originalGeometry.AsWkt();
//        var originalType = originalGeometry.Type;
//        var originalSrid = originalGeometry.Srid;
//        var originalPointCount = originalGeometry.TotalNumberOfPoints;

//        // Act - Convert to GML 2 with SRID and back
//        var gml2String = originalGeometry.AsGml2(includeSrid: true);
//        Assert.NotNull(gml2String);
//        Assert.NotEmpty(gml2String);
        
//        var restoredGeometry = Sta_GmlExtensions.FromGml2(gml2String, srid);
//        Assert.NotNull(restoredGeometry);
//        Assert.False(restoredGeometry.IsEmpty(), "Restored geometry should not be empty");

//        // Assert - Verify geometry properties
//        Assert.Equal(originalType, restoredGeometry.Type);
//        Assert.Equal(originalSrid, restoredGeometry.Srid);
//        Assert.Equal(originalPointCount, restoredGeometry.TotalNumberOfPoints);
        
//        // Compare WKT strings (normalized)
//        var restoredWkt = restoredGeometry.AsWkt();
//        Assert.Equal(originalWkt, restoredWkt);
//    }

//    [Theory]
//    [MemberData(nameof(StandardOgcWktTestData))]
//    public void Roundtrip_Gml3_ShouldPreserveGeometry(string wktString)
//    {
//        // Arrange
//        const int srid = 4326;
        
//        // Parse WKT to IGeometry using OGC standard WktReader
//        var originalGeometry = WktReader.Parse(wktString, srid);
//        Assert.NotNull(originalGeometry);
//        Assert.False(originalGeometry.IsEmpty(), "Original geometry should not be empty");
        
//        var originalWkt = originalGeometry.AsWkt();
//        var originalType = originalGeometry.Type;
//        var originalSrid = originalGeometry.Srid;
//        var originalPointCount = originalGeometry.TotalNumberOfPoints;

//        // Act - Convert to GML 3 and back
//        var gml3String = originalGeometry.AsGml3(includeSrid: false);
//        Assert.NotNull(gml3String);
//        Assert.NotEmpty(gml3String);
        
//        var restoredGeometry = Sta_GmlExtensions.FromGml3(gml3String, srid);
//        Assert.NotNull(restoredGeometry);
//        Assert.False(restoredGeometry.IsEmpty(), "Restored geometry should not be empty");

//        // Assert - Verify geometry properties
//        Assert.Equal(originalType, restoredGeometry.Type);
//        Assert.Equal(originalSrid, restoredGeometry.Srid);
//        Assert.Equal(originalPointCount, restoredGeometry.TotalNumberOfPoints);
        
//        // Compare WKT strings (normalized)
//        var restoredWkt = restoredGeometry.AsWkt();
//        Assert.Equal(originalWkt, restoredWkt);
//    }

//    [Theory]
//    [MemberData(nameof(StandardOgcWktTestData))]
//    public void Roundtrip_Gml3_WithSrid_ShouldPreserveGeometry(string wktString)
//    {
//        // Arrange
//        const int srid = 4326;
        
//        // Parse WKT to IGeometry using OGC standard WktReader
//        var originalGeometry = WktReader.Parse(wktString, srid);
//        Assert.NotNull(originalGeometry);
//        Assert.False(originalGeometry.IsEmpty(), "Original geometry should not be empty");
        
//        var originalWkt = originalGeometry.AsWkt();
//        var originalType = originalGeometry.Type;
//        var originalSrid = originalGeometry.Srid;
//        var originalPointCount = originalGeometry.TotalNumberOfPoints;

//        // Act - Convert to GML 3 with SRID and back
//        var gml3String = originalGeometry.AsGml3(includeSrid: true);
//        Assert.NotNull(gml3String);
//        Assert.NotEmpty(gml3String);
        
//        var restoredGeometry = Sta_GmlExtensions.FromGml3(gml3String, srid);
//        Assert.NotNull(restoredGeometry);
//        Assert.False(restoredGeometry.IsEmpty(), "Restored geometry should not be empty");

//        // Assert - Verify geometry properties
//        Assert.Equal(originalType, restoredGeometry.Type);
//        Assert.Equal(originalSrid, restoredGeometry.Srid);
//        Assert.Equal(originalPointCount, restoredGeometry.TotalNumberOfPoints);
        
//        // Compare WKT strings (normalized)
//        var restoredWkt = restoredGeometry.AsWkt();
//        Assert.Equal(originalWkt, restoredWkt);
//    }

//    [Theory]
//    [MemberData(nameof(StandardOgcWktTestData))]
//    public void Roundtrip_Gml_AutoDetect_ShouldPreserveGeometry(string wktString)
//    {
//        // Arrange
//        const int srid = 4326;
        
//        // Parse WKT to IGeometry using OGC standard WktReader
//        var originalGeometry = WktReader.Parse(wktString, srid);
//        Assert.NotNull(originalGeometry);
//        Assert.False(originalGeometry.IsEmpty(), "Original geometry should not be empty");
        
//        var originalWkt = originalGeometry.AsWkt();
//        var originalType = originalGeometry.Type;
//        var originalSrid = originalGeometry.Srid;
//        var originalPointCount = originalGeometry.TotalNumberOfPoints;

//        // Act - Convert to GML 3, then use auto-detect to parse back
//        var gml3String = originalGeometry.AsGml3(includeSrid: false);
//        Assert.NotNull(gml3String);
//        Assert.NotEmpty(gml3String);
        
//        var restoredGeometry = Sta_GmlExtensions.FromGml(gml3String, srid);
//        Assert.NotNull(restoredGeometry);
//        Assert.False(restoredGeometry.IsEmpty(), "Restored geometry should not be empty");

//        // Assert - Verify geometry properties
//        Assert.Equal(originalType, restoredGeometry.Type);
//        Assert.Equal(originalSrid, restoredGeometry.Srid);
//        Assert.Equal(originalPointCount, restoredGeometry.TotalNumberOfPoints);
        
//        // Compare WKT strings (normalized)
//        var restoredWkt = restoredGeometry.AsWkt();
//        Assert.Equal(originalWkt, restoredWkt);
//    }
//}

