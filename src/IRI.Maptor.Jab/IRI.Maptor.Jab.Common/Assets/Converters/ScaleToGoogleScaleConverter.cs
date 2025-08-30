using System;
using System.Globalization;
using System.Windows.Data;

using IRI.Maptor.Jab.Common.Models;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class ScaleToGoogleScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var mapScale = (double)value;

        //var scale = GoogleScale.Scales.FirstOrDefault(i => i.Scale > googleScale);

        return GoogleScale.GetNearestScale(mapScale);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return 1;
        }

        return 1.0 / (value as GoogleScale).InverseScale;

    }
}
