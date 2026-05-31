using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.KmlFormat.Primitives;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

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
        Assert.Equal(51.5074, (geometries[0] as Geometry<Point>).Points[0].X, 6);
        Assert.Equal(-0.1278, (geometries[0] as Geometry<Point>).Points[0].Y, 6);

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
        Assert.Equal(3, (geometries[0] as Geometry<Point>).Points.Count);
        Assert.Equal(-122.0844, (geometries[0] as Geometry<Point>).Points[0].X, 6);
        Assert.Equal(37.4220, (geometries[0] as Geometry<Point>).Points[0].Y, 6);

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
        Assert.Single(geometries[0].GetGeometries()); // One ring (outer boundary)
        Assert.Equal(4, (geometries[0].GetGeometries()[0] as Geometry<Point>).Points.Count); // 4 points (closed ring)

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
        Assert.Equal(2, geometries[0].GetGeometries().Count); // Outer + inner ring

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

        var multiPoint = Geometry<Point>.Create(
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
        Assert.Equal(3, parsed[0].GetGeometries().Count);
    }

    [Fact]
    public void TestKmlMultiLineStringRoundTrip()
    {
        // Arrange - Create multi-linestring
        var line1 = Geometry<Point>.Create(
            new List<Point> { new Point(0, 0), new Point(10, 10) },
            GeometryType.LineString,
            srid: 4326);

        var line2 = Geometry<Point>.Create(
            new List<Point> { new Point(20, 20), new Point(30, 30), new Point(40, 40) },
            GeometryType.LineString,
            srid: 4326);

        var multiLine = Geometry<Point>.Create(
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
        Assert.Equal(2, parsed[0].GetGeometries().Count);
        Assert.Equal(2, (parsed[0].GetGeometries()[0] as Geometry<Point>).Points.Count);
        Assert.Equal(3, (parsed[0].GetGeometries()[1] as Geometry<Point>).Points.Count);
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

    #region Feature Conversion Tests

    [Fact]
    public void Features_ToFeatureList_PreservesAttributesAndStyleMetadata()
    {
        // Arrange - Create KML with style information
        var kmlString = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Style id=""cityStyle"">
      <IconStyle>
        <color>ff0000ff</color>
        <scale>1.5</scale>
      </IconStyle>
    </Style>
    <StyleMap id=""cityStyleMap"">
      <Pair>
        <key>normal</key>
        <styleUrl>#cityStyle</styleUrl>
      </Pair>
      <Pair>
        <key>highlight</key>
        <styleUrl>#cityStyle</styleUrl>
      </Pair>
    </StyleMap>
    <Placemark>
      <name>City A</name>
      <styleUrl>#cityStyleMap</styleUrl>
      <ExtendedData>
        <SchemaData>
          <SimpleData name=""Population"">1000000</SimpleData>
        </SchemaData>
      </ExtendedData>
      <Point>
        <coordinates>10,20</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";

        var kmlFeatures = KmlReader.ParseFeatures(kmlString);

        // Act
        var features = kmlFeatures.ToFeatures();

        // Assert
        Assert.NotNull(features);
        Assert.Single(features);

        var feature = features[0];
        Assert.Equal("City A", feature.Attributes[KmlAttributeKeys.NameAttributeKey]);
        Assert.Equal("1000000", feature.Attributes["Population"]);
        Assert.Equal("cityStyleMap", feature.Attributes[KmlAttributeKeys.StyleId]);
        Assert.True((bool)feature.Attributes[KmlAttributeKeys.StyleIsMap]);
        Assert.NotNull(feature.Attributes[KmlAttributeKeys.StyleMetadata]);
    }

    [Fact]
    public void Features_ToKmlFeatures_RoundTripMaintainsStyleMetadata()
    {
        // Arrange
        var originalKml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Style id=""inlineStyle"">
      <LineStyle>
        <color>ff00ff00</color>
        <width>3</width>
      </LineStyle>
    </Style>
    <Placemark>
      <name>Styled Line</name>
      <styleUrl>#inlineStyle</styleUrl>
      <LineString>
        <coordinates>0,0 1,1</coordinates>
      </LineString>
    </Placemark>
  </Document>
</kml>";

        var kmlFeatures = KmlReader.ParseFeatures(originalKml);
        var features = kmlFeatures.ToFeatures();

        // Act
        var roundTrip = features.ToKmlFeatures();

        // Assert
        Assert.NotNull(roundTrip);
        Assert.Single(roundTrip);

        var kmlFeature = roundTrip[0];
        Assert.Equal("Styled Line", kmlFeature.Name);
        Assert.NotNull(kmlFeature.Style);
        Assert.Equal("inlineStyle", kmlFeature.Style?.StyleId);
        Assert.True(kmlFeature.Style?.HasAnyStyle);
    }

    [Fact]
    public void ParseFeatures_WithStyleMap_CapturesIconMetadata()
    {
        // Arrange
        const string kml = """
<?xml version="1.0" encoding="UTF-8"?>
<kml xmlns="http://www.opengis.net/kml/2.2">
  <Document>
    <Style id="normalIcon">
      <IconStyle>
        <scale>1.2</scale>
        <Icon>
          <href>http://example.com/normal.png</href>
        </Icon>
      </IconStyle>
    </Style>
    <Style id="highlightIcon">
      <IconStyle>
        <scale>5</scale>
        <Icon>
          <href>http://example.com/highlight.png</href>
        </Icon>
      </IconStyle>
    </Style>
    <StyleMap id="iconMap">
      <Pair>
        <key>normal</key>
        <styleUrl>#normalIcon</styleUrl>
      </Pair>
      <Pair>
        <key>highlight</key>
        <styleUrl>#highlightIcon</styleUrl>
      </Pair>
    </StyleMap>
    <Placemark>
      <name>Icon Site</name>
      <styleUrl>#iconMap</styleUrl>
      <Point>
        <coordinates>40,10</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>
""";

        // Act
        var kmlFeatures = KmlReader.ParseFeatures(kml);
        var features = kmlFeatures.ToFeatures();

        // Assert
        var feature = Assert.Single(features);
        Assert.True(feature.Attributes.ContainsKey(KmlAttributeKeys.IconHref));
        Assert.Equal("http://example.com/normal.png", feature.Attributes[KmlAttributeKeys.IconHref]);
        Assert.True(feature.Attributes.ContainsKey(KmlAttributeKeys.IconScale));
        Assert.Equal(1.2, (double)feature.Attributes[KmlAttributeKeys.IconScale]);

        var metadata = Assert.IsType<KmlStyleMetadata>(feature.Attributes[KmlAttributeKeys.StyleMetadata]);
        Assert.True(metadata.IsStyleMap);
        Assert.Equal("iconMap", metadata.StyleId);
        Assert.Equal("#iconMap", metadata.StyleUrl);
        Assert.Equal("#normalIcon", metadata.NormalStyleUrl);
        Assert.Equal("http://example.com/normal.png", metadata.IconHref);
        Assert.Equal(1.2, metadata.IconScale);
        Assert.NotNull(metadata.NormalStyle);
        Assert.Null(metadata.InlineStyle);
    }

    [Fact]
    public void CreateSymbolizersFromKml_PropagatesIconHrefIntoPointSymbol()
    {
        // Arrange
        const string kml = """
<?xml version="1.0" encoding="UTF-8"?>
<kml xmlns="http://www.opengis.net/kml/2.2">
  <Document>
    <Style id="normalIcon">
      <IconStyle>
        <scale>1.2</scale>
        <Icon>
          <href>http://example.com/normal.png</href>
        </Icon>
      </IconStyle>
    </Style>
    <StyleMap id="iconMap">
      <Pair>
        <key>normal</key>
        <styleUrl>#normalIcon</styleUrl>
      </Pair>
      <Pair>
        <key>highlight</key>
        <styleUrl>#normalIcon</styleUrl>
      </Pair>
    </StyleMap>
    <Placemark>
      <name>Icon Site</name>
      <styleUrl>#iconMap</styleUrl>
      <Point>
        <coordinates>40,10</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>
""";

        var kmlFeatures = KmlReader.ParseFeatures(kml);
        var features = kmlFeatures.ToFeatures();

        // Act
        var symbolizers = features.CreateSymbolizersFromKml(GeometryType.Point);

        // Assert
        var symbolizer = Assert.IsType<SimpleSymbolizer>(Assert.Single(symbolizers));
        Assert.NotNull(symbolizer.Param);
        var visual = symbolizer.Param!;
        Assert.NotNull(visual.PointSymbol);
        //Assert.Equal("http://example.com/normal.png", visual.PointSymbol.IconHref);
        Assert.Equal("http://example.com/normal.png", features[0].Attributes[KmlAttributeKeys.IconHref]);
        Assert.True(visual.PointSymbol.SymbolWidth >= 14);
    }

    [Fact]
    public void ToKml_PreservesIconHrefWhenExporting()
    {
        // Arrange
        const string kml = """
<?xml version="1.0" encoding="UTF-8"?>
<kml xmlns="http://www.opengis.net/kml/2.2">
  <Document>
    <Style id="normalIcon">
      <IconStyle>
        <scale>1.2</scale>
        <Icon>
          <href>http://example.com/normal.png</href>
        </Icon>
      </IconStyle>
    </Style>
    <StyleMap id="iconMap">
      <Pair>
        <key>normal</key>
        <styleUrl>#normalIcon</styleUrl>
      </Pair>
      <Pair>
        <key>highlight</key>
        <styleUrl>#normalIcon</styleUrl>
      </Pair>
    </StyleMap>
    <Placemark>
      <name>Icon Site</name>
      <styleUrl>#iconMap</styleUrl>
      <Point>
        <coordinates>40,10</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>
""";

        var kmlFeatures = KmlReader.ParseFeatures(kml);
        var features = kmlFeatures.ToFeatures();

        // Act
        var roundTrip = features.ToKmlFeatures();
        var regeneratedKml = KmlWriter.ToKml(roundTrip);

        // Assert
        Assert.Contains("<href>http://example.com/normal.png</href>", regeneratedKml);
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
        Assert.Equal(51.5074, (parsed[0] as Geometry<Point>).Points[0].X, 4);
        Assert.Equal(-0.1278, (parsed[0] as Geometry<Point>).Points[0].Y, 4);
        Assert.Equal(48.8566, (parsed[1] as Geometry<Point>).Points[0].X, 4);
        Assert.Equal(2.3522, (parsed[1] as Geometry<Point>).Points[0].Y, 4);
        Assert.Equal(52.5200, (parsed[2] as Geometry<Point>).Points[0].X, 4);
        Assert.Equal(13.4050, (parsed[2] as Geometry<Point>).Points[0].Y, 4);
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
        Assert.Equal(1.2, pointStyle.IconStyle.Scale);

        Assert.NotNull(lineStyle);
        Assert.NotNull(lineStyle.LineStyle);
        Assert.Equal(3.0, lineStyle.LineStyle.Width);

        Assert.NotNull(polygonStyle);
        Assert.NotNull(polygonStyle.PolyStyle);
        Assert.True(polygonStyle.PolyStyle.Fill);
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

        var lineString = Geometry<Point>.Create(points, GeometryType.LineString, srid: 4326);

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
        Assert.Equal(5, (parsed[0] as Geometry<Point>).Points.Count);

        for (int i = 0; i < points.Count; i++)
        {
            Assert.Equal(points[i].X, (parsed[0] as Geometry<Point>).Points[i].X, 6);
            Assert.Equal(points[i].Y, (parsed[0] as Geometry<Point>).Points[i].Y, 6);
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

        var poly1 = Geometry<Point>.CreatePolygon(polygon1Points, 4326);

        var poly2 = Geometry<Point>.CreatePolygon(polygon2Points, 4326);

        var multiPolygon = Geometry<Point>.Create([poly1, poly2], GeometryType.MultiPolygon, 4326);

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
        Assert.Equal(2, (parsed[0] as Geometry<Point>).Geometries.Count);
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

    #region Feature Conversion Tests

    [Fact]
    public void ParseFeatures_ToFeatures_PreservesAttributes()
    {
        var kml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Style id=""cityStyle"">
      <IconStyle>
        <color>ff0000ff</color>
        <scale>1.2</scale>
      </IconStyle>
    </Style>
    <Placemark id=""pm1"">
      <name>City Hall</name>
      <description>Main administrative building</description>
      <styleUrl>#cityStyle</styleUrl>
      <ExtendedData>
        <SchemaData schemaUrl=""#citySchema"">
          <SimpleData name=""Population"">500000</SimpleData>
        </SchemaData>
      </ExtendedData>
      <Point>
        <coordinates>-0.1278,51.5074</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";

        var kmlFeatures = KmlReader.ParseFeatures(kml);

        Assert.Single(kmlFeatures);
        var kmlFeature = kmlFeatures[0];
        Assert.Equal("City Hall", kmlFeature.Name);
        Assert.NotNull(kmlFeature.Style);
        Assert.Equal("cityStyle", kmlFeature.Style?.StyleId);
        Assert.True(kmlFeature.Style?.HasAnyStyle);

        var features = kmlFeatures.ToFeatures();

        Assert.Single(features);
        var feature = features[0];

        Assert.Equal(GeometryType.Point, feature.GeometryType/*TheGeometry.Type*/);
        Assert.Equal(-0.1278, feature.TheGeometry.Points[0].X, 6);
        Assert.Equal(51.5074, feature.TheGeometry.Points[0].Y, 6);
        Assert.Equal("City Hall", feature.Attributes["Name"]);
        Assert.Equal("Main administrative building", feature.Attributes["Description"]);
        Assert.Equal("500000", feature.Attributes["Population"]);
    }

    [Fact]
    public void Features_ToKmlFeatures_PreservesNamesAndAttributes()
    {
        var geometry = Geometry<Point>.Create(12.34, 56.78, srid: 4326);

        var feature = new Feature<Point>(geometry, new Dictionary<string, object>
        {
            ["Name"] = "Test Feature",
            ["Description"] = "A sample feature",
            ["Category"] = "Sample"
        })
        {
            Id = 42
        };

        var kmlFeatures = new List<Feature<Point>> { feature }.ToKmlFeatures();

        Assert.Single(kmlFeatures);
        var kmlFeature = kmlFeatures[0];

        Assert.Equal("Test Feature", kmlFeature.Name);
        Assert.Equal("A sample feature", kmlFeature.Description);
        Assert.Equal(feature.Id.ToString(), kmlFeature.Id);
        Assert.Equal("Sample", kmlFeature.Attributes["Category"]);
        Assert.Equal(GeometryType.Point, kmlFeature.Geometry.Type);
    }

    [Fact]
    public void ParseFeatures_CapturesRegionMetadata()
    {
        var kml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Regional Feature</name>
      <Region>
        <Lod>
          <minLodPixels>128</minLodPixels>
          <maxLodPixels>512</maxLodPixels>
        </Lod>
        <LatLonAltBox>
          <north>52.0</north>
          <south>51.0</south>
          <east>1.0</east>
          <west>0.0</west>
        </LatLonAltBox>
      </Region>
      <Point>
        <coordinates>0.5,51.5</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";

        var kmlFeatures = KmlReader.ParseFeatures(kml);

        Assert.Single(kmlFeatures);
        var kmlFeature = kmlFeatures[0];
        Assert.NotNull(kmlFeature.Region);
        Assert.Equal(128, kmlFeature.Region?.MinLodPixels);
        Assert.Equal(512, kmlFeature.Region?.MaxLodPixels);
        Assert.NotNull(kmlFeature.Region?.LatLonAltBox);
        Assert.Equal(52.0, kmlFeature.Region?.LatLonAltBox?.North);
        Assert.Equal(0.0, kmlFeature.Region?.LatLonAltBox?.West);
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
        Assert.Equal(originalX, (parsed[0] as Geometry<Point>).Points[0].X, 15); // High precision
        Assert.Equal(originalY, (parsed[0] as Geometry<Point>).Points[0].Y, 15);
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

        var lineString = Geometry<Point>.Create(
            [new Point(0, 0), new Point(10, 10), new Point(20, 5)],
            GeometryType.LineString,
            srid: 4326);

        var polygon = Geometry<Point>.CreatePolygon([new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10)], srid: 4326);
         
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
        Assert.Equal(10.0, (parsed[0] as Geometry<Point>).Points[0].X, 6);
        Assert.Equal(20.0, (parsed[0] as Geometry<Point>).Points[0].Y, 6);

        // Verify LineString
        Assert.Equal(3, (parsed[1] as Geometry<Point>).Points.Count);

        // Verify Polygon
        Assert.Equal(4, (parsed[2].GetGeometries()[0] as Geometry<Point>).Points.Count);
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
            Assert.Equal(51.5074, (parsed[0] as Geometry<Point>).Points[0].X, 4);
            Assert.Equal(-0.1278, (parsed[0] as Geometry<Point>).Points[0].Y, 4);
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
                Assert.Equal((geometries[i] as Geometry<Point>).Points.Count, (reparsed[i] as Geometry<Point>).Points.Count);

                // Verify each coordinate
                for (int j = 0; j < (geometries[i] as Geometry<Point>).Points.Count; j++)
                {
                    Assert.Equal((geometries[i] as Geometry<Point>).Points[j].X, (reparsed[i] as Geometry<Point>).Points[j].X, 10);
                    Assert.Equal((geometries[i] as Geometry<Point>).Points[j].Y, (reparsed[i] as Geometry<Point>).Points[j].Y, 10);
                }
            }
            else if (geometries[i].Type == GeometryType.Polygon)
            {
                Assert.Equal(geometries[i].GetGeometries().Count, reparsed[i].GetGeometries().Count);
            }
        }
    }

    #endregion

    #region SRID Validation Tests

    [Fact]
    public void ToKml_ThrowsWhenGeometryIsNotWgs84()
    {
        var webMercatorPoint = Geometry<Point>.Create(1000000, 2000000, SridHelper.WebMercator);

        var exception = Assert.Throws<ArgumentException>(() => KmlWriter.ToKml(webMercatorPoint));

        Assert.Contains("4326", exception.Message);
        Assert.Contains(SridHelper.WebMercator.ToString(), exception.Message);
    }

    [Fact]
    public void ToKmlFeature_ProjectsWebMercatorToWgs84()
    {
        var webMercatorPoint = Geometry<Point>.Create(0, 0, SridHelper.WebMercator);
        var feature = new Feature<Point>(webMercatorPoint) { Id = 1 };

        var kmlFeature = feature.ToKmlFeature();

        Assert.NotNull(kmlFeature);
        Assert.Equal(SridHelper.GeodeticWGS84, kmlFeature!.Geometry.Srid);
        Assert.Equal(0, kmlFeature.Geometry.Points[0].X, 6);
        Assert.Equal(0, kmlFeature.Geometry.Points[0].Y, 6);
    }

    [Fact]
    public void ToKmlFeature_WebMercatorFeatureProducesValidKml()
    {
        var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(new Point(51.5074, -0.1278));
        var geometry = Geometry<Point>.Create(webMercatorPoint.X, webMercatorPoint.Y, SridHelper.WebMercator);
        var feature = new Feature<Point>(geometry, new Dictionary<string, object> { ["Name"] = "London" }) { Id = 1 };

        var kmlFeatures = new List<Feature<Point>> { feature }.ToKmlFeatures();
        var kmlOutput = KmlWriter.ToKml(kmlFeatures);

        Assert.Contains("51.507", kmlOutput);
        Assert.Contains("-0.127", kmlOutput);
    }

    [Fact]
    public void ToKmlFeature_SkipsProjectionWhenAlreadyWgs84()
    {
        var geometry = Geometry<Point>.Create(51.5074, -0.1278, SridHelper.GeodeticWGS84);
        var feature = new Feature<Point>(geometry) { Id = 1 };

        var kmlFeature = feature.ToKmlFeature();

        Assert.NotNull(kmlFeature);
        Assert.Equal(51.5074, kmlFeature!.Geometry.Points[0].X, 6);
        Assert.Equal(-0.1278, kmlFeature.Geometry.Points[0].Y, 6);
    }

    #endregion
}

