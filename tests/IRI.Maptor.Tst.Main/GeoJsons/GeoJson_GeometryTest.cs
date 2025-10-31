using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives; 
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;

namespace IRI.Maptor.Tst.Main.GeoJsons;

public class GeoJson_GeometryTest
{
    [Fact]
    public void TestGeoJson_GeometryConversion()
    {
        //Test Point
        var geoJsonPoint = Geometry<Point>.Create(30, 10).AsGeoJson();
        var point = geoJsonPoint.Parse(true);
        Assert.Equal(point.Points[0].X, 30);
        Assert.Equal(point.Points[0].Y, 10);

        //Test MultiPoint 
        var multiPointString = "{\"type\": \"MultiPoint\", \"coordinates\": [[10.1, 40.1], [40.1, 30.1], [20.1, 20.1], [30.1, 10.1]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry geoJsonMultiPoint = GeoJson.Deserialize(multiPointString);
        Assert.Equal(multiPointString, geoJsonMultiPoint.Parse(true).AsGeoJson(true).Serialize(false, true));

        //Test LineString
        var lineStringString = "{\"type\": \"LineString\", \"coordinates\": [[30.1, 10.1], [10.1, 30.1], [40.1, 40.1]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry lineString = GeoJson.Deserialize(lineStringString);
        Assert.Equal(lineStringString, lineString.Parse(true).AsGeoJson(true).Serialize(false, true));

        //Test MultiLineString
        var multiLineStringString = "{\"type\": \"MultiLineString\",\"coordinates\": [[[10.1, 10.1], [20.1, 20.1], [10.1, 40.1]],[[40.1, 40.1], [30.1, 30.1], [40.1, 20.1], [30.1, 10.1]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry multiLineString = GeoJson.Deserialize(multiLineStringString);
        Assert.Equal(multiLineStringString, multiLineString.Parse(true).AsGeoJson(true).Serialize(false, true));

        //Test Polygon
        var polygonString1 = "{\"type\": \"Polygon\",\"coordinates\": [[[30.1, 10.1], [40.1, 40.1], [20.1, 40.1], [10.1, 20.1], [30.1, 10.1]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry polygon1 = GeoJson.Deserialize(polygonString1);
        Assert.Equal(polygonString1, polygon1.Parse(true).AsGeoJson(true).Serialize(false, true));

        var polygonString2 = "{\"type\": \"Polygon\",\"coordinates\": [[[35.1, 10.1], [45.1, 45.1], [15.1, 40.1], [10.1, 20.1], [35.1, 10.1]],[[20.1, 30.1], [35.1, 35.1], [30.1, 20.1], [20.1, 30.1]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry polygon2 = GeoJson.Deserialize(polygonString2);
        Assert.Equal(polygonString2, polygon2.Parse(true).AsGeoJson(true).Serialize(false, true));

        //Test MultiPolygon
        var multiPolygonString1 = "{\"type\": \"MultiPolygon\",\"coordinates\": [[[[30.1, 20.1], [45.1, 40.1], [10.1, 40.1], [30.1, 20.1]]],[[[15.1, 5.1], [40.1, 10.1], [10.1, 20.1], [5.1, 10.1], [15.1, 5.1]]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry multiPolygon1 = GeoJson.Deserialize(multiPolygonString1);
        Assert.Equal(multiPolygonString1, multiPolygon1.Parse(true).AsGeoJson(true).Serialize(false, true));

        var multiPolygonString2 = "{\"type\": \"MultiPolygon\",\"coordinates\": [[[[40.1, 40.1], [20.1, 45.1], [45.1, 30.1], [40.1, 40.1]]],[[[20.1, 35.1], [10.1, 30.1], [10.1, 10.1], [30.1, 5.1], [45.1, 20.1], [20.1, 35.1]],[[30.1, 20.1], [20.1, 15.1], [20.1, 25.1], [30.1, 20.1]]]]}".Replace(" ", string.Empty);
        IGeoJsonGeometry multiPolygon2 = GeoJson.Deserialize(multiPolygonString2);
        Assert.Equal(multiPolygonString2, multiPolygon2.Parse(true).AsGeoJson(true).Serialize(false, true));
    }
}
