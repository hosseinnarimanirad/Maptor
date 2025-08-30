using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;

namespace IRI.Maptor.Tst.Esri;


public class EsriShape_AsWktTest
{
    public EsriShape_AsWktTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();

        InitializeTest();
    }

    double[] x = new double[10], y = new double[10], z = new double[10], m = new double[10];
    EsriPoint[] points = new EsriPoint[10];
    EsriPointM[] pointsM = new EsriPointM[10];
    EsriPointZ[] pointsZ = new EsriPointZ[10];

    EsriPoint[][] pointCollection;
    EsriPointM[][] pointCollectionM;
    EsriPointZ[][] pointCollectionZ;

    IEsriShapeCollection polygons;

    //[TestInitialize]
    public void InitializeTest()
    {
        for (int i = 0; i < 10; i++)
        {
            x[i] = Math.Round(Math.Sin(i) * 100, 5);
            y[i] = Math.Round(Math.Cos(i) * 70, 5);
            points[i] = new EsriPoint(x[i], y[i], 0);
            pointsM[i] = new EsriPointM(x[i], y[i], x[i] + y[i], 0);
            pointsZ[i] = new EsriPointZ(x[i], y[i], x[i] + y[i], 0);
        }

        pointCollection = new EsriPoint[3][];
        pointCollection[0] = new EsriPoint[] { new EsriPoint(0.65, 0, 0), new EsriPoint(5, 5, 0), new EsriPoint(3.04, 6.4, 0) };
        pointCollection[1] = new EsriPoint[] { new EsriPoint(10, 0, 0), new EsriPoint(5.43, 15.65, 0), new EsriPoint(3.04, 6.4, 0), new EsriPoint(10, 90.34, 0), new EsriPoint(25.43, 15.65, 0), new EsriPoint(73.04, 61.44, 0) };
        pointCollection[2] = new EsriPoint[] { new EsriPoint(20, 10, 0), new EsriPoint(15, 5, 0), new EsriPoint(33.04, 64.4, 0) };

        pointCollectionM = new EsriPointM[4][];
        pointCollectionM[0] = new EsriPointM[] { pointsM[0], pointsM[1], pointsM[2] };
        pointCollectionM[1] = new EsriPointM[] { pointsM[3], pointsM[4], pointsM[5], pointsM[5], pointsM[6] };
        pointCollectionM[2] = new EsriPointM[] { pointsM[0], pointsM[7], pointsM[8] };
        pointCollectionM[3] = new EsriPointM[] { pointsM[0], pointsM[7], pointsM[8] };


        pointCollectionZ = new EsriPointZ[5][];
        pointCollectionZ[0] = new EsriPointZ[] { pointsZ[0], pointsZ[1], pointsZ[2] };
        pointCollectionZ[1] = new EsriPointZ[] { pointsZ[3], pointsZ[4], pointsZ[5], pointsZ[5], pointsZ[6] };
        pointCollectionZ[2] = new EsriPointZ[] { pointsZ[1], pointsZ[7] };
        pointCollectionZ[3] = new EsriPointZ[] { pointsZ[3], pointsZ[5], pointsZ[8] };
        pointCollectionZ[4] = new EsriPointZ[] { pointsZ[9], pointsZ[7], pointsZ[8] };

        System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory);

        //string fileName = string.Join("\\", info.Parent.Parent.FullName, "Resources\\regions.shp");
        //"E:\Programming\100.IRI.Japey\IRI.Tst\IRI.Maptor.Tst.Main\bin\Debug\net8.0-windows\Assets\ShapefileSamples2\regions.shp"
        string fileName = $@"{Environment.CurrentDirectory}\Assets\ShapefileSamples2\regions.shp";

        this.polygons = IRI.Maptor.Sta.ShapefileFormat.Shapefile.ReadShapes(fileName);

    }

    [Fact]
    public void TestEsriPointAsSqlServerWkt()
    {
        //Test EsriPoint
        EsriPoint point = new EsriPoint(x[0], y[0], 0);
        _ = SqlGeometry.STPointFromText(new SqlChars(new SqlString(point.AsSqlServerWkt())), 0);

        //Test EsriPointM
        EsriPointM pointM = new EsriPointM(x[0], y[0], m[0], 0);
        _ = SqlGeometry.STPointFromText(new SqlChars(new SqlString(pointM.AsSqlServerWkt())), 0);

        //Test EsriPointZ
        EsriPointZ pointZ = new EsriPointZ(x[0], y[0], z[0], 0);
        _ = SqlGeometry.STPointFromText(new SqlChars(new SqlString(pointZ.AsSqlServerWkt())), 0);

        //Test EsriPointZM
        EsriPointZ pointZM = new EsriPointZ(x[0], y[0], z[0], m[0], 0);
        _ = SqlGeometry.STPointFromText(new SqlChars(new SqlString(pointZM.AsSqlServerWkt())), 0);
    }

    [Fact]
    public void TestEsriPointCollectionAsSqlServerWkt()
    {
        //Test MultiEsriPoint
        EsriMultiPoint multiEsriPoint = new EsriMultiPoint(points);
        _ = SqlGeometry.STMPointFromText(new SqlChars(new SqlString(multiEsriPoint.AsSqlServerWkt())), 0);

        //Test MultiEsriPointM
        EsriMultiPointM multiEsriPointM = new EsriMultiPointM(pointsM);
        _ = SqlGeometry.STMPointFromText(new SqlChars(new SqlString(multiEsriPointM.AsSqlServerWkt())), 0);

        //Test MultiEsriPointZ
        EsriMultiPointZ multiEsriPointZ = new EsriMultiPointZ(pointsZ);
        _ = SqlGeometry.STMPointFromText(new SqlChars(new SqlString(multiEsriPointZ.AsSqlServerWkt())), 0);
    }

    [Fact]
    public void TestEsriPolylineAsSqlServerWkt()
    {
        EsriPolyline polyLine = new EsriPolyline(this.pointCollection);

        SqlGeometry sqlPolyline = SqlGeometry.STMLineFromText(new SqlChars(new SqlString(polyLine.AsSqlServerWkt())), 0);

        //PolyLineM polyLineM = new PolyLineM(this.pointCollectionM);

        //SqlGeometry sqlPolylineM = SqlGeometry.STMLineFromText(new SqlChars(new SqlString(polyLineM.AsSqlServerWkt())), 0);

        //Assert.Equal(true, sqlPolylineM.HasM);

        //PolyLineZ polyLineZ = new PolyLineZ(this.pointCollectionZ);

        //SqlGeometry sqlPolylineZ = SqlGeometry.STMLineFromText(new SqlChars(new SqlString(polyLineZ.AsSqlServerWkt())), 0);

        //Assert.Equal(true, sqlPolylineZ.HasZ);
    }

    [Fact]
    public void TestEsriPolygonAsSqlServerWkt()
    {
        foreach (EsriPolygon item in this.polygons)
        {
            SqlGeometry geometry = SqlGeometry.STPolyFromText(new SqlChars(new SqlString(item.AsSqlServerWkt())), 0);

            Assert.Equal(geometry.STNumPoints(), item.NumberOfPoints);

            Assert.Equal(geometry.STNumGeometries(), item.NumberOfParts);
        }

    }

}
