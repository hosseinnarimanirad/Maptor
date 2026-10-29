using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite.Frames;

namespace IRI.Maptor.Core.Satellite.Passes;

/// <summary>Where a satellite is in the sky of a place on the ground.</summary>
public readonly struct LookAngles
{
    private const double DegreesToRadians = Math.PI / 180.0;

    public LookAngles(double azimuth, double elevation, double rangeKm)
    {
        Azimuth = azimuth;
        Elevation = elevation;
        RangeKm = rangeKm;
    }

    /// <summary>Degrees clockwise from north, [0, 360).</summary>
    public double Azimuth { get; }

    /// <summary>Degrees above the horizon (negative below it); no atmospheric refraction.</summary>
    public double Elevation { get; }

    /// <summary>Straight-line distance from the place to the satellite, km.</summary>
    public double RangeKm { get; }

    /// <summary>
    /// The look angles from <paramref name="observer"/> (X = longitude, Y = latitude, degrees, WGS84) at
    /// <paramref name="heightKm"/> above the ellipsoid, to a satellite at <paramref name="satelliteEcef"/>.
    /// </summary>
    public static LookAngles From(Point observer, double heightKm, Vector3 satelliteEcef)
    {
        if (observer is null)
            throw new ArgumentNullException(nameof(observer));

        var site = Wgs84Geodetic.ToEcef(observer, heightKm);

        var d = satelliteEcef - site;

        var lat = observer.Y * DegreesToRadians;
        var lon = observer.X * DegreesToRadians;

        var sinLat = Math.Sin(lat);
        var cosLat = Math.Cos(lat);
        var sinLon = Math.Sin(lon);
        var cosLon = Math.Cos(lon);

        // east, north, up of the ellipsoid normal at the site
        var east = -sinLon * d.X + cosLon * d.Y;
        var north = -sinLat * cosLon * d.X - sinLat * sinLon * d.Y + cosLat * d.Z;
        var up = cosLat * cosLon * d.X + cosLat * sinLon * d.Y + sinLat * d.Z;

        var range = d.Length;

        var azimuth = Math.Atan2(east, north) / DegreesToRadians;

        if (azimuth < 0)
            azimuth += 360;

        // straight overhead, rounding can put up / range a hair above 1
        var elevation = Math.Asin(Math.Max(-1, Math.Min(1, up / range))) / DegreesToRadians;

        return new LookAngles(azimuth, elevation, range);
    }

    /// <summary>Look angles from <paramref name="observer"/> (on the ellipsoid) to the satellite at <paramref name="position"/>.</summary>
    public static LookAngles From(Point observer, SatellitePosition position)
    {
        if (position is null)
            throw new ArgumentNullException(nameof(position));

        return From(observer, 0, position.Ecef);
    }

    public override string ToString() => FormattableString.Invariant($"az {Azimuth:F1}°, el {Elevation:F1}°, {RangeKm:F0} km");
}
