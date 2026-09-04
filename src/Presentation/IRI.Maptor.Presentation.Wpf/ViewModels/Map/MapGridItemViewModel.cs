using IRI.Maptor.Core.Spatial.Helpers.MapGrids;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Map;

/// <summary>
/// One entry of the grids drop-down: a grid on offer, and whether it is on the map.
/// </summary>
/// <remarks>
/// <see cref="IsChecked"/> reads through to the layer rather than storing a flag of its own, so
/// there is exactly one truth about whether a grid is drawn — the layer's presence on the map.
/// The setter delegates to <see cref="MapGridsViewModel"/> because switching a grid on is not a
/// private act: it also decides where this grid's numbers sit relative to the others.
/// </remarks>
public class MapGridItemViewModel : Notifier
{
    private readonly MapGridsViewModel _owner;

    internal MapGridItemViewModel(MapGridsViewModel owner, MapGridDefinition definition)
    {
        _owner = owner;
        Definition = definition;
    }

    /// <summary>The grid this entry offers. Mutable in place — the live layers read it.</summary>
    public MapGridDefinition Definition { get; }

    /// <summary>Stable identifier, for settings.</summary>
    public string Key => Definition.Key;

    /// <summary>The menu text.</summary>
    public string Title => Definition.Title;

    /// <summary>The layer while the grid is drawn; null when it is not.</summary>
    internal VectorLayer? Layer { get; private set; }

    public bool IsChecked
    {
        get => Layer is not null;
        set
        {
            if (value == IsChecked)
                return;

            if (value)
                _owner.Show(this);
            else
                _owner.Hide(this);
        }
    }

    internal void SetLayer(VectorLayer? layer) => Layer = layer;

    internal void NotifyCheckedChanged() => RaisePropertyChanged(nameof(IsChecked));

    public override string ToString() => $"{Key}{(IsChecked ? " (on)" : string.Empty)}";
}
