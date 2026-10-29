using System;
using System.Globalization;
using System.Windows.Data;

namespace IRI.Maptor.Samples.Wpf.Gallery.Shell;

/// <summary>Shows a string in capitals: the navigation's small section headers.</summary>
public sealed class UpperCaseConverter : IValueConverter
{
    public static UpperCaseConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString()?.ToUpper(culture);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
