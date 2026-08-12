using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

using IRI.Maptor.Jab.Core;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

using Point = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Wpf.Models.Filters;

public class DistinctValueItem : Notifier
{
    private readonly Action _selectionChanged;

    public DistinctValueItem(object? value, string displayText, Action selectionChanged)
    {
        Value = value;

        DisplayText = displayText;

        _selectionChanged = selectionChanged;
    }

    /// <summary>null represents the empty-value bucket.</summary>
    public object? Value { get; }

    public string DisplayText { get; }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;

            RaisePropertyChanged();

            _selectionChanged();
        }
    }

    internal void SetSelectedSilently(bool value)
    {
        _isSelected = value;

        RaisePropertyChanged(nameof(IsSelected));
    }
}

public enum DistinctValuesSeedMode
{
    /// <summary>Distinct values computed from the data, lazily when the popup opens.</summary>
    Dynamic,

    /// <summary>Fixed list (AllowedValues fields, booleans).</summary>
    Fixed,
}

/// <summary>
/// Checklist filter over the distinct values of a column. Used for string columns,
/// AllowedValues columns, booleans and any column whose type cannot be resolved.
/// </summary>
public class DistinctValuesColumnFilter : ColumnFilterViewModel
{
    private readonly DistinctValuesSeedMode _seedMode;

    private HashSet<string>? _selectedKeys;   // null => everything selected (inactive)

    private bool _suppressItemEvents;

    public DistinctValuesColumnFilter(
        Field field,
        Func<IEnumerable<Feature<Point>>> featuresProvider,
        DistinctValuesSeedMode seedMode,
        IEnumerable<object?>? fixedValues = null,
        bool isSearchVisible = true)
        : base(field, featuresProvider)
    {
        _seedMode = seedMode;

        IsSearchVisible = isSearchVisible;

        Items = new ObservableCollection<DistinctValueItem>();

        ItemsView = CollectionViewSource.GetDefaultView(Items);

        ItemsView.Filter = item => PassesSearch((DistinctValueItem)item);

        if (seedMode == DistinctValuesSeedMode.Fixed && fixedValues != null)
        {
            foreach (var value in fixedValues)
                Items.Add(CreateItem(value));
        }
    }

    public ObservableCollection<DistinctValueItem> Items { get; }

    public ICollectionView ItemsView { get; }

    public bool IsSearchVisible { get; }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value ?? string.Empty;

            RaisePropertyChanged();

            ItemsView.Refresh();
        }
    }

    public override bool IsActive => _selectedKeys != null;

    public override bool Matches(object? rawValue)
    {
        if (_selectedKeys is null)
            return true;

        return _selectedKeys.Contains(GetKey(rawValue));
    }

    public override void Clear()
    {
        _suppressItemEvents = true;

        try
        {
            foreach (var item in Items)
                item.SetSelectedSilently(true);
        }
        finally
        {
            _suppressItemEvents = false;
        }

        _selectedKeys = null;

        SearchText = string.Empty;

        RaiseFilterChanged();
    }

    private RelayCommand? _selectAllCommand;
    public RelayCommand SelectAllCommand =>
        _selectAllCommand ??= new RelayCommand(param => SetAll(true));

    private RelayCommand? _clearAllCommand;
    public RelayCommand ClearAllCommand =>
        _clearAllCommand ??= new RelayCommand(param => SetAll(false));

    protected override void OnPopupOpened()
    {
        if (_seedMode == DistinctValuesSeedMode.Dynamic)
            RecomputeDistinctValues();
    }

    private void SetAll(bool selected)
    {
        _suppressItemEvents = true;

        try
        {
            foreach (var item in Items)
                item.SetSelectedSilently(selected);
        }
        finally
        {
            _suppressItemEvents = false;
        }

        OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        if (_suppressItemEvents)
            return;

        _selectedKeys = Items.All(i => i.IsSelected)
            ? null
            : new HashSet<string>(Items.Where(i => i.IsSelected).Select(i => GetKey(i.Value)), StringComparer.Ordinal);

        RaiseFilterChanged();
    }

    private void RecomputeDistinctValues()
    {
        // preserve what the user unchecked; new values default to checked so
        // edits never get silently filtered out
        var uncheckedKeys = new HashSet<string>(
            Items.Where(i => !i.IsSelected).Select(i => GetKey(i.Value)),
            StringComparer.Ordinal);

        var seen = new Dictionary<string, object?>(StringComparer.Ordinal);

        var hasEmpty = false;

        foreach (var feature in FeaturesProvider())
        {
            feature.Attributes.TryGetValue(FieldName, out var raw);

            if (IsEmptyValue(raw))
            {
                hasEmpty = true;
                continue;
            }

            var key = GetKey(raw);

            if (!seen.ContainsKey(key))
                seen[key] = raw;
        }

        _suppressItemEvents = true;

        try
        {
            Items.Clear();

            if (hasEmpty)
            {
                var emptyItem = CreateItem(null);
                emptyItem.SetSelectedSilently(!uncheckedKeys.Contains(GetKey(null)));
                Items.Add(emptyItem);
            }

            foreach (var pair in seen.OrderBy(p => GetDisplayText(p.Value), StringComparer.CurrentCulture))
            {
                var item = CreateItem(pair.Value);
                item.SetSelectedSilently(!uncheckedKeys.Contains(pair.Key));
                Items.Add(item);
            }
        }
        finally
        {
            _suppressItemEvents = false;
        }

        // re-derive the key set without raising: merely opening the popup must not
        // trigger a refresh; only raise when the effective filter really changed
        var newKeys = Items.All(i => i.IsSelected)
            ? null
            : new HashSet<string>(Items.Where(i => i.IsSelected).Select(i => GetKey(i.Value)), StringComparer.Ordinal);

        var changed = (_selectedKeys is null) != (newKeys is null) ||
                      (_selectedKeys != null && newKeys != null && !_selectedKeys.SetEquals(newKeys));

        _selectedKeys = newKeys;

        if (changed)
            RaiseFilterChanged();
    }

    private DistinctValueItem CreateItem(object? value) =>
        new DistinctValueItem(value, GetDisplayText(value), OnSelectionChanged);

    private static string GetKey(object? value) =>
        IsEmptyValue(value) ? string.Empty : Convert.ToString(value) ?? string.Empty;

    private bool PassesSearch(DistinctValueItem item)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        return item.DisplayText.IndexOf(_searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
