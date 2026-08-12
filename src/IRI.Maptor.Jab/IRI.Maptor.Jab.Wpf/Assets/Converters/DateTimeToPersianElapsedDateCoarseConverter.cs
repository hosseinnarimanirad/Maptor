using System;
using System.Windows.Data;
using System.Globalization;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class DateTimeToPersianElapsedDateCoarseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || !(value is DateTime))
        {
            return string.Empty;
        }

        var dateTime = (DateTime)value;

        return dateTime.GetPersianElapsedDateCoarse();

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
