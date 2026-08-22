using System;
using System.Globalization;
using System.Windows.Data;

using IRI.Maptor.Presentation.Wpf.Models.Filters;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class NumericFilterOperatorToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NumericFilterOperator op)
            return op switch
            {
                NumericFilterOperator.Equal => "=",
                NumericFilterOperator.NotEqual => "≠",
                NumericFilterOperator.LessThan => "<",
                NumericFilterOperator.LessThanOrEqual => "≤",
                NumericFilterOperator.GreaterThan => ">",
                NumericFilterOperator.GreaterThanOrEqual => "≥",
                NumericFilterOperator.Between => "≥…≤",
                _ => value.ToString() ?? string.Empty,
            };

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
