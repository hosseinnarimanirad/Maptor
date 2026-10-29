namespace IRI.Maptor.Core.Astronomy.Solar;

/// <summary>
/// The light at a place, by how high the Sun stands: the same limits as <see cref="SunAltitudes"/>.
/// </summary>
public enum SolarPhase
{
    /// <summary>Sun at or above the horizon (−0.8333°, its upper limb showing).</summary>
    Day,

    /// <summary>Sun between 0.8333° and 6° below the horizon.</summary>
    CivilTwilight,

    /// <summary>Sun between 6° and 12° below the horizon.</summary>
    NauticalTwilight,

    /// <summary>Sun between 12° and 18° below the horizon.</summary>
    AstronomicalTwilight,

    /// <summary>Sun more than 18° below the horizon.</summary>
    Night,
}

public static class SolarPhases
{
    /// <summary>The phase for a geometric elevation of the Sun's centre, in degrees.</summary>
    public static SolarPhase FromElevation(double elevation)
    {
        if (double.IsNaN(elevation))
            throw new ArgumentOutOfRangeException(nameof(elevation), "The elevation must be a number.");

        if (elevation >= SunAltitudes.Sunrise)
            return SolarPhase.Day;

        if (elevation >= SunAltitudes.CivilTwilight)
            return SolarPhase.CivilTwilight;

        if (elevation >= SunAltitudes.NauticalTwilight)
            return SolarPhase.NauticalTwilight;

        if (elevation >= SunAltitudes.AstronomicalTwilight)
            return SolarPhase.AstronomicalTwilight;

        return SolarPhase.Night;
    }
}
