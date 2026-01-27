using IRI.Maptor.Extensions;
using System;
using System.Linq;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

class MultiBooleanToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values,
                            Type targetType,
                            object parameter,
                            System.Globalization.CultureInfo culture)
    {
        if (values.IsNullOrEmpty())
            return System.Windows.Visibility.Collapsed;

        var notVisible = values.Any(x => x is not bool || (bool)x == false);

        //foreach (object value in values)
        //    if (value is bool)
        //    {
        //        visible = visible && (bool)value;
        //    }

        if (notVisible)
            return System.Windows.Visibility.Collapsed;

        else
            return System.Windows.Visibility.Visible;
    }

    public object[] ConvertBack(object value,
                                Type[] targetTypes,
                                object parameter,
                                System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}