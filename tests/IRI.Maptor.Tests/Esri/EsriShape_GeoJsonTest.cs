using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.IO.OgcSFA;

namespace IRI.Maptor.Tests.Esri;

public class EsriShape_GeoJsonTest
{
    public EsriShape_GeoJsonTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    [Theory]
    [InlineData("Point(1 2)")]
    [InlineData("MULTIPOINT((2 3), (7 8))")]
    [InlineData("LINESTRING(1 1, 2 0,  2 4, 3 3)")]
    [InlineData("MULTILINESTRING((1 1, 3 5), (-5 3, -8 -2))")]
    [InlineData("POLYGON((0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9))")]
    [InlineData("POLYGON((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0))")]
    [InlineData("POLYGON((-20 -20, 20 -20, 20 20, -20 20, -20 -20), (10 0, 0 -10, 0 10, 10 0), (-10 0, -15 0, -10 10, -10 0))")]
    [InlineData("MULTIPOLYGON(((0 0, 3 0, 3 3, 0 3, 0 0), (2 1, 1 1, 1 2, 2 1)), ((9 9, 10 9, 9 10, 9 9)))")]
    [InlineData("MULTIPOLYGON(((0 0, 6 0, 6 6, 0 6, 0 0), (1 5, 5 5, 5 1, 1 1, 1 5)), ((4 4, 2 4, 2 2, 4 2, 4 4),(3.5 3.5, 3.5 2.5, 2.5 2.5, 2.5 3.5, 3.5 3.5)))")]
    public void TestEsriShape_GeoJsonConversion(string wkt)
    {
        // Arrange
        var geometry = WktReader.Parse(wkt) as Geometry<Point>;

        // Act - Path1: Geometry -> GeoJson -> EsriShape -> WKT
        var esriShape1 = geometry.AsGeoJson().AsEsriShape(); 
        var wkt1 = esriShape1.AsSqlServerWkt();

        // Act - Path2: Geometry -> EsriShape -> WKT
        var esriShape2 = geometry.AsEsriShape();
        var wkt2 = esriShape2.AsSqlServerWkt();

        // Assert
        Assert.Equal(wkt1, wkt2);
    }
}
