using System;
using System.Globalization;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

/// <summary>
/// Converts an enum value to bool by comparing with the ConverterParameter.
/// Useful for binding radio buttons or toggle buttons to enum properties.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // Get the enum value as string
        var enumValue = value.ToString();

        // Get the parameter as string
        var paramValue = parameter.ToString();

        // Compare them
        return enumValue?.Equals(paramValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Binding.DoNothing;

        // If checkbox is checked, return the enum value specified in parameter
        if ((bool)value)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }

        // If unchecked, don't change the value
        return Binding.DoNothing;
    }
}



