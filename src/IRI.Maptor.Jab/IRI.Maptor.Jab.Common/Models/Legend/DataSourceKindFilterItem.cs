using System;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.Models.Legend;

public class DataSourceKindFilterItem : Notifier
{
    private bool _isSelected;

    public DataSourceKind Kind { get; }

    public string DisplayName => Kind.GetDescription();

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
        _isSelected = isSelected;
    }

    public override string ToString() => $"{Kind}, IsSelected: {IsSelected}";
}
