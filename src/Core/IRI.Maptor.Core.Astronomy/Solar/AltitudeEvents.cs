namespace IRI.Maptor.Core.Astronomy.Solar;

/// <summary>
/// When, on one solar day, the Sun rises above and sinks below a given elevation.
/// </summary>
/// <remarks>
/// At high latitudes either event can be missing: the Sun then stays above
/// (<see cref="IsAlwaysAbove"/>) or below (<see cref="IsAlwaysBelow"/>) the elevation all day.
/// </remarks>
public sealed class AltitudeEvents
{
    /// <summary>The elevation of the Sun's centre, degrees (see <see cref="SunAltitudes"/>).</summary>
    public double Altitude { get; }

    /// <summary>Morning crossing (sunrise, or dawn for a twilight elevation), in the requested UTC offset.</summary>
    public DateTimeOffset? Rising { get; }

    /// <summary>Evening crossing (sunset, or dusk for a twilight elevation), in the requested UTC offset.</summary>
    public DateTimeOffset? Setting { get; }

    public bool IsAlwaysAbove { get; }

    public bool IsAlwaysBelow { get; }

    /// <summary>
    /// Time between the crossings: 24 h when always above, zero when always below, and null on the rare
    /// transition day when only one of the two crossings happens.
    /// </summary>
    public TimeSpan? Duration
    {
        get
        {
            if (Rising.HasValue && Setting.HasValue)
                return Setting.Value - Rising.Value;

            if (IsAlwaysAbove)
                return TimeSpan.FromDays(1);

            if (IsAlwaysBelow)
                return TimeSpan.Zero;

            return null;
        }
    }

    internal AltitudeEvents(double altitude, DateTimeOffset? rising, DateTimeOffset? setting, bool isAlwaysAbove, bool isAlwaysBelow)
    {
        Altitude = altitude;
        Rising = rising;
        Setting = setting;
        IsAlwaysAbove = isAlwaysAbove;
        IsAlwaysBelow = isAlwaysBelow;
    }
}
