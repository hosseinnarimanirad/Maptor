using IRI.Maptor.Extensions;
using IRI.Maptor.Extensions;
using IRI.Maptor.Tst.NetFrameworkTest.Assets;
using Microsoft.SqlServer.Types;

namespace IRI.Maptor.Tst.Esri;


public class EsriShape_GeoJsonTest
{
    public EsriShape_GeoJsonTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }
     
    [Fact]
    public void TestEsriShape_GeoJsonConversion()
    { 
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.Point);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.Multipoint);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.Linestring);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.MultiLineString);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.Polygon);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.PolygonWithHole);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.PolygonWithTwoHole);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.MultiPolygon01);
        CheckGeoJsonEsriShapeConversion(SqlGeometrySamples.MultiPolygon02);
    }

    private void CheckGeoJsonEsriShapeConversion(SqlGeometry sqlGeometry)
    {
        var geoJson1 = sqlGeometry.AsGeoJson();
        var geoJson2 = sqlGeometry.AsGeometry().AsGeoJson();

        Assert.Equal(geoJson1.NumberOfGeometries(), geoJson2.NumberOfGeometries());
        Assert.Equal(geoJson1.NumberOfPoints(), geoJson2.NumberOfPoints());
        Assert.Equal(geoJson1.Serialize(false), geoJson2.Serialize(false));

        var esriShape1 = sqlGeometry.AsEsriShape();
        var esriShape2 = sqlGeometry.AsGeoJson().AsEsriShape();

        Assert.Equal(esriShape1.AsSqlServerWkt(), esriShape2.AsSqlServerWkt());

    }

}
