using System;
using System.Windows.Data;
using System.Globalization;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

class StringToFarsiStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
        {
            return value.ToString().LatinNumbersToFarsiNumbers();
        }
        else
        {
            return string.Empty;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
