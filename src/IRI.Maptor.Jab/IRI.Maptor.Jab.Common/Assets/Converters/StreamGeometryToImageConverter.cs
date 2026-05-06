using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace IRI.Maptor.Jab.Common.Converters;

public class StreamGeometryToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        DrawingImage result;

        var brush = Brushes.Black;

        try
        {
            if (value is StreamGeometry)
            {                    
                result = new DrawingImage(new GeometryDrawing(brush, null, value as StreamGeometry));
            }
            else
            {
                var pathGeometry = value.ToString();

                result = new DrawingImage(new GeometryDrawing(brush, null, Geometry.Parse(pathGeometry)));
            }
        }
        catch
        {
            result = new DrawingImage();
        }

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
