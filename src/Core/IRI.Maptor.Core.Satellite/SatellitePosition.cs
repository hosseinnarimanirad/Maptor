using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite.Frames;
using IRI.Maptor.Core.Satellite.Propagation;

namespace IRI.Maptor.Core.Satellite;

/// <summary>
/// Where a satellite is at an instant, ready for a map: the point beneath it, its height and speed.
/// </summary>
public sealed class SatellitePosition
{
    private SatellitePosition(DateTime utc, Point subSatellitePoint, double altitudeKm, double speedKmS, double groundSpeedKmS, Vector3 teme, Vector3 ecef)
    {
        Utc = utc;
        SubSatellitePoint = subSatellitePoint;
        AltitudeKm = altitudeKm;
        SpeedKmS = speedKmS;
        GroundSpeedKmS = groundSpeedKmS;
        Teme = teme;
        Ecef = ecef;
    }

    public DateTime Utc { get; }

    /// <summary>The point on the ground directly beneath the satellite (X = longitude, Y = latitude, degrees, WGS84).</summary>
    public Point SubSatellitePoint { get; }

    /// <summary>Height above the WGS84 ellipsoid, km.</summary>
    public double AltitudeKm { get; }

    /// <summary>Orbital (inertial) speed, km/s: about 7.66 for the ISS.</summary>
    public double SpeedKmS { get; }

    /// <summary>Speed relative to the rotating Earth, km/s.</summary>
    public double GroundSpeedKmS { get; }

    /// <summary>Position in SGP4's TEME frame, km.</summary>
    public Vector3 Teme { get; }

    /// <summary>Position in the Earth-fixed frame, km.</summary>
    public Vector3 Ecef { get; }

    /// <summary>The position of a valid propagation result; null when SGP4 reported an error.</summary>
    public static SatellitePosition? From(PropagationResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        if (!result.IsValid)
            return null;

        var ecef = EarthFrames.TemeToEcef(result.Position, result.Utc);

        var groundVelocity = EarthFrames.TemeVelocityToEcef(result.Position, result.Velocity, result.Utc);

        var point = Wgs84Geodetic.FromEcef(ecef, out var height);

        return new SatellitePosition(result.Utc, point, height, result.Velocity.Length, groundVelocity.Length, result.Position, ecef);
    }

    public override string ToString()
        => FormattableString.Invariant($"{Utc:u}  {SubSatellitePoint.Y:F4}°, {SubSatellitePoint.X:F4}°  {AltitudeKm:F1} km");
}

public static class Sgp4PropagatorExtensions
{
    /// <summary>The satellite's position at <paramref name="utc"/>, or null when SGP4 reports an error (e.g. it has decayed).</summary>
    public static SatellitePosition? GetPosition(this Sgp4Propagator propagator, DateTime utc)
        => SatellitePosition.From(propagator.Propagate(utc));
}
