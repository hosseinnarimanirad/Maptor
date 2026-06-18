using System;
using System.Globalization;
using System.Windows.Data;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Common.Converters;

public class LocalizedDateTimeConverter : IValueConverter
{
    /// <summary>
    /// Optional display format. Supports standard .NET DateTime format tokens
    /// (yyyy, MM, dd, HH, mm, ss). When null the default localized format is used.
    /// For Persian culture the same pattern tokens are applied against PersianCalendar.
    /// </summary>
    public string? Format { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        DateTime dateTime;
        if (value is DateTime dt)
        {
            dateTime = dt;
        }
        else if (value is DateTimeOffset dto)
        {
            dateTime = dto.LocalDateTime;
        }
        else if (value is string dateStr && DateTime.TryParse(dateStr, out var parsed))
        {
            dateTime = parsed;
        }
        else
        {
            return string.Empty;
        }

        var localTime = dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime;

        if (LocalizationManager.Instance.IsPersian)
        {
            if (!string.IsNullOrEmpty(Format))
                return ApplyPersianFormat(localTime, Format);

            return localTime.ToLongPersianDateTimeSimple(useFarsiNumbers: true);
        }

        if (!string.IsNullOrEmpty(Format))
            return localTime.ToString(Format, LocalizationManager.Instance.CurrentCulture);

        return localTime.ToString("g", LocalizationManager.Instance.CurrentCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static readonly PersianCalendar _persianCalendar = new PersianCalendar();

    // Earliest date representable in the Persian calendar
    private static readonly DateTime _minPersianDate = new DateTime(622, 3, 22);

    /// <summary>
    /// Applies a .NET-style format pattern against the Persian calendar.
    /// Supported tokens: yyyy, MM, dd, HH, mm, ss.
    /// All other characters are passed through as literals.
    /// The resulting numeric characters are converted to Farsi digits.
    /// </summary>
    private static string ApplyPersianFormat(DateTime dt, string format)
    {
        if (dt < _minPersianDate)
            dt = _minPersianDate;

        var year  = _persianCalendar.GetYear(dt);
        var month = _persianCalendar.GetMonth(dt);
        var day   = _persianCalendar.GetDayOfMonth(dt);
        var hour  = dt.Hour;
        var min   = dt.Minute;
        var sec   = dt.Second;

        // Replace longest tokens first to avoid partial replacements (e.g. MM before M).
        var result = format
            .Replace("yyyy", FormattableString.Invariant($"{year:0000}"))
            .Replace("MM",   FormattableString.Invariant($"{month:00}"))
            .Replace("dd",   FormattableString.Invariant($"{day:00}"))
            .Replace("HH",   FormattableString.Invariant($"{hour:00}"))
            .Replace("mm",   FormattableString.Invariant($"{min:00}"))
            .Replace("ss",   FormattableString.Invariant($"{sec:00}"));

        return result.LatinNumbersToFarsiNumbers();
    }
}
