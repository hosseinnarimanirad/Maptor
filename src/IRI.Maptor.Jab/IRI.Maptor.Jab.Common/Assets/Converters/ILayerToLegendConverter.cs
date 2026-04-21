using IRI.Maptor.Jab.Common;
using System;
using System.Windows.Data;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class ILayerToLegendConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        SpatialModelMode layerType = (SpatialModelMode)value;

        return layerType switch
        {
            SpatialModelMode.None => null,
            SpatialModelMode.Point => new EllipseGeometry(new System.Windows.Rect(0, 0, 5, 5)),
            //SpatialModelMode.Polyline => new LineGeometry(new System.Windows.Point(0, 5), new System.Windows.Point(10, 5)),
            //SpatialModelMode.Polyline => Geometry.Parse("M 0,5 L 4,2 L 8,5 L 12,2"),
            SpatialModelMode.Polyline => Geometry.Parse("M 2,16 L 6,8 L 12,12 L 16,4"),            
            //SpatialModelMode.Polygon => Geometry.Parse("F1 M 0.499,10.500L 9.769,10.500L 6.342,6.005L 9.825,1.230L 4.264,0.499L 0.837,4.938L 0.499,10.500 Z"),
            SpatialModelMode.Polygon => Geometry.Parse("M 9,2 L 15.7,6.8 L 13.1,14.7 L 4.9,14.7 L 2.3,6.8 Z"),
            SpatialModelMode.Label => null,
            SpatialModelMode.Raster => null,
            SpatialModelMode.Complex => null,
            _ => null
        };

        //if (layerType.HasFlag(LayerType.Raster) ||
        //    layerType.HasFlag(LayerType.BaseMap))
        //{
        //    return null; 
        //}
        //if (layerType == SpatialModelMode.Point)
        //{
        //    return new RectangleGeometry(new System.Windows.Rect(0, 0, 5, 5)); 
        //}
        ////else if (layerType.HasFlag(LayerType.Polyline))
        //else if (layerType == SpatialModelMode.Polyline)
        //{
        //    return new LineGeometry(new System.Windows.Point(0, 0), new System.Windows.Point(10, 10)); 
        //}
        ////else if (layerType.HasFlag(LayerType.Polygon))
        //else if (layerType == SpatialModelMode.Polygon)
        //{
        //    return Geometry.Parse("F1 M 0.499,10.500L 9.769,10.500L 6.342,6.005L 9.825,1.230L 4.264,0.499L 0.837,4.938L 0.499,10.500 Z"); 
        //}
        //else
        //{
        //    //return new EllipseGeometry(new System.Windows.Rect(0, 0, 5, 5));
        //    return null;
        //}
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
