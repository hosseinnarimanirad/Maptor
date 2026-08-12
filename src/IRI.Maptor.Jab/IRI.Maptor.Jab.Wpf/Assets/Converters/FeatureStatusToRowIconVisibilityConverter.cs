using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Wpf.Converters;

/// <summary>
/// Collapses the row status icon when Status is Unchanged; Visible otherwise.
/// </summary>
public class FeatureStatusToRowIconVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FeatureStatus status && status == FeatureStatus.Unchanged)
            return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
