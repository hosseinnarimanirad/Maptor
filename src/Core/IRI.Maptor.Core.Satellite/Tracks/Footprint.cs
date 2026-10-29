using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Satellite.Tracks;

/// <summary>
/// The area from which a satellite is above a minimum elevation: a circle on the Earth (a spherical
/// cap) around the point beneath it.
/// </summary>
/// <remarks>
/// Its angular radius, seen from the Earth's centre, is λ = arccos(R·cos ε / (R + h)) − ε for altitude h
/// and minimum elevation ε. With ε = 0 it is the horizon circle. This type only describes the cap;
/// <c>SphericalCapHelper</c> in Core.Spatial turns it into map polygons that are correct over the
/// poles and across the 180° meridian.
/// </remarks>
public sealed class Footprint
{
    /// <summary>The usual "usefully visible" limit: below about 10° the line of sight is mostly atmosphere and terrain.</summary>
    public const double DefaultMinimumElevation = 10.0;

    private Footprint(Point center, double angularRadius, double minimumElevation)
    {
        Center = center;
        AngularRadius = angularRadius;
        MinimumElevation = minimumElevation;
    }

    /// <summary>The point beneath the satellite (X = longitude, Y = latitude, degrees).</summary>
    public Point Center { get; }

    /// <summary>Earth-centre angle from <see cref="Center"/> to the edge, degrees.</summary>
    public double AngularRadius { get; }

    /// <summary>The radius along the ground, km.</summary>
    public double RadiusKm => AngularRadius * Math.PI / 180.0 * GreatCircle.MeanEarthRadiusKm;

    public double MinimumElevation { get; }

    public static Footprint Of(SatellitePosition position, double minimumElevation = DefaultMinimumElevation)
    {
        if (position is null)
            throw new ArgumentNullException(nameof(position));

        return new Footprint(position.SubSatellitePoint, AngularRadiusDegrees(position.AltitudeKm, minimumElevation), minimumElevation);
    }

    /// <summary>λ = arccos(R·cos ε / (R + h)) − ε, degrees.</summary>
    public static double AngularRadiusDegrees(double altitudeKm, double minimumElevation)
    {
        if (!(altitudeKm > 0))
            throw new ArgumentOutOfRangeException(nameof(altitudeKm), "The altitude must be positive.");

        if (!(minimumElevation >= 0 && minimumElevation < 90))
            throw new ArgumentOutOfRangeException(nameof(minimumElevation), "The minimum elevation must be in [0, 90).");

        const double R = GreatCircle.MeanEarthRadiusKm;

        var epsilon = minimumElevation * Math.PI / 180.0;

        var lambda = Math.Acos(R * Math.Cos(epsilon) / (R + altitudeKm)) - epsilon;

        return lambda * 180.0 / Math.PI;
    }
}
