using System;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Core.Spatial.IO.VectorTiles;

/// <summary>
/// Builds the transform from MVT tile-local coordinates (origin top-left, y down, range
/// [0, extent]) to spherical Web Mercator (EPSG:3857), for a given XYZ slippy-tile address.
/// Linear and exact for slippy tiles, matching the spherical model used elsewhere in the app.
/// </summary>
public static class MvtTileTransform
{
    /// <summary>Half the Web Mercator world span (≈ 20037508.34 m).</summary>
    public static readonly double MaxExtent = WebMercatorUtility.EarthRadius * Math.PI;

    /// <param name="zoom">Tile zoom level.</param>
    /// <param name="tileColumn">XYZ tile column (X).</param>
    /// <param name="tileRow">XYZ tile row (Y), origin at top.</param>
    /// <param name="extent">Tile-local extent (typically 4096).</param>
    public static Func<int, int, Point> LocalToWebMercator(int zoom, int tileColumn, int tileRow, uint extent)
    {
        double tileSpan = (2.0 * MaxExtent) / (1L << zoom);

        double xMin = -MaxExtent + tileColumn * tileSpan;
        double yMax = MaxExtent - tileRow * tileSpan;

        double unit = tileSpan / extent;

        return (localX, localY) => new Point(xMin + localX * unit, yMax - localY * unit);
    }
}
