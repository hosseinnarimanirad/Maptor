using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Tests.CoordinateSystems;


public class LambertConformalConic2PTests
{
    /// <summary>
    /// Tests Lambert Conformal Conic projection from geographic (WGS84) to projected coordinates.
    /// Uses Clarke 1866 ellipsoid with standard US State Plane parameters.
    /// </summary>
    [Theory]
    [InlineData(-75.0, 35.0, 1894410.9, 1564649.5, 1)]
    public void TestGeodeticToLcc_Clarke1866(double longitude, double latitude, double expectedX, double expectedY, int precision)
    {
        // Arrange: US State Plane North Carolina (Clarke 1866 ellipsoid)
        // Standard Parallels: 33°N and 45°N, Central Meridian: 96°W, Latitude of Origin: 23°N
        const double standardParallel1 = 33.0;
        const double standardParallel2 = 45.0;
        const double centralMeridian = -96.0;
        const double latitudeOfOrigin = 23.0;

        var projection = new LambertConformalConic2P(
            Ellipsoids.Clarke1866,
            standardParallel1,
            standardParallel2,
            centralMeridian,
            latitudeOfOrigin);
         
        var inputGeographic = new Point(longitude, latitude);
          
        // Act
        var projectedResult = projection.FromGeodetic(inputGeographic);

        // Assert
        Assert.Equal(expectedX, projectedResult.X, precision);
        Assert.Equal(expectedY, projectedResult.Y, precision);
    }
     

    /// <summary>
    /// Tests Lambert Conformal Conic projection with GRS80 ellipsoid across multiple geographic locations.
    /// Tests transformation from geodetic to projected coordinates with false easting/northing offsets.
    /// </summary>
    /// <param name="locationName">Descriptive name of test location</param>
    /// <param name="longitude">Input longitude in decimal degrees</param>
    /// <param name="latitude">Input latitude in decimal degrees</param>
    /// <param name="expectedEasting">Expected easting coordinate (including false easting)</param>
    /// <param name="expectedNorthing">Expected northing coordinate (including false northing)</param>
    [Theory]
    [InlineData(53.0, 33.0, 7830046.77, 1879902.99)] 
    [InlineData(-105.0, 54.0, -685838.46, 7633442.68)]
    public void TestGeodeticToLcc_GRS80(double longitude, double latitude, double expectedEasting, double expectedNorthing)
    {
        // Arrange: Lambert Conformal Conic with GRS80 ellipsoid
        // Standard Parallels: 35°N and 65°N, Central Meridian: 10°E, Latitude of Origin: 52°N
        const double latitudeOfOrigin = 52.0;
        const double standardParallel1 = 35.0;
        const double standardParallel2 = 65.0;
        const double centralMeridian = 10.0;

        // False easting and false northing offsets (in meters)
        const double falseEasting = 4_000_000.0;
        const double falseNorthing = 2_800_000.0;
        const int precision = 2; // ±0.01 meter tolerance

        var projection = new LambertConformalConic2P(
            Ellipsoids.GRS80,
            standardParallel1,
            standardParallel2,
            centralMeridian,
            latitudeOfOrigin);

        var inputGeographic = new Point(longitude, latitude);

        // Act
        var projectedResult = projection.FromGeodetic(inputGeographic);

        // Apply false easting and false northing offsets
        var adjustedEasting = projectedResult.X + falseEasting;
        var adjustedNorthing = projectedResult.Y + falseNorthing;

        // Assert
        Assert.Equal(expectedEasting, adjustedEasting, precision);
        Assert.Equal(expectedNorthing, adjustedNorthing, precision);
    }


    /// <summary>
    /// Tests inverse Lambert Conformal Conic projection (projected to geographic coordinates).
    /// Verifies that ToGeodetic correctly transforms projected coordinates back to WGS84 decimal degrees.
    /// Uses Clarke 1866 ellipsoid with standard US State Plane parameters.
    /// </summary>
    /// <param name="projectedX">Input X coordinate in meters (easting)</param>
    /// <param name="projectedY">Input Y coordinate in meters (northing)</param>
    /// <param name="expectedLongitude">Expected longitude in decimal degrees</param>
    /// <param name="expectedLatitude">Expected latitude in decimal degrees</param>
    [Theory]
    [InlineData(1894410.9, 1564649.5, -75.0, 35.0)]
    public void TestLccToGeodetic_Clarke1866_USStatePlane(double projectedX, double projectedY, double expectedLongitude, double expectedLatitude)
    {
        // Arrange: US State Plane North Carolina (Clarke 1866 ellipsoid)
        // Standard Parallels: 33°N and 45°N, Central Meridian: 96°W, Latitude of Origin: 23°N
        const double standardParallel1 = 33.0;
        const double standardParallel2 = 45.0;
        const double centralMeridian = -96.0;
        const double latitudeOfOrigin = 23.0;
        const int precision = 5; // ±0.00001 degrees (~1 meter at equator)

        var projection = new LambertConformalConic2P(
            Ellipsoids.Clarke1866,
            standardParallel1,
            standardParallel2,
            centralMeridian,
            latitudeOfOrigin);

        var inputProjected = new Point(projectedX, projectedY);

        // Act: Transform from projected to geographic coordinates
        var geographicResult = projection.ToGeodetic(inputProjected);

        // Assert: Verify longitude and latitude
        Assert.Equal(expectedLongitude, geographicResult.X, precision);
        Assert.Equal(expectedLatitude, geographicResult.Y, precision);
    }


    /// <summary>
    /// Tests iterative inverse Lambert Conformal Conic projection algorithm.
    /// Verifies the iterative method produces same results as standard ToGeodetic method.
    /// Uses Clarke 1866 ellipsoid with standard US State Plane parameters.
    /// </summary>
    /// <param name="projectedX">Input X coordinate in meters (easting)</param>
    /// <param name="projectedY">Input Y coordinate in meters (northing)</param>
    /// <param name="expectedLongitude">Expected longitude in decimal degrees</param>
    /// <param name="expectedLatitude">Expected latitude in decimal degrees</param>
    [Theory]
    [InlineData(1894410.9, 1564649.5, -75.0, 35.0)]
    public void TestLccToGeodetic_IterativeAlgorithm_Clarke1866(double projectedX, double projectedY, double expectedLongitude, double expectedLatitude)
    {
        // Arrange: US State Plane North Carolina (Clarke 1866 ellipsoid)
        // Standard Parallels: 33°N and 45°N, Central Meridian: 96°W, Latitude of Origin: 23°N
        const double standardParallel1 = 33.0;
        const double standardParallel2 = 45.0;
        const double centralMeridian = -96.0;
        const double latitudeOfOrigin = 23.0;
        const int precision = 5; // ±0.00001 degrees (~1 meter at equator)

        var projection = new LambertConformalConic2P(
            Ellipsoids.Clarke1866,
            standardParallel1,
            standardParallel2,
            centralMeridian,
            latitudeOfOrigin);

        var inputProjected = new Point(projectedX, projectedY);

        // Act: Use iterative algorithm for inverse transformation
        var geographicResult = projection.LCCToGeodeticIterative(inputProjected);

        // Assert: Verify the iterative method produces correct results
        Assert.Equal(expectedLongitude, geographicResult.X, precision);
        Assert.Equal(expectedLatitude, geographicResult.Y, precision);
    }

    //public void FourInOneLccTest()
    //{
    //    var clarke = Ellipsoids.Clarke1880Rgs;
    //    double phi0 = 32.5;
    //    double phi1 = 29.65508274166;
    //    double phi2 = 35.31468809166;
    //    double lambda0 = 45.0;
    //    var lccNioc = new LambertConformalConic2P(clarke, phi1, phi2, lambda0, phi0, 1500000.0, 1166200.0, 0.9987864078);

    //    var xLccNioc = 2047473.33479;
    //    var yLccNioc = 912594.777238;

    //    var xWgs84 = 50.689721;
    //    var yWgs84 = 30.072906;

    //    var xWebMercator = 5642753.9243;
    //    var yWebMercator = 3512924.70491;

    //    var xClarke1880Rgs = 50.689721;
    //    var yClarke1880Rgs = 30.075637;


    //    var wgs84 = lccNioc.ToWgs84Geodetic(new Point(xLccNioc, yLccNioc));

    //    Assert.Equal(xWgs84, wgs84.X, 6);
    //    Assert.Equal(yWgs84, wgs84.Y, 6);

    //    var clarke1880 = lccNioc.ToGeodetic(new Point(xLccNioc, yLccNioc));

    //    Assert.Equal(xClarke1880Rgs, clarke1880.X, 6);
    //    Assert.Equal(yClarke1880Rgs, clarke1880.Y, 6);

    //    var webMercator = MapProjects.GeodeticWgs84ToWebMercator(wgs84);

    //    Assert.Equal(xWebMercator, webMercator.X, 2 /*0.05*/);
    //    Assert.Equal(yWebMercator, webMercator.Y, 2 /*0.05*/);

    //    var clarke1880_2 = Transformations.ChangeDatum(wgs84, Ellipsoids.WGS84, Ellipsoids.Clarke1880Rgs);

    //    Assert.Equal(xClarke1880Rgs, clarke1880_2.X, 6);
    //    Assert.Equal(yClarke1880Rgs, clarke1880_2.Y, 6);

    //}

    /// <summary>
    /// Tests NIOC (National Iranian Oil Company) Lambert Conformal Conic projection.
    /// Verifies transformation from NIOC LCC to WGS84 geodetic coordinates.
    /// Uses Clarke 1880 (RGS) ellipsoid with NIOC-specific parameters.
    /// </summary>
    [Fact]
    public void TestNiocLcc_ToWgs84Geodetic()
    {
        // Arrange: NIOC Lambert Conformal Conic projection parameters
        // Standard Parallels: 29.655°N and 35.315°N, Central Meridian: 45°E, Latitude of Origin: 32.5°N
        const double latitudeOfOrigin = 32.5;
        const double standardParallel1 = 29.65508274166;
        const double standardParallel2 = 35.31468809166;
        const double centralMeridian = 45.0;
        const double falseEasting = 1500000.0;  // meters
        const double falseNorthing = 1166200.0; // meters
        const double scaleFactor = 0.9987864078;
        const int precision = 6; // ±0.000001 degrees (~0.1 meter)

        var lccNioc = new LambertConformalConic2P(
            Ellipsoids.Clarke1880Rgs,
            standardParallel1,
            standardParallel2,
            centralMeridian,
            latitudeOfOrigin,
            falseEasting,
            falseNorthing,
            scaleFactor);

        // Input: NIOC LCC projected coordinates (in meters)
        var inputLccNioc = new Point(2047473.33479, 912594.777238);
        
        // Expected: WGS84 geodetic coordinates (decimal degrees)
        const double expectedWgs84Longitude = 50.689721;
        const double expectedWgs84Latitude = 30.072906;

        // Act: Transform from NIOC LCC to WGS84 geodetic
        var resultWgs84 = lccNioc.ToWgs84Geodetic(inputLccNioc);

        // Assert: Verify WGS84 coordinates
        Assert.Equal(expectedWgs84Longitude, resultWgs84.X, precision);
        Assert.Equal(expectedWgs84Latitude, resultWgs84.Y, precision);
    }

    /// <summary>
    /// Tests NIOC LCC to Clarke 1880 (RGS) geodetic transformation.
    /// Verifies transformation stays within the same ellipsoid (Clarke 1880).
    /// </summary>
    [Fact]
    public void TestNiocLcc_ToClarke1880Geodetic()
    {
        // Arrange: NIOC Lambert Conformal Conic projection
        const double latitudeOfOrigin = 32.5;
        const double standardParallel1 = 29.65508274166;
        const double standardParallel2 = 35.31468809166;
        const double centralMeridian = 45.0;
        const double falseEasting = 1500000.0;
        const double falseNorthing = 1166200.0;
        const double scaleFactor = 0.9987864078;
        const int precision = 6;

        var lccNioc = new LambertConformalConic2P(
            Ellipsoids.Clarke1880Rgs,
            standardParallel1,
            standardParallel2,
            centralMeridian,
            latitudeOfOrigin,
            falseEasting,
            falseNorthing,
            scaleFactor);

        var inputLccNioc = new Point(2047473.33479, 912594.777238);
        
        // Expected: Clarke 1880 (RGS) geodetic coordinates (decimal degrees)
        const double expectedClarke1880Longitude = 50.689721;
        const double expectedClarke1880Latitude = 30.075637;

        // Act: Transform to Clarke 1880 geodetic (same ellipsoid as projection)
        var resultClarke1880 = lccNioc.ToGeodetic(inputLccNioc);

        // Assert
        Assert.Equal(expectedClarke1880Longitude, resultClarke1880.X, precision);
        Assert.Equal(expectedClarke1880Latitude, resultClarke1880.Y, precision);
    }

      
    /// <summary>
    /// Tests NIOC LCC round-trip transformation (forward and inverse).
    /// Verifies bidirectional transformation accuracy between WGS84 geodetic and NIOC LCC projected coordinates.
    /// Uses predefined NIOC LCC projection with Clarke 1880 (RGS) ellipsoid.
    /// Reference: National Iranian Oil Company standard projection for oil field mapping.
    /// </summary>
    /// <param name="wgs84Longitude">Input WGS84 longitude in decimal degrees</param>
    /// <param name="wgs84Latitude">Input WGS84 latitude in decimal degrees</param>
    /// <param name="expectedLccX">Expected NIOC LCC X coordinate (easting) in meters from reference data</param>
    /// <param name="expectedLccY">Expected NIOC LCC Y coordinate (northing) in meters from reference data</param>
    /// <param name="forwardPrecisionX">Precision tolerance for X coordinate (±0.1^n meters)</param>
    /// <param name="forwardPrecisionY">Precision tolerance for Y coordinate (±0.1^n meters)</param>
    [Theory]
    [InlineData(47.002509, 32.497152, 1687728.029, 1167963.306, 2, 0)]  // Western Iran - ±1m tolerance
    [InlineData(50.689721, 30.072906, 2047473.33479, 912594.777238, 1, 1)]  // Southern Iran (Ahvaz area) - ±0.1m tolerance
    public void TestNiocLcc_RoundTripTransformation(
        double wgs84Longitude,
        double wgs84Latitude,
        double expectedLccX,
        double expectedLccY,
        int forwardPrecisionX,
        int forwardPrecisionY)
    {
        // Arrange: Use predefined NIOC LCC projection with Clarke 1880 (RGS)
        // Parameters: φ₁=29.655°N, φ₂=35.315°N, λ₀=45°E, φ₀=32.5°N
        // False Easting: 1,500,000m, False Northing: 1,166,200m, Scale Factor: 0.9987864078
        var niocLccProjection = SrsBases.LccNiocWithClarke1880Rgs;
        const int inversePrecision = 6; // ±0.000001 degrees for round-trip accuracy

        var inputWgs84 = new Point(wgs84Longitude, wgs84Latitude);
        var inputLcc = new Point(expectedLccX, expectedLccY);

        // Act & Assert: Test forward transformation (WGS84 → NIOC LCC)
        var forwardResult = niocLccProjection.FromWgs84Geodetic(inputWgs84);
        
        Assert.Equal(expectedLccX, forwardResult.X, forwardPrecisionX);
        Assert.Equal(expectedLccY, forwardResult.Y, forwardPrecisionY);

        // Act & Assert: Test inverse transformation (NIOC LCC → WGS84)
        // This verifies round-trip accuracy
        var inverseResult = niocLccProjection.ToWgs84Geodetic(inputLcc);
        
        Assert.Equal(wgs84Longitude, inverseResult.X, inversePrecision);
        Assert.Equal(wgs84Latitude, inverseResult.Y, inversePrecision);
    }


    /// <summary>
    /// Tests FD58 (Ferdowsi 1958) Lambert Conformal Conic projection.
    /// Validates transformation from WGS84 geodetic to FD58 LCC projected coordinates.
    /// Reference values verified against EPSG.IO database.
    /// </summary>
    /// <param name="wgs84Longitude">Input WGS84 longitude in decimal degrees</param>
    /// <param name="wgs84Latitude">Input WGS84 latitude in decimal degrees</param>
    /// <param name="expectedFd58X">Expected FD58 LCC X coordinate (easting) in meters</param>
    /// <param name="expectedFd58Y">Expected FD58 LCC Y coordinate (northing) in meters</param>
    /// <param name="precisionX">Precision tolerance for X coordinate</param>
    /// <param name="precisionY">Precision tolerance for Y coordinate</param>
    [Theory]
    [InlineData(51.0, 35.0, 2047242.77, 1458475.69, 2, 0)]  // Central Iran (Tehran area)
    public void TestFd58Lcc_Wgs84ToFd58Projection(
        double wgs84Longitude,
        double wgs84Latitude,
        double expectedFd58X,
        double expectedFd58Y,
        int precisionX,
        int precisionY)
    {
        // Arrange: Use predefined FD58 Lambert Conformal Conic projection
        // FD58 is a historical coordinate system used in Iran
        // Reference: EPSG.IO database
        var fd58Projection = SrsBases.LccFd58;
        
        var inputWgs84 = new Point(wgs84Longitude, wgs84Latitude);

        // Act: Transform from WGS84 to FD58 LCC
        var fd58Result = fd58Projection.FromWgs84Geodetic(inputWgs84);

        // Assert: Verify projected coordinates match reference values
        Assert.Equal(expectedFd58X, fd58Result.X, precisionX);
        Assert.Equal(expectedFd58Y, fd58Result.Y, precisionY);
    }
}
