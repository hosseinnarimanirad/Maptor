using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Jab.Common.Converters;
using DocumentFormat.OpenXml.InkML;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public class LegendViewModel : Notifier
{
    private string _layerNameFilterText = string.Empty;

    private bool _triggerKindChanged = true;

    private Dictionary<ILayer, bool> _filterCache = new();

    public LegendViewModel()
    {
        _dataSourceKindFilterItems = new ObservableCollection<DataSourceKindFilterItem>(
            Enum.GetValues(typeof(DataSourceKind))
                .Cast<DataSourceKind>()
                .Select(k => new DataSourceKindFilterItem(k, isSelected: true)));

        foreach (var item in _dataSourceKindFilterItems)
            item.PropertyChanged += DataSourceKindFilterItem_PropertyChanged;
    }


    private void InvalidateFilterCache()
    {
        _filterCache.Clear();
        RequestRefreshView?.Invoke(); // now with cache cleared
    }

    public bool IsFilterPassedCached(ILayer layer)
    {
        if (layer == null) return false;

        if (_filterCache.TryGetValue(layer, out bool cachedResult))
            return cachedResult;

        bool result = IsFilterPassed(layer);

        _filterCache[layer] = result;

        return result;
    }

    private readonly ObservableCollection<DataSourceKindFilterItem> _dataSourceKindFilterItems;
    public ObservableCollection<DataSourceKindFilterItem> DataSourceKindFilterItems => _dataSourceKindFilterItems;

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

    public bool HasActiveFilter =>
        !string.IsNullOrWhiteSpace(_layerNameFilterText) ||
        (SelectedDataSourceKindCount > 0 && SelectedDataSourceKindCount < (_dataSourceKindFilterItems?.Count ?? 0));

    public Func<Task>? RequestRefreshView { get; set; }

    public Action? RequestNotifyFilterChanged { get; set; }

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

    private void DataSourceKindFilterItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_triggerKindChanged)
            return;

        if (e.PropertyName == nameof(DataSourceKindFilterItem.IsSelected))
        {
            RaisePropertyChanged(nameof(SelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(ShowSelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(HasActiveFilter));

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
            RequestNotifyFilterChanged?.Invoke();
        }
    }

    private RelayCommand? _selectAllDataSourceKindsCommand;
    public RelayCommand SelectAllDataSourceKindsCommand =>
        _selectAllDataSourceKindsCommand ??= new RelayCommand(_ =>
        {
            _triggerKindChanged = false;

            foreach (var item in _dataSourceKindFilterItems)
                item.IsSelected = true;

            _triggerKindChanged = true;

            RaisePropertyChanged(nameof(SelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(ShowSelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(HasActiveFilter));

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

            RaisePropertyChanged(nameof(SelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(ShowSelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(HasActiveFilter));

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
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

            RaisePropertyChanged(nameof(SelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(ShowSelectedDataSourceKindCount));
            RaisePropertyChanged(nameof(HasActiveFilter));

            InvalidateFilterCache();
            //RequestRefreshView?.Invoke();
            RequestNotifyFilterChanged?.Invoke();
        });
}
