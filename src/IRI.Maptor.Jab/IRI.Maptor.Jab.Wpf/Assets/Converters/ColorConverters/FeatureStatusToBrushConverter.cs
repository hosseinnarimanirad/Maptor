using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Wpf.Helpers;

namespace IRI.Maptor.Jab.Wpf.Converters;

/// <summary>
/// Foreground brush for a feature's edit status.
/// <para>
/// Maps onto the semantic status palette rather than a fixed set of colours: New reads as
/// Valid, Updated as Warning, Removed as Invalid, and everything with nothing to report as
/// Muted or plain theme foreground. Previously these were literals (<c>Brushes.Black</c> for
/// Unchanged, <c>MapAppColors</c> for the rest) which did not follow the light/dark swap, so
/// an unchanged row's glyph rendered black on a dark background.
/// </para>
/// </summary>
public class FeatureStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FeatureStatus status)
            return status switch
            {
                FeatureStatus.Unchanged => StatusBrushes.ThemeForeground,
                FeatureStatus.Updated => StatusBrushes.Warning,
                FeatureStatus.New => StatusBrushes.Valid,
                FeatureStatus.Removed => StatusBrushes.Invalid,
                FeatureStatus.CanceledNew => StatusBrushes.Muted,

                _ => StatusBrushes.Muted
            };

        return StatusBrushes.Muted;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
