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

    /// <summary>
    /// A UTC or local value is converted to <paramref name="timeZone"/>. A value without a <see cref="DateTimeKind"/>
    /// carries no zone (a file's wall-clock time, an older server) and is written as it is, in the chosen calendar.
    /// </summary>
    public static string Format(DateTime value, ExportTimeZone timeZone, ExportCalendar calendar, string? format = null)
    {
        var converted = value.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(value, GetTimeZone(timeZone)),
            DateTimeKind.Local => TimeZoneInfo.ConvertTimeFromUtc(value.ToUniversalTime(), GetTimeZone(timeZone)),
            _ => value,
        };

        format = string.IsNullOrWhiteSpace(format) ? DefaultFormat : format;

        return calendar == ExportCalendar.Persian
            ? converted.ToPersianString(format, useFarsiNumbers: false)
            : converted.ToString(format, CultureInfo.InvariantCulture);
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
