using IRI.Maptor.Core.ShapefileFormat.EsriType;

namespace IRI.Maptor.Tests.Esri;

public class EsriShapeTest
{

    EsriPoint[][] points;

    public EsriShapeTest()
    {
        Initialize();
    }

    //[TestInitialize()]
    public void Initialize()
    {
        this.points = new EsriPoint[3][];

        points[0] = [new (0.65, 0, 0), new (5, 5, 0), new (3.04, 6.4, 0)];

        points[1] = [new(10, 0, 0), new(5.43, 15.65, 0), new(3.04, 6.4, 0), new (10, 90.34, 0), new (25.43, 15.65, 0), new (73.04, 61.44, 0)];

        points[2] = [new (20, 10, 0), new (15, 5, 0), new (33.04, 64.4, 0)];
    }

    [Fact]
    public void TestPolylineConstructor()
    {
        EsriPolyline polyLine = new EsriPolyline(this.points);

        Assert.Equal(polyLine.Parts[0], 0);

        Assert.Equal(polyLine.Parts[1], this.points[0].Length);

        Assert.Equal(polyLine.Parts[2], this.points[0].Length + this.points[1].Length);
    }


    /// <summary>
    /// test whether arcmap can open polygons with wrong cw/ccw orientation
    /// </summary>
    [Fact]
    public void TestPolygonWrite()
    {
        //string polygonString = "MULTIPOLYGON(((0 0, 0 6, 6 6, 6 0, 0 0), (5 5, 5 1, 1 1, 1 5, 5 5)), ((4 4, 2 4, 2 2, 4 2, 4 4),(3.5 3.5, 2.5 3.5, 2.5 2.5, 3.5 2.5, 3.5 3.5)))";

        //SqlGeometry polygon = SqlGeometry.Parse(new SqlString(polygonString));

        //var shape = polygon.AsEsriShape();

        //IRI.Maptor.Core.ShapefileFormat.Shapefile.Save(@"D:\test5.shp", new EsriShapeCollection<EsriPolygon>(
        //    new List<EsriPolygon>() { (EsriPolygon)shape }), true);
    }
}
