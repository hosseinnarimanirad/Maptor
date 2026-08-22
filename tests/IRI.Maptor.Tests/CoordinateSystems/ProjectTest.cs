using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using System;

using System.Reflection;

namespace IRI.Maptor.Tests.CoordinateSystems;


public class ProjectTest
{
    [Fact]
    public void TestD900ToWebMercator()
    {
        var d900Prj = IRI.Maptor.Core.Common.Helpers.ResourceHelper.ReadAllText(Assembly.GetExecutingAssembly(), "IRI.Maptor.Tests.CoordinateSystem.Data.d900.txt");

        var webMercator = IRI.Maptor.Core.Common.Helpers.ResourceHelper.ReadAllText(Assembly.GetExecutingAssembly(), "IRI.Maptor.Tests.CoordinateSystem.Data.WGS 1984 Web Mercator (auxiliary sphere).txt");
         

    }


    /// <summary>
    /// Tests WGS84 to Web Mercator transformation for NIOC area coordinates.
    /// </summary>
    [Fact]
    public void TestNiocLcc_Wgs84ToWebMercator()
    {
        // Arrange: WGS84 geodetic coordinates
        var wgs84Point = new Point(50.689721, 30.072906);

        // Expected Web Mercator coordinates (in meters)
        const double expectedWebMercatorX = 5642753.93;
        const double expectedWebMercatorY = 3512924.70;
        const int precision = 1; // ±0.1 meter tolerance

        // Act: Transform WGS84 to Web Mercator
        var webMercatorResult = MapProjects.GeodeticWgs84ToWebMercator(wgs84Point);

        // Assert
        Assert.Equal(expectedWebMercatorX, webMercatorResult.X, precision);
        Assert.Equal(expectedWebMercatorY, webMercatorResult.Y, precision);
    }

}
