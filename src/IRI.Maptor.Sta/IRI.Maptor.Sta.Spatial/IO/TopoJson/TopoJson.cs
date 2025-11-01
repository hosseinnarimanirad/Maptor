using System.Text.Json;
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
}

