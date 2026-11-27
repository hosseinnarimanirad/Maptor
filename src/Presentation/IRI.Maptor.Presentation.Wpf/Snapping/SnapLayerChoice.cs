using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Topology;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>One layer in the snapping list of the sketch bar.</summary>
public sealed class SnapLayerItem : Notifier
{
    private readonly Action _onChanged;

    internal SnapLayerItem(VectorLayer layer, bool isChecked, Action onChanged)
    {
        Layer = layer;
        _isChecked = isChecked;
        _onChanged = onChanged;
    }

    public VectorLayer Layer { get; }

    public string Name => Layer.LayerName;

    /// <summary>False for a Live layer: it holds only part of its features, so it cannot be snapped to yet.</summary>
    public bool IsAvailable => !MapLayerFeatureSource.IsLiveLayer(Layer);

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
                return;

            _isChecked = value;
            RaisePropertyChanged();
            _onChanged();
        }
    }
}

/// <summary>
/// Whether snapping is on and which layers it uses: the state behind the snapping toggle and its layer list. One per
/// map, so the choice holds across drawing, editing and measuring. It does not load features or snap.
/// <para>
/// Snapping is on by default and every layer starts checked; a layer the user unchecks stays unchecked for the
/// session, also when it leaves the list (turned off, out of scale range) and comes back.
/// </para>
/// </summary>
public sealed class SnapLayerChoice : Notifier
{
    private readonly HashSet<Guid> _unchecked = new();

    /// <summary>Raised when snapping is turned on or off or a layer is checked or unchecked.</summary>
    public event Action? Changed;

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;

            _isEnabled = value;
            RaisePropertyChanged();
            Changed?.Invoke();
        }
    }

    public ObservableCollection<SnapLayerItem> Layers { get; } = new();

    public bool HasNoLayers => Layers.Count == 0;

    private bool _isOverLimit;
    /// <summary>True when the checked layers hold too many vertices in view to snap to; zooming in helps.</summary>
    public bool IsOverLimit
    {
        get => _isOverLimit;
        set
        {
            if (_isOverLimit == value)
                return;

            _isOverLimit = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>The layers to snap to: checked and available.</summary>
    public IReadOnlyList<VectorLayer> CheckedLayers => Layers.Where(i => i.IsChecked && i.IsAvailable).Select(i => i.Layer).ToList();

    /// <summary>Shows <paramref name="layers"/> in the list, keeping each layer's checked state; no change when they are the listed ones.</summary>
    public void Update(IEnumerable<VectorLayer> layers)
    {
        var wanted = layers.Distinct().ToList();

        if (wanted.SequenceEqual(Layers.Select(i => i.Layer)))
            return;

        Layers.Clear();

        foreach (var layer in wanted)
            Layers.Add(new SnapLayerItem(layer, !_unchecked.Contains(layer.LayerId), () => OnItemChanged(layer)));

        RaisePropertyChanged(nameof(HasNoLayers));
        Changed?.Invoke();
    }

    private void OnItemChanged(VectorLayer layer)
    {
        var item = Layers.FirstOrDefault(i => i.Layer == layer);

        if (item is null)
            return;

        if (item.IsChecked)
            _unchecked.Remove(layer.LayerId);
        else
            _unchecked.Add(layer.LayerId);

        Changed?.Invoke();
    }
}
