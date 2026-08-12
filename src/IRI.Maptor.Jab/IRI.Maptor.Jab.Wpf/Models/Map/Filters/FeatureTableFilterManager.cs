using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Wpf.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

using Point = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Wpf.Models.Filters;

/// <summary>
/// Owns the per-column filters and the sort state of a feature table. Filter/sort state
/// survives DataGrid column regeneration because it lives here, keyed by field name.
/// </summary>
public class FeatureTableFilterManager : Notifier
{
    private List<ColumnFilterViewModel> _activeFilters = new();

    public FeatureTableFilterManager(IEnumerable<Field>? fields, Func<IEnumerable<Feature<Point>>> featuresProvider)
    {
        ColumnFilters = new List<ColumnFilterViewModel>();

        if (fields is null)
            return;

        foreach (var field in fields)
        {
            if (!IsFilterableField(field))
                continue;

            var filter = CreateFor(field, featuresProvider);

            filter.FilterChanged += OnChildFilterChanged;

            filter.SortToggleRequested += ToggleSort;

            ColumnFilters.Add(filter);
        }
    }

    public List<ColumnFilterViewModel> ColumnFilters { get; }

    public ColumnFilterViewModel? GetFilter(string fieldName) =>
        ColumnFilters.FirstOrDefault(f => string.Equals(f.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

    public bool HasActiveFilter => ColumnFilters.Any(f => f.IsActive);

    public event Action? FilterChanged;

    private RelayCommand? _clearAllFiltersCommand;
    public RelayCommand ClearAllFiltersCommand =>
        _clearAllFiltersCommand ??= new RelayCommand(param => ClearAll());

    public void ClearAll(bool raiseEvent = true)
    {
        foreach (var filter in ColumnFilters)
        {
            filter.FilterChanged -= OnChildFilterChanged;

            try
            {
                filter.Clear();
            }
            finally
            {
                filter.FilterChanged += OnChildFilterChanged;
            }
        }

        _activeFilters = new List<ColumnFilterViewModel>();

        RaisePropertyChanged(nameof(HasActiveFilter));

        if (raiseEvent)
            FilterChanged?.Invoke();
    }

    private void OnChildFilterChanged()
    {
        _activeFilters = ColumnFilters.Where(f => f.IsActive).ToList();

        RaisePropertyChanged(nameof(HasActiveFilter));

        FilterChanged?.Invoke();
    }

    public bool Matches(Feature<Point> feature)
    {
        var filters = _activeFilters;

        foreach (var filter in filters)
        {
            feature.Attributes.TryGetValue(filter.FieldName, out var raw);

            if (!filter.Matches(raw))
                return false;
        }

        return true;
    }

    #region Sorting

    public string? SortFieldName { get; private set; }

    public ListSortDirection? SortDirection { get; private set; }

    public event Action? SortChanged;

    /// <summary>Resets the sort without raising SortChanged; caller refreshes the view.</summary>
    public void ClearSort()
    {
        SortFieldName = null;

        SortDirection = null;

        foreach (var filter in ColumnFilters)
            filter.SortDirection = null;

        RaisePropertyChanged(nameof(SortFieldName));
        RaisePropertyChanged(nameof(SortDirection));
    }

    /// <summary>Cycles the given column asc → desc → none; exclusive across columns.</summary>
    public void ToggleSort(ColumnFilterViewModel column)
    {
        var next = !string.Equals(SortFieldName, column.FieldName, StringComparison.OrdinalIgnoreCase)
            ? ListSortDirection.Ascending
            : SortDirection switch
            {
                ListSortDirection.Ascending => ListSortDirection.Descending,
                _ => (ListSortDirection?)null,
            };

        SortFieldName = next is null ? null : column.FieldName;

        SortDirection = next;

        foreach (var filter in ColumnFilters)
            filter.SortDirection = ReferenceEquals(filter, column) ? next : null;

        RaisePropertyChanged(nameof(SortFieldName));
        RaisePropertyChanged(nameof(SortDirection));

        SortChanged?.Invoke();
    }

    public IEnumerable<Feature<Point>> ApplySort(IEnumerable<Feature<Point>> features)
    {
        if (SortFieldName is null || SortDirection is null)
            return features;

        var column = GetFilter(SortFieldName);

        var fieldName = SortFieldName;

        // type-aware sort key: nulls first, then the typed value (LINQ OrderBy is stable)
        IComparable? KeySelector(Feature<Point> feature)
        {
            feature.Attributes.TryGetValue(fieldName, out var raw);

            if (raw is null || raw is DBNull)
                return null;

            switch (column)
            {
                case NumericColumnFilter:
                    return NumericColumnFilter.TryToDouble(raw, out var number) ? number : (IComparable?)null;

                case DateColumnFilter:
                    return DateColumnFilter.TryGetDateTime(raw, out var date) ? date : (IComparable?)null;

                default:
                    if (raw is bool b)
                        return b;

                    return Convert.ToString(raw) ?? string.Empty;
            }
        }

        var comparer = new NullsFirstComparer();

        return SortDirection == ListSortDirection.Ascending
            ? features.OrderBy(KeySelector, comparer)
            : features.OrderByDescending(KeySelector, comparer);
    }

    private sealed class NullsFirstComparer : IComparer<IComparable?>
    {
        public int Compare(IComparable? x, IComparable? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            if (x is string sx && y is string sy)
                return string.Compare(sx, sy, StringComparison.CurrentCulture);

            return x.CompareTo(y);
        }
    }

    #endregion

    #region Factory

    // mirrors the skip rules of DataGridDictionaryBehavior so filters and columns stay one-to-one
    private static bool IsFilterableField(Field field)
    {
        if (field is null || string.IsNullOrEmpty(field.Name))
            return false;

        if (field.TypeFullName?.ContainsIgnoreCase(FeatureTableHelper.NetTopologySuiteColumnName) == true)
            return false;

        if (field.Name.EqualsIgnoreCase("rowversion"))
            return false;

        return field.CanRead;
    }

    // mirrors the column-type dispatch of DataGridDictionaryBehavior, in the same order
    private static ColumnFilterViewModel CreateFor(Field field, Func<IEnumerable<Feature<Point>>> featuresProvider)
    {
        if (field.AllowedValues != null && field.AllowedValues.Length > 0)
        {
            var values = field.AllowedValues.Cast<object?>().ToList();

            if (field.IsNullable)
                values.Insert(0, null);

            return new DistinctValuesColumnFilter(field, featuresProvider, DistinctValuesSeedMode.Fixed, values);
        }

        var fieldType = field.TypeFullName is null ? null : Type.GetType(field.TypeFullName);

        if (fieldType is null)
            return new DistinctValuesColumnFilter(field, featuresProvider, DistinctValuesSeedMode.Dynamic);

        if (fieldType.IsBool())
        {
            var values = new List<object?> { true, false };

            if (field.IsNullable)
                values.Insert(0, null);

            return new DistinctValuesColumnFilter(field, featuresProvider, DistinctValuesSeedMode.Fixed, values, isSearchVisible: false);
        }

        if (fieldType.IsDateTime())
            return new DateColumnFilter(field, featuresProvider);

        if (fieldType.IsNumeric())
            return new NumericColumnFilter(field, featuresProvider);

        return new DistinctValuesColumnFilter(field, featuresProvider, DistinctValuesSeedMode.Dynamic);
    }

    #endregion
}
