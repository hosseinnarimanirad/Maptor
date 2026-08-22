using IRI.Maptor.Samples.Core.Runner;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Samples.Core.Geodesy;

/// <summary>
/// How much ground does one decimal place of a latitude/longitude value cover?
/// Moves a point east by 1°, 0.1°, 0.01° … and measures the spherical distance, at several latitudes.
/// </summary>
public static class CoordinatePrecision
{
    [Sample("geodesy/precision", "Decimal places of lat/long vs. ground distance")]
    public static void Run()
    {
        foreach (var latitude in new[] { 0.0, 45.0, 60.0 })
            PrintTable(latitude);
    }

    static void PrintTable(double latitude)
    {
        var basePoint = new Point(0, latitude);

        Console.WriteLine($"| decimal places | degrees      | E/W distance at {latitude}° |");
        Console.WriteLine($"| -------------- | ------------ | ------------------------ |");

        for (int decimals = 0; decimals <= 8; decimals++)
        {
            var step = Math.Pow(10, -decimals);
            var shifted = new Point(step, latitude);

            // great-circle distance on a sphere; use SpatialUtility.VincentyDistance for the ellipsoid
            var meters = SpatialUtility.GetSphericalLength(basePoint, shifted);

            Console.WriteLine($"| {decimals,-14} | {step,-12:0.########} | {FormatDistance(meters),-24} |");
        }

        Console.WriteLine();
    }

    static string FormatDistance(double meters) => meters switch
    {
        >= 1000 => $"{meters / 1000:0.##} km",
        >= 1 => $"{meters:0.##} m",
        >= 0.01 => $"{meters * 100:0.##} cm",
        _ => $"{meters * 1000:0.##} mm",
    };
}
