using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Satellite.Frames;

/// <summary>
/// Earth-fixed Cartesian coordinates (km) ↔ WGS84 latitude, longitude and height above the ellipsoid.
/// </summary>
public static class Wgs84Geodetic
{
    /// <summary>WGS84 semi-major axis, km.</summary>
    public const double SemiMajorAxisKm = 6378.137;

    public const double Flattening = 1.0 / 298.257223563;

    private static readonly double E2 = Flattening * (2.0 - Flattening);

    private const double DegreesToRadians = Math.PI / 180.0;

    private const double RadiansToDegrees = 180.0 / Math.PI;

    /// <summary>
    /// Latitude and longitude (X = longitude, Y = latitude, degrees; longitude in (−180, 180]) and the
    /// height above the ellipsoid in km. Iterated to well below a millimetre for any height a
    /// satellite can have, poles included.
    /// </summary>
    public static Point FromEcef(Vector3 ecef, out double heightKm)
    {
        var p = Math.Sqrt(ecef.X * ecef.X + ecef.Y * ecef.Y);

        var longitude = Math.Atan2(ecef.Y, ecef.X);

        // start from the latitude on a sphere scaled for the flattening, then refine
        var latitude = Math.Atan2(ecef.Z, p * (1.0 - E2));

        var n = 0.0;

        for (int i = 0; i < 10; i++)
        {
            var sin = Math.Sin(latitude);

            n = SemiMajorAxisKm / Math.Sqrt(1.0 - E2 * sin * sin);

            var next = Math.Atan2(ecef.Z + E2 * n * sin, p);

            var converged = Math.Abs(next - latitude) < 1e-14;

            latitude = next;

            if (converged)
                break;
        }

        var sinLat = Math.Sin(latitude);

        n = SemiMajorAxisKm / Math.Sqrt(1.0 - E2 * sinLat * sinLat);

        // stable at the poles as well as on the equator
        heightKm = p * Math.Cos(latitude) + (ecef.Z + E2 * n * sinLat) * sinLat - n;

        return new Point(longitude * RadiansToDegrees, latitude * RadiansToDegrees);
    }

    /// <summary>Earth-fixed coordinates, km, of a place (X = longitude, Y = latitude, degrees) at a height in km.</summary>
    public static Vector3 ToEcef(Point geographic, double heightKm)
    {
        if (geographic is null)
            throw new ArgumentNullException(nameof(geographic));

        var latitude = geographic.Y * DegreesToRadians;

        var longitude = geographic.X * DegreesToRadians;

        var sin = Math.Sin(latitude);

        var n = SemiMajorAxisKm / Math.Sqrt(1.0 - E2 * sin * sin);

        var cosLat = Math.Cos(latitude);

        return new Vector3(
            (n + heightKm) * cosLat * Math.Cos(longitude),
            (n + heightKm) * cosLat * Math.Sin(longitude),
            (n * (1.0 - E2) + heightKm) * sin);
    }
}
