using static IRI.Maptor.Core.Astronomy.AngleMath;

namespace IRI.Maptor.Core.Astronomy.Time;

/// <summary>
/// Nutation in longitude and obliquity, and the obliquity of the ecliptic, from the low-precision
/// series in Meeus, <i>Astronomical Algorithms</i>, ch. 22 (good to about 0.5″ in Δψ and 0.1″ in Δε).
/// </summary>
internal readonly struct Nutation
{
    /// <summary>Nutation in longitude, degrees.</summary>
    public double Longitude { get; }

    /// <summary>Nutation in obliquity, degrees.</summary>
    public double Obliquity { get; }

    /// <summary>Mean obliquity of the ecliptic, degrees.</summary>
    public double MeanObliquity { get; }

    /// <summary>True obliquity of the ecliptic (mean + nutation), degrees.</summary>
    public double TrueObliquity => MeanObliquity + Obliquity;

    private Nutation(double longitude, double obliquity, double meanObliquity)
    {
        Longitude = longitude;
        Obliquity = obliquity;
        MeanObliquity = meanObliquity;
    }

    /// <param name="t">Julian centuries (TT) since J2000.0.</param>
    public static Nutation Calculate(double t)
    {
        // Longitude of the Moon's ascending node, mean longitudes of the Sun and the Moon
        var omega = 125.04452 - 1934.136261 * t;
        var sunLongitude = 280.4665 + 36000.7698 * t;
        var moonLongitude = 218.3165 + 481267.8813 * t;

        var deltaPsiArcSeconds = -17.20 * SinDeg(omega) - 1.32 * SinDeg(2 * sunLongitude)
                                 - 0.23 * SinDeg(2 * moonLongitude) + 0.21 * SinDeg(2 * omega);

        var deltaEpsilonArcSeconds = 9.20 * CosDeg(omega) + 0.57 * CosDeg(2 * sunLongitude)
                                     + 0.10 * CosDeg(2 * moonLongitude) - 0.09 * CosDeg(2 * omega);

        // Meeus 22.2
        var meanObliquityArcSeconds = 84381.448 - 46.8150 * t - 0.00059 * t * t + 0.001813 * t * t * t;

        return new Nutation(deltaPsiArcSeconds / 3600.0, deltaEpsilonArcSeconds / 3600.0, meanObliquityArcSeconds / 3600.0);
    }
}
