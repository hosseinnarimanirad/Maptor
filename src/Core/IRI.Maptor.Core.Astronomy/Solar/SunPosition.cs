using IRI.Maptor.Core.Astronomy.Coordinates;
using IRI.Maptor.Core.Astronomy.Time;
using IRI.Maptor.Core.Common.Primitives;
using static IRI.Maptor.Core.Astronomy.AngleMath;

namespace IRI.Maptor.Core.Astronomy.Solar;

/// <summary>
/// The apparent position of the Sun at an instant, from the low-precision theory in Meeus,
/// <i>Astronomical Algorithms</i>, ch. 25 (about 0.01°, the same accuracy as NOAA's solar calculator).
/// </summary>
public sealed class SunPosition
{
    public DateTime Utc { get; }

    public double JulianEphemerisDay { get; }

    /// <summary>Apparent ecliptic longitude, degrees in [0, 360): 0 at the March equinox, 90 at the June solstice.</summary>
    public double ApparentLongitude { get; }

    public EquatorialCoordinate Equatorial { get; }

    public double RightAscension => Equatorial.RightAscension;

    public double Declination => Equatorial.Declination;

    /// <summary>Earth–Sun distance in astronomical units.</summary>
    public double DistanceAu { get; }

    /// <summary>Apparent minus mean solar time: positive when a sundial is ahead of the clock (about +16 min in early November).</summary>
    public TimeSpan EquationOfTime { get; }

    /// <summary>The point on the Earth with the Sun at the zenith (X = longitude, Y = latitude, degrees).</summary>
    public Point SubSolarPoint { get; }

    private SunPosition(DateTime utc, double julianEphemerisDay, double apparentLongitude, EquatorialCoordinate equatorial,
                        double distanceAu, TimeSpan equationOfTime, Point subSolarPoint)
    {
        Utc = utc;
        JulianEphemerisDay = julianEphemerisDay;
        ApparentLongitude = apparentLongitude;
        Equatorial = equatorial;
        DistanceAu = distanceAu;
        EquationOfTime = equationOfTime;
        SubSolarPoint = subSolarPoint;
    }

    internal static SunPosition Calculate(DateTime utc)
    {
        utc = AstroTime.ToUtc(utc);

        var jde = AstroTime.ToJulianEphemerisDay(utc);

        var t = AstroTime.JulianCenturiesSinceJ2000(jde);

        // Meeus 25.2–25.4: geometric mean longitude, mean anomaly, orbital eccentricity
        var meanLongitude = Normalize360(280.46646 + 36000.76983 * t + 0.0003032 * t * t);
        var meanAnomaly = Normalize360(357.52911 + 35999.05029 * t - 0.0001537 * t * t);
        var eccentricity = 0.016708634 - 0.000042037 * t - 0.0000001267 * t * t;

        var equationOfCenter = (1.914602 - 0.004817 * t - 0.000014 * t * t) * SinDeg(meanAnomaly)
                               + (0.019993 - 0.000101 * t) * SinDeg(2 * meanAnomaly)
                               + 0.000289 * SinDeg(3 * meanAnomaly);

        var trueLongitude = meanLongitude + equationOfCenter;
        var trueAnomaly = meanAnomaly + equationOfCenter;

        var distance = 1.000001018 * (1 - eccentricity * eccentricity) / (1 + eccentricity * CosDeg(trueAnomaly));

        // Apparent longitude: nutation and aberration (25.5), and the matching obliquity correction (25.8)
        var omega = 125.04 - 1934.136 * t;
        var apparentLongitude = Normalize360(trueLongitude - 0.00569 - 0.00478 * SinDeg(omega));

        var nutation = Nutation.Calculate(t);
        var obliquity = nutation.MeanObliquity + 0.00256 * CosDeg(omega);

        var rightAscension = Normalize360(Math.Atan2(CosDeg(obliquity) * SinDeg(apparentLongitude), CosDeg(apparentLongitude)) * RadiansToDegrees);
        var declination = Math.Asin(SinDeg(obliquity) * SinDeg(apparentLongitude)) * RadiansToDegrees;

        // Meeus 28.3
        var y = Math.Pow(Math.Tan(obliquity / 2 * DegreesToRadians), 2);
        var l0 = meanLongitude * DegreesToRadians;
        var m = meanAnomaly * DegreesToRadians;

        var equationOfTimeRadians = y * Math.Sin(2 * l0) - 2 * eccentricity * Math.Sin(m)
                                    + 4 * eccentricity * y * Math.Sin(m) * Math.Cos(2 * l0)
                                    - 0.5 * y * y * Math.Sin(4 * l0) - 1.25 * eccentricity * eccentricity * Math.Sin(2 * m);

        // One degree of hour angle is four minutes of time
        var equationOfTime = TimeSpan.FromMinutes(equationOfTimeRadians * RadiansToDegrees * 4);

        var subSolarLongitude = Normalize180(rightAscension - SiderealTime.GreenwichApparent(utc));

        return new SunPosition(utc, jde, apparentLongitude, new EquatorialCoordinate(rightAscension, declination),
                               distance, equationOfTime, new Point(subSolarLongitude, declination));
    }
}
