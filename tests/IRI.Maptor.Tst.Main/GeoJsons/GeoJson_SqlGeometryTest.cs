using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tst.GeoJsons;

public class GeoJson_SqlGeometryTest
{
    public GeoJson_SqlGeometryTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    [Fact]
    public void TestGeoJsonAndSqlGeometryConvert()
    {
        //Test Point
        var geoJsonPoint = Geometry<Point>.Create(30, 10).AsSqlGeometry().AsGeoJson();
        var point = geoJsonPoint.Parse(true);
        Assert.Equal(point.Points[0].X, 30);
        Assert.Equal(point.Points[0].Y, 10);

        //Test MultiPoint 
        var multiPointString = "{\"type\": \"MultiPoint\", \"coordinates\": [[10.1, 40.1], [40.1, 30.1], [20.1, 20.1], [30.1, 10.1]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry geoJsonMultiPoint = GeoJson.Deserialize(multiPointString);
        Assert.Equal(geoJsonMultiPoint.AsSqlGeometry(true).AsGeoJson().Serialize(false, true), multiPointString);

        //Test LineString
        var lineStringString = "{\"type\": \"LineString\", \"coordinates\": [[30.1, 10.1], [10.1, 30.1], [40.1, 40.1]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry lineString = GeoJson.Deserialize(lineStringString);
        Assert.Equal(lineString.AsSqlGeometry(true).AsGeoJson().Serialize(false, true), lineStringString);

        //Test MultiLineString
        var multiLineStringString = "{\"type\": \"MultiLineString\",\"coordinates\": [[[10.1, 10.1], [20.1, 20.1], [10.1, 40.1]],[[40.1, 40.1], [30.1, 30.1], [40.1, 20.1], [30.1, 10.1]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry multiLineString = GeoJson.Deserialize(multiLineStringString);
        Assert.Equal(multiLineString.AsSqlGeometry(true).AsGeoJson().Serialize(false, true), multiLineStringString);

        //Test Polygon
        var polygonString1 = "{\"type\": \"Polygon\",\"coordinates\": [[[30.1, 10.1], [40.1, 40.1], [20.1, 40.1], [10.1, 20.1], [30.1, 10.1]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry polygon1 = GeoJson.Deserialize(polygonString1);
        Assert.Equal(polygon1.AsSqlGeometry(true).AsGeoJson().Serialize(false, true), polygonString1);

        var polygonString2 = "{\"type\": \"Polygon\",\"coordinates\": [[[35.1, 10.1], [45.1, 45.1], [15.1, 40.1], [10.1, 20.1], [35.1, 10.1]],[[20.1, 30.1], [35.1, 35.1], [30.1, 20.1], [20.1, 30.1]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry polygon2 = GeoJson.Deserialize(polygonString2);
        var s1 = polygon2.AsSqlGeometry(true);
        var s2 = polygon2.AsSqlGeometry(true).AsGeoJson();
        Assert.Equal(polygon2.AsSqlGeometry(true).AsGeoJson().Serialize(false, true), polygonString2);

        //Test MultiPolygon
        var multiPolygonString1 = "{\"type\": \"MultiPolygon\",\"coordinates\": [[[[30.1, 20.1], [45.1, 40.1], [10.1, 40.1], [30.1, 20.1]]],[[[15.1, 5.1], [40.1, 10.1], [10.1, 20.1], [5.1, 10.1], [15.1, 5.1]]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry multiPolygon1 = GeoJson.Deserialize(multiPolygonString1);
        Assert.Equal(multiPolygon1.AsSqlGeometry(true).AsGeoJson().Serialize(false, true), multiPolygonString1);

        var multiPolygonString2 = "{\"type\": \"MultiPolygon\",\"coordinates\": [[[[40.1, 40.1], [20.1, 45.1], [45.1, 30.1], [40.1, 40.1]]],[[[20.1, 35.1], [10.1, 30.1], [10.1, 10.1], [30.1, 5.1], [45.1, 20.1], [20.1, 35.1]],[[30.1, 20.1], [20.1, 15.1], [20.1, 25.1], [30.1, 20.1]]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry multiPolygon2 = GeoJson.Deserialize(multiPolygonString2);
        Assert.Equal(multiPolygon2.AsSqlGeometry(true).AsGeoJson().Serialize(false, true), multiPolygonString2);

    }
}
