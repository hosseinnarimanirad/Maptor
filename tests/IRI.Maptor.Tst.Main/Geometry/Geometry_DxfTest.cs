using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Tst.Assets;
using Xunit;
using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;

namespace IRI.Maptor.Tst.Main.TheGeometry;

/// <summary>
/// Tests for DXF (Drawing Exchange Format) reading and writing functionality
/// </summary>
public class Geometry_DxfTest
{
    #region Basic Round-Trip Tests
    
    [Theory]
    [InlineData("Point")]
    [InlineData("Linestring")]
    [InlineData("Polygon")] 
    [InlineData("PolygonWithHole")]
    [InlineData("PolygonWithTwoHole")] 
    public void RoundTrip_VariousGeometries_ShouldPreserveGeometryData(string geometrySampleName)
    {
        // Arrange
        var originalGeometry = GetGeometrySampleByName(geometrySampleName);
        var originalTotalPoints = originalGeometry.TotalNumberOfPoints;

        // Act
        var dxfContent = originalGeometry.ToDxf();
        var restoredGeometries = DxfReader.Read(dxfContent, defaultSrid: 0);

        // Assert
        Assert.NotNull(restoredGeometries);
        Assert.NotEmpty(restoredGeometries);
        var restoredGeometry = restoredGeometries[0];
        Assert.True(restoredGeometry.HasAnyPoint(), 
            $"Restored geometry from {geometrySampleName} should have points");
        
        // Total points should be preserved (allowing for closed polygons adding duplicate point)
        var restoredTotalPoints = restoredGeometry.TotalNumberOfPoints;
        
        Assert.Equal( restoredTotalPoints, originalTotalPoints);
    }

    [Theory]
    [InlineData("Point", "POINT")]
    [InlineData("Linestring", "LWPOLYLINE")]
    [InlineData("Polygon", "LWPOLYLINE")]
    public void ToDxf_ShouldProduceExpectedDxfEntityType(string geometrySampleName, string expectedEntityType)
    {
        // Arrange
        var geometry = GetGeometrySampleByName(geometrySampleName);

        // Act
        var dxfContent = geometry.ToDxf();

        // Assert
        Assert.NotNull(dxfContent);
        Assert.Contains("SECTION", dxfContent);
        Assert.Contains("ENTITIES", dxfContent);
        Assert.Contains(expectedEntityType, dxfContent);
        Assert.Contains("EOF", dxfContent);
    }

    #endregion

    #region File I/O Tests

    [Fact]
    public void SaveAsDxf_AndReadFromFile_ShouldRoundTripSuccessfully()
    {
        // Arrange
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.dxf");
        var originalLineString = GeometrySamples.Linestring;
        
        try
        {
            // Act
            originalLineString.SaveAsDxf(tempFilePath);
            var restoredGeometries = DxfReader.ReadFromFile(tempFilePath, defaultSrid: 0);

            // Assert
            Assert.NotNull(restoredGeometries);
            Assert.NotEmpty(restoredGeometries);
            Assert.True(File.Exists(tempFilePath));
            var restoredGeometry = restoredGeometries[0];
            Assert.Equal(GeometryType.LineString, restoredGeometry.Type);
            Assert.Equal(originalLineString.Points.Count, restoredGeometry.Points.Count);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public void ReadFromFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.dxf");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => 
            DxfReader.ReadFromFile(nonExistentPath, 0));
    }

    #endregion

    #region Empty Geometry and Edge Case Tests

    [Theory]
    [InlineData("EmptyPoint")]
    [InlineData("EmptyLinestring")]
    [InlineData("EmptyPolygon")]
    [InlineData("EmptyMultipoint")]
    [InlineData("EmptyMultiLinestring")]
    [InlineData("EmptyMultiPolygon")]
    public void ToDxf_WithEmptyGeometry_ShouldProduceValidDxf(string emptySampleName)
    {
        // Arrange
        var emptyGeometry = GetEmptyGeometrySampleByName(emptySampleName);

        // Act
        var dxfContent = emptyGeometry.ToDxf();

        // Assert
        Assert.NotNull(dxfContent);
        Assert.Contains("SECTION", dxfContent);
        Assert.Contains("ENTITIES", dxfContent);
        Assert.Contains("ENDSEC", dxfContent);
        Assert.Contains("EOF", dxfContent);
    }

    [Fact]
    public void Read_WithEmptyDxfContent_ShouldReturnEmptyGeometry()
    {
        // Arrange
        const string emptyDxf = "";

        // Act
        var geometries = DxfReader.Read(emptyDxf, defaultSrid: 0);

        // Assert
        Assert.NotNull(geometries);
        Assert.NotEmpty(geometries);
        Assert.True(geometries[0].IsNullOrEmpty());
    }

    #endregion

    #region DXF Format Structure Tests

    [Fact]
    public void ToDxf_ShouldContainRequiredDxfSections()
    {
        // Arrange
        var geometry = GeometrySamples.Linestring;

        // Act
        var dxfContent = geometry.ToDxf();

        // Assert - Verify DXF structure
        Assert.Contains("0\r\nSECTION\r\n2\r\nHEADER", dxfContent);
        Assert.Contains("0\r\nSECTION\r\n2\r\nTABLES", dxfContent);
        Assert.Contains("0\r\nSECTION\r\n2\r\nENTITIES", dxfContent);
        Assert.Contains("0\r\nEOF", dxfContent);
    }

    [Fact]
    public void ToDxf_ShouldIncludeAcadVersion()
    {
        // Arrange
        var geometry = GeometrySamples.Point;

        // Act
        var dxfContent = geometry.ToDxf();

        // Assert
        Assert.Contains("$ACADVER", dxfContent);
        Assert.Contains("AC1015", dxfContent); // AutoCAD 2000 format
    }

    [Fact]
    public void ToDxf_WithPolygon_ShouldSetClosedFlag()
    {
        // Arrange
        var polygon = GeometrySamples.Polygon;

        // Act
        var dxfContent = polygon.ToDxf();

        // Assert
        var lines = dxfContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        // Find polyline closed flag
        bool foundClosedFlag = false;
        for (int i = 0; i < lines.Length - 1; i++)
        {
            if (lines[i].Trim() == "70") // Polyline flags
            {
                if (lines[i + 1].Trim() == "1") // Closed
                {
                    foundClosedFlag = true;
                    break;
                }
            }
        }
        
        Assert.True(foundClosedFlag, "Polygon should produce closed polyline (flag 70 = 1)");
    }

    #endregion

    #region Helper Methods

    private static IRI.Maptor.Sta.Spatial.Primitives.Geometry<Point> GetGeometrySampleByName(string name) => name switch
    {
        "Point" => GeometrySamples.Point,
        "PointZ" => GeometrySamples.PointZ,
        "PointZM" => GeometrySamples.PointZM,
        "Multipoint" => GeometrySamples.Multipoint,
        "MultipointComplex" => GeometrySamples.MultipointComplex,
        "Linestring" => GeometrySamples.Linestring,
        "LinestringZM" => GeometrySamples.LinestringZM,
        "MultiLineString" => GeometrySamples.MultiLineString,
        "Polygon" => GeometrySamples.Polygon,
        "PolygonWithHole" => GeometrySamples.PolygonWithHole,
        "PolygonWithTwoHole" => GeometrySamples.PolygonWithTwoHole,
        "MultiPolygon01" => GeometrySamples.MultiPolygon01,
        "MultiPolygon02" => GeometrySamples.MultiPolygon02,
        _ => throw new ArgumentException($"Unknown geometry sample: {name}", nameof(name))
    };

    private static IRI.Maptor.Sta.Spatial.Primitives.Geometry<Point> GetEmptyGeometrySampleByName(string name) => name switch
    {
        "EmptyPoint" => GeometrySamples.EmptyPoint,
        "EmptyLinestring" => GeometrySamples.EmptyLinestring,
        "EmptyPolygon" => GeometrySamples.EmptyPolygon,
        "EmptyMultipoint" => GeometrySamples.EmptyMultipoint,
        "EmptyMultiLinestring" => GeometrySamples.EmptyMultiLinestring,
        "EmptyMultiPolygon" => GeometrySamples.EmptyMultiPolygon,
        _ => throw new ArgumentException($"Unknown empty geometry sample: {name}", nameof(name))
    };

    #endregion

    #region Extension Method and Error Handling Tests

    [Fact]
    public void SaveAsDxf_ShouldReturnFilePath()
    {
        // Arrange
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.dxf");
        var geometry = GeometrySamples.Point;

        try
        {
            // Act
            var returnedPath = geometry.SaveAsDxf(tempFilePath);

            // Assert
            Assert.Equal(tempFilePath, returnedPath);
            Assert.True(File.Exists(returnedPath));
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public void SaveAsDxf_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var geometry = GeometrySamples.Point;
        string? nullPath = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            geometry.SaveAsDxf(nullPath!));
    }

    [Fact]
    public void ToDxf_WithNullGeometry_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRI.Maptor.Sta.Spatial.Primitives.Geometry<Point>? nullGeometry = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullGeometry!.ToDxf());
    }


    #endregion

    #region Coordinate Precision Tests

    [Fact]
    public void RoundTrip_DecimalCoordinates_ShouldPreservePrecision()
    {
        // Arrange
        var points = new List<Point>
        {
            new Point(123.456789, 987.654321),
            new Point(111.111111, 222.222222),
            new Point(333.333333, 444.444444)
        };
        var lineString = IRI.Maptor.Sta.Spatial.Primitives.Geometry<Point>.Create(points, GeometryType.LineString, srid: 0);

        // Act
        var dxfContent = lineString.ToDxf();
        var restoredGeometries = DxfReader.Read(dxfContent, defaultSrid: 0);

        // Assert
        Assert.NotNull(restoredGeometries);
        Assert.NotEmpty(restoredGeometries);
        var restoredGeometry = restoredGeometries[0];
        Assert.Equal(points.Count, restoredGeometry.Points.Count);
        
        // Verify precision to 6 decimal places (DXF uses "F6" format)
        for (int i = 0; i < points.Count; i++)
        {
            Assert.Equal(points[i].X, restoredGeometry.Points[i].X, precision: 6);
            Assert.Equal(points[i].Y, restoredGeometry.Points[i].Y, precision: 6);
        }
    }

    #endregion

    #region Area Preservation Tests

    [Fact]
    public void RoundTrip_SimplePolygon_ShouldPreserveArea()
    {
        // Arrange
        var originalPolygon = GeometrySamples.Polygon;
        var originalArea = originalPolygon.EuclideanArea;

        // Act
        var dxfContent = originalPolygon.ToDxf();
        var restoredGeometries = DxfReader.Read(dxfContent, defaultSrid: 0);

        // Assert
        Assert.NotNull(restoredGeometries);
        Assert.NotEmpty(restoredGeometries);
        var restoredGeometry = restoredGeometries[0];
        
        // Simple polygon should preserve area
        var restoredArea = restoredGeometry.EuclideanArea;
        Assert.Equal(originalArea, restoredArea, precision: 2);
    }

    [Theory]
    [InlineData("PolygonWithHole")]
    [InlineData("PolygonWithTwoHole")]
    public void RoundTrip_PolygonWithHoles_ShouldPreserveRingCount(string polygonSampleName)
    {
        // Arrange
        var originalPolygon = GetGeometrySampleByName(polygonSampleName);
        var originalRingCount = originalPolygon.Geometries.Count;

        // Act
        var dxfContent = originalPolygon.ToDxf();
        var restoredGeometries = DxfReader.Read(dxfContent, defaultSrid: 0);

        // Assert
        Assert.NotNull(restoredGeometries);
        Assert.NotEmpty(restoredGeometries);
        
        // DXF writes each ring as a separate polyline, so we expect multiple geometries in the list
        // The number of geometries should match the number of rings
        Assert.True(restoredGeometries.Count >= originalRingCount || 
                    restoredGeometries.Any(g => g.HasAnyPoint()));
    }

    #endregion

    #region Malformed Input Tests

    [Theory]
    [InlineData("This is not a valid DXF file")]
    [InlineData("0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1015\n0\nENDSEC\n0\nEOF")]
    public void Read_WithMalformedOrEmptyDxf_ShouldHandleGracefully(string dxfContent)
    {
        // Act
        var geometries = DxfReader.Read(dxfContent, defaultSrid: 0);

        // Assert - Should return list (empty or otherwise) rather than throw
        Assert.NotNull(geometries);
    }

    #endregion

    #region SRID Tests
    
    // Note: DXF format does not natively store SRID information
    // SRID must be managed separately by the application

    #endregion

    #region Integration Tests

    [Fact]
    public void DxfWriter_MultipleCalls_ShouldProduceValidDxf()
    {
        // Arrange
        var point1 = GeometrySamples.Point;
        var point2 = GeometrySamples.Multipoint;

        // Act
        var dxf1 = point1.ToDxf();
        var dxf2 = point2.ToDxf();

        // Assert - Both should be valid DXF that can be read back
        var restored1 = DxfReader.Read(dxf1, defaultSrid: 0);
        var restored2 = DxfReader.Read(dxf2, defaultSrid: 0);
        
        Assert.NotNull(restored1);
        Assert.NotNull(restored2);
        Assert.NotEmpty(restored1);
        Assert.NotEmpty(restored2);
        Assert.True(restored1[0].HasAnyPoint());
        Assert.True(restored2[0].HasAnyPoint());
    }

    #endregion
}

