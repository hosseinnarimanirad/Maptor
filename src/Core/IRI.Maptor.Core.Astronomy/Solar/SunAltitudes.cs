namespace IRI.Maptor.Core.Astronomy.Solar;

/// <summary>
/// Standard geometric elevations of the Sun's centre, in degrees, that define its daily events.
/// </summary>
public static class SunAltitudes
{
    /// <summary>Sunrise and sunset: the upper limb on the horizon, with 34′ of refraction and a 16′ semi-diameter.</summary>
    public const double Sunrise = -0.8333;

    public const double CivilTwilight = -6.0;

    public const double NauticalTwilight = -12.0;

    public const double AstronomicalTwilight = -18.0;
}
