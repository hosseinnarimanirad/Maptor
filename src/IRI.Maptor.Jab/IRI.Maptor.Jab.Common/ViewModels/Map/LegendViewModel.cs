using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public class LegendViewModel : Notifier
{
    private string _layerNameFilterText = string.Empty;

    private bool _triggerKindChanged = true;

    public LegendViewModel()
    {
        _dataSourceKindFilterItems = new ObservableCollection<DataSourceKindFilterItem>(
            Enum.GetValues(typeof(DataSourceKind))
                .Cast<DataSourceKind>()
                .Select(k => new DataSourceKindFilterItem(k, isSelected: true)));

        foreach (var item in _dataSourceKindFilterItems)
            item.PropertyChanged += DataSourceKindFilterItem_PropertyChanged;
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

            RequestRefresh?.Invoke();
        }
    }

    public int SelectedDataSourceKindCount => _dataSourceKindFilterItems?.Count(i => i.IsSelected) ?? 0;

    public bool ShowSelectedDataSourceKindCount => SelectedDataSourceKindCount != (DataSourceKindFilterItems?.Count ?? 0);

    public bool HasActiveFilter =>
        !string.IsNullOrWhiteSpace(_layerNameFilterText) ||
        (SelectedDataSourceKindCount > 0 && SelectedDataSourceKindCount < (_dataSourceKindFilterItems?.Count ?? 0));

    public Action? RequestRefresh { get; set; }

    public List<DataSourceKind> GetAllowedDataSourceKinds()
    {
        var selected = _dataSourceKindFilterItems?.Where(i => i.IsSelected).Select(i => i.Kind).ToList() ?? [];

        return selected;
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

            RequestRefresh?.Invoke();
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

            RequestRefresh?.Invoke();
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

            RequestRefresh?.Invoke();
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

            RequestRefresh?.Invoke();
        });
}
