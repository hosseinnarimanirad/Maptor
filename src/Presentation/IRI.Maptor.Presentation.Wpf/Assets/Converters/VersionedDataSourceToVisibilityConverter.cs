using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

using IRI.Maptor.Core.Versioning;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>
/// Visible when the bound data source submits edits for review instead of writing them to
/// live (see <see cref="IVersionedEditTarget"/>). Lets the legend badge a versioned layer
/// without the UI tier knowing which persistence type is behind it.
/// </summary>
public class VersionedDataSourceToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is IVersionedEditTarget ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
