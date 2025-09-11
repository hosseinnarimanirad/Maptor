using System;
using System.Windows.Data;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class ArabicStringToFarsiStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value == null ? null : value.ToString().ArabicToFarsi();
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value.ToString().ArabicToFarsi();
    }
}
