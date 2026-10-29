using IRI.Maptor.Core.Astronomy.Time;

namespace IRI.Maptor.Core.Satellite.Frames;

/// <summary>
/// From SGP4's TEME frame to the Earth-fixed frame (ECEF, the frame WGS84 coordinates live in).
/// </summary>
/// <remarks>
/// TEME and the pseudo-Earth-fixed frame differ by a rotation about the pole through Greenwich mean
/// sidereal time (IAU-82, which is what SGP4 is consistent with). Polar motion — at most about 15 m
/// on the ground — is left out: it is far below what a TLE can predict and invisible on a map.
/// </remarks>
public static class EarthFrames
{
    /// <summary>The Earth's rotation rate, rad/s.</summary>
    public const double EarthRotationRate = 7.292115146706979e-5;

    private const double DegreesToRadians = Math.PI / 180.0;

    /// <summary>Position, km, from TEME to Earth-fixed at a UTC instant.</summary>
    public static Vector3 TemeToEcef(Vector3 teme, DateTime utc)
    {
        var theta = SiderealTime.GreenwichMean(utc) * DegreesToRadians;

        return Rotate(teme, theta);
    }

    /// <summary>
    /// Velocity, km/s, from TEME to Earth-fixed: rotated, less the frame's own rotation (ω × r), so it
    /// is the velocity relative to the ground.
    /// </summary>
    public static Vector3 TemeVelocityToEcef(Vector3 temePosition, Vector3 temeVelocity, DateTime utc)
    {
        var theta = SiderealTime.GreenwichMean(utc) * DegreesToRadians;

        var r = Rotate(temePosition, theta);

        var v = Rotate(temeVelocity, theta);

        return new Vector3(v.X + EarthRotationRate * r.Y, v.Y - EarthRotationRate * r.X, v.Z);
    }

    /// <summary>Earth-fixed back to TEME, km.</summary>
    public static Vector3 EcefToTeme(Vector3 ecef, DateTime utc)
    {
        var theta = SiderealTime.GreenwichMean(utc) * DegreesToRadians;

        return Rotate(ecef, -theta);
    }

    /// <summary>Rotation of the frame by <paramref name="theta"/> about Z: a vector's coordinates turn by −θ.</summary>
    private static Vector3 Rotate(Vector3 v, double theta)
    {
        var c = Math.Cos(theta);

        var s = Math.Sin(theta);

        return new Vector3(c * v.X + s * v.Y, -s * v.X + c * v.Y, v.Z);
    }
}
