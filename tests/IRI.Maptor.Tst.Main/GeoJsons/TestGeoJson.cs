using System.IO;
using System.Linq;
using System.Reflection;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tst.GeoJsons;

public class TestGeoJson
{
    public TestGeoJson()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }
    
    [Fact]
    public void TestGeoJsonSerialize()
    {
        //Test Point
        var point = Geometry<Point>.Create(30, 10).AsSqlGeometry().AsGeoJson().Serialize(false).Replace(" ", string.Empty);
        Assert.Equal(point, "{\"type\": \"Point\", \"coordinates\": [30, 10]}".Replace(" ", string.Empty));

        //Test MultiPoint 
        var multiPointString = "{\"type\": \"MultiPoint\", \"coordinates\": [[10.1, 40.1], [40.1, 30.1], [20.1, 20.1], [30.1, 10.1]]}";
        IGeoJsonGeometry multiPoint = GeoJson.Deserialize(multiPointString);
        Assert.Equal(multiPoint.Serialize(false).Replace(" ", string.Empty), multiPointString.Replace(" ", string.Empty));

        //Test LineString
        var lineStringString = "{\"type\": \"LineString\", \"coordinates\": [[30.1, 10.1], [10.1, 30.1], [40.1, 40.1]]}";
        IGeoJsonGeometry lineString = GeoJson.Deserialize(lineStringString);
        Assert.Equal(lineString.Serialize(false).Replace(" ", string.Empty), lineStringString.Replace(" ", string.Empty));

        //Test MultiLineString
        var multiLineStringString = "{\"type\": \"MultiLineString\",\"coordinates\": [[[10.1, 10.1], [20.1, 20.1], [10.1, 40.1]],[[40.1, 40.1], [30.1, 30.1], [40.1, 20.1], [30.1, 10.1]]]}";
        IGeoJsonGeometry multiLineString = GeoJson.Deserialize(multiLineStringString);
        Assert.Equal(multiLineString.Serialize(false).Replace(" ", string.Empty), multiLineStringString.Replace(" ", string.Empty));

        //Test Polygon
        var polygonString1 = "{\"type\": \"Polygon\",\"coordinates\": [[[30.1, 10.1], [40.1, 40.1], [20.1, 40.1], [10.1, 20.1], [30.1, 10.1]]]}";
        IGeoJsonGeometry polygon1 = GeoJson.Deserialize(polygonString1);
        Assert.Equal(polygon1.Serialize(false).Replace(" ", string.Empty), polygonString1.Replace(" ", string.Empty));

        var polygonString2 = "{\"type\": \"Polygon\",\"coordinates\": [[[35.1, 10.1], [45.1, 45.1], [15.1, 40.1], [10.1, 20.1], [35.1, 10.1]],[[20.1, 30.1], [35.1, 35.1], [30.1, 20.1], [20.1, 30.1]]]}";
        IGeoJsonGeometry polygon2 = GeoJson.Deserialize(polygonString2);
        Assert.Equal(polygon2.Serialize(false).Replace(" ", string.Empty), polygonString2.Replace(" ", string.Empty));

        //Test MultiPolygon
        var multiPolygonString1 = "{\"type\": \"MultiPolygon\",\"coordinates\": [[[[30.1, 20.1], [45.1, 40.1], [10.1, 40.1], [30.1, 20.1]]],[[[15.1, 5.1], [40.1, 10.1], [10.1, 20.1], [5.1, 10.1], [15.1, 5.1]]]]}";
        IGeoJsonGeometry multiPolygon1 = GeoJson.Deserialize(multiPolygonString1);
        Assert.Equal(multiPolygon1.Serialize(false).Replace(" ", string.Empty), multiPolygonString1.Replace(" ", string.Empty));

        var multiPolygonString2 = "{\"type\": \"MultiPolygon\",\"coordinates\": [[[[40.1, 40.1], [20.1, 45.1], [45.1, 30.1], [40.1, 40.1]]],[[[20.1, 35.1], [10.1, 30.1], [10.1, 10.1], [30.1, 5.1], [45.1, 20.1], [20.1, 35.1]],[[30.1, 20.1], [20.1, 15.1], [20.1, 25.1], [30.1, 20.1]]]]}";
        IGeoJsonGeometry multiPolygon2 = GeoJson.Deserialize(multiPolygonString2);
        Assert.Equal(multiPolygon2.Serialize(false).Replace(" ", string.Empty), multiPolygonString2.Replace(" ", string.Empty));
    }

    [Fact]
    public void TestGeoJsonToWkt()
    {
        var networkblocks = GeoJson.ParseToGeoJsonFeatures(ReadFile("networkblock.json"));

        var checkSqlGeometryAndGeometryConversions = networkblocks.Select(f => f.Geometry.AsSqlGeometry().AsWkt() == f.Geometry.Parse().AsSqlGeometry().AsWkt());

        if (checkSqlGeometryAndGeometryConversions.Any(r => r == false))
            throw new NotImplementedException();

        var checkIShapeConversions = networkblocks.Select(f => f.Geometry.AsEsriShape().AsSqlGeometry().AsWkt() == f.Geometry.AsSqlGeometry().AsWkt());

        if (checkIShapeConversions.Any(r => r == false))
            throw new NotImplementedException();
         
        var stations = GeoJson.ParseToGeoJsonFeatures(ReadFile("stations.json"));

        var checkSqlGeometryAndGeometryConversions2 = stations.Select(f => f.Geometry.AsSqlGeometry().AsWkt() == f.Geometry.Parse().AsSqlGeometry().AsWkt());

        if (checkSqlGeometryAndGeometryConversions2.Any(r => r == false))
            throw new NotImplementedException();

        var checkIShapeConversions2 = stations.Select(f => f.Geometry.AsEsriShape().AsSqlGeometry().AsWkt() == f.Geometry.AsSqlGeometry().AsWkt());

        if (checkIShapeConversions2.Any(r => r == false))
            throw new NotImplementedException();
    }

    private string ReadFile(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resourceName = $"IRI.Maptor.Tst.Main.Assets.GeoJsonSamples.{fileName}";

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
}
