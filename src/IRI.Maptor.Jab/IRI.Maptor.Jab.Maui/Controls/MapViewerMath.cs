using IRI.Maptor.Sta.Spatial.Helpers;

namespace IRI.Maptor.Jab.Maui.Controls;

/// <summary>
/// Conversions between map <c>resolution</c> (WebMercator units per device-independent
/// pixel) and Google/WebMercator integer zoom levels. Tile pyramid uses 256px tiles.
/// </summary>
internal static class MapViewerMath
{
    public const int TileSize = 256;
    public const int MinZoom = 1;
    public const int MaxZoom = 19;

    private static readonly double EarthCircumference = 2.0 * Math.PI * WebMercatorUtility.EarthRadius;

    public static double ZoomToResolution(int zoom)
    {
        zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        return EarthCircumference / (TileSize * Math.Pow(2, zoom));
    }

    public static int ResolutionToZoom(double resolution)
    {
        if (resolution <= 0)
        {
            return MaxZoom;
        }

        var zoom = Math.Log(EarthCircumference / (TileSize * resolution), 2);
        return Math.Clamp((int)Math.Round(zoom), MinZoom, MaxZoom);
    }

    public static double ClampResolution(double resolution)
    {
        // Smaller resolution == more zoomed in (higher zoom level).
        var min = ZoomToResolution(MaxZoom);
        var max = ZoomToResolution(MinZoom);
        return Math.Clamp(resolution, min, max);
    }
}
