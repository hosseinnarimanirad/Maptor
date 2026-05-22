using System.Text.Json.Serialization;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON FeatureCollection object (RFC 7946).
/// </summary>
public class GeoJsonFeatureSet
{
    #region Fields & Properties

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

    ///// <summary>
    ///// Gets or sets the coordinate reference system. Note: CRS is deprecated in RFC 7946.
    ///// </summary>
    //[JsonPropertyName("crs")]
    //public GeoJsonCrs? Crs { get; set; }

    #endregion

    static GeoJsonFeatureSet()
    {
        Empty = new GeoJsonFeatureSet() { Type = _geoJsonFeatureSetType, Features = [], TotalFeatures = 0 };
    }

    /// <summary>
    /// Saves this FeatureCollection to a file.
    /// </summary>
    /// <param name="fileName">The path where the file will be saved.</param>
    /// <param name="indented">If true, the JSON output will be indented.</param>
    /// <param name="removeSpaces">If true, all spaces will be removed from the output.</param>
    public async Task SaveAsync(string fileName, bool indented, bool removeSpaces = false)
    {
        var result = JsonHelper.Serialize(this, indented);

        await System.IO.File.WriteAllTextAsync(fileName, removeSpaces ? result.Replace(" ", string.Empty) : result);
    }

    /// <summary>
    /// Loads a GeoJSON FeatureCollection from a file.
    /// </summary>
    /// <param name="fileName">The path to the GeoJSON file.</param>
    /// <returns>A GeoJsonFeatureSet instance.</returns>
    public static async Task<GeoJsonFeatureSet> LoadAsync(string fileName)
    {
        var geojsonText = await System.IO.File.ReadAllTextAsync(fileName);

        return Parse(geojsonText);
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
    /// Extracts sample points from a GeoJSON FeatureCollection for preview display.
    /// Walks Point, MultiPoint, LineString, Polygon (and multi variants) to collect coordinates.
    /// </summary>
    /// <param name="featureSet">The parsed GeoJSON FeatureCollection.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are [lon, lat]; otherwise [lat, lon].</param>
    /// <param name="maxPoints">Maximum number of points to extract. Default 50.</param>
    /// <returns>List of raw points (X, Y) for preview; no SRS conversion.</returns>
    public static IReadOnlyList<Point> ExtractSamplePoints(GeoJsonFeatureSet featureSet, bool isLongitudeFirst, int maxPoints = 50)
    {
        var result = new List<Point>();
        if (featureSet?.Features == null)
            return result;

        foreach (var f in featureSet.Features)
        {
            if (result.Count >= maxPoints)
                break;

            if (f?.Geometry == null || f.Geometry.IsNullOrEmpty())
                continue;

            AddPointsFromGeometry(f.Geometry, isLongitudeFirst, result, maxPoints);
        }
        return result;
    }

    private static void AddPointsFromGeometry(IGeoJsonGeometry geometry, bool isLongitudeFirst, List<Point> result, int maxPoints)
    {
        switch (geometry)
        {
            case GeoJsonPoint pt when pt.Coordinates != null && pt.Coordinates.Length >= 2:
                AddPoint(pt.Coordinates, isLongitudeFirst, result);
                break;

            case GeoJsonMultiPoint mp when mp.Coordinates != null:
                foreach (var c in mp.Coordinates)
                {
                    if (result.Count >= maxPoints) return;
                    if (c != null && c.Length >= 2) AddPoint(c, isLongitudeFirst, result);
                }
                break;

            case GeoJsonLineString ls when ls.Coordinates != null:
                foreach (var c in ls.Coordinates)
                {
                    if (result.Count >= maxPoints) return;
                    if (c != null && c.Length >= 2) AddPoint(c, isLongitudeFirst, result);
                }
                break;

            case GeoJsonMultiLineString mls when mls.Coordinates != null:
                foreach (var line in mls.Coordinates)
                {
                    if (result.Count >= maxPoints) return;
                    if (line == null) continue;
                    foreach (var c in line)
                    {
                        if (result.Count >= maxPoints) return;
                        if (c != null && c.Length >= 2) AddPoint(c, isLongitudeFirst, result);
                    }
                }
                break;

            case GeoJsonPolygon poly when poly.Coordinates != null:
                foreach (var ring in poly.Coordinates)
                {
                    if (result.Count >= maxPoints) return;
                    if (ring == null) continue;
                    foreach (var c in ring)
                    {
                        if (result.Count >= maxPoints) return;
                        if (c != null && c.Length >= 2) AddPoint(c, isLongitudeFirst, result);
                    }
                }
                break;

            case GeoJsonMultiPolygon mpoly when mpoly.Coordinates != null:
                foreach (var polygon in mpoly.Coordinates)
                {
                    if (result.Count >= maxPoints) return;
                    if (polygon == null) continue;
                    foreach (var ring in polygon)
                    {
                        if (result.Count >= maxPoints) return;
                        if (ring == null) continue;
                        foreach (var c in ring)
                        {
                            if (result.Count >= maxPoints) return;
                            if (c != null && c.Length >= 2) AddPoint(c, isLongitudeFirst, result);
                        }
                    }
                }
                break;
        }
    }

    private static void AddPoint(double[] coords, bool isLongitudeFirst, List<Point> result)
    {
        double x = isLongitudeFirst ? coords[0] : coords[1];
        double y = isLongitudeFirst ? coords[1] : coords[0];
        result.Add(new Point(x, y));
    }
}