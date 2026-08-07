using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Core.Layers;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.Converters;

public class FilteredSubLayersConverter : IMultiValueConverter
{
    private static readonly List<ILayer> EmptyLayers = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
            return GetSortedView(EmptyLayers);

        var layers = values[0] as System.Collections.IEnumerable;

        var filterText = (values[1] as string)?.Trim();

        var kinds = values[2] as IList<DataSourceKindFilterItem>;

        var allowedKinds = kinds?.Where(k => k.IsSelected).Select(k => k.Kind).ToList() ?? [];

        if (layers is null)
            return GetSortedView(EmptyLayers);
         
        var hasNameFilter = !string.IsNullOrEmpty(filterText);
        var hasKindFilter = kinds?.Any(k => k.IsSelected == false) == true;

        var view = GetSortedView(layers);

        if (!hasNameFilter && !hasKindFilter)
        {
            view.Filter = null;
            return layers;
        }
        else
        {
            view.Filter = obj =>
            {
                if (obj is not ILayer layer) return false;
                return FilterPredicate(layer, filterText!, allowedKinds, hasNameFilter, hasKindFilter);
            };
        }

        view.Refresh();
        return view;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    // Helper method: returns an ICollectionView sorted by TocOrder descending
    private static ICollectionView GetSortedView(IEnumerable collection)
    {
        var view = CollectionViewSource.GetDefaultView(collection);

        using (view.DeferRefresh())  // Prevents multiple refreshes
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("TocOrder", ListSortDirection.Descending));

            // live sorting, so a sub-layer reorder repositions the row the moment its
            // TocOrder changes; without it the swap is only visible after a full refresh
            // of the root legend view
            if (view is ICollectionViewLiveShaping { CanChangeLiveSorting: true } liveShaping)
            {
                liveShaping.LiveSortingProperties.Clear();
                liveShaping.LiveSortingProperties.Add("TocOrder");
                liveShaping.IsLiveSorting = true;
            }
        }

        return view;
    }

    private static bool FilterPredicate(ILayer layer, string filterText, List<DataSourceKind> allowedKinds, bool hasNameFilter, bool hasKindFilter)
    {
        if (layer == null) return false;

        var passesName = !hasNameFilter || LayerNameMatches(layer, filterText) ||
            (layer.IsGroupLayer && layer.SubLayers != null && HasMatchingDescendant(layer, filterText));

        var passesKind = !hasKindFilter || IsDataSourceKindAllowed(layer, allowedKinds) ||
            (layer.IsGroupLayer && layer.SubLayers != null && HasDescendantWithAllowedDataSourceKind(layer, allowedKinds));

        return passesName && passesKind;
    }


    private static bool IsDataSourceKindAllowed(ILayer layer, List<DataSourceKind> allowedKinds)
    {
        var kind = layer.DataSource?.DataSourceKind ?? DataSourceKind.Other;
        return allowedKinds.Contains(kind);
    }

    public static bool HasDescendantWithAllowedDataSourceKind(ILayer layer, List<DataSourceKind> allowedKinds)
    {
        if (layer == null) return false;

        if (!layer.IsGroupLayer && IsDataSourceKindAllowed(layer, allowedKinds))
            return true;

        if (layer.SubLayers is null) return false;

        foreach (var child in layer.SubLayers)
        {
            if (HasDescendantWithAllowedDataSourceKind(child, allowedKinds))
                return true;
        }

        return false;
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
