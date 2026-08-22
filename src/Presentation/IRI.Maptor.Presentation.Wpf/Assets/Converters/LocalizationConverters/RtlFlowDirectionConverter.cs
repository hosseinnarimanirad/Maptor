using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class RtlFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;
        if (!string.IsNullOrEmpty(text) && IsRtlCharacter(text[0]))
            return FlowDirection.RightToLeft;

        return FlowDirection.LeftToRight;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;

    private bool IsRtlCharacter(char ch)
    {
        // Hebrew, Arabic, Syriac, Thaana, N'Ko, etc.
        return ch >= 0x0590 && ch <= 0x08FF ||
               ch >= 0xFB1D && ch <= 0xFEFC;
    }
}
