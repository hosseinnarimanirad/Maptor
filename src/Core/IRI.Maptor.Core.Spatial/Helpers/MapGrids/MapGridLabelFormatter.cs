using System;
using System.Globalization;

using IRI.Maptor.Core.Common.Helpers;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// Turns a grid line's value into the text written beside it.
/// </summary>
/// <remarks>
/// <para>
/// The two families are written differently, and deliberately so.
/// </para>
/// <para>
/// A <strong>geodetic</strong> value follows the convention a printed sheet uses: spelled out once —
/// on the first line met along each edge, and again whenever the degree rolls over — with every line
/// after that carrying only the part that changed. <c>51°10′E</c>, then <c>20′ 30′ 40′</c>. It works
/// because the abbreviated form keeps its own unit mark, so a bare <c>30′</c> still reads as
/// minutes.
/// </para>
/// <para>
/// A <strong>metric</strong> value is written out in full on every line — <c>534000</c>. The same
/// sheet convention was tried here first (<c>⁵34⁰⁰⁰ mE</c> once, then <c>35 36 37</c>) and the user
/// rejected it: two bare digits with no unit and no anchor is a puzzle, not an abbreviation.
/// </para>
/// </remarks>
public static class MapGridLabelFormatter
{
    /// <summary>One arc-minute, in degrees.</summary>
    private const double OneMinute = 1.0 / 60.0;

    /// <summary>Tolerance for comparing a ladder interval against 1° or 1′, neither of which is exact in binary.</summary>
    private const double IntervalEpsilon = 1e-12;

    #region Geodetic

    /// <summary>
    /// A meridian's or parallel's value: <c>51°30′E</c> in full, <c>30′</c> abbreviated.
    /// </summary>
    /// <param name="degrees">The line's value, signed.</param>
    /// <param name="isLatitude">True for a parallel — decides N/S versus E/W.</param>
    /// <param name="interval">The grid interval in degrees; decides which part is the changing one.</param>
    /// <param name="full">
    /// True for the spelled-out form. At intervals of a whole degree or coarser the two forms are
    /// the same, because the degrees <em>are</em> the changing part.
    /// </param>
    public static string FormatGeodetic(double degrees, bool isLatitude, double interval, bool full)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
            return string.Empty;

        // Whole arc-seconds: every ladder step is an exact multiple of one, and rounding here
        // avoids the 59.999…″ artefacts a truncating split produces.
        var totalSeconds = (long)Math.Round(Math.Abs(degrees) * 3600.0);

        var d = totalSeconds / 3600;
        var m = (totalSeconds % 3600) / 60;
        var s = totalSeconds % 60;

        if (!full && interval < 1.0 - IntervalEpsilon)
        {
            return interval >= OneMinute - IntervalEpsilon
                ? m.ToString("00", CultureInfo.InvariantCulture) + DegreeHelper.minuteSign
                : s.ToString("00", CultureInfo.InvariantCulture) + DegreeHelper.secondSign;
        }

        var hemisphere = GetHemisphere(degrees, isLatitude);

        // Zero parts are dropped rather than printed: "51°E" beats "51°00′00″E" on a screen, and
        // the ladder guarantees the dropped parts really are zero.
        if (m == 0 && s == 0)
            return FormattableString.Invariant($"{d}°{hemisphere}");

        if (s == 0)
            return FormattableString.Invariant($"{d}°{m:00}{DegreeHelper.minuteSign}{hemisphere}");

        return FormattableString.Invariant($"{d}°{m:00}{DegreeHelper.minuteSign}{s:00}{DegreeHelper.secondSign}{hemisphere}");
    }

    /// <summary>
    /// The part of a geodetic value that sits <em>above</em> the changing digits. A label is
    /// spelled out in full whenever this differs from the previous line's.
    /// </summary>
    /// <remarks>
    /// At a whole-degree interval or coarser every line gets a distinct value here, which is the
    /// intended effect: there is nothing to abbreviate, so every label is written in full.
    /// </remarks>
    public static long GetGeodeticHighPart(double degrees, double interval)
    {
        if (double.IsNaN(degrees) || double.IsNaN(interval) || interval <= 0)
            return 0;

        if (interval >= 1.0 - IntervalEpsilon)
            return (long)Math.Round(degrees / interval);

        return interval >= OneMinute - IntervalEpsilon
            ? (long)Math.Floor(degrees)
            : (long)Math.Floor(degrees * 60.0);
    }

    private static string GetHemisphere(double degrees, bool isLatitude)
    {
        if (degrees == 0)
            return string.Empty;

        return isLatitude
            ? (degrees < 0 ? "S" : "N")
            : (degrees < 0 ? "W" : "E");
    }

    #endregion

    #region Metric

    /// <summary>
    /// A projected grid line's value: the whole number of metres, <c>534000</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written out on every line, with no unit and no abbreviation. An earlier version followed the
    /// topographic-sheet convention — <c>⁵34⁰⁰⁰ mE</c> once per edge and then <c>35 36 37</c>, with
    /// the digits a sheet prints small set as Unicode superscripts — and the user rejected it on
    /// sight: abbreviating to two digits was the confusing part, not the length.
    /// </para>
    /// <para>
    /// Nothing distinguishes an easting from a northing in the text, because their positions already
    /// do: eastings are written along the bottom and top of the view, northings up the sides.
    /// </para>
    /// </remarks>
    /// <param name="value">The line's value in metres.</param>
    public static string FormatMetric(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return string.Empty;

        // The metric ladder stops at 10 m, so every line lands on a whole metre.
        return Math.Round(value).ToString("F0", CultureInfo.InvariantCulture);
    }

    #endregion

}
