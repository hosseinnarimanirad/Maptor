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
        if (values?.Length > 0 && values[0] is Color explicitColor)
            return explicitColor;

        var textColor = values?.Length > 1 && values[1] is Color color ? color : Colors.Black;

        return ColorToContrastBrushConverter.GetContrastColor(textColor);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
