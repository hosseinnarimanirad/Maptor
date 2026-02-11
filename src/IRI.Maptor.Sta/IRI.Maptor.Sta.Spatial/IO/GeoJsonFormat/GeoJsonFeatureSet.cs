using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON FeatureCollection object (RFC 7946).
/// </summary>
public class GeoJsonFeatureSet
{
    private const string _geoJsonFeatureSetType = "FeatureCollection";

    public static readonly GeoJsonFeatureSet Empty;

    /// <summary>
    /// Gets or sets the type of the GeoJSON object. Must be "FeatureCollection".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = GeoJson.FeatureCollection;

    /// <summary>
    /// Gets or sets the total number of features in the collection.
    /// </summary>
    [JsonPropertyName("totalFeatures")]
    public int TotalFeatures { get; set; }

    /// <summary>
    /// Gets or sets the list of features in the collection.
    /// </summary>
    [JsonPropertyName("features")]
    public List<GeoJsonFeature>? Features { get; set; }

    /// <summary>
    /// Gets or sets the coordinate reference system. Note: CRS is deprecated in RFC 7946.
    /// </summary>
    [JsonPropertyName("crs")]
    public GeoJsonCrs? Crs { get; set; }

    /// <summary>
    /// Saves this FeatureCollection to a file.
    /// </summary>
    /// <param name="fileName">The path where the file will be saved.</param>
    /// <param name="indented">If true, the JSON output will be indented.</param>
    /// <param name="removeSpaces">If true, all spaces will be removed from the output.</param>
    public void Save(string fileName, bool indented, bool removeSpaces = false)
    {
        var result = JsonHelper.Serialize(this, indented);

        System.IO.File.WriteAllText(fileName, removeSpaces ? result.Replace(" ", string.Empty) : result);
    }

    static GeoJsonFeatureSet()
    {
        Empty = new GeoJsonFeatureSet() { Type = _geoJsonFeatureSetType, Features = [], TotalFeatures = 0 };
    }

    /// <summary>
    /// Loads a GeoJSON FeatureCollection from a file.
    /// </summary>
    /// <param name="fileName">The path to the GeoJSON file.</param>
    /// <returns>A GeoJsonFeatureSet instance.</returns>
    public static GeoJsonFeatureSet Load(string fileName)
    {
        return Parse(System.IO.File.ReadAllText(fileName));
    }

    /// <summary>
    /// Parses a GeoJSON FeatureCollection string.
    /// </summary>
    /// <param name="geoJsonFeaturesSetString">The GeoJSON FeatureCollection string to parse.</param>
    /// <returns>A GeoJsonFeatureSet instance.</returns>
    public static GeoJsonFeatureSet Parse(string geoJsonFeaturesSetString)
    {
        return JsonHelper.Deserialize<GeoJsonFeatureSet>(geoJsonFeaturesSetString) ?? Empty;
    }

    public static GeoJsonFeatureSet Create(IGeoJsonGeometry geometry, Dictionary<string, object>? attributes = null)
    {
        if (geometry == null)
            return Empty;

        return new GeoJsonFeatureSet()
        {
            TotalFeatures = 1,
            Type = _geoJsonFeatureSetType,             
            Features = [GeoJsonFeature.Create(geometry, attributes)],
        };
    }

    /// <summary>
    /// Converts a delimited file (CSV, TSV, etc.) to a GeoJSON FeatureCollection of points.
    /// </summary>
    /// <param name="fileName">The path to the delimited file.</param>
    /// <param name="userFirstLineAsHeader">If true, the first line is treated as a header row.</param>
    /// <param name="delimited">The delimiter characters used in the file.</param>
    /// <returns>A GeoJsonFeatureSet containing point features.</returns>
    public static GeoJsonFeatureSet DelimitedToPointGeoJson(string fileName, bool userFirstLineAsHeader, params char[] delimited)
    {
        var rawData = IOHelper.ReadAllDelimitedFile(fileName, delimited);

        return CreateFromDelimited(rawData, userFirstLineAsHeader);
    }

    public static async Task<GeoJsonFeatureSet> DelimitedToPointGeoJsonAsync(string fileName, bool userFirstLineAsHeader, params char[] delimited)
    {
        var rawData = await IOHelper.ReadAllDelimitedFileAsync(fileName, delimited);

        return CreateFromDelimited(rawData, userFirstLineAsHeader);
    }

    /// <summary>
    /// Converts a CSV file to a GeoJSON FeatureCollection of points.
    /// </summary>
    /// <param name="fileName">The path to the CSV file.</param>
    /// <param name="userFirstLineAsHeader">If true, the first line is treated as a header row.</param>
    /// <returns>A GeoJsonFeatureSet containing point features.</returns>
    public static GeoJsonFeatureSet CsvToPointGeoJson(string fileName, bool userFirstLineAsHeader)
    {
        return DelimitedToPointGeoJson(fileName, userFirstLineAsHeader, IOHelper.CsvDelimiterChar);
    }

    /// <summary>
    /// Converts a TSV (Tab-Separated Values) file to a GeoJSON FeatureCollection of points.
    /// </summary>
    /// <param name="fileName">The path to the TSV file.</param>
    /// <param name="userFirstLineAsHeader">If true, the first line is treated as a header row.</param>
    /// <returns>A GeoJsonFeatureSet containing point features.</returns>
    public static GeoJsonFeatureSet TsvToPointGeoJson(string fileName, bool userFirstLineAsHeader)
    {
        return DelimitedToPointGeoJson(fileName, userFirstLineAsHeader, IOHelper.TsvDelimiterChar);
    }

    public static Task<GeoJsonFeatureSet> CsvToPointGeoJsonAsync(string fileName, bool userFirstLineAsHeader)
    {
        return DelimitedToPointGeoJsonAsync(fileName, userFirstLineAsHeader, IOHelper.CsvDelimiterChar);
    }

    public static Task<GeoJsonFeatureSet> TsvToPointGeoJsonAsync(string fileName, bool userFirstLineAsHeader)
    {
        return DelimitedToPointGeoJsonAsync(fileName, userFirstLineAsHeader, IOHelper.TsvDelimiterChar);
    }

    private static GeoJsonFeatureSet CreateFromDelimited(List<string[]> rawData, bool userFirstLineAsHeader)
    {
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
                GeometryName = $"point {i}",
                Id = i.ToString(),
                Type = GeoJson.Point,
                Properties = dictionary
            });
        }

        return new GeoJsonFeatureSet() { Features = result, TotalFeatures = result.Count, Type = GeoJson.FeatureSet };
    }
}