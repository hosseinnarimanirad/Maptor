using System.Linq;
using IRI.Maptor.Tst.NetFrameworkTest.Assets;
using IRI.Maptor.Extensions;
using Microsoft.SqlServer.Types;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tst.Esri;


public class EsriShape_GeometryTest
{
    public EsriShape_GeometryTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }


    [Fact]
    public void TestEsriShape_GeometryConversion()
    {
        var geometries = GeometrySamples.AllGeometries.Where(g => !g.IsNullOrEmpty()).ToList();

        var esriShapes = geometries.Select(g => g.AsSqlGeometry().AsEsriShape()).ToList();

        for (int i = 0; i < esriShapes.Count; i++)
        {
            var geometry = geometries[i];

            var newGeometry = esriShapes[i].AsGeometry();

            Assert.Equal(newGeometry.NumberOfPoints, geometry.NumberOfPoints);
            Assert.Equal(newGeometry.Geometries?.Count, geometry.Geometries?.Count);
            Assert.Equal(newGeometry.Type, geometry.Type);

            Assert.True(geometry.GetAllPoints().SequenceEqual(newGeometry.GetAllPoints()));
        }
    }


    [Fact]
    public void TestEsriShape_SqlGeometry_GeometryConversion()
    {
        Test(SqlGeometrySamples.Point);
        Test(SqlGeometrySamples.PointZ);
        Test(SqlGeometrySamples.PointZM);
        Test(SqlGeometrySamples.Multipoint);
        Test(SqlGeometrySamples.MultipointComplex);
        Test(SqlGeometrySamples.Linestring);
        Test(SqlGeometrySamples.LinestringZM);
        Test(SqlGeometrySamples.MultiLineString);

        Test(SqlGeometrySamples.Polygon);
        Test(SqlGeometrySamples.PolygonWithHole);
        //Test(SqlGeometrySamples.MultiPolygon01);
        //Test(SqlGeometrySamples.MultiPolygon02);
    }

    private void Test(SqlGeometry sqlGeometry)
    {
        var esriShape = sqlGeometry.AsEsriShape();

        var geometry = esriShape.AsGeometry();

        var geometry2 = sqlGeometry.AsGeometry();

        Assert.Equal(geometry2.AsSqlGeometry().AsWkt(), geometry.AsSqlGeometry().AsWkt());
    }
}
