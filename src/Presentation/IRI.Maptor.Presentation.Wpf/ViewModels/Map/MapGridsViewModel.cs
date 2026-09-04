using System;
using System.Collections.ObjectModel;
using System.Linq;

using IRI.Maptor.Core.Spatial.Helpers.MapGrids;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models.Map;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Map;

/// <summary>
/// The grids on offer and which of them are on the map — the model behind the ribbon's
/// multi-select drop-down.
/// </summary>
/// <remarks>
/// <para>
/// Several grids may be on at once, which is the point of the feature, and the one thing that has
/// to be managed centrally is where each grid's numbers sit. Every grid writes its values against
/// the edges of the view, so a second grid at the same inset would print straight over the first.
/// Tiers are handed out in the order grids are switched on and re-packed when one is switched off,
/// so the rows stay adjacent with no gap.
/// </para>
/// <para>
/// Checked state is <em>derived</em> from whether the layer is still on the map, never cached as a
/// separate flag. A grid's group layer is user-deletable, so it can leave by the legend as well as
/// by this menu, and a cached flag would then be wrong — the menu would claim the grid was on and
/// the next click would remove nothing. That is exactly the bug the MGRS toggle had.
/// </para>
/// </remarks>
public class MapGridsViewModel : Notifier
{
    private readonly MapViewModelBase _map;

    public MapGridsViewModel(MapViewModelBase map)
    {
        _map = map;

        Items = new ObservableCollection<MapGridItemViewModel>(
            MapGridCatalog.CreateDefaults().Select(definition => new MapGridItemViewModel(this, definition)));
    }

    /// <summary>One entry per offered grid, checked when it is on the map.</summary>
    public ObservableCollection<MapGridItemViewModel> Items { get; }

    /// <summary>The grids currently drawn.</summary>
    public int ActiveCount => Items.Count(item => item.Layer is not null);

    #region Persistence

    private const char KeySeparator = ',';

    /// <summary>True while a restore is running, so the restore does not write itself back.</summary>
    private bool _isRestoring;

    /// <summary>
    /// Puts back the grids that were on when the application last closed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called once the map can accept layers — <c>MapInitializationHelper.InitializeMapAsync</c>,
    /// after <c>MapViewer.Register</c> has wired the layer delegates. Doing it in the constructor
    /// instead would depend on this view model being built late enough, which nothing guarantees:
    /// it is created the first time the ribbon binds to it.
    /// </para>
    /// <para>Safe to call twice; the second call finds the grids already on and does nothing.</para>
    /// </remarks>
    public void RestoreFromSettings()
    {
        var stored = _map.GeneralSettings?.MapGrids_SelectedKeys;

        if (string.IsNullOrWhiteSpace(stored))
            return;

        var wanted = stored!
            .Split(new[] { KeySeparator }, StringSplitOptions.RemoveEmptyEntries)
            .Select(key => key.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _isRestoring = true;

        try
        {
            // In catalogue order rather than in the stored order, so the label tiers come back the
            // same way round however the keys were written.
            foreach (var item in Items)
            {
                // A key from a catalogue that has since changed simply finds no item and is dropped.
                if (wanted.Contains(item.Key) && item.Layer is null)
                    Show(item);
            }
        }
        finally
        {
            _isRestoring = false;
        }
    }

    /// <summary>
    /// Writes the checked grids back to the settings. The host saves off the settings'
    /// <c>PropertyChanged</c>, so this is the whole of the save path.
    /// </summary>
    private void Persist()
    {
        if (_isRestoring || _map.GeneralSettings is null)
            return;

        var keys = string.Join(KeySeparator.ToString(), Items.Where(item => item.Layer is not null).Select(item => item.Key));

        if (_map.GeneralSettings.MapGrids_SelectedKeys == keys)
            return;

        _map.GeneralSettings.MapGrids_SelectedKeys = keys;
    }

    #endregion

    internal void Show(MapGridItemViewModel item)
    {
        if (item.Layer is not null)
            return;

        // Assigned before the layer is built so the first render already has it, though the data
        // source reads the definition live and would pick up a later change anyway.
        item.Definition.LabelTier = ActiveCount;

        var layer = MapGridLayers.Create(item.Definition);

        item.SetLayer(layer);

        _map.AddLayer(layer);

        item.NotifyCheckedChanged();

        RaisePropertyChanged(nameof(ActiveCount));

        Persist();
    }

    internal void Hide(MapGridItemViewModel item)
    {
        var layer = item.Layer;

        if (layer is null)
            return;

        // Cleared first, so the removal this triggers finds nothing left to reconcile.
        item.SetLayer(null);

        // ClearLayer rather than RequestRemoveLayer: the latter only drops the layer from the layer
        // manager and leaves what is already drawn on the canvas, so the grid stayed visible after
        // it had been switched off. ClearLayer clears the visuals as well.
        _map.ClearLayer(layer, remove: true, forceRemove: true);

        item.NotifyCheckedChanged();

        RepackTiers();

        RaisePropertyChanged(nameof(ActiveCount));

        Persist();
    }

    /// <summary>
    /// Called when the map's layer collection changes, so a grid deleted from the legend unchecks
    /// itself here.
    /// </summary>
    internal void OnLayersChanged()
    {
        var changed = false;

        foreach (var item in Items)
        {
            if (item.Layer is null || _map.Layers?.Contains(item.Layer) == true)
                continue;

            item.SetLayer(null);

            item.NotifyCheckedChanged();

            changed = true;
        }

        if (!changed)
            return;

        RepackTiers();

        RaisePropertyChanged(nameof(ActiveCount));

        // A grid deleted from the legend is still the user switching it off, so it must not come
        // back on the next start.
        Persist();
    }

    /// <summary>
    /// Closes the gap a removed grid leaves in the tiers, so the remaining rows of numbers sit
    /// against the edge rather than floating one row in.
    /// </summary>
    private void RepackTiers()
    {
        var tier = 0;

        var moved = false;

        foreach (var item in Items)
        {
            if (item.Layer is null)
                continue;

            if (item.Definition.LabelTier != tier)
            {
                item.Definition.LabelTier = tier;

                moved = true;
            }

            tier++;
        }

        // The data sources hold the definitions by reference, so the new tier is already in effect;
        // the map just has to be asked to draw again. Same extent, so nothing is refetched.
        if (moved)
            _map.Refresh(false);
    }
}
