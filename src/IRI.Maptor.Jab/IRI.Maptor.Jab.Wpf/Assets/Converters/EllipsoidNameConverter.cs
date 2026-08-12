using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class EllipsoidNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Ellipsoid ellipsoid)
        {
            return $" | Ellipsoid: {ellipsoid.Name}";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

