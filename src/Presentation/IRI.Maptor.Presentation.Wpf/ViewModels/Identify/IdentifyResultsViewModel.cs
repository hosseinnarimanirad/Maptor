using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models.Identify;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Identify;

/// <summary>
/// Master–detail state of the identify results view: a tree of layer → feature nodes on
/// the master side, the selected node's attributes on the detail side.
/// One instance lives as long as the (modeless) results window; every new identify click
/// calls <see cref="Update"/> on it, so the window refreshes in place instead of stacking.
/// Map coupling (highlight / zoom) goes through the <c>Request*</c> delegates, wired
/// by the host exactly like <c>FeatureChangesViewModel</c>.
/// </summary>
public class IdentifyResultsViewModel : Notifier
{
    private static LocalizationManager Localization => LocalizationManager.Instance;

    // the drawing legend's own header, so the group reads exactly like the legend section a
    // selected drawing is shown in
    private const string DrawingsGroupTitleKey = "ui_header_drawingLegend";

    private bool _isSyncingSelection;

    public IdentifyResultsViewModel()
    {
        ZoomToCommand = new RelayCommand(_ => ZoomToSelection(), _ => SelectedNode is not null);

        CopyAttributesCommand = new RelayCommand(_ => CopySelectedAttributes(), _ => SelectedFeature is not null);

        ShowInAttributeTableCommand = new RelayCommand(async _ => await ShowSelectionInAttributeTableAsync(), _ => CanShowInAttributeTable);

        ClearFilterCommand = new RelayCommand(_ => FilterText = string.Empty, _ => !string.IsNullOrEmpty(FilterText));

        Localization.LanguageChanged += OnLanguageChanged;
    }

    #region Map callbacks (wired by the host)

    /// <summary>Zoom the map so all given features are visible.</summary>
    public Action<IReadOnlyList<Feature<Point>>>? RequestZoomTo { get; set; }

    /// <summary>Draw the highlight overlay for the given features; second argument is the layer's stroke thickness.</summary>
    public Func<IReadOnlyList<Feature<Point>>, double?, Task>? RequestHighlightAsync { get; set; }

    /// <summary>Remove the highlight overlay.</summary>
    public Action? RequestClearHighlight { get; set; }

    /// <summary>
    /// Select this drawing item in the drawing legend (its own highlight replaces the overlay),
    /// or, with null, release the legend selection an earlier call made.
    /// </summary>
    public Action<DrawingItemLayer?>? RequestSelectDrawing { get; set; }

    /// <summary>Show these features of the layer in its attribute-table tab (added if missing, highlighted, scrolled to).</summary>
    public Func<VectorLayer, IReadOnlyList<Feature<Point>>, Task>? RequestShowInAttributeTable { get; set; }

    #endregion

    #region Results

    public ObservableCollection<IdentifyLayerNode> Layers { get; } = new ObservableCollection<IdentifyLayerNode>();

    /// <summary>Clicked location in Web Mercator; null before the first identify.</summary>
    public Point? Location { get; private set; }

    /// <summary>"lat, lon" in WGS84, Latin digits, 6 decimals; empty before the first identify.</summary>
    public string LocationText
    {
        get
        {
            if (Location is null)
                return string.Empty;

            var wgs84 = MapProjects.WebMercatorToGeodeticWgs84(Location);

            return string.Format(CultureInfo.InvariantCulture, "{0:F6}, {1:F6}", wgs84.Y, wgs84.X);
        }
    }

    public int LayerCount => Layers.Count;

    public int TotalFeatureCount => Layers.Sum(l => l.Count);

    public bool HasResults => TotalFeatureCount > 0;

    /// <summary>True while a filter hides every feature (distinct from "nothing was found").</summary>
    public bool HasVisibleResults => Layers.Any(l => l.HasVisibleFeatures);

    /// <summary>"N features in M layers" or the no-results message, localized.</summary>
    public string SummaryText
    {
        get
        {
            if (!HasResults)
                return Localization["identify_noResults"];

            return string.Format(
                Localization["identify_summary"],
                LocalizationManager.GetLocalizedNumberString(TotalFeatureCount),
                LocalizationManager.GetLocalizedNumberString(LayerCount));
        }
    }

    /// <summary>
    /// Replace the results. Called on first open and on every later identify click.
    /// Pairs whose layer is null or whose set holds no feature are skipped. Drawing items under
    /// the click, if any, go into one drawings group after every data layer.
    /// </summary>
    public void Update(
        Point? location,
        IEnumerable<(VectorLayer Layer, FeatureSet<Point> Hits)>? results,
        IEnumerable<DrawingItemLayer>? drawings = null)
    {
        DetachNodes();

        Layers.Clear();

        if (results is not null)
        {
            foreach (var (layer, hits) in results)
            {
                if (layer is null || hits?.Features is null || hits.Features.Count == 0)
                    continue;

                var node = new IdentifyLayerNode(layer, hits);

                AttachNode(node);

                Layers.Add(node);
            }
        }

        if (drawings is not null)
        {
            var group = new IdentifyLayerNode(Localization[DrawingsGroupTitleKey], drawings);

            if (group.Count > 0)
            {
                AttachNode(group);

                Layers.Add(group);
            }
        }

        Location = location;

        // a stale filter from the previous click must not hide the new results
        _filterText = string.Empty;
        RaisePropertyChanged(nameof(FilterText));

        RaiseResultProperties();

        SelectedNode = Layers.FirstOrDefault()?.Features.FirstOrDefault();
    }

    /// <summary>Drop every result (the host calls this when the window closes).</summary>
    public void Clear() => Update(null, null);

    #endregion

    #region Filter

    private string _filterText = string.Empty;
    public string FilterText
    {
        get { return _filterText; }
        set
        {
            var text = value ?? string.Empty;

            if (_filterText == text)
                return;

            _filterText = text;
            RaisePropertyChanged();

            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        foreach (var layer in Layers)
            layer.ApplyFilter(_filterText);

        RaisePropertyChanged(nameof(HasVisibleResults));

        // a selected feature that the filter just hid would leave the details pane showing
        // something the tree no longer lists; move to the first visible feature instead
        if (SelectedFeature is not null && !SelectedFeature.Layer.VisibleFeatures.Contains(SelectedFeature))
            SelectedNode = Layers.SelectMany(l => l.VisibleFeatures).FirstOrDefault();
    }

    #endregion

    #region Selection

    private object? _selectedNode;
    /// <summary>The selected tree node: an <see cref="IdentifyLayerNode"/>, an <see cref="IdentifyFeatureNode"/>, or null.</summary>
    public object? SelectedNode
    {
        get { return _selectedNode; }
        set { SetSelectedNode(value); }
    }

    public IdentifyFeatureNode? SelectedFeature { get; private set; }

    /// <summary>The selected layer node, or the parent of the selected feature.</summary>
    public IdentifyLayerNode? SelectedLayer { get; private set; }

    public bool IsFeatureSelected => SelectedFeature is not null;

    public bool IsLayerSelected => SelectedFeature is null && SelectedLayer is not null;

    public IReadOnlyList<IdentifyAttributeRow>? SelectedAttributes => SelectedFeature?.Attributes;

    /// <summary>Features the current selection stands for: the feature itself, or a layer's visible hits.</summary>
    public IReadOnlyList<Feature<Point>> GetSelectedFeatures()
    {
        if (SelectedFeature is not null)
            return new[] { SelectedFeature.Feature };

        if (SelectedLayer is not null)
            return SelectedLayer.VisibleFeatures.Select(f => f.Feature).ToList();

        return Array.Empty<Feature<Point>>();
    }

    private void SetSelectedNode(object? node)
    {
        if (ReferenceEquals(_selectedNode, node))
            return;

        if (node is not null && node is not IIdentifyNode)
            throw new ArgumentException("Only identify tree nodes can be selected.", nameof(node));

        _isSyncingSelection = true;

        try
        {
            if (_selectedNode is IIdentifyNode previous)
                previous.IsSelected = false;

            _selectedNode = node;

            if (node is IIdentifyNode current)
                current.IsSelected = true;
        }
        finally
        {
            _isSyncingSelection = false;
        }

        SelectedFeature = node as IdentifyFeatureNode;

        SelectedLayer = node as IdentifyLayerNode ?? SelectedFeature?.Layer;

        RaisePropertyChanged(nameof(SelectedNode));
        RaisePropertyChanged(nameof(SelectedFeature));
        RaisePropertyChanged(nameof(SelectedLayer));
        RaisePropertyChanged(nameof(SelectedAttributes));
        RaisePropertyChanged(nameof(IsFeatureSelected));
        RaisePropertyChanged(nameof(IsLayerSelected));
        RaisePropertyChanged(nameof(CanShowInAttributeTable));

        CommandManager.InvalidateRequerySuggested();

        _ = HighlightSelectionAsync();
    }

    private void OnNodeIsSelectedChanged(object? sender, EventArgs e)
    {
        if (_isSyncingSelection || sender is not IIdentifyNode node)
            return;

        // Only react to a node becoming selected. The TreeView deselects the old item before
        // it selects the new one, and treating that as "nothing selected" would clear the map
        // highlight and repaint it a moment later.
        if (node.IsSelected)
            SelectedNode = node;
    }

    private async Task HighlightSelectionAsync()
    {
        // A visible drawing is selected in the drawing legend instead: the legend draws its own
        // highlight, so the overlay would only duplicate it. A hidden drawing gets no legend
        // highlight and keeps the overlay.
        if (SelectedFeature?.Drawing is { } drawing && drawing.Visibility == System.Windows.Visibility.Visible)
        {
            RequestClearHighlight?.Invoke();
            RequestSelectDrawing?.Invoke(drawing);
            return;
        }

        RequestSelectDrawing?.Invoke(null);

        var features = GetSelectedFeatures();

        if (features.Count == 0)
        {
            RequestClearHighlight?.Invoke();
            return;
        }

        if (RequestHighlightAsync is not null)
            await RequestHighlightAsync(features, SelectedLayer?.StrokeThickness);
    }

    #endregion

    #region Commands

    public ICommand ZoomToCommand { get; }

    public ICommand CopyAttributesCommand { get; }

    public ICommand ShowInAttributeTableCommand { get; }

    /// <summary>A data layer's feature or layer node is selected (a drawing has no attribute table).</summary>
    public bool CanShowInAttributeTable => SelectedLayer?.Layer is not null && GetSelectedFeatures().Count > 0;

    private async Task ShowSelectionInAttributeTableAsync()
    {
        if (SelectedLayer?.Layer is not VectorLayer layer || RequestShowInAttributeTable is null)
            return;

        var features = GetSelectedFeatures();

        if (features.Count > 0)
            await RequestShowInAttributeTable(layer, features);
    }

    public ICommand ClearFilterCommand { get; }

    private void ZoomToSelection()
    {
        var features = GetSelectedFeatures();

        if (features.Count > 0)
            RequestZoomTo?.Invoke(features);
    }

    private void CopySelectedAttributes()
    {
        if (SelectedFeature is null)
            return;

        try
        {
            ClipboardHelper.CopyText(BuildAttributesText(SelectedFeature));
        }
        catch (Exception)
        {
            // the clipboard is a shared OS resource and can be locked by another process;
            // a failed copy is not worth an error dialog
        }
    }

    /// <summary>Tab-separated "name ⇥ value" lines, one per displayed attribute, ready for a spreadsheet.</summary>
    public static string BuildAttributesText(IdentifyFeatureNode node)
    {
        var builder = new StringBuilder();

        foreach (var row in node.Attributes)
        {
            builder.Append(row.DisplayName);
            builder.Append('\t');
            builder.AppendLine(row.IsNull ? string.Empty : row.DisplayText);
        }

        return builder.ToString();
    }

    #endregion

    #region Plumbing

    private void AttachNode(IdentifyLayerNode layer)
    {
        layer.IsSelectedChanged += OnNodeIsSelectedChanged;

        foreach (var feature in layer.Features)
            feature.IsSelectedChanged += OnNodeIsSelectedChanged;
    }

    private void DetachNodes()
    {
        foreach (var layer in Layers)
        {
            layer.IsSelectedChanged -= OnNodeIsSelectedChanged;

            foreach (var feature in layer.Features)
                feature.IsSelectedChanged -= OnNodeIsSelectedChanged;
        }
    }

    private void RaiseResultProperties()
    {
        RaisePropertyChanged(nameof(Location));
        RaisePropertyChanged(nameof(LocationText));
        RaisePropertyChanged(nameof(LayerCount));
        RaisePropertyChanged(nameof(TotalFeatureCount));
        RaisePropertyChanged(nameof(HasResults));
        RaisePropertyChanged(nameof(HasVisibleResults));
        RaisePropertyChanged(nameof(SummaryText));
    }

    private void OnLanguageChanged()
    {
        foreach (var layer in Layers)
            layer.SetGroupTitle(Localization[DrawingsGroupTitleKey]);

        RaisePropertyChanged(nameof(SummaryText));
        RaisePropertyChanged(nameof(LocationText));
    }

    #endregion
}
