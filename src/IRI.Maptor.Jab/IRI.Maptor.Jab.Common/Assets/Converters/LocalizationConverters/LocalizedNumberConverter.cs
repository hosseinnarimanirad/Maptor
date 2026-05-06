using System;
using System.Windows.Data;
using System.Globalization;
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.Converters;

public class LocalizedNumberConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return null;

        return LocalizationManager.GetLocalizedNumberString(value);

        //if (value is IFormattable formattable)
        //{
        //    // Use culture from binding if available, otherwise fallback to current thread culture
        //    //culture ??= CultureInfo.CurrentUICulture;
        //    culture = Localization.LocalizationManager.Instance.CurrentCulture;

        //    if (string.Equals(culture.Name, "fa-IR", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return formattable?.ToString()?.LatinNumbersToFarsiNumbers();
        //    }
        //    else
        //    {
        //        return formattable.ToString(null, culture);
        //    }
        //}

        //return value.ToString();
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return null;

        string? number;

        if (string.Equals(culture.Name, "fa-IR", StringComparison.OrdinalIgnoreCase))
        {
            number = value?.ToString()?.FarsiNumbersToLatinNumbers();
        }
        else
        {
            number = value?.ToString();
        }

        // Parse back using culture (if you need two-way binding)
        if (double.TryParse(number, NumberStyles.Any, culture, out double result))
            return result;

        return value;
    }
}
