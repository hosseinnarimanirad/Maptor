using System;
using System.Windows.Data;
using System.Globalization;
using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class LocalizedNumberConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return null;
         
        string format = parameter as string ?? "N0";

        decimal number;
        string formatted;

        bool isNumeric = false;

        try
        {
            // Try to get a numeric value from the input
            if (value is string str)
            {
                // Parse the string as a number (allowing decimal separators, signs, etc.)
                isNumeric = decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out number);
            }
            else
            {
                number = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                isNumeric = true;
            }

            formatted = isNumeric ? number.ToString(format, CultureInfo.InvariantCulture) : value.ToString() ?? string.Empty;

            return LocalizationManager.GetLocalizedNumberString(formatted);
        }
        catch (Exception)
        {
            return Binding.DoNothing;
        }
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
