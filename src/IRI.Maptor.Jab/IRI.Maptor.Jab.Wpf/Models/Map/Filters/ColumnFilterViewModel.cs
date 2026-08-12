using System;
using System.Collections.Generic;
using System.ComponentModel;

using IRI.Maptor.Jab.Core;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

using Point = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Wpf.Models.Filters;

/// <summary>
/// Base class for per-column feature-table filters. State is keyed by field name and owned
/// by the view-model layer because the DataGrid columns are regenerated on every
/// DataContext change.
/// </summary>
public abstract class ColumnFilterViewModel : Notifier
{
    protected ColumnFilterViewModel(Field field, Func<IEnumerable<Feature<Point>>> featuresProvider)
    {
        Field = field;

        FeaturesProvider = featuresProvider;
    }

    public Field Field { get; }

    public string FieldName => Field.Name;

    public string Alias => string.IsNullOrWhiteSpace(Field.Alias) ? Field.Name : Field.Alias!;

    protected Func<IEnumerable<Feature<Point>>> FeaturesProvider { get; }

    /// <summary>True when this filter narrows the result set and must be evaluated.</summary>
    public abstract bool IsActive { get; }

    /// <summary>Evaluates the raw attribute value (may be null when the key is missing).</summary>
    public abstract bool Matches(object? rawValue);

    public virtual void Clear()
    {
        RaiseFilterChanged();
    }

    private bool _isPopupOpen;
    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set
        {
            if (_isPopupOpen == value)
                return;

            _isPopupOpen = value;

            RaisePropertyChanged();

            if (value)
                OnPopupOpened();
        }
    }

    protected virtual void OnPopupOpened() { }

    private RelayCommand? _clearCommand;
    public RelayCommand ClearCommand =>
        _clearCommand ??= new RelayCommand(param =>
        {
            Clear();
            IsPopupOpen = false;
        });

    public event Action? FilterChanged;

    protected void RaiseFilterChanged()
    {
        RaisePropertyChanged(nameof(IsActive));

        FilterChanged?.Invoke();
    }

    #region Sorting

    private ListSortDirection? _sortDirection;
    /// <summary>Set by the filter manager (sorting is exclusive across columns).</summary>
    public ListSortDirection? SortDirection
    {
        get => _sortDirection;
        internal set
        {
            if (_sortDirection == value)
                return;

            _sortDirection = value;

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsSortedAscending));
            RaisePropertyChanged(nameof(IsSortedDescending));
        }
    }

    public bool IsSortedAscending => _sortDirection == ListSortDirection.Ascending;

    public bool IsSortedDescending => _sortDirection == ListSortDirection.Descending;

    internal event Action<ColumnFilterViewModel>? SortToggleRequested;

    private RelayCommand? _toggleSortCommand;
    public RelayCommand ToggleSortCommand =>
        _toggleSortCommand ??= new RelayCommand(param => SortToggleRequested?.Invoke(this));

    #endregion

    #region Helpers

    protected static bool IsEmptyValue(object? value)
    {
        if (value is null || value is DBNull)
            return true;

        return value is string s && string.IsNullOrWhiteSpace(s);
    }

    protected static string GetDisplayText(object? value)
    {
        if (IsEmptyValue(value))
            return IRI.Maptor.Jab.Core.Properties.Resources.featureTable_filter_empty;

        return Convert.ToString(value) ?? string.Empty;
    }

    #endregion
}
