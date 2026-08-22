using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Core.SpatialReferenceSystem;
using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;

namespace IRI.Maptor.Presentation.Wpf.Converters;

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

