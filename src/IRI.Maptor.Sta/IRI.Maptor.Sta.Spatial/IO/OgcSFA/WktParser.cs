using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.OgcSFA;

/// <summary>
/// Compatibility wrapper for WktReader and WktWriter.
/// For new code, use WktReader.Parse() and WktWriter.AsWkt() directly.
/// </summary>
public static class WktParser
{
    /// <summary>
    /// Parses a WKT string into a Geometry. Supports 2D, Z, M, and ZM coordinate variants.
    /// </summary>
    public static Geometry<Point> Parse(string wktString, int srid = 0)
    {
        return WktReader.Parse(wktString, srid);
    }

    /// <summary>
    /// Converts a Geometry to WKT string format. Automatically detects and writes Z/M/ZM variants.
    /// </summary>
    internal static string AsWkt<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        return WktWriter.AsWkt(geometry);
    }
}
