using IRI.Maptor.Jab.Wpf.Models;
using IRI.Maptor.Jab.Wpf.Models.CoordinateEditor;
using IRI.Maptor.Jab.Wpf.ViewModels.CoordinateEditor;
using System;
using System.Globalization;
using System.Windows.Data;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class PointNumberConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var result = 0;

        if (values == null || values.Length < 4)
            return result.ToString();

        if (values[2] is Locateable point &&
            values[3] is GeometryEditorViewModel presenter)
        {
            result = presenter.GetPointNumber(point);
        }

        return result.ToString();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

