namespace IRI.Maptor.Core.Satellite.Elements;

/// <summary>The usual kinds of Earth orbit, by height, period and shape.</summary>
public enum OrbitClass
{
    /// <summary>Low Earth orbit: entirely below 2,000 km (the ISS, Earth observation, Starlink).</summary>
    LowEarth,

    /// <summary>Medium Earth orbit: between 2,000 km and geosynchronous height (GPS, Galileo, GLONASS, BeiDou).</summary>
    MediumEarth,

    /// <summary>One revolution per sidereal day, but inclined or eccentric: it traces a figure eight.</summary>
    Geosynchronous,

    /// <summary>Geosynchronous over the equator, nearly circular: it stays above one place.</summary>
    Geostationary,

    /// <summary>Eccentricity of 0.25 or more (Molniya, Tundra, transfer orbits).</summary>
    HighlyElliptical,
}

/// <summary>Classifies orbital elements into an <see cref="OrbitClass"/>.</summary>
public static class OrbitClasses
{
    /// <summary>Equatorial radius (WGS-72), km.</summary>
    private const double EarthRadiusKm = 6378.135;

    /// <summary>Gravitational parameter (WGS-72), km³/s².</summary>
    private const double Mu = 398600.8;

    /// <summary>One sidereal day, minutes.</summary>
    public const double SiderealDayMinutes = 1436.0682;

    /// <summary>The class of the orbit described by <paramref name="elements"/>.</summary>
    /// <remarks>
    /// Geosynchronous is a period within 30 minutes of a sidereal day; geostationary adds an inclination
    /// under 3° and an eccentricity under 0.01. Heights come from the mean motion by Kepler's third law,
    /// which is plenty to tell the classes apart.
    /// </remarks>
    public static OrbitClass Of(OrbitalElements elements)
    {
        if (elements is null)
            throw new ArgumentNullException(nameof(elements));

        if (elements.Eccentricity >= 0.25)
            return OrbitClass.HighlyElliptical;

        if (Math.Abs(elements.PeriodMinutes - SiderealDayMinutes) < 30)
            return elements.Inclination < 3 && elements.Eccentricity < 0.01 ? OrbitClass.Geostationary : OrbitClass.Geosynchronous;

        var (perigee, apogee) = HeightsKm(elements);

        return apogee < 2000 ? OrbitClass.LowEarth : perigee >= 2000 ? OrbitClass.MediumEarth : OrbitClass.HighlyElliptical;
    }

    /// <summary>Perigee and apogee heights above the equatorial radius, km.</summary>
    public static (double Perigee, double Apogee) HeightsKm(OrbitalElements elements)
    {
        if (elements is null)
            throw new ArgumentNullException(nameof(elements));

        var n = elements.MeanMotion * 2 * Math.PI / 86400.0;

        var a = Math.Pow(Mu / (n * n), 1.0 / 3.0);

        return (a * (1 - elements.Eccentricity) - EarthRadiusKm, a * (1 + elements.Eccentricity) - EarthRadiusKm);
    }
}
