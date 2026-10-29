namespace IRI.Maptor.Core.Astronomy.Events;

/// <summary>
/// Finds the instant a time-dependent value (usually an elevation) crosses a threshold.
/// </summary>
public static class CrossingSolver
{
    /// <summary>
    /// Bisection on [<paramref name="start"/>, <paramref name="end"/>]. The value must be monotonic on
    /// the interval (for the Sun: between a lower and an upper transit); returns null when it does not
    /// change sides of the threshold there.
    /// </summary>
    public static DateTime? FindCrossing(Func<DateTime, double> value, DateTime start, DateTime end, double threshold, TimeSpan tolerance)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (end < start)
            throw new ArgumentException("The end of the interval must not be before its start.", nameof(end));

        if (tolerance <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "The tolerance must be positive.");

        var startSide = value(start) - threshold;

        var endSide = value(end) - threshold;

        if (startSide == 0)
            return start;

        if (endSide == 0)
            return end;

        if (Math.Sign(startSide) == Math.Sign(endSide))
            return null;

        while (end - start > tolerance)
        {
            var middle = start + TimeSpan.FromTicks((end - start).Ticks / 2);

            var middleSide = value(middle) - threshold;

            if (Math.Sign(middleSide) == Math.Sign(startSide))
                start = middle;
            else
                end = middle;
        }

        return start + TimeSpan.FromTicks((end - start).Ticks / 2);
    }
}
