using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Types;

using IRI.Maptor.Extensions;
using IRI.Maptor.Tst.Assets;
using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;

namespace IRI.Maptor.Tst.Main.Esri;

public class EsriShape_GeometryTest
{
    public EsriShape_GeometryTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    // Commented out original test methods - replaced with parameterized versions below

    //[Fact]
    //public void TestEsriShape_GeometryConversion()
    //{
    //    var geometries = GeometrySamples.AllGeometries.Where(g => !g.IsNullOrEmpty()).ToList();

    //    var esriShapes = geometries.Select(g => g.AsEsriShape()).ToList();

    //    for (int i = 0; i < esriShapes.Count; i++)
    //    {
    //        var geometry = geometries[i];

    //        var newGeometry = esriShapes[i].AsGeometry();

    //        Assert.Equal(newGeometry.NumberOfPoints, geometry.NumberOfPoints);
    //        Assert.Equal(newGeometry.Geometries?.Count, geometry.Geometries?.Count);
    //        Assert.Equal(newGeometry.Type, geometry.Type);

    //        Assert.True(geometry.GetAllPoints().SequenceEqual(newGeometry.GetAllPoints()));
    //    }
    //}


    //[Fact]
    //public void TestEsriShape_SqlGeometry_GeometryConversion()
    //{
    //    Test(SqlGeometrySamples.Point);
    //    Test(SqlGeometrySamples.PointZ);
    //    Test(SqlGeometrySamples.PointZM);
    //    Test(SqlGeometrySamples.Multipoint);
    //    Test(SqlGeometrySamples.MultipointComplex);
    //    Test(SqlGeometrySamples.Linestring);
    //    Test(SqlGeometrySamples.LinestringZM);
    //    Test(SqlGeometrySamples.MultiLineString);

    //    Test(SqlGeometrySamples.Polygon);
    //    Test(SqlGeometrySamples.PolygonWithHole);
    //    //Test(SqlGeometrySamples.MultiPolygon01);
    //    //Test(SqlGeometrySamples.MultiPolygon02);
    //}

    //private void Test(SqlGeometry sqlGeometry)
    //{
    //    var esriShape = sqlGeometry.AsEsriShape();

    //    var geometry = esriShape.AsGeometry();

    //    var geometry2 = sqlGeometry.AsGeometry();

    //    Assert.Equal(geometry2.AsSqlGeometry().AsWkt(), geometry.AsSqlGeometry().AsWkt());
    //}

    // MemberData methods for parameterized tests

    public static IEnumerable<object[]> GeometryConversionTestData()
    {
        var geometries = GeometrySamples.AllGeometries.Where(g => !g.IsNullOrEmpty()).ToList();
        foreach (var geometry in geometries)
        {
            yield return new object[] { geometry };
        }
    }

    public static IEnumerable<object[]> SqlGeometryConversionTestData()
    {
        yield return new object[] { SqlGeometrySamples.Point };
        yield return new object[] { SqlGeometrySamples.PointZ };
        yield return new object[] { SqlGeometrySamples.PointZM };
        yield return new object[] { SqlGeometrySamples.Multipoint };
        yield return new object[] { SqlGeometrySamples.MultipointComplex };
        yield return new object[] { SqlGeometrySamples.Linestring };
        yield return new object[] { SqlGeometrySamples.LinestringZM };
        yield return new object[] { SqlGeometrySamples.MultiLineString };
        yield return new object[] { SqlGeometrySamples.Polygon };
        yield return new object[] { SqlGeometrySamples.PolygonWithHole };
        // Uncomment below if needed:
        //yield return new object[] { SqlGeometrySamples.MultiPolygon01 };
        //yield return new object[] { SqlGeometrySamples.MultiPolygon02 };
    }

    // New parameterized test methods with AAA pattern

    [Theory]
    [MemberData(nameof(GeometryConversionTestData))]
    public void TestEsriShape_GeometryConversion_Parameterized(Geometry geometry)
    {
        // Arrange
        // (Input geometry is provided by MemberData)

        // Act
        var esriShape = geometry.AsEsriShape();
        var newGeometry = esriShape.AsGeometry();

        // Assert
        Assert.Equal(newGeometry.NumberOfPoints, geometry.NumberOfPoints);
        Assert.Equal(newGeometry.Geometries?.Count, geometry.Geometries?.Count);
        Assert.Equal(newGeometry.Type, geometry.Type);
        Assert.True(geometry.GetAllPoints().SequenceEqual(newGeometry.GetAllPoints()));
    }

    [Theory]
    [MemberData(nameof(SqlGeometryConversionTestData))]
    public void TestEsriShape_SqlGeometry_GeometryConversion_Parameterized(SqlGeometry sqlGeometry)
    {
        // Arrange
        // (Input sqlGeometry is provided by MemberData)

        // Act
        var esriShape = sqlGeometry.AsEsriShape();
        var geometry = esriShape.AsGeometry();
        var expectedGeometry = sqlGeometry.AsGeometry();

        // Assert
        Assert.Equal(expectedGeometry.AsSqlGeometry().AsWkt(), geometry.AsSqlGeometry().AsWkt());
    }
}
