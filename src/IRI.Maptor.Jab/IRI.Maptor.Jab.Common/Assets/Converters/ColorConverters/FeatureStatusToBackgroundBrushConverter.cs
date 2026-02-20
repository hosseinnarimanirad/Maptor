using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Assets.ColorBrushes;
using IRI.Maptor.Jab.Common.Helpers;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

/// <summary>
/// Converts FeatureStatus to background-appropriate brushes (light tints).
/// Used for DataGrid row backgrounds; Removed/CanceledNew are typically filtered out of the grid.
/// </summary>
public class FeatureStatusToBackgroundBrushConverter : IValueConverter
{
    private static readonly Brush LightEmeraldBrush = BrushHelper.CreateBrush(ModernUIColors.GreenString, 0.2);
    private static readonly Brush LightOrangeBrush = BrushHelper.CreateBrush(ModernUIColors.OrangeString, 0.2);
    private static readonly Brush LightRedBrush = BrushHelper.CreateBrush(ModernUIColors.RedString, 0.3);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FeatureStatus status)
            return status switch
            {
                FeatureStatus.Unchanged => Brushes.Transparent,
                FeatureStatus.New => LightEmeraldBrush,
                FeatureStatus.Updated => LightOrangeBrush,
                FeatureStatus.Removed => LightRedBrush,
                FeatureStatus.CanceledNew => Brushes.Transparent,
                _ => Brushes.Transparent
            };

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
