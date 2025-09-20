using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.Helpers;

namespace IRI.Maptor.Tag.SampleCodes.Geodesy;

public class GoogleMap_PixelDistance
{
    public static void GoogleMapGroundResolution()
    {

        // Print table header
        Console.WriteLine($"| zoom level | Resolution (Equator) | Resolution (45 N/S)  | Resolution (80 N/S) |");
        Console.WriteLine($"| ---------- | -------------------- | -------------------- | ------------------- |");

        for (int i = 1; i <= 24; i++)
        {
            var distance_00 = WebMercatorUtility.CalculateGroundResolution(i, 00);
            var distance_45 = WebMercatorUtility.CalculateGroundResolution(i, 45);
            var distance_80 = WebMercatorUtility.CalculateGroundResolution(i, 80);

            Console.WriteLine($"| {i,-10} | {FormatDistance(distance_00),-21}| {FormatDistance(distance_45),-21}| {FormatDistance(distance_80),-21}|");
        }


        Console.WriteLine();
        Console.WriteLine($"| zoom level |   number of tiles    | Scale (Equator) |  Scale (45 N/S) |  Scale (80 N/S) |");
        Console.WriteLine($"| ---------- | -------------------- | --------------- | --------------- | --------------- |");

        for (int i = 1; i <= 24; i++)
        {
            var scale_00 = 1.0 / WebMercatorUtility.CalculateMapScale(i, 00);
            var scale_45 = 1.0 / WebMercatorUtility.CalculateMapScale(i, 45);
            var scale_80 = 1.0 / WebMercatorUtility.CalculateMapScale(i, 80);

            var numberOfTiles = Math.Pow(2, 2 * i);

            Console.WriteLine($"| {i,-10} | {numberOfTiles,20:N0} | {"1:" + scale_00.ToString("N0"),15} | {"1:" + scale_45.ToString("N0"),15} | {"1:" + scale_80.ToString("N0"),15} |");
        }

        Console.WriteLine(" -------------------------------------------------- ");

    }


    static string FormatDistance(double meters)
    {
        if (meters >= 1000)
            return $"{meters / 1000:000.0} km";
        if (meters >= 1)
            return $"{meters:000.0} m";
        if (meters >= 0.01)
            return $"{meters * 100:000.0} cm";
        return $"{meters * 1000:000.0} mm";
    }
}
