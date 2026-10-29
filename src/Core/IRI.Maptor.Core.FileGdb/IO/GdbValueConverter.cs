namespace IRI.Maptor.Core.FileGdb.IO;

/// <summary>
/// Converts the raw date, time and GUID encodings of a File Geodatabase to .NET values.
/// </summary>
internal static class GdbValueConverter
{
    // ESRI dates count days from 1899-12-30, like OLE Automation dates. DateTime.FromOADate is not
    // used on purpose: for negative values it treats the fraction as a positive time of day, while
    // ESRI (and GDAL) treat the value as a plain linear day count.
    private static readonly DateTime Epoch = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);

    private static readonly double MinDays = (DateTime.MinValue - Epoch).TotalDays;

    private static readonly double MaxDays = (DateTime.MaxValue - Epoch).TotalDays;

    /// <summary>
    /// Days since 1899-12-30 to <see cref="DateTime"/> (kind Unspecified), rounded to the millisecond.
    /// </summary>
    public static DateTime DaysToDateTime(double days)
    {
        if (double.IsNaN(days) || days < MinDays || days > MaxDays)
            throw new InvalidDataException($"Invalid date value: {days} days since 1899-12-30.");

        var milliseconds = Math.Round(days * TimeSpan.FromDays(1).TotalMilliseconds);

        return Epoch.AddMilliseconds(milliseconds);
    }

    /// <summary>
    /// Fraction of a day to a time of day, rounded to the millisecond.
    /// </summary>
    public static TimeSpan DayFractionToTime(double fraction)
    {
        if (double.IsNaN(fraction) || fraction < 0 || fraction > 1)
            throw new InvalidDataException($"Invalid time value: {fraction} (expected a fraction of a day).");

        var milliseconds = Math.Round(fraction * TimeSpan.FromDays(1).TotalMilliseconds);

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// Local date-time as days since 1899-12-30 plus its UTC offset in minutes.
    /// </summary>
    public static DateTimeOffset DaysToDateTimeOffset(double days, short utcOffsetMinutes)
    {
        if (utcOffsetMinutes < -14 * 60 || utcOffsetMinutes > 14 * 60)
            throw new InvalidDataException($"Invalid UTC offset: {utcOffsetMinutes} minutes.");

        return new DateTimeOffset(DaysToDateTime(days), TimeSpan.FromMinutes(utcOffsetMinutes));
    }

    /// <summary>
    /// The 16 GUID bytes are stored in the Windows/COM layout (first three groups little-endian),
    /// which is exactly what <see cref="Guid(ReadOnlySpan{byte})"/> expects.
    /// </summary>
    public static Guid BytesToGuid(ReadOnlySpan<byte> bytes) => new Guid(bytes);
}
