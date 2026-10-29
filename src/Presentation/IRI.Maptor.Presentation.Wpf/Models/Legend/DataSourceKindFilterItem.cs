using System;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Presentation.Wpf.Models.Legend;

public class DataSourceKindFilterItem : Notifier
{
    private bool _isSelected;

    public DataSourceKind Kind { get; }

    /// <summary>Vector / Raster / Service / None; the filter popup groups its rows by it.</summary>
    public DataSourceCategory Category { get; }

    public string DisplayName => Kind.GetDescription();

    private int? _layerCount;
    /// <summary>
    /// Layers of this kind in the legend's TOC group (group layers are counted by their leaves).
    /// Null until the owner reports counts, so a legend whose host never does keeps every kind available.
    /// </summary>
    public int? LayerCount
    {
        get => _layerCount;
        set
        {
            if (_layerCount == value) return;
            _layerCount = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsAvailable));
        }
    }

    /// <summary>False when the map has no layer of this kind: filtering it would change nothing.</summary>
    public bool IsAvailable => _layerCount is null || _layerCount > 0;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            RaisePropertyChanged();
        }
    }

    public DataSourceKindFilterItem(DataSourceKind kind, bool isSelected = true)
    {
        Kind = kind;
        Category = kind.GetCategory();
        _isSelected = isSelected;
    }

    public override string ToString() => $"{Kind}, IsSelected: {IsSelected}";
}
