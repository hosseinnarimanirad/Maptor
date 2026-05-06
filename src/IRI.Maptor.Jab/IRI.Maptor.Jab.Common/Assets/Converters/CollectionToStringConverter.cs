using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Common.Converters;

public class CollectionToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable collection)
        {
            var items = collection.Cast<object>().Select(i => i?.ToString() ?? string.Empty);
            return string.Join(", ", items);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}




