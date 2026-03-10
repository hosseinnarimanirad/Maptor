using System;
using System.Linq;
using System.Windows.Data;

using IRI.Maptor.Extensions;


namespace IRI.Maptor.Jab.Common.Assets.Converters;
public class MultiBooleanToBooleanConverter : IMultiValueConverter
{
    public object Convert(object[] values,
                            Type targetType,
                            object parameter,
                            System.Globalization.CultureInfo culture)
    {
        if (values.IsNullOrEmpty())
            return false;

        var result = values.All(x => x is bool && (bool)x == true);

        return result;
    }

    public object[] ConvertBack(object value,
                                Type[] targetTypes,
                                object parameter,
                                System.Globalization.CultureInfo culture)
    {
        return [(bool)value, (bool)value];
    }
}