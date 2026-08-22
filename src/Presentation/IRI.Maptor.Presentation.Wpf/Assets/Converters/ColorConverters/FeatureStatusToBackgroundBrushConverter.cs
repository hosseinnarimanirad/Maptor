using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>
/// Row-background brush for a feature's edit status.
/// Used for DataGrid row backgrounds; Removed/CanceledNew are typically filtered out of the grid.
/// <para>
/// Pairs with <see cref="FeatureStatusToBrushConverter"/>: this returns the ".Fill" tint of the
/// same semantic colour the foreground converter returns, so the two can never disagree. They
/// previously drew on two different palettes — a foreground orange of #FFFF8130 against a row
/// fill derived from #FFFA6900, and neither followed the theme.
/// </para>
/// </summary>
public class FeatureStatusToBackgroundBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FeatureStatus status)
            return status switch
            {
                FeatureStatus.Unchanged => Brushes.Transparent,
                FeatureStatus.New => StatusBrushes.ValidFill,
                FeatureStatus.Updated => StatusBrushes.WarningFill,
                FeatureStatus.Removed => StatusBrushes.InvalidFill,
                FeatureStatus.CanceledNew => Brushes.Transparent,
                _ => Brushes.Transparent
            };

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
