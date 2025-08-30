using IRI.Maptor.Extensions;
using IRI.Maptor.Tst.NetFrameworkTest.Assets;


namespace IRI.Maptor.Tst.Main.TheGeometry;


public class GeometryTest
{
    public GeometryTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    [Fact]
    public void TestAreaCalculation()
    {
        foreach (var item in GeometrySamples.AllGeometries)
        {
            var area1 = item.EuclideanArea;
            var area2 = item.AsSqlGeometry().STArea().Value;

            Assert.Equal(area1, area2);
        }
    }

    [Fact]
    public void TestLengthCalculation()
    {
        foreach (var item in GeometrySamples.AllGeometries)
        {
            var length1 = item.CalculateEuclideanLength();
            var length2 = item.AsSqlGeometry().STLength().Value;

            Assert.Equal(length1, length2);
        }
    }

    [Fact]
    public void TestMeanAngularChange()
    {
        var meanAngularChange = SqlGeometrySamples.LineString_ForAngularChange.AsGeometry().CalculateMeanAngularChange() * 180 / Math.PI;

        Assert.Equal(72.1864, meanAngularChange, 4 /*0.0001*/);


        var meanAngularChangeForPolygon = SqlGeometrySamples.Polygon_ForAngularChange.AsGeometry().CalculateMeanAngularChange() * 180 / Math.PI;

        Assert.Equal(75.687, meanAngularChangeForPolygon, 4 /*0.0001*/);
    }

    // 1401.03.12
    [Fact]
    public void TestTotalVectorDispalcement()
    {
        var original = SqlGeometrySamples.LineString_ForVectorDisplacement_Original.AsGeometry();
        var simplified = SqlGeometrySamples.LineString_ForVectorDisplacement_Simplified.AsGeometry();

        var dispacement = original.CalculateTotalVectorDisplacement(simplified);

        Assert.Equal(6.324, dispacement, 3 /*0.001*/);

    }


    //[Fact]
    //public void Test()
    //{
    //    foreach (var item in GeometrySamples.AllGeometries)
    //    {
    //        if (item.IsNullOrEmpty())
    //        {
    //            continue;
    //        }

    //        var stat = new AreaStatistics(item);
    //    }
    //}

}
