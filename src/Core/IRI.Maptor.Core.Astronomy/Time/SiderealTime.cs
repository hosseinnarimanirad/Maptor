using static IRI.Maptor.Core.Astronomy.AngleMath;

namespace IRI.Maptor.Core.Astronomy.Time;

/// <summary>
/// Sidereal time as an angle in degrees, [0, 360). Divide by 15 for hours.
/// </summary>
public static class SiderealTime
{
    /// <summary>Greenwich mean sidereal time (Meeus 12.4).</summary>
    public static double GreenwichMean(DateTime utc)
    {
        var jd = AstroTime.ToJulianDate(utc);

        var t = AstroTime.JulianCenturiesSinceJ2000(jd);

        var theta = 280.46061837 + 360.98564736629 * (jd - AstroTime.J2000) + 0.000387933 * t * t - t * t * t / 38710000.0;

        return Normalize360(theta);
    }

    /// <summary>Greenwich apparent sidereal time: the mean value corrected by the equation of the equinoxes.</summary>
    public static double GreenwichApparent(DateTime utc)
    {
        var nutation = Nutation.Calculate(AstroTime.JulianCenturiesSinceJ2000(AstroTime.ToJulianEphemerisDay(utc)));

        return Normalize360(GreenwichMean(utc) + nutation.Longitude * CosDeg(nutation.TrueObliquity));
    }

    /// <summary>Local apparent sidereal time at a longitude (degrees, east positive).</summary>
    public static double LocalApparent(DateTime utc, double longitude)
    {
        return Normalize360(GreenwichApparent(utc) + longitude);
    }
}
