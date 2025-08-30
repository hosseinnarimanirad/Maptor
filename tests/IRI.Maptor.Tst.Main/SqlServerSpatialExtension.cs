using System.Linq;
using System;

using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;
using IRI.Maptor.Sta.Spatial.Primitives;


using System.Reflection;
using System.IO;
using IRI.Maptor.Sta.ShapefileFormat.Prj;
using IRI.Maptor.Ket.SqlServerSpatialExtension;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Extensions;
using IRI.Maptor.Tst.NetFrameworkTest.Assets;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tst.NetFrameworkTest.SqlServerSpatialExtension;


public class UnitTest
{
    public UnitTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    [Fact]
    public void TestExtractPoint()
    {
        var polygon = SqlGeometry.Parse(new SqlString("POLYGON( (0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9) )"));

        var linestring = SqlGeometry.Parse(new SqlString("LINESTRING( 4 4 4 4, 9 0 4 4 )"));

        var multiPolygon = SqlGeometry.Parse(new SqlString("MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (1 1, 1 2, 2 1, 1 1)), ((9 9, 9 10, 10 9, 9 9)))"));

        var polygonWithHole = SqlGeometry.Parse(new SqlString("POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))"));

        var geometry = polygon.AsGeometry();

        var temp = geometry.AsSqlGeometry();

        Assert.Equal(polygon.AsWkt(), temp.AsWkt());

        Assert.Equal(4, geometry.Geometries.Sum(i => i.Points.Count()));


        geometry = linestring.AsGeometry();

        temp = geometry.AsSqlGeometry();

        Assert.Equal(linestring.AsWkt(), temp.AsWkt());

        Assert.Equal(2, geometry.Points.Count);


        geometry = multiPolygon.AsGeometry();

        temp = geometry.AsSqlGeometry();

        Assert.Equal(multiPolygon.AsWkt(), temp.AsWkt());

        Assert.Equal(4, geometry.Geometries[0].Geometries[0].Points.Count);

        Assert.Equal(3, geometry.Geometries[0].Geometries[1].Points.Count);

        Assert.Equal(3, geometry.Geometries[1].Geometries[0].Points.Count);


        geometry = polygonWithHole.AsGeometry();

        temp = geometry.AsSqlGeometry();

        Assert.Equal(polygonWithHole.AsWkt(), temp.AsWkt());

        Assert.Equal(4, geometry.Geometries[0].Points.Count);

        Assert.Equal(3, geometry.Geometries[1].Points.Count);
    }

    [Fact]
    public void TestCreateSqlGeometryFromPointList()
    {

    }

    [Fact]
    [Trait("Author", "Hossein Narimani Rad")]
    public void TestCoordinateSystemExtensions()
    {
        var prjString = ReadFile("Clarke 1880 (RGS).prj");

        //var prjString = ReadFile("lccnioc.prj");

        EsriPrjFile prjFile = EsriPrjFile.Parse(prjString);

        var prjFile2 = prjFile.AsMapProjection().AsEsriPrj();
         
        Assert.Equal(prjFile.AsEsriCrsWkt(), prjFile2.AsEsriCrsWkt());
    }
     
    private string ReadFile(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resourceName = $"IRI.Maptor.Tst.Main.Assets.PrjSamples.{fileName}";

        string result;

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
        }

        return result;
    }

    [Fact]
    public void TestSqlSpatialToGml()
    {
        foreach (var geo in SqlGeometrySamples.AllGeometries)
        {
            GmlExtensions.ParseGML3(geo.AsGml3(), 3857);
        }
    }


    //[Fact]
    //public void TestSqlSpatialToMarkup()
    //{
    //    //var markup1 = SqlGeometrySamples.Linestring.ParseToPathMarkup();
    //    //var markup2 = SqlGeometrySamples.MultiLineString.ParseToPathMarkup();
    //    //var markup3 = SqlGeometrySamples.Polygon.ParseToPathMarkup();
    //    //var markup4 = SqlGeometrySamples.PolygonWithHole.ParseToPathMarkup();
    //    //var markup5 = SqlGeometrySamples.MultiPolygon01.ParseToPathMarkup();
    //    //var markup6 = SqlGeometrySamples.MultiPolygon02.ParseToPathMarkup();


    //}
}
