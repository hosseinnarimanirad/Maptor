using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Tst.CoordinateSystems;

public class WebMercatorUtilityTest
{
    /// <summary>
    /// Tests conversion from WGS84 geographic coordinates (latitude/longitude) to Web Mercator tile numbers.
    /// Verifies the tile numbering scheme used by Google Maps, OpenStreetMap, and other slippy map services.
    /// Tests include various zoom levels, extreme latitudes, and coordinates across all hemispheres.
    /// </summary>
    /// <param name="latitude">Input latitude in decimal degrees (WGS84, -85.05° to 85.05°)</param>
    /// <param name="longitude">Input longitude in decimal degrees (WGS84, -180° to 180°)</param>
    /// <param name="zoomLevel">Web Mercator zoom level (0-22, where 0 is world view)</param>
    /// <param name="expectedTileX">Expected tile X coordinate (column number, increases west to east)</param>
    /// <param name="expectedTileY">Expected tile Y coordinate (row number, increases north to south)</param>
    [Theory]
    // Basic coverage across zoom levels and hemispheres
    [InlineData(66.5, 89.9999, 2, 2, 1)]           // High latitude, Eastern Hemisphere, zoom 2
    [InlineData(0.01, -45.1, 3, 2, 3)]             // Near equator, Western Hemisphere, zoom 3
    [InlineData(-31.953, 22.501, 7, 72, 76)]       // Southern Hemisphere, Africa region, zoom 7
    [InlineData(27.197, 60.678, 15, 21907, 13809)] // Mid latitude, Middle East region, high zoom 15
    // Edge cases: extreme latitudes and specific regions
    [InlineData(60, -140, 3, 0, 2)]                // High northern latitude, Alaska region, zoom 3
    [InlineData(-60, -140, 3, 0, 5)]               // High southern latitude, Antarctic region, zoom 3
    [InlineData(-84, 40, 3, 4, 7)]                 // Near max southern latitude (~Web Mercator limit), Africa longitude, zoom 3
    [InlineData(31.1, 42.5, 8, 158, 104)]          // Mid latitude, Middle East, zoom 8
    [InlineData(84.6, -179.3, 6, 0, 0)]            // Near max northern latitude, edge of date line, zoom 6 (top-left tile)
    [InlineData(4.6, -170.3, 6, 1, 31)]            // Near equator, Pacific Ocean, zoom 6
    [InlineData(-4.3, -170.3, 6, 1, 32)]           // Just south of equator, same longitude (tests equator crossing), zoom 6
    [InlineData(-79.5, -2, 6, 31, 56)]             // Far southern latitude, near prime meridian, zoom 6
    [InlineData(22.5, 2, 6, 32, 27)]               // North Africa, near prime meridian, zoom 6
    public void TestLatLongToImageNumber_CalculatesTileCoordinates(
        double latitude,
        double longitude,
        int zoomLevel,
        int expectedTileX,
        int expectedTileY)
    {
        // Arrange: Input WGS84 coordinates and zoom level
        // Web Mercator tile system (EPSG:3857, used by Google Maps, OSM, Bing Maps):
        // - Tile X (column): increases west to east, range [0, 2^zoom - 1], X=0 at -180°
        // - Tile Y (row): increases north to south, range [0, 2^zoom - 1], Y=0 at ~85.05°N
        // - Grid size: 2^zoom × 2^zoom tiles covering the world
        // - Latitude range: approximately -85.05° to 85.05° (Web Mercator projection limit)

        // Act: Convert geodetic coordinates to tile row and column indices
        var tileCoordinates = WebMercatorUtility.LatLonToImageNumber(latitude, longitude, zoomLevel);

        // Assert: Verify calculated tile X and Y match expected values
        Assert.Equal(expectedTileX, tileCoordinates.X);
        Assert.Equal(expectedTileY, tileCoordinates.Y);
    }

    /// <summary>
    /// Tests conversion from Web Mercator tile coordinates to WGS84 geographic bounding boxes.
    /// Verifies the geographic extent (lat/lon bounds) of individual tiles at various zoom levels.
    /// Reference values validated against MapTiler tile bounds calculator.
    /// </summary>
    /// <param name="tileX">Tile X coordinate (column number)</param>
    /// <param name="tileY">Tile Y coordinate (row number)</param>
    /// <param name="zoomLevel">Web Mercator zoom level</param>
    /// <param name="expectedMinLon">Expected minimum longitude (west bound) in decimal degrees</param>
    /// <param name="expectedMaxLon">Expected maximum longitude (east bound) in decimal degrees</param>
    /// <param name="expectedMinLat">Expected minimum latitude (south bound) in decimal degrees</param>
    /// <param name="expectedMaxLat">Expected maximum latitude (north bound) in decimal degrees</param>
    /// <param name="precisionLon">Precision for longitude comparison (decimal places)</param>
    /// <param name="precisionLat">Precision for latitude comparison (decimal places)</param>
    [Theory]
    [InlineData(6, 0, 3, -180, -135, -79.17133463824213, -66.51326043946698, 0, 12)]  // Bottom-left tile, zoom 3 (Antarctica)
    [InlineData(1, 5, 3, 45, 90, 66.51326043946698, 79.17133463824213, 0, 12)]        // High northern latitude tile, zoom 3 (Arctic)
    [InlineData(100, 164, 8, 50.625, 52.03125, 35.460669951495305, 36.59788913307022, 0, 8)]  // Mid-latitude tile, zoom 8 (Turkey/Iran region)
    public void TestTileNumberToBoundingBox_CalculatesGeographicExtent(
        int tileX,
        int tileY,
        int zoomLevel,
        double expectedMinLon,
        double expectedMaxLon,
        double expectedMinLat,
        double expectedMaxLat,
        int precisionLon,
        int precisionLat)
    {
        // Arrange: Input tile coordinates (X, Y) and zoom level
        // Each tile represents a rectangular geographic area in WGS84 coordinates
        // Tile dimensions in degrees vary with latitude and zoom level

        // Act: Calculate the WGS84 geographic bounding box for the specified tile
        var boundingBox = WebMercatorUtility.GetWgs84ImageBoundingBox(tileX, tileY, zoomLevel);

        // Assert: Verify bounding box matches expected geographic extent
        Assert.Equal(expectedMinLon, boundingBox.XMin, precisionLon);
        Assert.Equal(expectedMaxLon, boundingBox.XMax, precisionLon);
        Assert.Equal(expectedMinLat, boundingBox.YMin, precisionLat);
        Assert.Equal(expectedMaxLat, boundingBox.YMax, precisionLat);
    }

    /// <summary>
    /// Tests map scale calculation for Web Mercator tiles at various zoom levels and latitudes.
    /// Map scale varies with both zoom level and latitude due to Web Mercator projection distortion.
    /// Scale is expressed as a representative fraction (e.g., 1:100000 = 1.0/100000).
    /// </summary>
    /// <param name="zoomLevel">Web Mercator zoom level (0-22)</param>
    /// <param name="latitude">Latitude in decimal degrees where scale is calculated</param>
    /// <param name="expectedScale">Expected map scale as a representative fraction</param>
    /// <param name="precision">Precision for scale comparison (decimal places)</param>
    [Theory]
    [InlineData(0, 0.0, 1.0 / 591658710.91, 2)]    // Zoom 0, equator - world view scale
    [InlineData(0, 75.0, 1.0 / 153132542.58, 2)]   // Zoom 0, high latitude - scale increases toward poles
    [InlineData(5, 0.0, 1.0 / 18489334.72, 2)]     // Zoom 5, equator - continental view
    [InlineData(5, 75.0, 1.0 / 4785391.96, 2)]     // Zoom 5, high latitude
    [InlineData(13, 0.0, 1.0 / 72223.96, 2)]       // Zoom 13, equator - city view scale
    [InlineData(13, 75.0, 1.0 / 18692.94, 2)]      // Zoom 13, high latitude
    [InlineData(22, 0.0, 1.0 / 141.06, 2)]         // Zoom 22, equator - maximum zoom, street level
    [InlineData(22, 75.0, 1.0 / 36.51, 2)]         // Zoom 22, high latitude - highest detail
    public void TestCalculateMapScale_ReturnsCorrectScaleForZoomAndLatitude(
        int zoomLevel,
        double latitude,
        double expectedScale,
        int precision)
    {
        // Arrange: Zoom level and latitude
        // Map scale in Web Mercator varies with:
        // - Zoom level: scale doubles with each zoom level increase
        // - Latitude: scale increases (map more distorted) toward poles due to Mercator projection
        // Representative fraction format: 1:X means 1 unit on map = X units on ground

        // Act: Calculate the map scale at the given zoom level and latitude
        var actualScale = WebMercatorUtility.CalculateMapScale(zoomLevel, latitude);

        // Assert: Verify calculated scale matches expected value within precision
        Assert.Equal(expectedScale, actualScale, precision);
    }

    /// <summary>
    /// Tests round-trip zoom level calculation: converts zoom level to ground distance,
    /// then estimates zoom level back from that distance. Validates the bidirectional
    /// relationship between zoom levels and ground resolution for all standard zoom levels (1-22).
    /// Uses standard tile size of 256 pixels.
    /// </summary>
    [Fact]
    public void TestZoomLevelEstimation_RoundTripConversion()
    {
        // Arrange: Standard Web Mercator tile size (256x256 pixels)
        const int tileSizeInPixels = 256;
        
        // Act & Assert: Test round-trip conversion for all standard zoom levels
        // Web Mercator supports zoom levels 0-22, testing 1-22 for practical use cases
        for (int originalZoomLevel = 1; originalZoomLevel < 23; originalZoomLevel++)
        {
            // Act: Convert zoom level to ground distance (meters per tile at this zoom level)
            double groundDistanceMeters = WebMercatorUtility.ToWebMercatorLength(
                originalZoomLevel, 
                tileSizeInPixels);

            // Act: Estimate zoom level back from the ground distance
            int estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(
                groundDistanceMeters, 
                tileSizeInPixels);

            // Assert: Round-trip should return to original zoom level
            Assert.Equal(originalZoomLevel, estimatedZoomLevel);
        }
    }

    /// <summary>
    /// Tests determination of upper (more zoomed out) and lower (more zoomed in) zoom levels 
    /// based on a given map scale and latitude. Useful for finding appropriate zoom levels 
    /// for a desired map scale or resolution.
    /// </summary>
    /// <param name="mapScale">Input map scale as representative fraction (e.g., 1/150000000)</param>
    /// <param name="latitude">Latitude in decimal degrees where scale applies</param>
    /// <param name="expectedUpperZoom">Expected upper (zoomed out) zoom level</param>
    /// <param name="expectedLowerZoom">Expected lower (zoomed in) zoom level</param>
    [Theory]
    [InlineData(1.0 / 150000000, 30, 1, 2)]  // Scale ~1:150M at lat 30° → zoom 1 (zoomed out) / zoom 2 (zoomed in)
    [InlineData(1.0 / 140000000, 30, 1, 2)]  // Scale ~1:140M at lat 30° → same zoom levels (within same zoom range)
    public void TestGetUpperAndLowerZoomLevels_DeterminesAppropriateZooms(
        double mapScale,
        double latitude,
        int expectedUpperZoom,
        int expectedLowerZoom)
    {
        // Arrange: Map scale and latitude
        // Upper level = less detailed, more zoomed out (smaller zoom number)
        // Lower level = more detailed, more zoomed in (larger zoom number)
        // These methods help find which zoom levels bracket a given scale

        // Act: Determine the upper (zoomed out) and lower (zoomed in) zoom levels
        var upperLevel = WebMercatorUtility.GetUpperLevel(mapScale, latitude);
        var lowerLevel = WebMercatorUtility.GetLowerLevel(mapScale, latitude);

        // Assert: Verify zoom levels match expected values
        Assert.Equal(expectedUpperZoom, upperLevel.ZoomLevel);
        Assert.Equal(expectedLowerZoom, lowerLevel.ZoomLevel);
    }


    //[Fact]
    //public void TestGoogleImageNumberTo
}
