using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Converters;

/// <summary>
/// The halo colour of a text label: values[0] = an explicit halo colour (Color?, null for
/// automatic), values[1] = the text colour. Without an explicit colour the halo contrasts with
/// the text (see <see cref="ColorToContrastBrushConverter"/>).
/// </summary>
public class HaloColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var haloColor = values?.Length > 0 && values[0] is Color explicitColor ? explicitColor : (Color?)null;

        var textColor = values?.Length > 1 && values[1] is Color color ? color : Colors.Black;

        return GetHaloColor(haloColor, textColor);
    }

    public static Color GetHaloColor(Color? haloColor, Color textColor)
        => haloColor ?? ColorToContrastBrushConverter.GetContrastColor(textColor);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
