using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tst.Main.Geometry;

public class EllipsoidalAreaTests
{
    // EPSG:4326 (WGS84)
    private const int Wgs84Srid = 4326;

    [Theory]
    [InlineData("POLYGON((0 0,0 0.01,0.01 0.01,0.01 0,0 0))")]       
    [InlineData("POLYGON((10 10,10 10.01,10.01 10.01,10.01 10,10 10))")] 
    [InlineData("POLYGON((30 30,30 30.02,30.02 30.02,30.02 30,30 30))")] 
    [InlineData("POLYGON((0 0,0 0.005,0.005 0.005,0.005 0,0 0))")]  
    [InlineData("POLYGON((-5 -5,-5 -4.99,-4.99 -4.99,-4.99 -5,-5 -5))")]  
    [InlineData("POLYGON((0 0,0.05 0,0.05 0.05,0 0.05,0 0),(0.01 0.02,0.02 0.02,0.02 0.01,0.01 0.01,0.01 0.02))")]
    [InlineData("MULTIPOLYGON(((0.02 0.03,0.02 0.02,0.03 0.02,0.03 0.03,0.02 0.03)),((0.00 0.01,0.00 0.00,0.01 0.00,0.01 0.01,0.00 0.01)))")]
    [InlineData("MULTIPOLYGON(((1.00 1.03,1.00 1.00,1.03 1.00,1.03 1.03,1.00 1.03)),((0.00 0.04,0.00 0.00,0.04 0.00,0.04 0.04,0.00 0.04),(0.01 0.03,0.03 0.03,0.03 0.01,0.01 0.01,0.01 0.03)))")]
    [InlineData("MULTIPOLYGON(((50.0000 60.0523,50.0000 60.0000,50.0523 60.0000,50.0523 60.0523,50.0000 60.0523)),((30.0000 40.0805,30.0000 40.0000,30.0817 40.0000,30.0817 40.0805,30.0000 40.0805), (30.0314 40.0317,30.0716 40.0317,30.0716 40.0118,30.0314 40.0118,30.0314 40.0317), (30.0215 40.0711,30.0613 40.0711,30.0613 40.0513,30.0215 40.0513,30.0215 40.0711)),((10.0514 20.0814,10.0514 20.0711,10.0611 20.0711,10.0611 20.0814,10.0514 20.0814)),((10.0000 20.1012,10.0000 20.0000,10.1025 20.0000,10.1025 20.1012,10.0000 20.1012), (10.0415 20.0412,10.0812 20.0412,10.0812 20.0214,10.0415 20.0214,10.0415 20.0412), (10.0314 20.0814,10.0514 20.0814,10.0514 20.0912,10.0611 20.0912,10.0611 20.0814,10.0713 20.0814,10.0713 20.0613,10.0314 20.0613,10.0314 20.0814)))")]
    public void Compare_CalculateSphereArea_With_SqlGeography(string wkt)
    {
        // Arrange
        var sqlGeo = SqlGeography.STGeomFromText(new SqlChars(wkt), Wgs84Srid);
        if (sqlGeo.EnvelopeAngle() >= 180)
        {
            sqlGeo = sqlGeo.ReorientObject();
        }

        double expected = sqlGeo.STArea().Value; // SQL Server area in m²
         
        var geometry = Geometry<IRI.Maptor.Sta.Common.Primitives.Point>.FromWkt(wkt, Wgs84Srid);
        var newGeo = geometry.Project(new Sta.SpatialReferenceSystem.MapProjections.CylindricalEqualArea());


        // Act 
        double actual = newGeo.EuclideanArea; 
         
        // Assert
        double diff = Math.Abs(expected - actual);
        double relativeError = diff / expected;
         
        Assert.True(relativeError < 0.0001,
            $"Area mismatch: WKT={wkt}, Expected={expected}, Actual={actual}, Diff={diff}, RelErr={relativeError}");

        // does not pass for large polygons
        //Assert.Equal(expected, actual, 5);
    }
}
