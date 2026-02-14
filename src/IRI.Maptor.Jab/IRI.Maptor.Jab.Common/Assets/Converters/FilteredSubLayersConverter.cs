using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

using IRI.Maptor.Jab.Common;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class FilteredSubLayersConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
            return new List<ILayer>();

        var layers = values[0] as System.Collections.IEnumerable;

        var filterText = (values[1] as string)?.Trim();

        if (layers is null)
            return new List<ILayer>();

        if (string.IsNullOrEmpty(filterText))
            return layers;/*is IList<ILayer> list ? list : layers.Cast<ILayer>().ToList();*/

        var result = new List<ILayer>();

        foreach (ILayer layer in layers)
        {
            if (layer == null) continue;

            if (LayerNameMatches(layer, filterText))
                result.Add(layer);

            else if (layer.IsGroupLayer && layer.SubLayers != null && HasMatchingDescendant(layer, filterText))
                result.Add(layer);
        }

        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static bool LayerNameMatches(ILayer layer, string filter)
    {
        return !string.IsNullOrEmpty(layer.LayerName) &&
               layer.LayerName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool HasMatchingDescendant(ILayer layer, string filter)
    {
        if (layer == null) return false;

        if (!layer.IsGroupLayer && LayerNameMatches(layer, filter))
            return true;

        if (layer.SubLayers is null) return false;

        foreach (var child in layer.SubLayers)
        {
            if (HasMatchingDescendant(child, filter))
                return true;
        }

        return false;
    }
}
