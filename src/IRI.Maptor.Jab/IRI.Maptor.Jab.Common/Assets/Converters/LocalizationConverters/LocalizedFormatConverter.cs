using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class LocalizedFormatConverter : IMultiValueConverter
{
    public string FormatKey { get; set; } = string.Empty;

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var formatKey = parameter as string ?? FormatKey;
        
        if (string.IsNullOrEmpty(formatKey))
        {
            return string.Empty;
        }

        var formatString = LocalizationManager.Instance[formatKey];
        
        if (string.IsNullOrEmpty(formatString))
        {
            return $"#{formatKey}";
        }

        try
        {
            return string.Format(formatString, values);
        }
        catch
        {
            return formatString;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

