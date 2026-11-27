using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;

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
/// <para>
/// A project file keeps the unchecked layers by name (<see cref="UncheckedLayerNames"/>,
/// <see cref="RestoreUncheckedLayerNames"/>), as layer ids change every session. A restored name stays in force for
/// layers that appear later (server layers load after the project), and applies to every layer of that name.
/// </para>
/// </summary>
public sealed class SnapLayerChoice : Notifier
{
    // unchecked in this session, by layer id (with the name, for saving)
    private readonly Dictionary<Guid, string> _unchecked = new();

    // unchecked by name, from a project file
    private readonly HashSet<string> _uncheckedNames = new();

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

    private bool _isDrawingsChecked = true;
    /// <summary>
    /// Whether the user's drawings (the drawing items on the map, text labels apart) are snapped to: one entry for
    /// all of them, at the top of the layer list. On by default; kept in the project file.
    /// </summary>
    public bool IsDrawingsChecked
    {
        get => _isDrawingsChecked;
        set
        {
            if (_isDrawingsChecked == value)
                return;

            _isDrawingsChecked = value;
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

    /// <summary>The layers to snap to.</summary>
    public IReadOnlyList<VectorLayer> CheckedLayers => Layers.Where(i => i.IsChecked).Select(i => i.Layer).ToList();

    /// <summary>Shows <paramref name="layers"/> in the list, keeping each layer's checked state; no change when they are the listed ones.</summary>
    public void Update(IEnumerable<VectorLayer> layers)
    {
        var wanted = layers.Distinct().ToList();

        if (wanted.SequenceEqual(Layers.Select(i => i.Layer)))
            return;

        Layers.Clear();

        foreach (var layer in wanted)
            Layers.Add(new SnapLayerItem(layer, !IsUnchecked(layer), () => OnItemChanged(layer)));

        RaisePropertyChanged(nameof(HasNoLayers));
        Changed?.Invoke();
    }

    /// <summary>The names of the unchecked layers, for a project file: this session's and those restored but not seen yet.</summary>
    public IReadOnlyList<string> UncheckedLayerNames =>
        _unchecked.Values.Concat(_uncheckedNames).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();

    /// <summary>Replaces the unchecked layers with those of a project file (by name); every other layer is checked.</summary>
    public void RestoreUncheckedLayerNames(IEnumerable<string>? names)
    {
        _unchecked.Clear();
        _uncheckedNames.Clear();

        foreach (var name in names ?? Enumerable.Empty<string>())
        {
            if (!string.IsNullOrEmpty(name))
                _uncheckedNames.Add(name);
        }

        // rebuild the items so their check boxes show the restored state
        var listed = Layers.Select(i => i.Layer).ToList();

        Layers.Clear();

        Update(listed);

        Changed?.Invoke();
    }

    private bool IsUnchecked(VectorLayer layer)
    {
        if (_unchecked.ContainsKey(layer.LayerId))
            return true;

        if (!_uncheckedNames.Contains(layer.LayerName))
            return false;

        _unchecked[layer.LayerId] = layer.LayerName;

        return true;
    }

    private void OnItemChanged(VectorLayer layer)
    {
        var item = Layers.FirstOrDefault(i => i.Layer == layer);

        if (item is null)
            return;

        if (item.IsChecked)
        {
            _unchecked.Remove(layer.LayerId);
            _uncheckedNames.Remove(layer.LayerName);
        }
        else
        {
            _unchecked[layer.LayerId] = layer.LayerName;
        }

        Changed?.Invoke();
    }
}
