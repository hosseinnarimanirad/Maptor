using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Helpers;

public static class DegreeHelper
{

    public const char minuteSign = '\u2019';

    public const char secondSign = '\u201d';

    public static string ToDms(double degreeValue, bool roundSecond = false)
    {
        int degreePart, minutePart;

        double secondPart;

        ToDms(degreeValue, roundSecond, out degreePart, out minutePart, out secondPart);

        if (roundSecond)
        {
            return FormattableString.Invariant($" {degreePart:000}° {minutePart:00}{minuteSign} {secondPart:00.00}{secondSign} ");
        }
        else
        {
            return FormattableString.Invariant($" {degreePart:000}° {minutePart:00}{minuteSign} {secondPart:00.#}{secondSign} ");
        }

    }

    public static void ToDms(double degreeValue, bool roundSecond, out int degree, out int minute, out double second)
    {
        degree = (int)Math.Truncate(degreeValue);

        minute = (int)Math.Truncate((degreeValue - degree) * 60);

        second = (degreeValue - degree - minute / 60.0) * 3600;
    }

    /// <summary>
    /// Splits an angle into unsigned degrees, minutes and seconds plus a sign flag, rounding the
    /// seconds to <paramref name="secondDecimals"/> places and carrying the overflow so that
    /// 59.999″ becomes the next minute rather than printing as 60.00″.
    /// </summary>
    public static void ToDmsComponents(double degreeValue, int secondDecimals, out bool isNegative, out int degree, out int minute, out double second)
    {
        isNegative = degreeValue < 0;

        double totalSeconds = Math.Round(Math.Abs(degreeValue) * 3600.0, secondDecimals);

        degree = (int)Math.Floor(totalSeconds / 3600.0);

        totalSeconds -= degree * 3600.0;

        minute = (int)Math.Floor(totalSeconds / 60.0);

        second = Math.Round(totalSeconds - minute * 60.0, secondDecimals);

        // guard against floating noise pushing the remainder to exactly 60
        if (second >= 60.0)
        {
            second = 0;
            minute++;
        }

        if (minute >= 60)
        {
            minute = 0;
            degree++;
        }
    }

    /// <summary>
    /// Human-readable DMS with a hemisphere letter, e.g. <c>35°41′23.45″ N</c> or
    /// <c>51°23′12.00″ E</c>. Always invariant and always Latin digits.
    /// </summary>
    public static string ToDmsWithHemisphere(double degreeValue, bool isLatitude, int secondDecimals = 2)
    {
        ToDmsComponents(degreeValue, secondDecimals, out var isNegative, out var degree, out var minute, out var second);

        var hemisphere = isLatitude ? (isNegative ? "S" : "N") : (isNegative ? "W" : "E");

        var secondFormat = secondDecimals > 0 ? "00." + new string('0', secondDecimals) : "00";

        return FormattableString.Invariant($"{degree}°{minute:00}′{second.ToString(secondFormat, System.Globalization.CultureInfo.InvariantCulture)}″ {hemisphere}");
    }
}
