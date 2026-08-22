using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using IRI.Maptor.Presentation.Core.Layers;

namespace IRI.Maptor.Presentation.Wpf.Controls;

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
