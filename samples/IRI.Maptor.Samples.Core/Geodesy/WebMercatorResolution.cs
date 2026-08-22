using IRI.Maptor.Samples.Core.Runner;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Samples.Core.Geodesy;

/// <summary>
/// Ground resolution (metres per pixel) and map scale of Web Mercator tiles, per zoom level,
/// at the equator and at 45° / 80° latitude — the numbers behind every "zoom level" in a tile map.
/// </summary>
public static class WebMercatorResolution
{
    [Sample("geodesy/web-mercator-resolution", "Web Mercator ground resolution and scale per zoom level")]
    public static void Run()
    {
        Console.WriteLine("| zoom | resolution @0°  | resolution @45° | resolution @80° |");
        Console.WriteLine("| ---- | --------------- | --------------- | --------------- |");

        for (int zoom = 1; zoom <= 24; zoom++)
        {
            var r00 = WebMercatorUtility.CalculateGroundResolution(zoom, 0);
            var r45 = WebMercatorUtility.CalculateGroundResolution(zoom, 45);
            var r80 = WebMercatorUtility.CalculateGroundResolution(zoom, 80);

            Console.WriteLine($"| {zoom,-4} | {FormatDistance(r00),-15} | {FormatDistance(r45),-15} | {FormatDistance(r80),-15} |");
        }

        Console.WriteLine();
        Console.WriteLine("| zoom | tiles               | scale @0°        | scale @45°       | scale @80°       |");
        Console.WriteLine("| ---- | ------------------- | ---------------- | ---------------- | ---------------- |");

        for (int zoom = 1; zoom <= 24; zoom++)
        {
            var s00 = 1.0 / WebMercatorUtility.CalculateMapScale(zoom, 0);
            var s45 = 1.0 / WebMercatorUtility.CalculateMapScale(zoom, 45);
            var s80 = 1.0 / WebMercatorUtility.CalculateMapScale(zoom, 80);

            var tiles = Math.Pow(2, 2 * zoom);

            Console.WriteLine($"| {zoom,-4} | {tiles,19:N0} | {"1:" + s00.ToString("N0"),-16} | {"1:" + s45.ToString("N0"),-16} | {"1:" + s80.ToString("N0"),-16} |");
        }
    }

    static string FormatDistance(double meters) => meters switch
    {
        >= 1000 => $"{meters / 1000:0.0} km",
        >= 1 => $"{meters:0.0} m",
        >= 0.01 => $"{meters * 100:0.0} cm",
        _ => $"{meters * 1000:0.0} mm",
    };
}
