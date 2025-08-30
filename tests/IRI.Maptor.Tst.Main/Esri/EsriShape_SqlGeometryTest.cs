using IRI.Maptor.Extensions;
using IRI.Maptor.Extensions;
using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;

namespace IRI.Maptor.Tst.Esri;


public class EsriShape_SqlGeometryTest
{
    public EsriShape_SqlGeometryTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    [Fact]
    public void TestSqlSpatialToEsriShapefile()
    {
        //
        string pointString = "POINT(-20 -20)";
        Assert.Equal(pointString, SqlGeometry.Parse(new SqlString(pointString)).AsEsriShape().AsSqlServerWkt());

        //
        string multiPointString = "MULTIPOINT((2 3),(7 8))";
        Assert.Equal(multiPointString, SqlGeometry.Parse(new SqlString(multiPointString)).AsEsriShape().AsSqlServerWkt());

        //
        string lineStringString = "LINESTRING(1 1,3 5,5 10,-70 34)";
        Assert.Equal(lineStringString, SqlGeometry.Parse(new SqlString(lineStringString)).AsEsriShape().AsSqlServerWkt());

        //
        string multiLineStringString = "MULTILINESTRING((1 1,3 5),(-5 3,-8 -2))";
        Assert.Equal(multiLineStringString, SqlGeometry.Parse(new SqlString(multiLineStringString)).AsEsriShape().AsSqlServerWkt());

        //
        string polygonString = "POLYGON((-20 -20,-20 20,20 20,20 -20,-20 -20))";
        Assert.Equal(polygonString, SqlGeometry.Parse(new SqlString(polygonString)).AsEsriShape().AsSqlServerWkt());

        //string multiPolygonString = "MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (1 1, 1 2, 2 1, 1 1)), ((9 9, 9 10, 10 9, 9 9)))";
        //SqlGeometry polygon = SqlGeometry.Parse(
        // new System.Data.SqlTypes.SqlString(multiPolygonString));

        //var shape = polygon.ParseToEsriShapefile();

        //string NewMultiPolygonString = SqlGeometry.Parse(
        // new System.Data.SqlTypes.SqlString(shape.AsSqlServerWkt())).MakeValid().STAsText().ToSqlString().Value;

        //Assert.Equal(multiPolygonString, NewMultiPolygonString);
    }
}
