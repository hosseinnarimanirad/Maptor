using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using Xunit;

namespace IRI.Maptor.Tests.GeoJsons;

/// <summary>
/// Tests for GeoJSON RFC 7946 compliance, including ring orientation validation and empty geometry handling.
/// </summary>
public class GeoJson_ComplianceTest
{
    #region Polygon Ring Orientation Validation Tests

    /// <summary>
    /// Tests that a polygon with correct ring orientations (counterclockwise external, clockwise internal) passes validation.
    /// </summary>
    [Fact]
    public void ValidateRingOrientations_CorrectOrientations_ShouldPass()
    {
        // Arrange - External ring counterclockwise, internal ring clockwise
        var polygonJson = @"{
            ""type"": ""Polygon"",
            ""coordinates"": [
                [[30, 10], [40, 40], [20, 40], [10, 20], [30, 10]],
                [[20, 30], [35, 35], [30, 20], [20, 30]]
            ]
        }";

        var polygon = GeoJson.DeserializeGeometry(polygonJson) as GeoJsonPolygon;
        Assert.NotNull(polygon);

        // Act
        var (isValid, errors) = polygon.ValidateRingOrientations();

        // Assert
        Assert.True(isValid, "Polygon with correct orientations should pass validation");
        Assert.Empty(errors);
    }

    /// <summary>
    /// Tests that a polygon with incorrect external ring orientation (clockwise) fails validation.
    /// </summary>
    [Fact]
    public void ValidateRingOrientations_IncorrectExternalRing_ShouldFail()
    {
        // Arrange - External ring clockwise (incorrect)
        var polygonJson = @"{
            ""type"": ""Polygon"",
            ""coordinates"": [
                [[30, 10], [10, 20], [20, 40], [40, 40], [30, 10]]
            ]
        }";

        var polygon = GeoJson.DeserializeGeometry(polygonJson) as GeoJsonPolygon;
        Assert.NotNull(polygon);

        // Act
        var (isValid, errors) = polygon.ValidateRingOrientations();

        // Assert
        Assert.False(isValid, "Polygon with incorrect external ring orientation should fail validation");
        Assert.NotEmpty(errors);
        Assert.Contains("External ring", errors[0]);
        Assert.Contains("counterclockwise", errors[0]);
    }

    /// <summary>
    /// Tests that a polygon with incorrect internal ring orientation (counterclockwise) fails validation.
    /// </summary>
    [Fact]
    public void ValidateRingOrientations_IncorrectInternalRing_ShouldFail()
    {
        // Arrange - External ring counterclockwise (correct), internal ring counterclockwise (incorrect)
        var polygonJson = @"{
            ""type"": ""Polygon"",
            ""coordinates"": [
                [[30, 10], [40, 40], [20, 40], [10, 20], [30, 10]],
                [[20, 30], [30, 20], [35, 35], [20, 30]]
            ]
        }";

        var polygon = GeoJson.DeserializeGeometry(polygonJson) as GeoJsonPolygon;
        Assert.NotNull(polygon);

        // Act
        var (isValid, errors) = polygon.ValidateRingOrientations();

        // Assert
        Assert.False(isValid, "Polygon with incorrect internal ring orientation should fail validation");
        Assert.NotEmpty(errors);
        Assert.Contains("Internal ring", errors[0]);
        Assert.Contains("clockwise", errors[0]);
    }

    /// <summary>
    /// Tests MultiPolygon ring orientation validation with correct orientations.
    /// </summary>
    [Fact]
    public void ValidateMultiPolygonRingOrientations_CorrectOrientations_ShouldPass()
    {
        // Arrange
        var multiPolygonJson = @"{
            ""type"": ""MultiPolygon"",
            ""coordinates"": [
                [[[30, 20], [45, 40], [10, 40], [30, 20]]],
                [[[15, 5], [40, 10], [10, 20], [5, 10], [15, 5]]]
            ]
        }";

        var multiPolygon = GeoJson.DeserializeGeometry(multiPolygonJson) as GeoJsonMultiPolygon;
        Assert.NotNull(multiPolygon);

        // Act
        var (isValid, errors) = multiPolygon.ValidateRingOrientations();

        // Assert
        Assert.True(isValid, "MultiPolygon with correct orientations should pass validation");
        Assert.Empty(errors);
    }

    /// <summary>
    /// Tests MultiPolygon ring orientation validation with incorrect orientations.
    /// </summary>
    [Fact]
    public void ValidateMultiPolygonRingOrientations_IncorrectOrientations_ShouldFail()
    {
        // Arrange - First polygon has incorrect external ring
        var multiPolygonJson = @"{
            ""type"": ""MultiPolygon"",
            ""coordinates"": [
                [[[30, 20], [10, 40], [45, 40], [30, 20]]],
                [[[15, 5], [40, 10], [10, 20], [5, 10], [15, 5]]]
            ]
        }";

        var multiPolygon = GeoJson.DeserializeGeometry(multiPolygonJson) as GeoJsonMultiPolygon;
        Assert.NotNull(multiPolygon);

        // Act
        var (isValid, errors) = multiPolygon.ValidateRingOrientations();

        // Assert
        Assert.False(isValid, "MultiPolygon with incorrect orientations should fail validation");
        Assert.NotEmpty(errors);
        Assert.Contains("Polygon 0", errors[0]);
    }

    #endregion

    #region Empty Geometry Handling Tests

    /// <summary>
    /// Tests that empty Point geometries serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void EmptyPoint_SerializeDeserialize_ShouldWork()
    {
        // Arrange
        var emptyPoint = GeoJsonPoint.Empty;

        // Act
        string json = emptyPoint.Serialize(indented: false);
        var deserialized = GeoJson.DeserializeGeometry(json) as GeoJsonPoint;

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsNullOrEmpty());
        Assert.True(emptyPoint.IsNullOrEmpty());
    }

    /// <summary>
    /// Tests that empty MultiPoint geometries serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void EmptyMultiPoint_SerializeDeserialize_ShouldWork()
    {
        // Arrange
        var emptyMultiPoint = GeoJsonMultiPoint.Empty;

        // Act
        string json = emptyMultiPoint.Serialize(indented: false);
        var deserialized = GeoJson.DeserializeGeometry(json) as GeoJsonMultiPoint;

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsNullOrEmpty());
        Assert.True(emptyMultiPoint.IsNullOrEmpty());
    }

    /// <summary>
    /// Tests that empty LineString geometries serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void EmptyLineString_SerializeDeserialize_ShouldWork()
    {
        // Arrange
        var emptyLineString = GeoJsonLineString.Empty;

        // Act
        string json = emptyLineString.Serialize(indented: false);
        var deserialized = GeoJson.DeserializeGeometry(json) as GeoJsonLineString;

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsNullOrEmpty());
        Assert.True(emptyLineString.IsNullOrEmpty());
    }

    /// <summary>
    /// Tests that empty MultiLineString geometries serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void EmptyMultiLineString_SerializeDeserialize_ShouldWork()
    {
        // Arrange
        var emptyMultiLineString = GeoJsonMultiLineString.Empty;

        // Act
        string json = emptyMultiLineString.Serialize(indented: false);
        var deserialized = GeoJson.DeserializeGeometry(json) as GeoJsonMultiLineString;

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsNullOrEmpty());
        Assert.True(emptyMultiLineString.IsNullOrEmpty());
    }

    /// <summary>
    /// Tests that empty Polygon geometries serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void EmptyPolygon_SerializeDeserialize_ShouldWork()
    {
        // Arrange
        var emptyPolygon = GeoJsonPolygon.Empty;

        // Act
        string json = emptyPolygon.Serialize(indented: false);
        var deserialized = GeoJson.DeserializeGeometry(json) as GeoJsonPolygon;

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsNullOrEmpty());
        Assert.True(emptyPolygon.IsNullOrEmpty());
    }

    /// <summary>
    /// Tests that empty MultiPolygon geometries serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void EmptyMultiPolygon_SerializeDeserialize_ShouldWork()
    {
        // Arrange
        var emptyMultiPolygon = GeoJsonMultiPolygon.Empty;

        // Act
        string json = emptyMultiPolygon.Serialize(indented: false);
        var deserialized = GeoJson.DeserializeGeometry(json) as GeoJsonMultiPolygon;

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsNullOrEmpty());
        Assert.True(emptyMultiPolygon.IsNullOrEmpty());
    }

    /// <summary>
    /// Tests that geometries with null coordinates are handled correctly.
    /// </summary>
    [Fact]
    public void NullCoordinates_ShouldBeHandledAsEmpty()
    {
        // Arrange
        var pointWithNullCoords = new GeoJsonPoint { Coordinates = null };

        // Act & Assert
        Assert.True(pointWithNullCoords.IsNullOrEmpty());
    }

    /// <summary>
    /// Tests that geometries with empty coordinate arrays are handled correctly.
    /// </summary>
    [Fact]
    public void EmptyCoordinateArray_ShouldBeHandledAsEmpty()
    {
        // Arrange
        var pointWithEmptyCoords = new GeoJsonPoint { Coordinates = [] };

        // Act & Assert
        Assert.True(pointWithEmptyCoords.IsNullOrEmpty());
    }

    ///// <summary>
    ///// Tests that empty geometries can be parsed to IGeometry.
    ///// </summary>
    //[Fact]
    //public void EmptyGeometry_Parse_ShouldReturnEmptyGeometry()
    //{
    //    // Arrange
    //    var emptyPoint = GeoJsonPoint.Empty;

    //    // Act
    //    IGeometry geometry = emptyPoint.Parse();

    //    // Assert
    //    Assert.NotNull(geometry);
    //    Assert.True(geometry.IsNullOrEmpty());
    //}

    #endregion

    #region Edge Cases Tests

    /// <summary>
    /// Tests that polygon with single ring (no holes) validates correctly.
    /// </summary>
    [Fact]
    public void Polygon_SingleRing_ShouldValidate()
    {
        // Arrange
        var polygonJson = @"{
            ""type"": ""Polygon"",
            ""coordinates"": [
                [[30, 10], [40, 40], [20, 40], [10, 20], [30, 10]]
            ]
        }";

        var polygon = GeoJson.DeserializeGeometry(polygonJson) as GeoJsonPolygon;
        Assert.NotNull(polygon);

        // Act
        var (isValid, errors) = polygon.ValidateRingOrientations();

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    /// <summary>
    /// Tests that polygon with closed ring (first point equals last point) validates correctly.
    /// </summary>
    [Fact]
    public void Polygon_ClosedRing_ShouldValidate()
    {
        // Arrange - Ring is closed (first point equals last point)
        var polygonJson = @"{
            ""type"": ""Polygon"",
            ""coordinates"": [
                [[30, 10], [40, 40], [20, 40], [10, 20], [30, 10]]
            ]
        }";

        var polygon = GeoJson.DeserializeGeometry(polygonJson) as GeoJsonPolygon;
        Assert.NotNull(polygon);

        // Act
        var (isValid, errors) = polygon.ValidateRingOrientations();

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    #endregion

    #region 4D Coordinate Extension Tests

    /// <summary>
    /// Tests that 4D coordinates (PointZM) are supported as an extension beyond RFC 7946.
    /// </summary>
    [Fact]
    public void FourDimensionalCoordinates_ShouldBeSupported()
    {
        // Arrange - 4D coordinates [longitude, latitude, elevation, measure]
        var point4DJson = @"{
            ""type"": ""Point"",
            ""coordinates"": [30, 10, 100, 5]
        }";

        // Act
        var geoJson = GeoJson.DeserializeGeometry(point4DJson);
        IGeometry geometry = geoJson.Parse();

        // Assert
        Assert.NotNull(geometry);
        Assert.True(geometry is Geometry<PointZM>);
        
        var pointZM = geometry as Geometry<PointZM>;
        Assert.NotNull(pointZM);
        Assert.Single(pointZM.Points);
        Assert.Equal(30, pointZM.Points[0].X);
        Assert.Equal(10, pointZM.Points[0].Y);
        Assert.Equal(100, pointZM.Points[0].Z);
        Assert.Equal(5, pointZM.Points[0].M);
    }

    /// <summary>
    /// Tests round-trip conversion with 4D coordinates.
    /// </summary>
    [Fact]
    public void FourDimensionalCoordinates_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalJson = @"{
            ""type"": ""Point"",
            ""coordinates"": [30, 10, 100, 5]
        }";

        // Act
        var geoJson = GeoJson.DeserializeGeometry(originalJson);
        IGeometry geometry = geoJson.Parse();
        IGeoJsonGeometry roundTrip = geometry.AsGeoJson();
        string roundTripJson = roundTrip.Serialize(indented: false, removeSpaces: true);

        // Assert
        var normalizedOriginal = originalJson.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        Assert.Equal(normalizedOriginal, roundTripJson);
    }

    #endregion
}

