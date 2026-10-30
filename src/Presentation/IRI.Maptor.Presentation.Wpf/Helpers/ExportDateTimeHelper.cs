using System;
using System.Globalization;
using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core.Data;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Writes date/time attribute values as text in a chosen time zone and calendar (feature table's Excel export).
/// The text always uses Latin digits and a fixed year-first layout, so it sorts correctly as text.
/// </summary>
public static class ExportDateTimeHelper
{
    public const string DefaultFormat = "yyyy/MM/dd HH:mm";

    private static readonly Lazy<TimeZoneInfo> _iran = new(FindIranTimeZone);

    public static TimeZoneInfo IranTimeZone => _iran.Value;

    public static TimeZoneInfo GetTimeZone(ExportTimeZone zone) => zone switch
    {
        ExportTimeZone.Utc => TimeZoneInfo.Utc,
        ExportTimeZone.Local => TimeZoneInfo.Local,
        _ => IranTimeZone,
    };

    /// <summary>A DateTime, DateTimeOffset or date text; anything else is not a date.</summary>
    public static bool TryGetDateTime(object? value, out DateTime result)
    {
        switch (value)
        {
            case DateTime dateTime:
                result = dateTime;
                return true;

            case DateTimeOffset offset:
                result = offset.UtcDateTime;
                return true;

            case string text when DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed):
                result = parsed;
                return true;

            default:
                result = default;
                return false;
        }
    }

    /// <param name="storedTimeZone">
    /// The zone a value without a <see cref="DateTimeKind"/> is in, i.e. the zone the server writes its times in.
    /// </param>
    public static string Format(DateTime value, TimeZoneInfo storedTimeZone, ExportTimeZone timeZone, ExportCalendar calendar, string? format = null)
    {
        var converted = TimeZoneInfo.ConvertTimeFromUtc(ToUtc(value, storedTimeZone), GetTimeZone(timeZone));

        format = string.IsNullOrWhiteSpace(format) ? DefaultFormat : format;

        return calendar == ExportCalendar.Persian
            ? converted.ToPersianString(format, useFarsiNumbers: false)
            : converted.ToString(format, CultureInfo.InvariantCulture);
    }

    private static DateTime ToUtc(DateTime value, TimeZoneInfo storedTimeZone)
    {
        switch (value.Kind)
        {
            case DateTimeKind.Utc:
                return value;

            case DateTimeKind.Local:
                return value.ToUniversalTime();

            default:
                try
                {
                    return TimeZoneInfo.ConvertTimeToUtc(value, storedTimeZone);
                }
                catch (ArgumentException)
                {
                    // A clock time skipped by a daylight-saving change: shift by the standard offset.
                    return DateTime.SpecifyKind(value - storedTimeZone.BaseUtcOffset, DateTimeKind.Utc);
                }
        }
    }

    private static TimeZoneInfo FindIranTimeZone()
    {
        foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception)
            {
            }
        }

        // Iran has had no daylight saving since 2022: a fixed +03:30 is right for current data.
        return TimeZoneInfo.CreateCustomTimeZone("Iran Standard Time", TimeSpan.FromMinutes(210), "Iran Standard Time", "Iran Standard Time");
    }
}
