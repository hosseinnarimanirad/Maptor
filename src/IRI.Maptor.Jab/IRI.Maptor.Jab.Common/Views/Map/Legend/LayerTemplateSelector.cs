using IRI.Maptor.Jab.Common.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace IRI.Maptor.Jab.Controls;

public class LayerTemplateSelector : DataTemplateSelector
{
    public DataTemplate GroupLayerTemplate { get; set; }
    public DataTemplate NormalLayerTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is ILayer layer && layer.IsGroupLayer)
            return GroupLayerTemplate;
        else
            return NormalLayerTemplate;
    }
}
