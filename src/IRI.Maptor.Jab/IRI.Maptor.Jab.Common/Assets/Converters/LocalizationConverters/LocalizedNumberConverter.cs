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

        //if (value is not double number)
        //    return Binding.DoNothing;
        decimal number;

        try
        {
            number = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return Binding.DoNothing; // Not a numeric type
        }

        // Get format string from parameter, default to "N1"
        string format = parameter as string ?? "N1";

        // Format the number using invariant culture to get consistent digits
        string formatted = number.ToString(format, CultureInfo.InvariantCulture);

        return LocalizationManager.GetLocalizedNumberString(formatted);

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
