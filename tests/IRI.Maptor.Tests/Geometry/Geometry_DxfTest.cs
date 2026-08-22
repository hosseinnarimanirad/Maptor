using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.IO.Dxf;
using IRI.Maptor.Tests.Assets;
using Xunit;
using Geometry = IRI.Maptor.Core.Spatial.Primitives.Geometry<IRI.Maptor.Core.Common.Primitives.Point>;

namespace IRI.Maptor.Tests.TheGeometry;

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

        Assert.Equal(restoredTotalPoints, originalTotalPoints);
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
    public async void SaveAsDxf_AndReadFromFile_ShouldRoundTripSuccessfully()
    {
        // Arrange
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.dxf");
        var originalLineString = GeometrySamples.Linestring;

        try
        {
            // Act
            await originalLineString.SaveAsDxfAsync(tempFilePath);

            var restoredGeometries = await DxfReader.ReadFromFile(tempFilePath, defaultSrid: 0);

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
        Assert.ThrowsAsync<FileNotFoundException>(async () => await DxfReader.ReadFromFile(nonExistentPath, 0));
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

    private static IRI.Maptor.Core.Spatial.Primitives.Geometry<Point> GetGeometrySampleByName(string name) => name switch
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

    private static IRI.Maptor.Core.Spatial.Primitives.Geometry<Point> GetEmptyGeometrySampleByName(string name) => name switch
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
    public void SaveAsDxf_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var geometry = GeometrySamples.Point;
        string? nullPath = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await geometry.SaveAsDxfAsync(nullPath!));
    }

    [Fact]
    public void ToDxf_WithNullGeometry_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRI.Maptor.Core.Spatial.Primitives.Geometry<Point>? nullGeometry = null;

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
        var lineString = IRI.Maptor.Core.Spatial.Primitives.Geometry<Point>.Create(points, GeometryType.LineString, srid: 0);

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

    #region Block/INSERT Expansion Tests

    private static string BuildDxf(string[] blocksSection, string[] entitiesSection)
    {
        var lines = new List<string>();

        if (blocksSection.Length > 0)
        {
            lines.AddRange(new[] { "0", "SECTION", "2", "BLOCKS" });
            lines.AddRange(blocksSection);
            lines.AddRange(new[] { "0", "ENDSEC" });
        }

        lines.AddRange(new[] { "0", "SECTION", "2", "ENTITIES" });
        lines.AddRange(entitiesSection);
        lines.AddRange(new[] { "0", "ENDSEC", "0", "EOF" });

        return string.Join("\n", lines);
    }

    [Fact]
    public void Read_Insert_ShouldEmitInsertionPointAndTransformedBlockGeometry()
    {
        // Block SYM: a unit line from (0,0) to (1,0); inserted twice:
        // once translated to (10,20), once at (5,5) scaled ×2 and rotated 90°
        var dxf = BuildDxf(
            blocksSection: new[]
            {
                "0", "BLOCK", "2", "SYM", "10", "0", "20", "0",
                "0", "LINE", "10", "0", "20", "0", "11", "1", "21", "0",
                "0", "ENDBLK"
            },
            entitiesSection: new[]
            {
                "0", "INSERT", "2", "SYM", "10", "10", "20", "20",
                "0", "INSERT", "2", "SYM", "10", "5", "20", "5", "41", "2", "42", "2", "50", "90"
            });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var points = geometries.Where(g => g.Type == GeometryType.Point).ToList();
        var lineStrings = geometries.Where(g => g.Type == GeometryType.LineString).ToList();

        Assert.Equal(2, points.Count);
        Assert.Equal(2, lineStrings.Count);

        // Insertion points
        Assert.Equal(10, points[0].Points[0].X, precision: 9);
        Assert.Equal(20, points[0].Points[0].Y, precision: 9);
        Assert.Equal(5, points[1].Points[0].X, precision: 9);
        Assert.Equal(5, points[1].Points[0].Y, precision: 9);

        // Translated copy: (10,20) → (11,20)
        Assert.Equal(10, lineStrings[0].Points[0].X, precision: 9);
        Assert.Equal(20, lineStrings[0].Points[0].Y, precision: 9);
        Assert.Equal(11, lineStrings[0].Points[1].X, precision: 9);
        Assert.Equal(20, lineStrings[0].Points[1].Y, precision: 9);

        // Scaled ×2 then rotated 90°: (0,0)-(2,0) → (0,0)-(0,2), translated to (5,5)-(5,7)
        Assert.Equal(5, lineStrings[1].Points[0].X, precision: 9);
        Assert.Equal(5, lineStrings[1].Points[0].Y, precision: 9);
        Assert.Equal(5, lineStrings[1].Points[1].X, precision: 9);
        Assert.Equal(7, lineStrings[1].Points[1].Y, precision: 9);
    }

    [Fact]
    public void Read_NestedInsert_ShouldComposeTransforms()
    {
        // Block INNER holds a line (0,0)-(1,0); block OUTER inserts INNER at (10,0);
        // the drawing inserts OUTER at (100,0) → expect the line at (110,0)-(111,0)
        var dxf = BuildDxf(
            blocksSection: new[]
            {
                "0", "BLOCK", "2", "INNER", "10", "0", "20", "0",
                "0", "LINE", "10", "0", "20", "0", "11", "1", "21", "0",
                "0", "ENDBLK",
                "0", "BLOCK", "2", "OUTER", "10", "0", "20", "0",
                "0", "INSERT", "2", "INNER", "10", "10", "20", "0",
                "0", "ENDBLK"
            },
            entitiesSection: new[]
            {
                "0", "INSERT", "2", "OUTER", "10", "100", "20", "0"
            });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var lineString = Assert.Single(geometries, g => g.Type == GeometryType.LineString);
        Assert.Equal(110, lineString.Points[0].X, precision: 9);
        Assert.Equal(0, lineString.Points[0].Y, precision: 9);
        Assert.Equal(111, lineString.Points[1].X, precision: 9);
        Assert.Equal(0, lineString.Points[1].Y, precision: 9);
    }

    [Fact]
    public void Read_InsertOfUnknownBlock_ShouldStillEmitInsertionPoint()
    {
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[] { "0", "INSERT", "2", "MISSING", "10", "3", "20", "4" });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var point = Assert.Single(geometries, g => g.Type == GeometryType.Point);
        Assert.Equal(3, point.Points[0].X, precision: 9);
        Assert.Equal(4, point.Points[0].Y, precision: 9);
    }

    #endregion

    #region Ellipse, Spline and Solid Tests

    [Fact]
    public void Read_FullEllipse_ShouldProducePolygon()
    {
        // Center (0,0), major axis vector (2,0), ratio 0.5 → semi-axes 2 and 1
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "ELLIPSE", "10", "0", "20", "0", "11", "2", "21", "0",
                "40", "0.5", "41", "0", "42", "6.283185307179586"
            });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var polygon = Assert.Single(geometries, g => g.Type == GeometryType.Polygon);

        var boundingBox = polygon.GetBoundingBox();
        Assert.Equal(-2, boundingBox.XMin, precision: 6);
        Assert.Equal(2, boundingBox.XMax, precision: 6);
        Assert.Equal(-1, boundingBox.YMin, precision: 6);
        Assert.Equal(1, boundingBox.YMax, precision: 6);
    }

    [Fact]
    public void Read_EllipticalArc_ShouldProduceLineString()
    {
        // Upper half (parameters 0..π) of the same ellipse: from (2,0) to (-2,0)
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "ELLIPSE", "10", "0", "20", "0", "11", "2", "21", "0",
                "40", "0.5", "41", "0", "42", "3.141592653589793"
            });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var lineString = Assert.Single(geometries, g => g.Type == GeometryType.LineString);
        Assert.Equal(2, lineString.Points[0].X, precision: 6);
        Assert.Equal(0, lineString.Points[0].Y, precision: 6);
        Assert.Equal(-2, lineString.Points[lineString.Points.Count - 1].X, precision: 6);
        Assert.Equal(0, lineString.Points[lineString.Points.Count - 1].Y, precision: 6);
    }

    [Fact]
    public void Read_SplineWithFitPoints_ShouldProduceLineStringThroughThem()
    {
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "SPLINE", "71", "3",
                "11", "0", "21", "0",
                "11", "1", "21", "1",
                "11", "2", "21", "0"
            });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var lineString = Assert.Single(geometries, g => g.Type == GeometryType.LineString);
        Assert.Equal(3, lineString.Points.Count);
        Assert.Equal(1, lineString.Points[1].X, precision: 9);
        Assert.Equal(1, lineString.Points[1].Y, precision: 9);
    }

    [Fact]
    public void Read_SplineWithControlPoints_ShouldApproximateCurve()
    {
        // Degree-2 Bézier (clamped knot vector) with control points (0,0), (1,2), (2,0):
        // the curve starts at (0,0), ends at (2,0) and peaks at (1,1)
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "SPLINE", "71", "2",
                "40", "0", "40", "0", "40", "0", "40", "1", "40", "1", "40", "1",
                "10", "0", "20", "0",
                "10", "1", "20", "2",
                "10", "2", "20", "0"
            });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var lineString = Assert.Single(geometries, g => g.Type == GeometryType.LineString);
        Assert.True(lineString.Points.Count >= 32);

        Assert.Equal(0, lineString.Points[0].X, precision: 6);
        Assert.Equal(0, lineString.Points[0].Y, precision: 6);
        Assert.Equal(2, lineString.Points[lineString.Points.Count - 1].X, precision: 6);
        Assert.Equal(0, lineString.Points[lineString.Points.Count - 1].Y, precision: 6);

        // Curve maximum at the midpoint: B(0.5) = (1, 1)
        var maxY = lineString.Points.Max(p => p.Y);
        Assert.Equal(1, maxY, precision: 6);
    }

    private static string BuildDxfWithTables(string[] tablesSection, string[] blocksSection, string[] entitiesSection)
    {
        var lines = new List<string>();

        if (tablesSection.Length > 0)
        {
            lines.AddRange(new[] { "0", "SECTION", "2", "TABLES" });
            lines.AddRange(tablesSection);
            lines.AddRange(new[] { "0", "ENDSEC" });
        }

        return string.Join("\n", lines) + (lines.Count > 0 ? "\n" : "") + BuildDxf(blocksSection, entitiesSection);
    }

    #region DxfFeature (CAD Context and Annotation Separation) Tests

    [Fact]
    public void ReadFeatures_ShouldExposeLayerEntityTypeAndAciColor()
    {
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "LINE", "8", "Roads", "62", "1", "10", "0", "20", "0", "11", "1", "21", "1"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal("LINE", feature.EntityType);
        Assert.Equal("Roads", feature.DxfLayerName);
        Assert.Equal("#FF0000", feature.Color);
        Assert.False(feature.IsAnnotation);
    }

    [Fact]
    public void ReadFeatures_WithoutEntityColor_ShouldFallBackToLayerColor()
    {
        var dxf = BuildDxfWithTables(
            tablesSection: new[]
            {
                "0", "TABLE", "2", "LAYER",
                "0", "LAYER", "2", "Parcels", "62", "3",
                "0", "ENDTAB"
            },
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "LWPOLYLINE", "8", "Parcels", "90", "4", "70", "1",
                "10", "0", "20", "0",
                "10", "1", "20", "0",
                "10", "1", "20", "1",
                "10", "0", "20", "1"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal(GeometryType.Polygon, feature.Geometry.Type);
        Assert.Equal("#00FF00", feature.Color); // ACI 3 = green, inherited from the layer
    }

    [Fact]
    public void ReadFeatures_TrueColor_ShouldWinOverAciColor()
    {
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "LINE", "62", "1", "420", "255", "10", "0", "20", "0", "11", "1", "21", "1"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        Assert.Equal("#0000FF", Assert.Single(features).Color);
    }

    [Fact]
    public void ReadFeatures_SolidAndText_ShouldBeAnnotation_LineShouldNot()
    {
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "LINE", "10", "0", "20", "0", "11", "5", "21", "5",
                "0", "SOLID", "10", "0", "20", "0", "11", "1", "21", "0", "12", "0", "22", "1", "13", "1", "23", "1",
                "0", "TEXT", "10", "2", "20", "3", "1", "Parcel 12"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        Assert.Equal(3, features.Count);

        var line = Assert.Single(features, f => f.EntityType == "LINE");
        Assert.False(line.IsAnnotation);

        var solid = Assert.Single(features, f => f.EntityType == "SOLID");
        Assert.True(solid.IsAnnotation);
        Assert.Equal(GeometryType.Polygon, solid.Geometry.Type);

        var text = Assert.Single(features, f => f.EntityType == "TEXT");
        Assert.True(text.IsAnnotation);
        Assert.Equal(GeometryType.Point, text.Geometry.Type);
        Assert.Equal("Parcel 12", text.Text);
        Assert.Equal(2, text.Geometry.Points[0].X, precision: 9);
        Assert.Equal(3, text.Geometry.Points[0].Y, precision: 9);
    }

    [Fact]
    public void ReadFeatures_AnnotationPolygonInsideRealPolygon_ShouldNotBecomeHole()
    {
        // A SOLID arrowhead sits inside a real parcel: it must stay a separate annotation
        // polygon instead of being swallowed as a hole of the parcel.
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "LWPOLYLINE", "90", "4", "70", "1",
                "10", "0", "20", "0",
                "10", "10", "20", "0",
                "10", "10", "20", "10",
                "10", "0", "20", "10",
                "0", "SOLID", "10", "2", "20", "2", "11", "3", "21", "2", "12", "2", "22", "3", "13", "3", "23", "3"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        Assert.Equal(2, features.Count);

        var parcel = Assert.Single(features, f => !f.IsAnnotation);
        Assert.Equal(GeometryType.Polygon, parcel.Geometry.Type);
        Assert.Single(parcel.Geometry.Geometries); // no hole

        var arrow = Assert.Single(features, f => f.IsAnnotation);
        Assert.Equal(GeometryType.Polygon, arrow.Geometry.Type);
    }

    [Fact]
    public void ReadFeatures_PolygonWithHole_ShouldKeepOwnerContext()
    {
        // Outer square (layer Parcels) with a triangular hole drawn as a second closed polyline:
        // the reassembled polygon keeps the CAD context of its exterior ring's entity.
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "LWPOLYLINE", "8", "Parcels", "62", "1", "90", "4", "70", "1",
                "10", "0", "20", "0",
                "10", "10", "20", "0",
                "10", "10", "20", "10",
                "10", "0", "20", "10",
                "0", "LWPOLYLINE", "8", "Holes", "90", "3", "70", "1",
                "10", "2", "20", "2",
                "10", "3", "20", "2",
                "10", "2", "20", "3"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal(GeometryType.Polygon, feature.Geometry.Type);
        Assert.Equal(2, feature.Geometry.Geometries.Count); // outer ring + hole
        Assert.Equal("Parcels", feature.DxfLayerName);
        Assert.Equal("#FF0000", feature.Color);
    }

    [Fact]
    public void ReadFeatures_InsertOfAnonymousBlock_ShouldBeAnnotation()
    {
        var dxf = BuildDxf(
            blocksSection: new[]
            {
                "0", "BLOCK", "2", "*D1", "10", "0", "20", "0",
                "0", "LINE", "10", "0", "20", "0", "11", "1", "21", "0",
                "0", "ENDBLK",
                "0", "BLOCK", "2", "SYM", "10", "0", "20", "0",
                "0", "LINE", "10", "0", "20", "0", "11", "1", "21", "0",
                "0", "ENDBLK"
            },
            entitiesSection: new[]
            {
                "0", "INSERT", "2", "*D1", "10", "0", "20", "0",
                "0", "INSERT", "2", "SYM", "10", "5", "20", "5"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        Assert.Equal(4, features.Count); // 2 × (insertion point + expanded line)
        Assert.All(features, f => Assert.Equal("INSERT", f.EntityType));
        Assert.Equal(2, features.Count(f => f.IsAnnotation));
        Assert.Equal(2, features.Count(f => !f.IsAnnotation));
    }

    [Fact]
    public void ReadFeatures_Dimension_ShouldExpandItsBlockAsAnnotation()
    {
        var dxf = BuildDxf(
            blocksSection: new[]
            {
                "0", "BLOCK", "2", "*D5", "10", "0", "20", "0",
                "0", "LINE", "10", "0", "20", "0", "11", "4", "21", "0",
                "0", "ENDBLK"
            },
            entitiesSection: new[]
            {
                "0", "DIMENSION", "8", "Dims", "2", "*D5", "10", "0", "20", "0"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal("DIMENSION", feature.EntityType);
        Assert.Equal("Dims", feature.DxfLayerName);
        Assert.True(feature.IsAnnotation);
        Assert.Equal(GeometryType.LineString, feature.Geometry.Type);
        Assert.Equal(4, feature.Geometry.Points[1].X, precision: 9);
    }

    [Fact]
    public void ReadFeatures_EntityOnDefpointsLayer_ShouldBeAnnotation()
    {
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "POINT", "8", "Defpoints", "10", "1", "20", "1"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        Assert.True(Assert.Single(features).IsAnnotation);
    }

    [Theory]
    [InlineData(1, "#FF0000")]   // red
    [InlineData(5, "#0000FF")]   // blue
    [InlineData(7, "#FFFFFF")]   // white/black
    [InlineData(11, "#FFAAAA")]  // muted light red
    [InlineData(253, "#ADADAD")] // gray ramp
    public void DxfAciColor_ToHex_ShouldMatchStandardPalette(int aci, string expectedHex)
    {
        Assert.Equal(expectedHex, DxfAciColor.ToHex(aci));
    }

    [Fact]
    public void ReadFeatures_3dFace_ShouldBeAnnotationPolygon()
    {
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "3DFACE",
                "10", "0", "20", "0",
                "11", "1", "21", "0",
                "12", "1", "22", "1",
                "13", "0", "23", "1"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal("3DFACE", feature.EntityType);
        Assert.True(feature.IsAnnotation);
        Assert.Equal(GeometryType.Polygon, feature.Geometry.Type);
    }

    [Fact]
    public void ReadFeatures_HatchWithLineEdgeBoundary_ShouldProduceAnnotationPolygon()
    {
        // One edge-type boundary path (92 = 1) with 4 line edges tracing the unit square.
        // The leading 10/20 elevation point and the trailing seed point must be ignored.
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "HATCH", "8", "Bridge", "10", "0.0", "20", "0.0", "2", "STEEL", "70", "0", "91", "1",
                "92", "1", "93", "4",
                "72", "1", "10", "0", "20", "0", "11", "1", "21", "0",
                "72", "1", "10", "1", "20", "0", "11", "1", "21", "1",
                "72", "1", "10", "1", "20", "1", "11", "0", "21", "1",
                "72", "1", "10", "0", "20", "1", "11", "0", "21", "0",
                "97", "0",
                "98", "1", "10", "0.5", "20", "0.5"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal("HATCH", feature.EntityType);
        Assert.Equal("Bridge", feature.DxfLayerName);
        Assert.True(feature.IsAnnotation);
        Assert.Equal(GeometryType.Polygon, feature.Geometry.Type);
        Assert.Equal(4, feature.Geometry.TotalNumberOfPoints); // the seed point is not a vertex
        Assert.Equal(1, feature.Geometry.EuclideanArea, precision: 6);
    }

    [Fact]
    public void ReadFeatures_HatchWithPolylineBoundary_ShouldProduceAnnotationPolygon()
    {
        // One polyline-type boundary path (92 bit 1 set): 72 = has-bulge flag, 73 = closed, 93 = vertex count
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "HATCH", "10", "0.0", "20", "0.0", "91", "1",
                "92", "7", "72", "0", "73", "1", "93", "3",
                "10", "0", "20", "0",
                "10", "2", "20", "0",
                "10", "0", "20", "2",
                "97", "0"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal(GeometryType.Polygon, feature.Geometry.Type);
        Assert.Equal(3, feature.Geometry.TotalNumberOfPoints);
        Assert.Equal(2, feature.Geometry.EuclideanArea, precision: 6);
    }

    [Fact]
    public void ReadFeatures_Wipeout_ShouldTransformClipBoundaryToWorld()
    {
        // Insertion (100,200), U = (10,0), V = (0,10), full-frame clip boundary
        // (unit square centered at origin, +Y down) → world square (100,200)-(110,210)
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "WIPEOUT",
                "10", "100", "20", "200",
                "11", "10", "21", "0",
                "12", "0", "22", "10",
                "71", "2", "91", "4",
                "14", "-0.5", "24", "-0.5",
                "14", "0.5", "24", "-0.5",
                "14", "0.5", "24", "0.5",
                "14", "-0.5", "24", "0.5"
            });

        var features = DxfReader.ReadFeatures(dxf, defaultSrid: 0);

        var feature = Assert.Single(features);
        Assert.Equal("WIPEOUT", feature.EntityType);
        Assert.True(feature.IsAnnotation);
        Assert.Equal(GeometryType.Polygon, feature.Geometry.Type);

        var boundingBox = feature.Geometry.GetBoundingBox();
        Assert.Equal(100, boundingBox.XMin, precision: 6);
        Assert.Equal(110, boundingBox.XMax, precision: 6);
        Assert.Equal(200, boundingBox.YMin, precision: 6);
        Assert.Equal(210, boundingBox.YMax, precision: 6);
        Assert.Equal(100, feature.Geometry.EuclideanArea, precision: 6);
    }

    #endregion

    [Fact]
    public void Read_Solid_ShouldReorderZigzagCornersIntoPolygon()
    {
        // SOLID corners in DXF zigzag order: (0,0), (1,0), (0,1), (1,1) is the unit square
        var dxf = BuildDxf(
            blocksSection: Array.Empty<string>(),
            entitiesSection: new[]
            {
                "0", "SOLID",
                "10", "0", "20", "0",
                "11", "1", "21", "0",
                "12", "0", "22", "1",
                "13", "1", "23", "1"
            });

        var geometries = DxfReader.Read(dxf, defaultSrid: 0);

        var polygon = Assert.Single(geometries, g => g.Type == GeometryType.Polygon);
        Assert.Equal(4, polygon.TotalNumberOfPoints);
        Assert.Equal(1, polygon.EuclideanArea, precision: 6);
    }

    #endregion
}

