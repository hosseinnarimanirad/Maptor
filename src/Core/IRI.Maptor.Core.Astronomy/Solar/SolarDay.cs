using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Astronomy.Solar;

/// <summary>
/// The Sun's daily events at one place on one local date: solar noon, sunrise/sunset and the three twilights.
/// </summary>
/// <remarks>
/// The day is the solar day whose noon falls on <see cref="Date"/> in local time; its sunrise is the one
/// before that noon and its sunset the one after, even when (in polar regions or with an unusual UTC
/// offset) one of them falls on the neighbouring calendar date. All times carry <see cref="UtcOffset"/>.
/// </remarks>
public sealed class SolarDay
{
    public Point Location { get; }

    /// <summary>The local calendar date (time of day is zero).</summary>
    public DateTime Date { get; }

    public TimeSpan UtcOffset { get; }

    /// <summary>The Sun crosses the local meridian: its highest point of the day.</summary>
    public DateTimeOffset SolarNoon { get; }

    /// <summary>Geometric elevation of the Sun at solar noon, degrees.</summary>
    public double SolarNoonElevation { get; }

    public AltitudeEvents Daylight { get; }

    public AltitudeEvents CivilTwilight { get; }

    public AltitudeEvents NauticalTwilight { get; }

    public AltitudeEvents AstronomicalTwilight { get; }

    public DateTimeOffset? Sunrise => Daylight.Rising;

    public DateTimeOffset? Sunset => Daylight.Setting;

    /// <summary>Direction of the Sun at sunrise, degrees from north; null when there is no sunrise.</summary>
    public double? SunriseAzimuth { get; }

    /// <summary>Direction of the Sun at sunset, degrees from north; null when there is no sunset.</summary>
    public double? SunsetAzimuth { get; }

    /// <summary>Midnight sun: the Sun does not set.</summary>
    public bool IsPolarDay => Daylight.IsAlwaysAbove;

    /// <summary>The Sun does not rise.</summary>
    public bool IsPolarNight => Daylight.IsAlwaysBelow;

    /// <inheritdoc cref="AltitudeEvents.Duration"/>
    public TimeSpan? DayLength => Daylight.Duration;

    internal SolarDay(Point location, DateTime date, TimeSpan utcOffset, DateTimeOffset solarNoon, double solarNoonElevation,
                      AltitudeEvents daylight, AltitudeEvents civil, AltitudeEvents nautical, AltitudeEvents astronomical,
                      double? sunriseAzimuth, double? sunsetAzimuth)
    {
        Location = location;
        Date = date;
        UtcOffset = utcOffset;
        SolarNoon = solarNoon;
        SolarNoonElevation = solarNoonElevation;
        Daylight = daylight;
        CivilTwilight = civil;
        NauticalTwilight = nautical;
        AstronomicalTwilight = astronomical;
        SunriseAzimuth = sunriseAzimuth;
        SunsetAzimuth = sunsetAzimuth;
    }
}
