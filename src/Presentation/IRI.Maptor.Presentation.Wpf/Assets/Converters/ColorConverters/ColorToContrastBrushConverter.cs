using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>
/// Black for light colours, white for dark ones, so content drawn on top of (or around) the
/// colour stays readable. A non-Color value (e.g. null for "theme brush") gets white. Returns
/// a Color instead of a brush when the target is a Color (e.g. DropShadowEffect.Color).
/// </summary>
public class ColorToContrastBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isLight = value is Color color
            && (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0 > 0.6;

        if (targetType == typeof(Color))
            return isLight ? Colors.Black : Colors.White;

        return isLight ? Brushes.Black : Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
