using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Jab.Wpf.ViewModels.Symbology;

namespace IRI.Maptor.Jab.Controls.Symbology.Sld;

public class SymbolizerDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate PointTemplate { get; set; }
    public DataTemplate LineTemplate { get; set; }
    public DataTemplate PolygonTemplate { get; set; }
    public DataTemplate TextTemplate { get; set; }
    public DataTemplate RasterTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is PointSymbolizerViewModel)
            return PointTemplate;

        if (item is LineSymbolizerViewModel)
            return LineTemplate;

        if (item is PolygonSymbolizerViewModel)
            return PolygonTemplate;

        if (item is TextSymbolizerViewModel)
            return TextTemplate;

        if (item is RasterSymbolizerViewModel)
            return RasterTemplate;

        return base.SelectTemplate(item, container);
    }
}

