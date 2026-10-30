using System;
using System.Globalization;
using System.Windows.Data;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Converters;

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
        // machine text (ISO) first: with the fa-IR culture, which uses the Persian calendar, a plain TryParse reads
        // "2026-10-30" as the Persian year 2026
        else if (value is string dateStr &&
                 (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ||
                  DateTime.TryParse(dateStr, out parsed)))
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
                return localTime.ToPersianString(Format);

            return localTime.ToLongPersianDateTimeSimple(useFarsiNumbers: true);
        }

        if (!string.IsNullOrEmpty(Format))
            return localTime.ToString(Format, LocalizationManager.Instance.CurrentCulture);

        return localTime.ToString("g", LocalizationManager.Instance.CurrentCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
