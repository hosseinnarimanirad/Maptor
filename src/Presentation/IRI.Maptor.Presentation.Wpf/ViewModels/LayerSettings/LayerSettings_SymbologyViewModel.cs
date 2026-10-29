using System;
using System.Linq;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Cartography.Legend;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;

/// <summary>
/// The Symbology tab of the layer settings dialog: the layer's styles (named symbologies the admin defined,
/// e.g. pipes by size / by material), switching between them, the active style's legend, and the user's own
/// changes on top of it (quick edit, advanced SLD editor, reset). Styles are created on the server only.
/// </summary>
public class LayerSettings_SymbologyViewModel : Notifier
{
    private readonly SymbolizableLayer? _layer;

    private bool _isSyncing;

    public LayerSettings_SymbologyViewModel(SymbolizableLayer? layer)
    {
        _layer = layer;

        QuickEditCommand = new RelayCommand(_ => RequestQuickEdit?.Invoke(), _ => _layer is not null && RequestQuickEdit is not null);
        AdvancedEditCommand = new RelayCommand(_ => RequestAdvancedEdit?.Invoke(), _ => _layer is not null && RequestAdvancedEdit is not null);
        ResetCommand = new RelayCommand(_ => Reset(), _ => _layer is { CanResetSymbology: true, IsSymbologyUserModified: true });

        if (_layer is not null)
        {
            // weak: the dialog's view model must not keep a long-lived layer's handlers alive
            PropertyChangedEventManager.AddHandler(_layer, OnLayerPropertyChanged, string.Empty);
        }

        Sync();
    }

    /// <summary>False for a layer that cannot be styled (raster, tiles): the tab says so.</summary>
    public bool IsAvailable => _layer is not null;

    public ObservableCollection<LayerStyleItem> Styles { get; } = new();

    /// <summary>The style list shows only when there is a choice; otherwise the legend takes the whole width.</summary>
    public bool HasMultipleStyles => Styles.Count > 1;

    /// <summary>A layer drawn with the simple colours (no SLD): only Quick edit applies.</summary>
    public bool IsSimple => _layer is { SourceSld: null };

    /// <summary>"Legend: By size", or just "Legend" for a layer whose style has no title.</summary>
    public string LegendTitle => SelectedStyle is { } style && (HasMultipleStyles || !string.IsNullOrWhiteSpace(style.Title))
        ? string.Format(Localize("layerStyles_previewOf"), style.DisplayTitle)
        : Localize("layerStyles_tab_legend");

    /// <summary>What the "My changes" card says: none, which styles the user changed, or just "changed".</summary>
    public string ChangesText
    {
        get
        {
            if (!IsUserModified)
                return Localize("layerStyles_tab_noChanges");

            var changed = Styles.Where(s => s.IsUserChanged).Select(s => s.DisplayTitle).ToList();

            return HasMultipleStyles && changed.Count > 0
                ? string.Format(Localize("layerStyles_tab_changedStyles"), string.Join(" · ", changed))
                : Localize("layerStyles_tab_hasChanges");
        }
    }

    private LayerStyleItem? _selectedStyle;

    /// <summary>The active style; choosing another one switches the layer (and the map) to it.</summary>
    public LayerStyleItem? SelectedStyle
    {
        get => _selectedStyle;
        set
        {
            if (ReferenceEquals(_selectedStyle, value))
                return;

            _selectedStyle = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(LegendTitle));

            if (!_isSyncing && value?.Name is { } name)
                _layer?.SetActiveStyle(name);
        }
    }

    /// <summary>The rules of the active style, as the map legend's details panel shows them.</summary>
    public IReadOnlyList<LegendRuleRow> LegendRows { get; private set; } = [];

    public bool HasLegendRows => LegendRows.Count > 0;

    /// <summary>
    /// The simple colour dialog is offered only for a layer without an SLD: an SLD's rules and styles do
    /// not fit it (opening it on such a layer goes to the SLD editor instead).
    /// </summary>
    public bool CanQuickEdit => _layer is { SourceSld: null };

    /// <summary>The user changed the symbology on this computer (kept in the project); Reset undoes it.</summary>
    public bool IsUserModified => _layer?.IsSymbologyUserModified == true;

    /// <summary>The simple colour dialog (fill, stroke, width).</summary>
    public ICommand QuickEditCommand { get; }

    /// <summary>The SLD editor on the active style.</summary>
    public ICommand AdvancedEditCommand { get; }

    /// <summary>Back to the server's styles (all of them); the active style stays when it still exists.</summary>
    public ICommand ResetCommand { get; }

    public ICommand? ExportLegendCommand => _layer?.ExportSymbologyLegendCommand;

    public Action? RequestQuickEdit { get; set; }

    public Action? RequestAdvancedEdit { get; set; }

    private void Reset()
    {
        if (_layer?.ResetSymbologyToDefault() == true)
            _layer.RequestRepaint?.Invoke(_layer);
    }

    private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SymbolizableLayer.ActiveStyleName) or nameof(SymbolizableLayer.Styles)
            or nameof(SymbolizableLayer.SymbologyLegend) or nameof(SymbolizableLayer.IsSymbologyUserModified))
        {
            Sync();
        }
    }

    // re-reads styles, the active one and its legend from the layer (after a switch, an edit or a reset)
    private void Sync()
    {
        _isSyncing = true;

        try
        {
            var items = LayerStyleItem.FromSld(_layer?.SourceSld, _layer?.ActiveStyleName);

            var unchanged = items.Count == Styles.Count
                && items.Zip(Styles, (a, b) => a.Name == b.Name && a.Title == b.Title && a.RuleCount == b.RuleCount).All(same => same);

            if (!unchanged)
            {
                Styles.Clear();

                foreach (var item in items)
                    Styles.Add(item);
            }
            else
            {
                for (var i = 0; i < items.Count; i++)
                    Styles[i].IsActive = items[i].IsActive;
            }

            foreach (var item in Styles)
                item.IsUserChanged = IsUserModified && IsStyleChanged(item.Name);

            SelectedStyle = Styles.FirstOrDefault(s => s.IsActive);

            LegendRows = _layer?.SymbologyLegend.Groups.SelectMany(g => g.Rows).ToList() ?? [];
        }
        finally
        {
            _isSyncing = false;
        }

        RaisePropertyChanged(nameof(HasMultipleStyles));
        RaisePropertyChanged(nameof(LegendRows));
        RaisePropertyChanged(nameof(HasLegendRows));
        RaisePropertyChanged(nameof(IsUserModified));
        RaisePropertyChanged(nameof(CanQuickEdit));
        RaisePropertyChanged(nameof(IsSimple));
        RaisePropertyChanged(nameof(LegendTitle));
        RaisePropertyChanged(nameof(ChangesText));

        // an edit or a reset can come from outside the dialog (legend, SLD editor) without any input here
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    // a style differs from the server's when the server has no such style or its SLD reads differently
    private bool IsStyleChanged(string? styleName)
    {
        var current = _layer?.SourceSld;
        var original = _layer?.DefaultSld;

        if (current is null || original is null)
            return false;

        if (styleName is not null && !original.HasStyle(styleName))
            return true;

        return !string.Equals(Serialize(current.ExtractStyle(styleName)), Serialize(original.ExtractStyle(styleName)), StringComparison.Ordinal);
    }

    private static string? Serialize(StyledLayerDescriptor sld) => SldHelper.Serialize(sld, indented: false);

    private static string Localize(string key) => LocalizationManager.Instance[key];
}
