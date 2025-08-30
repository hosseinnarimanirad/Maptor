using IRI.Maptor.Jab.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Controls.Assets.Converters
{
    public class ILayerToLegendConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            LayerType layerType = (LayerType)value;

            if (layerType.HasFlag(LayerType.Raster) ||
                layerType.HasFlag(LayerType.BaseMap))
            {
                return new RectangleGeometry(new System.Windows.Rect(0, 0, 5, 5));
                //return new Rectangle()
                //{
                //    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                //    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                //    Stroke = parameters.VisualParameters.Stroke,
                //    StrokeThickness = 1.0,
                //    Fill = new SolidColorBrush(Colors.LightBlue),
                //    Width = 5,
                //    Height = 5
                //};

            }

            if (layerType.HasFlag(LayerType.Point))
            {
                return new RectangleGeometry(new System.Windows.Rect(0, 0, 5, 5));
                //return new Rectangle()
                //{
                //    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                //    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                //    Stroke = parameters.VisualParameters.Stroke,
                //    StrokeThickness = 1.0,
                //    Fill = parameters.VisualParameters.Fill,
                //    Width = 5,
                //    Height = 5
                //};
            }
            else if (layerType.HasFlag(LayerType.Polyline))
            {
                return new LineGeometry(new System.Windows.Point(0, 0), new System.Windows.Point(10, 10));
                //return new Line()
                //{
                //    StrokeThickness = 3,
                //    X1 = 0,
                //    Y1 = 10,
                //    X2 = 10,
                //    Y2 = 10,
                //    Stroke = parameters.VisualParameters.Stroke,
                //    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                //    VerticalAlignment = System.Windows.VerticalAlignment.Center
                //};
            }
            else if (layerType.HasFlag(LayerType.Polygon))
            {
                return Geometry.Parse("F1 M 0.499,10.500L 9.769,10.500L 6.342,6.005L 9.825,1.230L 4.264,0.499L 0.837,4.938L 0.499,10.500 Z");
                //return new Path()
                //{
                //    Data = Geometry.Parse("F1 M 0.499,10.500L 9.769,10.500L 6.342,6.005L 9.825,1.230L 4.264,0.499L 0.837,4.938L 0.499,10.500 Z"),
                //    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                //    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                //    Stroke = parameters.VisualParameters.Stroke,
                //    StrokeThickness = parameters.VisualParameters.StrokeThickness,
                //    Fill = parameters.VisualParameters.Fill,
                //    Width = 15,
                //    Stretch = Stretch.Uniform
                //    //Height = 15
                //};
            }
            else
            {
                return new EllipseGeometry(new System.Windows.Rect(0, 0, 5, 5));
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
