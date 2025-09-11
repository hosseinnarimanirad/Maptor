using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class StringToRtlFlowDirection : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return FlowDirection.LeftToRight;
        }

        var result = value.ToString().Trim().First() > 128 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        return result;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
