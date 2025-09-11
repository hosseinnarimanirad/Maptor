using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool)
        {
            if (!string.IsNullOrWhiteSpace(parameter?.ToString()))
            {
                var colors = parameter.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (colors.Length != 2)
                {
                    return Colors.Transparent;
                }

                return (bool)value ? Helpers.BrushHelper.CreateFromNameOrHex(colors[0]) : Helpers.BrushHelper.CreateFromNameOrHex(colors[1]);

            }
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
