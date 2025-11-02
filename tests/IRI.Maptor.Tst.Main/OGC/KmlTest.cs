using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using IRI.Maptor.Extensions;
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Ket.KmlFormat.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IRI.Maptor.Tst.Standards.OGC.KML;

/// <summary>
/// Tests for KML import/export functionality
/// Tests round-trip conversion: KML string → Geometry<Point> → KML string
/// </summary>
public class KmlTest
{
    #region Point Tests
     

    [Fact]
    public void TestKmlPointRoundTrip()
    {
        // Arrange - Create KML point string
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Test Point</name>
      <description>A test point</description>
      <Point>
        <coordinates>51.5074,-0.1278</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";

        // Act - Parse KML to geometry
        var geometries = KmlReader.Parse(kmlString, targetSrid: 4326);

        // Assert - Verify parsing
        Assert.NotNull(geometries);
        Assert.Single(geometries);
        Assert.Equal(GeometryType.Point, geometries[0].Type);
        Assert.Equal(51.5074, geometries[0].Points[0].X, 6);
        Assert.Equal(-0.1278, geometries[0].Points[0].Y, 6);

        // Act - Convert back to KML
        var kmlOutput = KmlWriter.ToKml(geometries[0], "Test Point", "A test point");

        // Assert - Verify output contains expected elements
        Assert.Contains("<Point>", kmlOutput);
        Assert.Contains("<coordinates>", kmlOutput);
        // Check that coordinates are approximately correct (allowing for floating point precision)
        Assert.Contains("51.507", kmlOutput);
        Assert.Contains("-0.127", kmlOutput);
    }

    #endregion

    #region LineString Tests

    [Fact]
    public void TestKmlLineStringRoundTrip()
    {
        // Arrange - Create KML linestring
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Test Line</name>
      <LineString>
        <coordinates>-122.0844,37.4220 -122.0856,37.4220 -122.0856,37.4230</coordinates>
      </LineString>
    </Placemark>
  </Document>
</kml>";

        // Act - Parse KML
        var geometries = KmlReader.Parse(kmlString);

        // Assert - Verify parsing
        Assert.NotNull(geometries);
        Assert.Single(geometries);
        Assert.Equal(GeometryType.LineString, geometries[0].Type);
        Assert.Equal(3, geometries[0].Points.Count);
        Assert.Equal(-122.0844, geometries[0].Points[0].X, 6);
        Assert.Equal(37.4220, geometries[0].Points[0].Y, 6);

        // Act - Convert back to KML
        var kmlOutput = KmlWriter.ToKml(geometries[0], "Test Line");

        // Assert - Verify output
        Assert.Contains("<LineString>", kmlOutput);
        Assert.Contains("<coordinates>", kmlOutput);
        Assert.Contains("-122.0844", kmlOutput);
    }

    #endregion

    #region Polygon Tests

    [Fact]
    public void TestKmlPolygonRoundTrip()
    {
        // Arrange - Create KML polygon (simple rectangle)
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Test Polygon</name>
      <Polygon>
        <outerBoundaryIs>
          <LinearRing>
            <coordinates>
              0,0 10,0 10,10 0,10 0,0
            </coordinates>
          </LinearRing>
        </outerBoundaryIs>
      </Polygon>
    </Placemark>
  </Document>
</kml>";

        // Act - Parse KML
        var geometries = KmlReader.Parse(kmlString);

        // Assert - Verify parsing
        Assert.NotNull(geometries);
        Assert.Single(geometries);
        Assert.Equal(GeometryType.Polygon, geometries[0].Type);
        Assert.Single(geometries[0].Geometries); // One ring (outer boundary)
        Assert.Equal(5, geometries[0].Geometries[0].Points.Count); // 5 points (closed ring)

        // Act - Convert back to KML
        var kmlOutput = KmlWriter.ToKml(geometries[0], "Test Polygon");

        // Assert - Verify output
        Assert.Contains("<Polygon>", kmlOutput);
        Assert.Contains("<outerBoundaryIs>", kmlOutput);
        Assert.Contains("<LinearRing>", kmlOutput);
        Assert.Contains("<coordinates>", kmlOutput);
    }

    [Fact]
    public void TestKmlPolygonWithHoleRoundTrip()
    {
        // Arrange - Create KML polygon with hole
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Polygon with Hole</name>
      <Polygon>
        <outerBoundaryIs>
          <LinearRing>
            <coordinates>0,0 20,0 20,20 0,20 0,0</coordinates>
          </LinearRing>
        </outerBoundaryIs>
        <innerBoundaryIs>
          <LinearRing>
            <coordinates>5,5 15,5 15,15 5,15 5,5</coordinates>
          </LinearRing>
        </innerBoundaryIs>
      </Polygon>
    </Placemark>
  </Document>
</kml>";

        // Act - Parse KML
        var geometries = KmlReader.Parse(kmlString);

        // Assert - Verify parsing
        Assert.NotNull(geometries);
        Assert.Single(geometries);
        Assert.Equal(GeometryType.Polygon, geometries[0].Type);
        Assert.Equal(2, geometries[0].Geometries.Count); // Outer + inner ring

        // Act - Convert back to KML
        var kmlOutput = KmlWriter.ToKml(geometries[0], "Polygon with Hole");

        // Assert - Verify output has both boundaries
        Assert.Contains("<outerBoundaryIs>", kmlOutput);
        Assert.Contains("<innerBoundaryIs>", kmlOutput);
    }

    #endregion

    #region MultiGeometry Tests

    [Fact]
    public void TestKmlMultiPointRoundTrip()
    {
        // Arrange - Create geometry
        var point1 = Geometry<Point>.Create(10.0, 20.0, srid: 4326);
        var point2 = Geometry<Point>.Create(30.0, 40.0, srid: 4326);
        var point3 = Geometry<Point>.Create(50.0, 60.0, srid: 4326);

        var multiPoint = new Geometry<Point>(
            new List<Geometry<Point>> { point1, point2, point3 },
            GeometryType.MultiPoint,
            srid: 4326);

        // Act - Convert to KML
        var kmlOutput = KmlWriter.ToKml(multiPoint, "Multi Point Test");

        // Assert - Verify KML output
        Assert.Contains("<MultiGeometry>", kmlOutput);
        Assert.Contains("<Point>", kmlOutput);

        // Act - Parse back
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify round-trip
        Assert.NotNull(parsed);
        Assert.Single(parsed);
        Assert.Equal(GeometryType.MultiPoint, parsed[0].Type);
        Assert.Equal(3, parsed[0].Geometries.Count);
    }

    [Fact]
    public void TestKmlMultiLineStringRoundTrip()
    {
        // Arrange - Create multi-linestring
        var line1 = new Geometry<Point>(
            new List<Point> { new Point(0, 0), new Point(10, 10) },
            GeometryType.LineString,
            srid: 4326);

        var line2 = new Geometry<Point>(
            new List<Point> { new Point(20, 20), new Point(30, 30), new Point(40, 40) },
            GeometryType.LineString,
            srid: 4326);

        var multiLine = new Geometry<Point>(
            new List<Geometry<Point>> { line1, line2 },
            GeometryType.MultiLineString,
            srid: 4326);

        // Act - Convert to KML
        var kmlOutput = KmlWriter.ToKml(multiLine, "Multi Line Test");

        // Assert - Verify KML output
        Assert.Contains("<MultiGeometry>", kmlOutput);
        Assert.Contains("<LineString>", kmlOutput);

        // Act - Parse back
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify round-trip
        Assert.NotNull(parsed);
        Assert.Single(parsed);
        Assert.Equal(GeometryType.MultiLineString, parsed[0].Type);
        Assert.Equal(2, parsed[0].Geometries.Count);
        Assert.Equal(2, parsed[0].Geometries[0].Points.Count);
        Assert.Equal(3, parsed[0].Geometries[1].Points.Count);
    }

    #endregion

    #region Feature with Attributes Tests

    [Fact]
    public void TestKmlFeaturesWithAttributesRoundTrip()
    {
        // Arrange - Create KML with extended data
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>London</name>
      <description>Capital of England</description>
      <ExtendedData>
        <SchemaData>
          <SimpleData name=""Population"">9000000</SimpleData>
          <SimpleData name=""Country"">UK</SimpleData>
          <SimpleData name=""Founded"">43 AD</SimpleData>
        </SchemaData>
      </ExtendedData>
      <Point>
        <coordinates>51.5074,-0.1278</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";

        // Act - Parse features with attributes
        var features = KmlReader.ParseFeatures(kmlString);

        // Assert - Verify parsing
        Assert.NotNull(features);
        Assert.Single(features);
        Assert.Equal("London", features[0].Name);
        Assert.Equal("Capital of England", features[0].Description);
        Assert.Equal(3, features[0].Attributes.Count);
        Assert.Equal("9000000", features[0].Attributes["Population"]);
        Assert.Equal("UK", features[0].Attributes["Country"]);
        Assert.Equal("43 AD", features[0].Attributes["Founded"]);

        // Act - Convert back to KML
        var kmlOutput = KmlWriter.ToKml(features, "Cities");

        // Assert - Verify output
        Assert.Contains("<name>London</name>", kmlOutput);
        Assert.Contains("<ExtendedData>", kmlOutput);
        Assert.Contains("<SimpleData name=\"Population\">9000000</SimpleData>", kmlOutput);
    }

    #endregion

    #region Multiple Features Tests

    [Fact]
    public void TestKmlMultipleFeaturesRoundTrip()
    {
        // Arrange - Create multiple geometries
        var geometries = new List<Geometry<Point>>
        {
            Geometry<Point>.Create(51.5074, -0.1278, srid: 4326), // London
            Geometry<Point>.Create(48.8566, 2.3522, srid: 4326),  // Paris
            Geometry<Point>.Create(52.5200, 13.4050, srid: 4326)  // Berlin
        };

        // Act - Convert to KML
        var kmlOutput = KmlWriter.ToKml(geometries, "European Capitals");

        // Assert - Verify KML output
        Assert.Contains("<Document>", kmlOutput);
        Assert.Contains("<name>European Capitals</name>", kmlOutput);

        // Act - Parse back
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify round-trip
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Count);
        Assert.All(parsed, g => Assert.Equal(GeometryType.Point, g.Type));

        // Verify coordinates
        Assert.Equal(51.5074, parsed[0].Points[0].X, 4);
        Assert.Equal(-0.1278, parsed[0].Points[0].Y, 4);
        Assert.Equal(48.8566, parsed[1].Points[0].X, 4);
        Assert.Equal(2.3522, parsed[1].Points[0].Y, 4);
        Assert.Equal(52.5200, parsed[2].Points[0].X, 4);
        Assert.Equal(13.4050, parsed[2].Points[0].Y, 4);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void TestKmlValidation_ValidKml()
    {
        // Arrange
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Valid Point</name>
      <Point>
        <coordinates>51.5074,-0.1278</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";

        // Act
        var isValid = KmlValidator.Validate(kmlString, out var errors, out var warnings);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void TestKmlValidation_InvalidCoordinates()
    {
        // Arrange - Invalid longitude (> 180)
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <Point>
        <coordinates>200.0,50.0</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";

        // Act
        var isValid = KmlValidator.Validate(kmlString, out var errors, out var warnings);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void TestCoordinateValidation()
    {
        // Valid coordinates
        Assert.True(KmlValidator.ValidateCoordinates(51.5074, -0.1278));
        Assert.True(KmlValidator.ValidateCoordinates(-180, -90));
        Assert.True(KmlValidator.ValidateCoordinates(180, 90));

        // Invalid coordinates
        Assert.False(KmlValidator.ValidateCoordinates(181, 0));  // Longitude > 180
        Assert.False(KmlValidator.ValidateCoordinates(-181, 0)); // Longitude < -180
        Assert.False(KmlValidator.ValidateCoordinates(0, 91));   // Latitude > 90
        Assert.False(KmlValidator.ValidateCoordinates(0, -91));  // Latitude < -90
    }

    #endregion

    #region Style Tests

    [Fact]
    public void TestKmlStyleBuilder()
    {
        // Arrange - Create various styles
        var pointStyle = new KmlStyleBuilder()
            .WithIconStyle(
                "http://maps.google.com/mapfiles/kml/pushpin/red-pushpin.png",
                scale: 1.2)
            .Build();

        var lineStyle = new KmlStyleBuilder()
            .WithLineStyle(red: 255, green: 0, blue: 0, width: 3.0)
            .Build();

        var polygonStyle = new KmlStyleBuilder()
            .WithPolyStyle(red: 0, green: 255, blue: 0, alpha: 128, fill: true, outline: true)
            .WithLineStyle(red: 0, green: 128, blue: 0, width: 2.0)
            .Build();

        // Assert - Verify styles are created
        Assert.NotNull(pointStyle);
        Assert.NotNull(pointStyle.IconStyle);
        Assert.Equal(1.2, pointStyle.IconStyle.scale);

        Assert.NotNull(lineStyle);
        Assert.NotNull(lineStyle.LineStyle);
        Assert.Equal(3.0, lineStyle.LineStyle.width);

        Assert.NotNull(polygonStyle);
        Assert.NotNull(polygonStyle.PolyStyle);
        Assert.True(polygonStyle.PolyStyle.fill);
    }

    [Fact]
    public void TestKmlColorConversion()
    {
        // Arrange & Act
        var red = KmlStyleBuilder.CreateKmlColor(255, 0, 0, 255);
        var semiTransparentGreen = KmlStyleBuilder.CreateKmlColor(0, 255, 0, 128);
        var blueFromHex = KmlStyleBuilder.CreateKmlColorFromHex("#0000FF");
        var semiTransparentYellow = KmlStyleBuilder.CreateKmlColorFromHex("#80FFFF00");

        // Assert - Verify KML color format (aabbggrr)
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, red); // Full opacity red
        Assert.Equal(new byte[] { 128, 0, 255, 0 }, semiTransparentGreen); // Semi-transparent green
        Assert.Equal(new byte[] { 255, 255, 0, 0 }, blueFromHex); // Full opacity blue
        Assert.Equal(new byte[] { 128, 0, 255, 255 }, semiTransparentYellow); // Semi-transparent yellow
    }

    #endregion

    #region Complex Geometry Tests

    [Fact]
    public void TestKmlComplexGeometryRoundTrip()
    {
        // Arrange - Create a complex linestring with multiple points
        var points = new List<Point>
        {
            new Point(-122.084, 37.422),
            new Point(-122.085, 37.422),
            new Point(-122.085, 37.423),
            new Point(-122.086, 37.423),
            new Point(-122.086, 37.424)
        };

        var lineString = new Geometry<Point>(points, GeometryType.LineString, srid: 4326);

        // Act - Convert to KML
        var kmlOutput = KmlWriter.ToKml(lineString, "Path", "A complex path");

        // Assert - Output is valid
        Assert.Contains("<LineString>", kmlOutput);
        Assert.Contains("<coordinates>", kmlOutput);

        // Act - Parse back
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify round-trip accuracy
        Assert.NotNull(parsed);
        Assert.Single(parsed);
        Assert.Equal(5, parsed[0].Points.Count);

        for (int i = 0; i < points.Count; i++)
        {
            Assert.Equal(points[i].X, parsed[0].Points[i].X, 6);
            Assert.Equal(points[i].Y, parsed[0].Points[i].Y, 6);
        }
    }

    [Fact]
    public void TestKmlMultiPolygonRoundTrip()
    {
        // Arrange - Create two simple polygons
        var polygon1Points = new List<Point>
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        };

        var polygon2Points = new List<Point>
        {
            new Point(20, 20),
            new Point(30, 20),
            new Point(30, 30),
            new Point(20, 30),
            new Point(20, 20)
        };

        var poly1 = new Geometry<Point>(
            new Geometry<Point>(polygon1Points, GeometryType.LineString, true, 4326),
            GeometryType.Polygon,
            4326);

        var poly2 = new Geometry<Point>(
            new Geometry<Point>(polygon2Points, GeometryType.LineString, true, 4326),
            GeometryType.Polygon,
            4326);

        var multiPolygon = new Geometry<Point>(
            new List<Geometry<Point>> { poly1, poly2 },
            GeometryType.MultiPolygon,
            4326);

        // Act - Convert to KML
        var kmlOutput = KmlWriter.ToKml(multiPolygon, "Two Polygons");

        // Assert - Verify KML output
        Assert.Contains("<MultiGeometry>", kmlOutput);
        Assert.Contains("<Polygon>", kmlOutput);

        // Act - Parse back
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify round-trip
        Assert.NotNull(parsed);
        Assert.Single(parsed);
        Assert.Equal(GeometryType.MultiPolygon, parsed[0].Type);
        Assert.Equal(2, parsed[0].Geometries.Count);
    }

    #endregion

    #region Folder Tests

    [Fact]
    public void TestKmlWithFoldersRoundTrip()
    {
        // Arrange - Create geometries organized in folders
        var cityPoints = new List<Geometry<Point>>
        {
            Geometry<Point>.Create(51.5074, -0.1278, srid: 4326), // London
            Geometry<Point>.Create(48.8566, 2.3522, srid: 4326)   // Paris
        };

        var landmarkPoints = new List<Geometry<Point>>
        {
            Geometry<Point>.Create(51.5007, -0.1246, srid: 4326), // Big Ben
            Geometry<Point>.Create(48.8584, 2.2945, srid: 4326)   // Eiffel Tower
        };

        var folders = new Dictionary<string, List<Geometry<Point>>>
        {
            ["Cities"] = cityPoints,
            ["Landmarks"] = landmarkPoints
        };

        // Act - Convert to KML with folders
        var kmlOutput = KmlWriter.ToKmlWithFolders(folders, "European Places");

        // Assert - Verify folder structure
        Assert.Contains("<Document>", kmlOutput);
        Assert.Contains("<Folder>", kmlOutput);
        Assert.Contains("<name>Cities</name>", kmlOutput);
        Assert.Contains("<name>Landmarks</name>", kmlOutput);

        // Act - Parse back
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify all geometries are parsed
        Assert.NotNull(parsed);
        Assert.Equal(4, parsed.Count); // 2 cities + 2 landmarks
    }

    #endregion

    #region Precision Tests

    [Fact]
    public void TestKmlCoordinatePrecision()
    {
        // Arrange - Create point with high precision
        var originalX = 51.507351987654321;
        var originalY = -0.127758123456789;

        var geometry = Geometry<Point>.Create(originalX, originalY, srid: 4326);

        // Act - Convert to KML and parse back
        var kmlOutput = KmlWriter.ToKml(geometry);
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify precision is maintained (G17 format)
        Assert.Equal(originalX, parsed[0].Points[0].X, 15); // High precision
        Assert.Equal(originalY, parsed[0].Points[0].Y, 15);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TestKmlEmptyDocument()
    {
        // Arrange - KML with empty document
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
  </Document>
</kml>";

        // Act
        var geometries = KmlReader.Parse(kmlString);

        // Assert - Should return empty list, not throw exception
        Assert.NotNull(geometries);
        Assert.Empty(geometries);
    }

    [Fact]
    public void TestKmlInvalidInput()
    {
        // Act & Assert - Null or empty string
        Assert.Throws<ArgumentException>(() => KmlReader.Parse(null!));
        Assert.Throws<ArgumentException>(() => KmlReader.Parse(""));
        Assert.Throws<ArgumentException>(() => KmlReader.Parse("   "));
    }

    [Fact]
    public void TestKmlInvalidXml()
    {
        // Arrange - Invalid XML
        var invalidKml = "<kml>This is not valid XML";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => KmlReader.Parse(invalidKml));
    }

    #endregion

    #region Complete Round-Trip Test

    [Fact]
    public void TestKmlCompleteRoundTrip_AllGeometryTypes()
    {
        // Arrange - Create one of each geometry type
        var point = Geometry<Point>.Create(10.0, 20.0, srid: 4326);
        
        var lineString = new Geometry<Point>(
            new List<Point> { new Point(0, 0), new Point(10, 10), new Point(20, 5) },
            GeometryType.LineString,
            srid: 4326);

        var polygonRing = new Geometry<Point>(
            new List<Point> { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) },
            GeometryType.LineString,
            true,
            srid: 4326);

        var polygon = new Geometry<Point>(polygonRing, GeometryType.Polygon, 4326);

        var geometries = new List<Geometry<Point>> { point, lineString, polygon };

        // Act - Convert to KML
        var kmlOutput = KmlWriter.ToKml(geometries, "All Types");

        // Verify KML is valid
        var isValid = KmlValidator.IsValid(kmlOutput);
        Assert.True(isValid, "Generated KML should be valid");

        // Act - Parse back
        var parsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify all geometries are preserved
        Assert.Equal(3, parsed.Count);
        Assert.Equal(GeometryType.Point, parsed[0].Type);
        Assert.Equal(GeometryType.LineString, parsed[1].Type);
        Assert.Equal(GeometryType.Polygon, parsed[2].Type);

        // Verify Point
        Assert.Equal(10.0, parsed[0].Points[0].X, 6);
        Assert.Equal(20.0, parsed[0].Points[0].Y, 6);

        // Verify LineString
        Assert.Equal(3, parsed[1].Points.Count);
        
        // Verify Polygon
        Assert.Equal(5, parsed[2].Geometries[0].Points.Count);
    }

    #endregion

    #region File I/O Tests

    [Fact]
    public async Task TestKmlFileReadWriteAsync()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), $"test_kml_{Guid.NewGuid()}.kml");
        var geometry = Geometry<Point>.Create(51.5074, -0.1278, srid: 4326);

        try
        {
            // Act - Write to file
            await KmlWriter.WriteToFileAsync(
                new List<Geometry<Point>> { geometry },
                testFilePath,
                "Test File");

            // Assert - File exists
            Assert.True(File.Exists(testFilePath));

            // Act - Read from file
            var parsed = await KmlReader.ReadFromFileAsync(testFilePath);

            // Assert - Verify content
            Assert.NotNull(parsed);
            Assert.Single(parsed);
            Assert.Equal(51.5074, parsed[0].Points[0].X, 4);
            Assert.Equal(-0.1278, parsed[0].Points[0].Y, 4);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    #endregion

    #region Coordinate Precision Round-Trip

    [Fact]
    public void TestKmlGeometryToKmlStringRoundTrip()
    {
        // This is the main test requested: Parse KML string → Geometry<Point> → KML string

        // Arrange - Original KML string with various geometry types
        var originalKml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <name>Round Trip Test</name>
    <Placemark>
      <name>Point Feature</name>
      <description>Test point for round-trip</description>
      <Point>
        <coordinates>-122.0822035425683,37.42228990140251</coordinates>
      </Point>
    </Placemark>
    <Placemark>
      <name>LineString Feature</name>
      <LineString>
        <coordinates>
          -122.08223,37.42254 -122.08219,37.42281 -122.08244,37.42292
        </coordinates>
      </LineString>
    </Placemark>
    <Placemark>
      <name>Polygon Feature</name>
      <Polygon>
        <outerBoundaryIs>
          <LinearRing>
            <coordinates>
              -122.084,37.4220 -122.085,37.4220 -122.085,37.4230 -122.084,37.4230 -122.084,37.4220
            </coordinates>
          </LinearRing>
        </outerBoundaryIs>
      </Polygon>
    </Placemark>
  </Document>
</kml>";

        // Act - Step 1: Parse KML string to Geometry<Point>
        var geometries = KmlReader.Parse(originalKml, targetSrid: 4326);

        // Assert - Verify parsing worked
        Assert.NotNull(geometries);
        Assert.Equal(3, geometries.Count);
        Assert.Equal(GeometryType.Point, geometries[0].Type);
        Assert.Equal(GeometryType.LineString, geometries[1].Type);
        Assert.Equal(GeometryType.Polygon, geometries[2].Type);

        // Act - Step 2: Convert Geometry<Point> back to KML string
        var kmlOutput = KmlWriter.ToKml(geometries, "Round Trip Test");

        // Assert - Verify output is valid KML
        Assert.NotNull(kmlOutput);
        Assert.Contains("<?xml version=\"1.0\"", kmlOutput);
        Assert.Contains("<kml", kmlOutput);
        Assert.Contains("<Document>", kmlOutput);
        Assert.Contains("<Placemark>", kmlOutput);

        // Validate the output KML
        var isValid = KmlValidator.Validate(kmlOutput, out var errors, out var warnings);
        Assert.True(isValid, $"Output KML should be valid. Errors: {string.Join(", ", errors)}");

        // Act - Step 3: Parse the output KML back to verify round-trip
        var reparsed = KmlReader.Parse(kmlOutput);

        // Assert - Verify geometries match
        Assert.Equal(geometries.Count, reparsed.Count);
        
        for (int i = 0; i < geometries.Count; i++)
        {
            Assert.Equal(geometries[i].Type, reparsed[i].Type);
            
            // Verify coordinate count
            if (geometries[i].Type == GeometryType.Point || geometries[i].Type == GeometryType.LineString)
            {
                Assert.Equal(geometries[i].Points.Count, reparsed[i].Points.Count);
                
                // Verify each coordinate
                for (int j = 0; j < geometries[i].Points.Count; j++)
                {
                    Assert.Equal(geometries[i].Points[j].X, reparsed[i].Points[j].X, 10);
                    Assert.Equal(geometries[i].Points[j].Y, reparsed[i].Points[j].Y, 10);
                }
            }
            else if (geometries[i].Type == GeometryType.Polygon)
            {
                Assert.Equal(geometries[i].Geometries.Count, reparsed[i].Geometries.Count);
            }
        }
    }

    #endregion
}

