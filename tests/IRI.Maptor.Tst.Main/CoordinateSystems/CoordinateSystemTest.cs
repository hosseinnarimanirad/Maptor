using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Common.Primitives;
using System;
using Xunit;
using IRI.Maptor.Sta.Metrics;

namespace IRI.Maptor.Tst.CoordinateSystems;

public class CoordinateSystemTest
{
    /// <summary>
    /// Tests datum transformation from WGS84 to Clarke 1880 (RGS).
    /// Verifies that datum transformation handles ellipsoid changes correctly.
    /// </summary>
    [Fact]
    public void TestNiocLcc_DatumTransformation_Wgs84ToClarke1880()
    {
        // Arrange: WGS84 geodetic coordinate
        var wgs84Point = new Point(50.689721, 30.072906);

        // Expected Clarke 1880 (RGS) coordinates after datum transformation
        const double expectedClarke1880Longitude = 50.689721;
        const double expectedClarke1880Latitude = 30.075637;
        const int precision = 6;

        // Act: Transform datum from WGS84 to Clarke 1880 (RGS)
        var clarke1880Point = Transformations.ChangeDatum(wgs84Point, Ellipsoids.WGS84, Ellipsoids.Clarke1880Rgs);

        // Assert: Verify datum transformation accuracy
        Assert.Equal(expectedClarke1880Longitude, clarke1880Point.X, precision);
        Assert.Equal(expectedClarke1880Latitude, clarke1880Point.Y, precision);
    }

    [Fact]
    [Trait("Author", "Hossein Narimani Rad")]
    public void TestGeodeticToAlbersEqualAreaConic()
    {
        // Arrange
        double phi1_deg = 29.5;    // Standard Parallel 1
        double phi2_deg = 45.5;    // Standard Parallel 2
        double phi0_deg = 23.0;    // Latitude of Origin
        double lambda0_deg = -96.0; // Central Meridian

        double inputPhi_deg = 35.0;    // Latitude of point to project
        double inputLambda_deg = -75.0; // Longitude of point to project

        double expectedX_meters = 1885472.7;
        double expectedY_meters = 1535925.0;

        // Precision for Assert.Equal (number of decimal places to check)
        // A precision of 1 means comparing up to 0.1 meters if values are in meters.
        int precision = 1;

        // Act
        // Assuming the GeodeticToAlbersEqualAreaConic method expects degrees.
        // The method takes arrays, so we pass single-element arrays.
        double[][] actualResult = MapProjects.GeodeticToAlbersEqualAreaConic(
            [inputLambda_deg],
            [inputPhi_deg],
            Ellipsoids.Clarke1866, // Assuming this is a predefined Ellipsoid object
            lambda0_deg,
            phi0_deg,
            phi1_deg,
            phi2_deg);

        // Assert
        Assert.NotNull(actualResult);
        Assert.True(actualResult.Length >= 2, "Actual result should have X and Y arrays.");
        Assert.True(actualResult[0]?.Length >= 1, "Actual result X array should have at least one value.");
        Assert.True(actualResult[1]?.Length >= 1, "Actual result Y array should have at least one value.");

        Assert.Equal(expectedX_meters, actualResult[0][0], precision);
        Assert.Equal(expectedY_meters, actualResult[1][0], precision);
    }

    [Fact]
    public void TestLccNahrawan_FromGeodetic()
    {
        // Arrange 
        // Input point in Nahrawan geodetic coordinates (degrees)
        var nahrawanGeodeticPoint = new Point(51 + 22.0 / 60.0 + 09.42 / 3600.0, 29 + 14.0 / 60.0 + 08.65 / 3600.0);

        // Expected LCC Nahrawan projected coordinates (meters)
        var expectedLccNahrawanPoint = new Point(2119090.03, 823058.15);

        // Precision for X and Y comparison (number of decimal places)
        int precisionX = 1; // As per original test Assert.Equal(..., ..., 1)
        int precisionY = 0; // As per original test Assert.Equal(..., ..., 0)

        // Act
        var projectedPoint = IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections.SrsBases.LccNahrawan.FromGeodetic(nahrawanGeodeticPoint);

        // Assert
        Assert.NotNull(projectedPoint);
        Assert.Equal(expectedLccNahrawanPoint.X, projectedPoint.X, precisionX);
        Assert.Equal(expectedLccNahrawanPoint.Y, projectedPoint.Y, precisionY);
    }

    [Fact]
    public void TestChangeDatum_WGS84ToFD58()
    {
        // Arrange
        // Input point in WGS84 geodetic coordinates (degrees)
        var wgsGeodeticPoint = new Point(51 + 22.0 / 60.0 + 12.72 / 3600.0, 29 + 14.0 / 60.0 + 14.68 / 3600.0);

        // Expected point in Nahrawan/FD58 geodetic coordinates (degrees)
        var expectedNahrawanEquivalentPoint = new Point(51 + 22.0 / 60.0 + 09.42 / 3600.0, 29 + 14.0 / 60.0 + 08.65 / 3600.0);

        // Precision for X and Y comparison (number of decimal places)
        int precisionX = 4; // As per original test /*1E-4*/ implies ~4-5 decimal places for degree values
        int precisionY = 3; // As per original test /*1E-3*/ implies ~3-4 decimal places for degree values

        // Act
        var transformedPoint = Transformations.ChangeDatum(wgsGeodeticPoint, Ellipsoids.WGS84, Ellipsoids.FD58);

        // Assert
        Assert.NotNull(transformedPoint);
        Assert.Equal(expectedNahrawanEquivalentPoint.X, transformedPoint.X, precisionX);
        Assert.Equal(expectedNahrawanEquivalentPoint.Y, transformedPoint.Y, precisionY);
    }

    [Fact]
    public void TestGeodeticToAT()
    {
        var ellipsoid = Ellipsoids.WGS84;

        var phi = 35.123456;

        var lambda = 51.123456;

        var testPoint = new IRI.Maptor.Sta.Common.Primitives.Point(lambda, phi);

        var result1 = Transformations.ToCartesian(testPoint, ellipsoid);

        var result2 =
            new GeodeticPoint<Meter, Degree>(ellipsoid, new Meter(0),
            new Degree(lambda),
            new Degree(phi)).ToCartesian<Meter>();


        Assert.Equal(result2.X.Value, result1.X, 9/* 1E-9*/);
        Assert.Equal(result2.Y.Value, result1.Y, 9 /*1E-9*/);
        Assert.Equal(result2.Z.Value, result1.Z, 9 /*1E-9*/);


        var result3 = Transformations.ToGeodetic<Point>(result1, ellipsoid);


        Assert.Equal(testPoint.X, result3.X, 9 /*1E-9*/);
        Assert.Equal(testPoint.Y, result3.Y, 9 /*1E-9*/);
        //Assert.Equal(result1.Z, result3.Z, 1E-9);
    }
}
