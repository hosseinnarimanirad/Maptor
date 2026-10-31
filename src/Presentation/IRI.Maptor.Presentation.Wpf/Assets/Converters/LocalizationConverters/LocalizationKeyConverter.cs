using System;
using System.Globalization;
using System.Windows.Data;

using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>
/// Looks up a resource key that comes from data (e.g. a list item's NameKey), which the
/// {Binding [key]} indexer syntax cannot do since the key is not a literal.
/// values[0] = the key; values[1] = any binding on the LocalizationManager (e.g. CurrentCulture),
/// only there so the text is re-evaluated when the language changes.
/// </summary>
public class LocalizationKeyConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 1 || values[0] is not string key || key.Length == 0)
            return string.Empty;

        return LocalizationManager.Instance[key];
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
