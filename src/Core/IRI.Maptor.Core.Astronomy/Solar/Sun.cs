using IRI.Maptor.Core.Astronomy.Coordinates;
using IRI.Maptor.Core.Astronomy.Events;
using IRI.Maptor.Core.Common.Primitives;
using static IRI.Maptor.Core.Astronomy.AngleMath;

namespace IRI.Maptor.Core.Astronomy.Solar;

/// <summary>
/// Entry point for the Sun: its position, how it looks from a place, and its daily events.
/// </summary>
/// <remarks>Locations are geographic <see cref="Point"/>s: X = longitude, Y = latitude, in degrees.</remarks>
public static class Sun
{
    private static readonly TimeSpan EventTolerance = TimeSpan.FromSeconds(1);

    public static SunPosition GetPosition(DateTime utc) => SunPosition.Calculate(utc);

    /// <summary>The point on the Earth with the Sun at the zenith.</summary>
    public static Point GetSubSolarPoint(DateTime utc) => SunPosition.Calculate(utc).SubSolarPoint;

    /// <summary>Azimuth and geometric elevation of the Sun's centre (no refraction).</summary>
    public static HorizontalCoordinate GetHorizontal(Point location, DateTime utc)
    {
        return Observer.ToHorizontal(SunPosition.Calculate(utc).Equatorial, location, utc);
    }

    /// <summary>Day, one of the three twilights, or night, at a place and instant.</summary>
    public static SolarPhase GetPhase(Point location, DateTime utc) => SolarPhases.FromElevation(GetHorizontal(location, utc).Elevation);

    /// <param name="location">X = longitude, Y = latitude, degrees.</param>
    /// <param name="date">The local calendar date; its time of day is ignored.</param>
    /// <param name="utcOffset">The offset the returned times are expressed in (whole minutes, at most ±14 h).</param>
    public static SolarDay GetSolarDay(Point location, DateTime date, TimeSpan utcOffset)
    {
        var frame = SolarDayFrame.Create(location, date, utcOffset);

        var daylight = GetAltitudeEvents(frame, SunAltitudes.Sunrise);

        double? AzimuthAt(DateTimeOffset? time) => time.HasValue ? GetHorizontal(location, time.Value.UtcDateTime).Azimuth : (double?)null;

        return new SolarDay(location, date.Date, utcOffset, frame.ToOffset(frame.NoonUtc), frame.NoonElevation,
                            daylight,
                            GetAltitudeEvents(frame, SunAltitudes.CivilTwilight),
                            GetAltitudeEvents(frame, SunAltitudes.NauticalTwilight),
                            GetAltitudeEvents(frame, SunAltitudes.AstronomicalTwilight),
                            AzimuthAt(daylight.Rising), AzimuthAt(daylight.Setting));
    }

    /// <summary>As <see cref="GetSolarDay(Point, DateTime, TimeSpan)"/>, with the offset <paramref name="timeZone"/> has at noon of that date.</summary>
    public static SolarDay GetSolarDay(Point location, DateTime date, TimeZoneInfo timeZone)
    {
        return GetSolarDay(location, date, GetOffset(date, timeZone));
    }

    /// <summary>
    /// When the Sun's centre crosses any elevation on one local date: the building block for custom
    /// twilight definitions and, later, prayer times.
    /// </summary>
    public static AltitudeEvents GetAltitudeEvents(Point location, DateTime date, TimeSpan utcOffset, double altitude)
    {
        return GetAltitudeEvents(SolarDayFrame.Create(location, date, utcOffset), altitude);
    }

    private static AltitudeEvents GetAltitudeEvents(SolarDayFrame frame, double altitude)
    {
        // The elevation only climbs between the previous lower transit and noon, and only falls after it,
        // so each half holds at most one crossing and bisection cannot miss or double-count one.
        var rising = CrossingSolver.FindCrossing(frame.Elevation, frame.PreviousMidnightUtc, frame.NoonUtc, altitude, EventTolerance);

        var setting = CrossingSolver.FindCrossing(frame.Elevation, frame.NoonUtc, frame.NextMidnightUtc, altitude, EventTolerance);

        var noCrossing = rising is null && setting is null;

        return new AltitudeEvents(altitude,
                                  rising.HasValue ? frame.ToOffset(rising.Value) : (DateTimeOffset?)null,
                                  setting.HasValue ? frame.ToOffset(setting.Value) : (DateTimeOffset?)null,
                                  isAlwaysAbove: noCrossing && frame.LowestElevation > altitude,
                                  isAlwaysBelow: noCrossing && frame.NoonElevation < altitude);
    }

    private static TimeSpan GetOffset(DateTime date, TimeZoneInfo timeZone)
    {
        if (timeZone is null)
            throw new ArgumentNullException(nameof(timeZone));

        return timeZone.GetUtcOffset(DateTime.SpecifyKind(date.Date.AddHours(12), DateTimeKind.Unspecified));
    }

    /// <summary>Solar noon of a local date and the lower transits (solar midnights) either side of it.</summary>
    private sealed class SolarDayFrame
    {
        public Func<DateTime, double> Elevation { get; }

        public TimeSpan UtcOffset { get; }

        public DateTime NoonUtc { get; }

        public DateTime PreviousMidnightUtc { get; }

        public DateTime NextMidnightUtc { get; }

        public double NoonElevation { get; }

        public double LowestElevation { get; }

        private SolarDayFrame(Func<DateTime, double> elevation, TimeSpan utcOffset, DateTime noonUtc, DateTime previousMidnightUtc, DateTime nextMidnightUtc)
        {
            Elevation = elevation;
            UtcOffset = utcOffset;
            NoonUtc = noonUtc;
            PreviousMidnightUtc = previousMidnightUtc;
            NextMidnightUtc = nextMidnightUtc;
            NoonElevation = elevation(noonUtc);
            LowestElevation = Math.Min(elevation(previousMidnightUtc), elevation(nextMidnightUtc));
        }

        public DateTimeOffset ToOffset(DateTime utc) => new DateTimeOffset(utc.Ticks, TimeSpan.Zero).ToOffset(UtcOffset);

        public static SolarDayFrame Create(Point location, DateTime date, TimeSpan utcOffset)
        {
            ValidateLocation(location);

            if (utcOffset.Ticks % TimeSpan.TicksPerMinute != 0 || utcOffset.Duration() > TimeSpan.FromHours(14))
                throw new ArgumentOutOfRangeException(nameof(utcOffset), "The UTC offset must be whole minutes and at most ±14 hours.");

            var longitude = Normalize180(location.X);

            var localDate = date.Date;

            // Solar noon in UTC is near 12:00 minus the longitude in hours; its UTC date can differ from the local one.
            var noon = FindTransit(location, DateTime.SpecifyKind(localDate, DateTimeKind.Utc).AddHours(12 - longitude / 15.0), 0);

            var localNoonDate = (noon + utcOffset).Date;

            if (localNoonDate < localDate)
                noon = FindTransit(location, noon.AddDays(1), 0);
            else if (localNoonDate > localDate)
                noon = FindTransit(location, noon.AddDays(-1), 0);

            double Elevation(DateTime utc) => GetHorizontal(location, utc).Elevation;

            return new SolarDayFrame(Elevation, utcOffset, noon,
                                     FindTransit(location, noon.AddHours(-12), 180),
                                     FindTransit(location, noon.AddHours(12), 180));
        }

        /// <summary>Iterates to the instant the Sun's local hour angle equals <paramref name="hourAngle"/> (0 = noon, 180 = midnight).</summary>
        private static DateTime FindTransit(Point location, DateTime guessUtc, double hourAngle)
        {
            var time = guessUtc;

            for (int i = 0; i < 10; i++)
            {
                var position = SunPosition.Calculate(time);

                var error = Normalize180(Observer.GetHourAngle(position.Equatorial, location.X, time) - hourAngle);

                // The Sun's hour angle advances about 360° per day
                time = time.AddDays(-error / 360.0);

                if (Math.Abs(error) < 1e-6)
                    break;
            }

            return time;
        }
    }
}
