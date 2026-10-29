using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Satellite.Tracks;

/// <summary>
/// Great-circle navigation on a sphere of the Earth's mean radius: good to about 0.5 % against the
/// ellipsoid, far finer than a swath edge needs. Points are X = longitude, Y = latitude, degrees.
/// </summary>
public static class GreatCircle
{
    /// <summary>IUGG mean Earth radius, km.</summary>
    public const double MeanEarthRadiusKm = 6371.0088;

    private const double DegreesToRadians = Math.PI / 180.0;

    private const double RadiansToDegrees = 180.0 / Math.PI;

    /// <summary>Distance between two points along the surface, km (haversine).</summary>
    public static double DistanceKm(Point a, Point b)
    {
        var lat1 = a.Y * DegreesToRadians;
        var lat2 = b.Y * DegreesToRadians;
        var dLat = lat2 - lat1;
        var dLon = (b.X - a.X) * DegreesToRadians;

        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * MeanEarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>Initial bearing from <paramref name="from"/> towards <paramref name="to"/>, degrees clockwise from north, [0, 360).</summary>
    public static double Bearing(Point from, Point to)
    {
        var lat1 = from.Y * DegreesToRadians;
        var lat2 = to.Y * DegreesToRadians;
        var dLon = (to.X - from.X) * DegreesToRadians;

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        var bearing = Math.Atan2(y, x) * RadiansToDegrees;

        return bearing < 0 ? bearing + 360 : bearing;
    }

    /// <summary>The point reached by going <paramref name="distanceKm"/> from <paramref name="start"/> on <paramref name="bearing"/>; longitude in [−180, 180).</summary>
    public static Point Destination(Point start, double bearing, double distanceKm)
    {
        var lat1 = start.Y * DegreesToRadians;
        var lon1 = start.X * DegreesToRadians;
        var theta = bearing * DegreesToRadians;
        var delta = distanceKm / MeanEarthRadiusKm;

        var sinLat2 = Math.Sin(lat1) * Math.Cos(delta) + Math.Cos(lat1) * Math.Sin(delta) * Math.Cos(theta);
        var lat2 = Math.Asin(Math.Max(-1, Math.Min(1, sinLat2)));

        var lon2 = lon1 + Math.Atan2(Math.Sin(theta) * Math.Sin(delta) * Math.Cos(lat1), Math.Cos(delta) - Math.Sin(lat1) * sinLat2);

        return new Point(NormalizeLongitude(lon2 * RadiansToDegrees), lat2 * RadiansToDegrees);
    }

    /// <summary>A longitude in [−180, 180).</summary>
    public static double NormalizeLongitude(double longitude)
    {
        var result = (longitude + 180.0) % 360.0;

        if (result < 0)
            result += 360.0;

        return result - 180.0;
    }
}
