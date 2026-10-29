namespace IRI.Maptor.Core.Astronomy.Time;

/// <summary>
/// Conversions between <see cref="DateTime"/> and the astronomical time scales.
/// </summary>
/// <remarks>
/// A <see cref="DateTime"/> of kind <see cref="DateTimeKind.Unspecified"/> is taken as UTC; a local one
/// is converted to UTC first. UT1 (the Earth's rotation angle) is approximated by UTC; the two never
/// differ by more than 0.9 s, which is far below what map display or rise/set times can show.
/// </remarks>
public static class AstroTime
{
    /// <summary>Julian date of the J2000.0 epoch, 2000-01-01 12:00 TT.</summary>
    public const double J2000 = 2451545.0;

    public const double DaysPerJulianCentury = 36525.0;

    private const double UnixEpochJulianDate = 2440587.5;

    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DateTime ToUtc(DateTime time)
    {
        return time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : DateTime.SpecifyKind(time, DateTimeKind.Utc);
    }

    /// <summary>Julian date (UT) of a UTC instant.</summary>
    public static double ToJulianDate(DateTime utc)
    {
        return UnixEpochJulianDate + (ToUtc(utc) - UnixEpoch).TotalDays;
    }

    /// <summary>The UTC instant of a Julian date (UT).</summary>
    public static DateTime FromJulianDate(double julianDate)
    {
        return UnixEpoch.AddTicks((long)Math.Round((julianDate - UnixEpochJulianDate) * TimeSpan.TicksPerDay));
    }

    /// <summary>Julian Ephemeris Day: the Julian date on the Terrestrial Time (TT) scale, used by the ephemerides.</summary>
    public static double ToJulianEphemerisDay(DateTime utc)
    {
        return ToJulianDate(utc) + DeltaT(utc) / 86400.0;
    }

    /// <summary>Julian centuries elapsed since J2000.0.</summary>
    public static double JulianCenturiesSinceJ2000(double julianDate)
    {
        return (julianDate - J2000) / DaysPerJulianCentury;
    }

    /// <summary>
    /// ΔT = TT − UT in seconds, from the Espenak–Meeus polynomials (NASA eclipse web site). Within a
    /// second or two for 1900–2025; beyond that it is an extrapolation, which is unavoidable because the
    /// Earth's rotation cannot be predicted.
    /// </summary>
    public static double DeltaT(DateTime utc)
    {
        utc = ToUtc(utc);

        double y = utc.Year + (utc.Month - 0.5) / 12.0;

        double t;

        if (y < 1900 || y >= 2150)
        {
            var u = (y - 1820) / 100.0;

            return -20 + 32 * u * u;
        }

        if (y < 1920)
        {
            t = y - 1900;
            return -2.79 + 1.494119 * t - 0.0598939 * t * t + 0.0061966 * t * t * t - 0.000197 * t * t * t * t;
        }

        if (y < 1941)
        {
            t = y - 1920;
            return 21.20 + 0.84493 * t - 0.076100 * t * t + 0.0020936 * t * t * t;
        }

        if (y < 1961)
        {
            t = y - 1950;
            return 29.07 + 0.407 * t - t * t / 233.0 + t * t * t / 2547.0;
        }

        if (y < 1986)
        {
            t = y - 1975;
            return 45.45 + 1.067 * t - t * t / 260.0 - t * t * t / 718.0;
        }

        if (y < 2005)
        {
            t = y - 2000;
            return 63.86 + 0.3345 * t - 0.060374 * t * t + 0.0017275 * t * t * t + 0.000651814 * t * t * t * t + 0.00002373599 * t * t * t * t * t;
        }

        if (y < 2050)
        {
            t = y - 2000;
            return 62.92 + 0.32217 * t + 0.005589 * t * t;
        }

        var v = (y - 1820) / 100.0;

        return -20 + 32 * v * v - 0.5628 * (2150 - y);
    }
}
