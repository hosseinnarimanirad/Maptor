using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.Converters;
using IRI.Maptor.Presentation.Wpf.Models.Legend;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Core;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Map;

public class LegendViewModel : Notifier
{
    public const string DefaultTocGroup = "DEFAULT";

    public const string NoneTocGroup = "None";

    private string _layerNameFilterText = string.Empty;

    private bool _triggerKindChanged = true;

    //private Dictionary<ILayer, bool> _filterCache = new();

    private string _tocGroup = DefaultTocGroup;
    public string TocGroup
    {
        get { return _tocGroup; }
        set
        {
            _tocGroup = value;
            RaisePropertyChanged();
        }
    }


    private readonly ObservableCollection<DataSourceKindFilterItem> _dataSourceKindFilterItems;
    public ObservableCollection<DataSourceKindFilterItem> DataSourceKindFilterItems => _dataSourceKindFilterItems;

    /// <summary>The same items grouped by category (Vector, Raster, Service, then the rest).</summary>
    public IReadOnlyList<DataSourceKindFilterGroup> DataSourceKindFilterGroups { get; }

    public string LayerNameFilterText
    {
        get => _layerNameFilterText;
        set
        {
            if (_layerNameFilterText == value) return;

            _layerNameFilterText = value ?? string.Empty;

            RaisePropertyChanged(nameof(LayerNameFilterText));
            RaisePropertyChanged(nameof(HasActiveFilter));

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
            RequestNotifyFilterChanged?.Invoke();
        }
    }

    public int SelectedDataSourceKindCount => _dataSourceKindFilterItems?.Count(i => i.IsSelected) ?? 0;

    public bool ShowSelectedDataSourceKindCount => SelectedDataSourceKindCount != (DataSourceKindFilterItems?.Count ?? 0);

    /// <summary>Kinds that have layers on the map but are filtered out (what the filter button's badge shows).</summary>
    public int HiddenDataSourceKindCount => _dataSourceKindFilterItems?.Count(i => !i.IsSelected && i.IsAvailable) ?? 0;

    /// <summary>True while the layer-type filter actually hides layers.</summary>
    public bool IsDataSourceKindFilterActive => HiddenDataSourceKindCount > 0;

    public bool HasActiveFilter =>
        !string.IsNullOrWhiteSpace(_layerNameFilterText) ||
        (SelectedDataSourceKindCount > 0 && SelectedDataSourceKindCount < (_dataSourceKindFilterItems?.Count ?? 0));


    public Func<Task>? RequestRefreshView { get; set; }

    public Action? RequestNotifyFilterChanged { get; set; }


    public LegendViewModel()
    {
        _dataSourceKindFilterItems = new ObservableCollection<DataSourceKindFilterItem>(
            Enum.GetValues(typeof(DataSourceKind))
                .Cast<DataSourceKind>()
                .Select(k => new DataSourceKindFilterItem(k, isSelected: true)));

        foreach (var item in _dataSourceKindFilterItems)
            item.PropertyChanged += DataSourceKindFilterItem_PropertyChanged;

        DataSourceKindFilterGroups = _dataSourceKindFilterItems
            .GroupBy(i => i.Category)
            .OrderBy(g => CategoryOrder(g.Key))
            .Select(g => new DataSourceKindFilterGroup(g.Key, g.ToList()))
            .ToList();
    }

    private static int CategoryOrder(DataSourceCategory category) => category switch
    {
        DataSourceCategory.Vector => 0,
        DataSourceCategory.Raster => 1,
        DataSourceCategory.Service => 2,
        _ => 3
    };


    private void InvalidateFilterCache()
    {
        RequestRefreshView?.Invoke(); 
    }

    public bool IsFilterPassedCached(ILayer layer)
    {
        if (layer == null) return false;
         
        bool result = IsFilterPassed(layer);
         
        return result;
    }

    public List<DataSourceKind> GetAllowedDataSourceKinds()
    {
        var selected = _dataSourceKindFilterItems?.Where(i => i.IsSelected).Select(i => i.Kind).ToList() ?? [];

        return selected;
    }

    private bool PassKindFilter(ILayer layer)
    {
        var allowedKinds = this.GetAllowedDataSourceKinds();

        var passesKindFilter = layer.IsGroupLayer
            ? FilteredSubLayersConverter.HasDescendantWithAllowedDataSourceKind(layer, allowedKinds)
            : allowedKinds.Contains(layer.DataSource?.DataSourceKind ?? DataSourceKind.Other);

        return passesKindFilter;
    }

    private bool PassNameFilter(ILayer layer)
    {
        var filter = this.LayerNameFilterText?.Trim();

        if (string.IsNullOrEmpty(filter))
            return true;

        if (layer.IsGroupLayer)
            return FilteredSubLayersConverter.HasMatchingDescendant(layer, filter);

        else
            return layer.LayerName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool IsFilterPassed(ILayer layer)
    {
        return PassKindFilter(layer) && PassNameFilter(layer);
    }

    private void RaiseKindFilterStateChanged()
    {
        RaisePropertyChanged(nameof(SelectedDataSourceKindCount));
        RaisePropertyChanged(nameof(ShowSelectedDataSourceKindCount));
        RaisePropertyChanged(nameof(HiddenDataSourceKindCount));
        RaisePropertyChanged(nameof(IsDataSourceKindFilterActive));
        RaisePropertyChanged(nameof(HasActiveFilter));
    }

    private void DataSourceKindFilterItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_triggerKindChanged)
            return;

        // a kind gaining or losing its last layer changes what counts as hidden, not the filter itself
        if (e.PropertyName == nameof(DataSourceKindFilterItem.IsAvailable))
        {
            RaisePropertyChanged(nameof(HiddenDataSourceKindCount));
            RaisePropertyChanged(nameof(IsDataSourceKindFilterActive));
            return;
        }

        if (e.PropertyName == nameof(DataSourceKindFilterItem.IsSelected))
        {
            RaiseKindFilterStateChanged();

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
            RequestNotifyFilterChanged?.Invoke();
        }
    }

    /// <summary>
    /// Recounts the layers per data-source kind for this legend's TOC group. <paramref name="layers"/> are
    /// the map's top-level layers; group layers are walked down to their leaves. Only the layer types the
    /// legend lists (vector, raster, image pyramid) count, so drawings, selections and base maps never
    /// enable a kind the user cannot see in the legend.
    /// </summary>
    public void UpdateLayerCounts(IEnumerable<ILayer> layers)
    {
        var counts = new Dictionary<DataSourceKind, int>();

        void Count(ILayer layer)
        {
            if (layer.IsGroupLayer)
            {
                foreach (var child in layer.SubLayers ?? Enumerable.Empty<ILayer>())
                    Count(child);

                return;
            }

            if (layer.Type != LayerType.VectorLayer && layer.Type != LayerType.Raster && layer.Type != LayerType.ImagePyramid)
                return;

            var kind = layer.DataSource?.DataSourceKind ?? DataSourceKind.Other;

            counts[kind] = counts.TryGetValue(kind, out var n) ? n + 1 : 1;
        }

        foreach (var layer in layers ?? Enumerable.Empty<ILayer>())
        {
            if (layer is null || !string.Equals(layer.TocGroup, TocGroup, StringComparison.OrdinalIgnoreCase))
                continue;

            Count(layer);
        }

        foreach (var item in _dataSourceKindFilterItems)
            item.LayerCount = counts.TryGetValue(item.Kind, out var n) ? n : 0;
    }

    #region Commands

    private RelayCommand? _selectAllDataSourceKindsCommand;
    public RelayCommand SelectAllDataSourceKindsCommand =>
        _selectAllDataSourceKindsCommand ??= new RelayCommand(_ =>
        {
            _triggerKindChanged = false;

            foreach (var item in _dataSourceKindFilterItems)
                item.IsSelected = true;

            _triggerKindChanged = true;

            RaiseKindFilterStateChanged();

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
            RequestNotifyFilterChanged?.Invoke();
        });


    private RelayCommand? _clearAllDataSourceKindsCommand;
    public RelayCommand ClearAllDataSourceKindsCommand =>
        _clearAllDataSourceKindsCommand ??= new RelayCommand(_ =>
        {
            _triggerKindChanged = false;

            foreach (var item in _dataSourceKindFilterItems)
                item.IsSelected = false;

            _triggerKindChanged = true;

            RaiseKindFilterStateChanged();

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
            RequestNotifyFilterChanged?.Invoke();
        });


    private RelayCommand? _showOnlyDataSourceKindCommand;
    /// <summary>Parameter: the <see cref="DataSourceKindFilterItem"/> to keep; every other kind is deselected.</summary>
    public RelayCommand ShowOnlyDataSourceKindCommand =>
        _showOnlyDataSourceKindCommand ??= new RelayCommand(param =>
        {
            if (param is not DataSourceKindFilterItem only)
                return;

            _triggerKindChanged = false;

            foreach (var item in _dataSourceKindFilterItems)
                item.IsSelected = ReferenceEquals(item, only);

            _triggerKindChanged = true;

            RaiseKindFilterStateChanged();

            InvalidateFilterCache();
            RequestNotifyFilterChanged?.Invoke();
        });


    private RelayCommand? _clearFilterCommand;
    public RelayCommand ClearFilterCommand =>
        _clearFilterCommand ??= new RelayCommand(_ =>
        {
            LayerNameFilterText = string.Empty;

            _triggerKindChanged = false;

            foreach (var item in _dataSourceKindFilterItems)
                item.IsSelected = true;

            _triggerKindChanged = true;

            RaiseKindFilterStateChanged();

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
            RequestNotifyFilterChanged?.Invoke();
        });

    #endregion

}
