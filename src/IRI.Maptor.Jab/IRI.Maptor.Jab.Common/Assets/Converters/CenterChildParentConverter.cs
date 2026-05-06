using System;
using System.Windows.Data;
using System.Globalization;

namespace IRI.Maptor.Jab.Common.Converters;

class CenterChildParentConverter : IMultiValueConverter
{

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double parent = (double)values[0];

        double child = (double)values[1];

        return (parent - child) / 2;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}
