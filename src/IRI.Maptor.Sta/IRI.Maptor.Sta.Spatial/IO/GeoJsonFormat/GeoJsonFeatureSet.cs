using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;


public class GeoJsonFeatureSet
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "FeatureCollection";

    [JsonPropertyName("totalFeatures")]
    public int TotalFeatures { get; set; }

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; set; }

    [JsonPropertyName("crs")]
    public GeoJsonCrs Crs { get; set; }

    public void Save(string fileName, bool indented, bool removeSpaces = false)
    {
        var result = JsonHelper.Serialize(this, indented);

        System.IO.File.WriteAllText(fileName, removeSpaces ? result.Replace(" ", string.Empty) : result);
    }

    public static GeoJsonFeatureSet Load(string fileName)
    {
        return Parse(System.IO.File.ReadAllText(fileName));
    }

    public static GeoJsonFeatureSet Parse(string geoJsonFeaturesSetString)
    {
        return JsonHelper.Deserialize<GeoJsonFeatureSet>(geoJsonFeaturesSetString);
    }


    public static GeoJsonFeatureSet DelimitedToPointGeoJson(string fileName, bool userFirstLineAsHeader, params char[] delimited)
    {
        var rawData = IOHelper.ReadAllDelimitedFile(fileName, delimited);

        List<GeoJsonFeature> result = new List<GeoJsonFeature>();

        int startIndex = 0;

        List<string> header = new List<string>();

        if (userFirstLineAsHeader)
        {
            startIndex = 1;

            header = rawData[0].Skip(2).ToList();
        }
        else
        {
            header = Enumerable.Range(1, rawData[0].Length - 2).Select(i => $"header {i}").ToList();
        }

        for (int i = startIndex; i < rawData.Count; i++)
        {
            double longitude = double.Parse(rawData[i][0]);

            double latitude = double.Parse(rawData[i][1]);

            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            for (int p = 2; p < rawData[i].Length; p++)
            {
                dictionary.Add(header[p - 2], rawData[i][p]);
            }

            result.Add(new GeoJsonFeature()
            {
                Geometry = GeoJsonPoint.Create(longitude, latitude),
                Geometry_name = $"point {i}",
                Id = i.ToString(),
                Type = GeoJson.Point,
                Properties = dictionary
            });
        }

        return new GeoJsonFeatureSet() { Features = result, TotalFeatures = result.Count, Type = "FeatureSet" };
    }

    public static GeoJsonFeatureSet CsvToPointGeoJson(string fileName, bool userFirstLineAsHeader)
    {
        return DelimitedToPointGeoJson(fileName, userFirstLineAsHeader, IOHelper.CsvDelimiterChar);
    }

    public static GeoJsonFeatureSet TsvToPointGeoJson(string fileName, bool userFirstLineAsHeader)
    {
        return DelimitedToPointGeoJson(fileName, userFirstLineAsHeader, IOHelper.TsvDelimiterChar);
    }
}

public class GeoJsonCrs
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; }
}

public class Properties
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
