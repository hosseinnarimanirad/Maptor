using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.TopoJson;

/// <summary>
/// Main entry point for TopoJSON operations
/// </summary>
public static class TopoJson
{
    public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = false,
        // default; can be overridden per call
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };




    /// <summary>
    /// Read TopoJSON from file
    /// </summary>
    public static async Task<TopoJsonTopology> ReadFromFileAsync(string fileName)
    {
        var topoJsonString = await File.ReadAllTextAsync(fileName);

        return Parse(topoJsonString);
    }

    /// <summary>
    /// Parse TopoJSON string to topology object
    /// </summary>
    public static TopoJsonTopology Parse(string topoJsonString)
    {
        var topology = JsonHelper.Deserialize<TopoJsonTopology>(topoJsonString, DefaultOptions)
               ?? throw new InvalidOperationException("Failed to parse TopoJSON");

        return topology;
    }

    ///// <summary>
    ///// Writes a list of Geometry objects to a TopoJSON file, automatically assigning names.
    ///// </summary>
    ///// <param name="geometries">List of geometries to export.</param>
    ///// <param name="fileName">Output file path.</param>
    ///// <param name="objectNamePrefix">Prefix for auto-generated object names (default "geometry").</param>
    ///// <param name="quantize">Whether to apply quantization (default true).</param>
    ///// <param name="quantizationFactor">Quantization factor (default 10000).</param>
    ///// <param name="indented">Whether to produce pretty-printed JSON (default true).</param>
    //public static async Task WriteToFileAsync(
    //    IReadOnlyList<Geometry<Point>> geometries,
    //    string fileName,
    //    string objectNamePrefix = "geometry",
    //    bool quantize = true,
    //    int quantizationFactor = 10000,
    //    bool indented = true)
    //{
    //    if (geometries == null || geometries.Count == 0)
    //        throw new ArgumentException("The geometries list is null or empty.", nameof(geometries));

    //    // Build a dictionary with automatically named objects
    //    var namedGeometries = new Dictionary<string, Geometry<Point>>();

    //    for (int i = 0; i < geometries.Count; i++)
    //    {
    //        var geom = geometries[i];

    //        if (geom != null && !geom.IsNullOrEmpty())
    //        {
    //            string name = $"{objectNamePrefix}{i}";

    //            namedGeometries[name] = geom;
    //        }
    //    }

    //    if (namedGeometries.Count == 0)
    //        throw new InvalidOperationException("No valid geometries to write.");

    //    var topology = FromGeometries(namedGeometries, quantize, quantizationFactor);

    //    await WriteToFileAsync(topology, fileName, indented);
    //}

    /// <summary>
    /// Writes a list of Feature<Point> objects to a TopoJSON file, preserving attributes and IDs.
    /// </summary>
    /// <param name="features">List of features to write.</param>
    /// <param name="fileName">Output file path.</param>
    /// <param name="quantize">Whether to apply quantization (default true).</param>
    /// <param name="quantizationFactor">Quantization factor (default 10000).</param>
    /// <param name="indented">Whether to produce pretty-printed JSON (default true).</param>  
    public static async Task WriteToFileAsync(
        IReadOnlyList<Feature<Point>> features,
        string fileName,
        bool quantize = true,
        int quantizationFactor = 10000,
        bool indented = true,
        string collectionName = "data")
    {
        if (features == null || features.Count == 0)
            throw new ArgumentException("The features list is null or empty.", nameof(features));

        var topology = TopoJsonConverter.FromFeatures(features, quantize, quantizationFactor, collectionName);

        await WriteToFileAsync(topology, fileName, indented);
    }

    /// <summary>
    /// Write TopoJSON to file
    /// </summary>
    public static async Task WriteToFileAsync(TopoJsonTopology topology, string fileName, bool indented = true)
    {
        var json = Serialize(topology, indented);

        await File.WriteAllTextAsync(fileName, json);
    }

    /// <summary>
    /// Serialize topology to TopoJSON string
    /// </summary>
    public static string Serialize(TopoJsonTopology topology, bool indented = true)
    {
        return JsonHelper.Serialize(topology, indented);
    }

    ///// <summary>
    ///// Convert Geometry to TopoJSON
    ///// </summary>
    //public static TopoJsonTopology FromGeometry(Geometry<Point> geometry, string objectName = "geometry", bool quantize = true, int quantizationFactor = 10000)
    //{
    //    return TopoJsonConverter.FromGeometry(geometry, objectName, quantize, quantizationFactor);
    //}

    /// <summary>
    /// Convert TopoJSON to Geometry
    /// </summary>
    public static Dictionary<string, Feature<Point>> ToFeature(TopoJsonTopology topology, int srid = 4326)
    {
        return TopoJsonConverter.ToGeometry(topology, srid);
    }

    ///// <summary>
    ///// Convert multiple geometries to TopoJSON with shared topology
    ///// </summary>
    //public static TopoJsonTopology FromGeometries(Dictionary<string, Geometry<Point>> geometries, bool quantize = true, int quantizationFactor = 10000)
    //{
    //    return TopoJsonConverter.FromGeometries(geometries, quantize, quantizationFactor);
    //}

    /// <summary>
    /// Extracts sample points from a TopoJSON topology for preview display.
    /// </summary>
    /// <param name="topology">The parsed TopoJSON topology.</param>
    /// <param name="srid">Spatial reference system for coordinate interpretation. Default 4326 (WGS84).</param>
    /// <param name="maxPoints">Maximum number of points to extract. Default 50.</param>
    /// <returns>List of raw points (X, Y) for preview.</returns>
    public static IReadOnlyList<Point> ExtractSamplePoints(TopoJsonTopology topology, int srid = 4326, int maxPoints = 50)
    {
        var result = new List<Point>();
        if (topology?.Objects == null)
            return result;

        var geometries = TopoJsonConverter.ToGeometry(topology, srid);

        foreach (var kvp in geometries)
        {
            if (result.Count >= maxPoints)
                break;
            var geom = kvp.Value;

            if (geom == null || geom.TheGeometry.IsNullOrEmpty())
                continue;

            var points = geom.TheGeometry.GetAllPoints();

            if (points == null) continue;
            foreach (var p in points)
            {
                if (result.Count >= maxPoints)
                    break;
                result.Add(new Point(p.X, p.Y));
            }
        }
        return result;
    }
}

