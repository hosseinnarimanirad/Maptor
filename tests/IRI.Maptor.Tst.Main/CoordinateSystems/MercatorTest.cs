using IRI.Maptor.Sta.Spatial.Primitives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Tst.CoordinateSystems;


public class MercatorTest
{
    [Fact]
    public void TestGeodeticToMercator()
    {
        //WGS84(51.190512, 35.817032)
        Point wgs84Point = new Point(51.190512, 35.817032);

        var mercator = MapProjects.GeodeticToMercator(wgs84Point);

        Assert.Equal(5698501.75902, mercator.X, 0);
        Assert.Equal(4250468.61959, mercator.Y, 1);
    }

    [Fact]
    public void TestWebMercator()
    {
        //WGS84(51.190512, 35.817032)
        Point wgs84Point = new Point(51.190512, 35.817032);

        //var a = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoids.WGS84.SemiMajorAxis.Value;

        //Web Mercator has Sphere-based calculation but the resuling phi,lambda is assumend to be based on WGS84

        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(wgs84Point);

        Assert.Equal(5698501.75902, webMercator.X, 0);
        Assert.Equal(4275474.36482, webMercator.Y, 1);

        var mercator = MapProjects.GeodeticToMercator(wgs84Point);

        var calculatedMercator = MapProjects.WebMercatorToMercatorWgs84(webMercator);

        Assert.Equal(mercator.X, calculatedMercator.X, 1);
        Assert.Equal(mercator.Y, calculatedMercator.Y, 1);

        var calculatedWgs84Point = MapProjects.WebMercatorToGeodeticWgs84(webMercator);

        Assert.Equal(wgs84Point.X, calculatedWgs84Point.X, 6);
        Assert.Equal(wgs84Point.Y, calculatedWgs84Point.Y, 6);

    }


    [Fact]
    public void TestWebMercatorForBothMethods()
    {
        TestForPoint(new Point(51.12345678, 35.581234567));

        TestForPoint(new Point(51.17, -35.52));

        TestForPoint(new Point(-51.1, 35.53));

        TestForPoint(new Point(-51.15, -35.5));
    }

    private void TestForPoint(Point geodeticPoint)
    {

        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint);

        var geodetic1 = MapProjects.WebMercatorToGeodeticWgs84(webMercator);

        var geodetic2 = MapProjects.WebMercatorToGeodeticWgs84Slow(webMercator);

        Assert.Equal(geodetic1.X, geodetic2.X);

        Assert.Equal(geodetic1.Y, geodetic2.Y, 10 /*1E-10*/);


        Assert.Equal(geodeticPoint.X, geodetic2.X, 12/*1E-12*/);

        Assert.Equal(geodeticPoint.Y, geodetic2.Y, 10 /*1E-10*/);
    }
}
