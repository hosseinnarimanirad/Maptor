using System;
using System.Globalization;
using System.Windows.Data;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.Converters;

public class LocalizedDateTimeConverter : IValueConverter
{
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
            return localTime.ToLongPersianDateTimeSimple(useFarsiNumbers: true);

        return localTime.ToString("g", LocalizationManager.Instance.CurrentCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
