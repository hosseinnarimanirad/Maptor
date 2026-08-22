using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using Xunit;

namespace IRI.Maptor.Tests.GeoJsons;

/// <summary>
/// Tests for GeoJSON geometry conversion and round-trip serialization.
/// </summary>
public class GeoJson_GeometryTest
{
    #region Helper Methods

    /// <summary>
    /// Normalizes a GeoJSON string by removing all spaces for consistent comparison.
    /// </summary>
    /// <param name="geoJson">The GeoJSON string to normalize.</param>
    /// <returns>A normalized GeoJSON string without spaces.</returns>
    private static string NormalizeGeoJson(string geoJson)
    {
        return geoJson.Replace(" ", string.Empty);
    }

    #endregion

    #region Point Tests

    /// <summary>
    /// Tests round-trip conversion for Point geometries.
    /// Verifies that Parse > AsGeoJson > Serialize preserves the original GeoJSON data.
    /// According to RFC 7946 Section 3.1.1, positions MUST have 2 or more elements, with the third element being elevation.
    /// Implementations SHOULD NOT extend position arrays beyond 3 elements.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON Point string to test.</param>
    [Theory]
    // Point (2D) - [longitude, latitude] - RFC 7946 compliant
    [InlineData("{\"type\":\"Point\",\"coordinates\":[30,10]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[0,0]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[-10.5,20.75]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[100,200]}")]
    // PointZ (3D) - [longitude, latitude, elevation] - RFC 7946 compliant (third element is elevation/altitude)
    [InlineData("{\"type\":\"Point\",\"coordinates\":[30,10,100]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[0,0,0]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[-10.5,20.75,500.25]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[100,200,1500]}")]
    // Extended coordinates (non-standard) - Testing implementation compatibility beyond RFC 7946
    // Note: RFC 7946 Section 3.1.1 states "Implementations SHOULD NOT extend position arrays beyond 3 elements"
    // These tests verify the implementation handles extended coordinates for compatibility with systems that use M or ZM
    [InlineData("{\"type\":\"Point\",\"coordinates\":[30,10,100,5]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[0,0,0,0]}")]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[-10.5,20.75,500.25,10.5]}")]
    public void TestPoint_RoundTrip_ShouldPreserveData(string geoJsonString)
    {
        // Arrange
        var normalizedInput = NormalizeGeoJson(geoJsonString);

        // Act
        var geometry = GeoJson.DeserializeGeometry(normalizedInput);
        var result = geometry.Parse(true).AsGeoJson(true).Serialize(false, true);

        // Assert
        Assert.Equal(normalizedInput, result);
    }

    #endregion

    #region LineString Tests

    /// <summary>
    /// Tests round-trip conversion for LineString geometries.
    /// Verifies that Parse > AsGeoJson > Serialize preserves the original GeoJSON data.
    /// Includes tests for 2D and 3D (with elevation) coordinates per RFC 7946.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON LineString string to test.</param>
    [Theory]
    // LineString (2D) - [longitude, latitude]
    [InlineData("{\"type\":\"LineString\",\"coordinates\":[[30.1,10.1],[10.1,30.1],[40.1,40.1]]}")]
    [InlineData("{\"type\":\"LineString\",\"coordinates\":[[0,0],[10,10]]}")]
    [InlineData("{\"type\":\"LineString\",\"coordinates\":[[1,1],[2,2],[3,3],[4,4]]}")]
    // LineStringZ (3D) - [longitude, latitude, elevation]
    [InlineData("{\"type\":\"LineString\",\"coordinates\":[[30.1,10.1,100],[10.1,30.1,200],[40.1,40.1,150]]}")]
    [InlineData("{\"type\":\"LineString\",\"coordinates\":[[0,0,0],[10,10,50],[20,20,100]]}")]
    [InlineData("{\"type\":\"LineString\",\"coordinates\":[[1,1,10],[2,2,20],[3,3,30]]}")]
    public void TestLineString_RoundTrip_ShouldPreserveData(string geoJsonString)
    {
        // Arrange
        var normalizedInput = NormalizeGeoJson(geoJsonString);

        // Act
        var geometry = GeoJson.DeserializeGeometry(normalizedInput);
        var result = geometry.Parse(true).AsGeoJson(true).Serialize(false, true);

        // Assert
        Assert.Equal(normalizedInput, result);
    }

    #endregion

    #region Polygon Tests

    /// <summary>
    /// Tests round-trip conversion for Polygon geometries.
    /// Verifies that Parse > AsGeoJson > Serialize preserves the original GeoJSON data.
    /// Includes tests for 2D and 3D (with elevation) coordinates per RFC 7946.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON Polygon string to test.</param>
    [Theory]
    // Polygon (2D) - [longitude, latitude]
    [InlineData("{\"type\":\"Polygon\",\"coordinates\":[[[30.1,10.1],[40.1,40.1],[20.1,40.1],[10.1,20.1],[30.1,10.1]]]}")]
    [InlineData("{\"type\":\"Polygon\",\"coordinates\":[[[35.1,10.1],[45.1,45.1],[15.1,40.1],[10.1,20.1],[35.1,10.1]],[[20.1,30.1],[35.1,35.1],[30.1,20.1],[20.1,30.1]]]}")]
    [InlineData("{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[30,0],[30,30],[0,30],[0,0]]]}")]
    // PolygonZ (3D) - [longitude, latitude, elevation]
    [InlineData("{\"type\":\"Polygon\",\"coordinates\":[[[30.1,10.1,100],[40.1,40.1,150],[20.1,40.1,120],[10.1,20.1,110],[30.1,10.1,100]]]}")]
    [InlineData("{\"type\":\"Polygon\",\"coordinates\":[[[0,0,0],[30,0,0],[30,30,0],[0,30,0],[0,0,0]]]}")]
    public void TestPolygon_RoundTrip_ShouldPreserveData(string geoJsonString)
    {
        // Arrange
        var normalizedInput = NormalizeGeoJson(geoJsonString);

        // Act
        var geometry = GeoJson.DeserializeGeometry(normalizedInput);
        var result = geometry.Parse(true).AsGeoJson(true).Serialize(false, true);

        // Assert
        Assert.Equal(normalizedInput, result);
    }

    #endregion

    #region MultiPoint Tests

    /// <summary>
    /// Tests round-trip conversion for MultiPoint geometries.
    /// Verifies that Parse > AsGeoJson > Serialize preserves the original GeoJSON data.
    /// Includes tests for 2D and 3D (with elevation) coordinates per RFC 7946.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON MultiPoint string to test.</param>
    [Theory]
    // MultiPoint (2D) - [longitude, latitude]
    [InlineData("{\"type\":\"MultiPoint\",\"coordinates\":[[10.1,40.1],[40.1,30.1],[20.1,20.1],[30.1,10.1]]}")]
    [InlineData("{\"type\":\"MultiPoint\",\"coordinates\":[[0,0],[10,10],[20,20]]}")]
    [InlineData("{\"type\":\"MultiPoint\",\"coordinates\":[[1,1],[2,2]]}")]
    // MultiPointZ (3D) - [longitude, latitude, elevation]
    [InlineData("{\"type\":\"MultiPoint\",\"coordinates\":[[10.1,40.1,100],[40.1,30.1,200],[20.1,20.1,150],[30.1,10.1,120]]}")]
    [InlineData("{\"type\":\"MultiPoint\",\"coordinates\":[[0,0,0],[10,10,50],[20,20,100]]}")]
    [InlineData("{\"type\":\"MultiPoint\",\"coordinates\":[[1,1,10],[2,2,20]]}")]
    public void TestMultiPoint_RoundTrip_ShouldPreserveData(string geoJsonString)
    {
        // Arrange
        var normalizedInput = NormalizeGeoJson(geoJsonString);

        // Act
        var geometry = GeoJson.DeserializeGeometry(normalizedInput);
        var result = geometry.Parse(true).AsGeoJson(true).Serialize(false, true);

        // Assert
        Assert.Equal(normalizedInput, result);
    }

    #endregion

    #region MultiLineString Tests

    /// <summary>
    /// Tests round-trip conversion for MultiLineString geometries.
    /// Verifies that Parse > AsGeoJson > Serialize preserves the original GeoJSON data.
    /// Includes tests for 2D and 3D (with elevation) coordinates per RFC 7946.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON MultiLineString string to test.</param>
    [Theory]
    // MultiLineString (2D) - [longitude, latitude]
    [InlineData("{\"type\":\"MultiLineString\",\"coordinates\":[[[10.1,10.1],[20.1,20.1],[10.1,40.1]],[[40.1,40.1],[30.1,30.1],[40.1,20.1],[30.1,10.1]]]}")]
    [InlineData("{\"type\":\"MultiLineString\",\"coordinates\":[[[0,0],[10,10]],[[20,20],[30,30]]]}")]
    [InlineData("{\"type\":\"MultiLineString\",\"coordinates\":[[[1,1],[2,2],[3,3]]]}")]
    // MultiLineStringZ (3D) - [longitude, latitude, elevation]
    [InlineData("{\"type\":\"MultiLineString\",\"coordinates\":[[[10.1,10.1,100],[20.1,20.1,150],[10.1,40.1,120]],[[40.1,40.1,200],[30.1,30.1,180],[40.1,20.1,160],[30.1,10.1,140]]]}")]
    [InlineData("{\"type\":\"MultiLineString\",\"coordinates\":[[[0,0,0],[10,10,50]],[[20,20,100],[30,30,150]]]}")]
    [InlineData("{\"type\":\"MultiLineString\",\"coordinates\":[[[1,1,10],[2,2,20],[3,3,30]]]}")]
    public void TestMultiLineString_RoundTrip_ShouldPreserveData(string geoJsonString)
    {
        // Arrange
        var normalizedInput = NormalizeGeoJson(geoJsonString);

        // Act
        var geometry = GeoJson.DeserializeGeometry(normalizedInput);
        var result = geometry.Parse(true).AsGeoJson(true).Serialize(false, true);

        // Assert
        Assert.Equal(normalizedInput, result);
    }

    #endregion

    #region MultiPolygon Tests

    /// <summary>
    /// Tests round-trip conversion for MultiPolygon geometries.
    /// Verifies that Parse > AsGeoJson > Serialize preserves the original GeoJSON data.
    /// Includes tests for 2D and 3D (with elevation) coordinates per RFC 7946.
    /// </summary>
    /// <param name="geoJsonString">The GeoJSON MultiPolygon string to test.</param>
    [Theory]
    // MultiPolygon (2D) - [longitude, latitude]
    [InlineData("{\"type\":\"MultiPolygon\",\"coordinates\":[[[[30.1,20.1],[45.1,40.1],[10.1,40.1],[30.1,20.1]]],[[[15.1,5.1],[40.1,10.1],[10.1,20.1],[5.1,10.1],[15.1,5.1]]]]}")]
    [InlineData("{\"type\":\"MultiPolygon\",\"coordinates\":[[[[40.1,40.1],[20.1,45.1],[45.1,30.1],[40.1,40.1]]],[[[20.1,35.1],[10.1,30.1],[10.1,10.1],[30.1,5.1],[45.1,20.1],[20.1,35.1]],[[30.1,20.1],[20.1,15.1],[20.1,25.1],[30.1,20.1]]]]}")]
    [InlineData("{\"type\":\"MultiPolygon\",\"coordinates\":[[[[0,0],[30,0],[30,30],[0,30],[0,0]]],[[[10,10],[20,10],[20,20],[10,20],[10,10]]]]}")]
    // MultiPolygonZ (3D) - [longitude, latitude, elevation]
    [InlineData("{\"type\":\"MultiPolygon\",\"coordinates\":[[[[30.1,20.1,100],[45.1,40.1,150],[10.1,40.1,120],[30.1,20.1,100]]],[[[15.1,5.1,80],[40.1,10.1,110],[10.1,20.1,90],[5.1,10.1,85],[15.1,5.1,80]]]]}")]
    [InlineData("{\"type\":\"MultiPolygon\",\"coordinates\":[[[[40.1,40.1,200],[20.1,45.1,180],[45.1,30.1,190],[40.1,40.1,200]]],[[[20.1,35.1,150],[10.1,30.1,140],[10.1,10.1,120],[30.1,5.1,130],[45.1,20.1,160],[20.1,35.1,150]],[[30.1,20.1,140],[20.1,15.1,135],[20.1,25.1,145],[30.1,20.1,140]]]]}")]
    [InlineData("{\"type\":\"MultiPolygon\",\"coordinates\":[[[[0,0,0],[30,0,0],[30,30,0],[0,30,0],[0,0,0]]],[[[10,10,0],[20,10,0],[20,20,0],[10,20,0],[10,10,0]]]]}")]
    public void TestMultiPolygon_RoundTrip_ShouldPreserveData(string geoJsonString)
    {
        // Arrange
        var normalizedInput = NormalizeGeoJson(geoJsonString);

        // Act
        var geometry = GeoJson.DeserializeGeometry(normalizedInput);
        var result = geometry.Parse(true).AsGeoJson(true).Serialize(false, true);

        // Assert
        Assert.Equal(normalizedInput, result);
    }

    #endregion
}
