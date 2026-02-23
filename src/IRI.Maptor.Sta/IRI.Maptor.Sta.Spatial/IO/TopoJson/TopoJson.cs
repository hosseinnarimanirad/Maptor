using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    /// <summary>
    /// Read TopoJSON from file
    /// </summary>
    public static TopoJsonTopology ReadFromFile(string fileName)
    {
        var topoJsonString = File.ReadAllText(fileName);
        return Parse(topoJsonString);
    }

    /// <summary>
    /// Parse TopoJSON string to topology object
    /// </summary>
    public static TopoJsonTopology Parse(string topoJsonString)
    {
        return JsonHelper.Deserialize<TopoJsonTopology>(topoJsonString) 
               ?? throw new InvalidOperationException("Failed to parse TopoJSON");
    }

    /// <summary>
    /// Write TopoJSON to file
    /// </summary>
    public static void WriteToFile(TopoJsonTopology topology, string fileName, bool indented = true)
    {
        var json = Serialize(topology, indented);
        File.WriteAllText(fileName, json);
    }

    /// <summary>
    /// Serialize topology to TopoJSON string
    /// </summary>
    public static string Serialize(TopoJsonTopology topology, bool indented = true)
    {
        return JsonHelper.Serialize(topology, indented);
    }

    /// <summary>
    /// Convert Geometry to TopoJSON
    /// </summary>
    public static TopoJsonTopology FromGeometry(Geometry<Point> geometry, string objectName = "geometry", bool quantize = true, int quantizationFactor = 10000)
    {
        return TopoJsonConverter.FromGeometry(geometry, objectName, quantize, quantizationFactor);
    }

    /// <summary>
    /// Convert TopoJSON to Geometry
    /// </summary>
    public static Dictionary<string, Geometry<Point>> ToGeometry(TopoJsonTopology topology, int srid = 4326)
    {
        return TopoJsonConverter.ToGeometry(topology, srid);
    }

    /// <summary>
    /// Convert multiple geometries to TopoJSON with shared topology
    /// </summary>
    public static TopoJsonTopology FromGeometries(Dictionary<string, Geometry<Point>> geometries, bool quantize = true, int quantizationFactor = 10000)
    {
        return TopoJsonConverter.FromGeometries(geometries, quantize, quantizationFactor);
    }

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
            if (geom == null || geom.IsNullOrEmpty())
                continue;
            var points = geom.GetAllPoints();
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

