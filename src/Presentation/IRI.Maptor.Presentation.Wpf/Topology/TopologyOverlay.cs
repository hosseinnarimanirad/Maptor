using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.ViewModels;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>
/// The issues drawn on the map (D44): on named highlight layers owned by the Topology tool, never in the legend or the
/// drawings, so they are never saved or taken for data. Coloured by severity (Okabe–Ito: errors reddish purple, as the
/// span panel's problem ring; warnings orange; notices blue); the selected issue drawn stronger. Above
/// <see cref="Limit"/> issues only errors are drawn. The map clears highlight layers when its selection changes, so the
/// overlay draws itself again then.
/// </summary>
public sealed class TopologyOverlay
{
    /// <summary>More issues than this and only the errors are drawn.</summary>
    public const int Limit = 3000;

    private const string Prefix = "__$topology.";

    private static readonly string[] LayerNames = { Prefix + "info", Prefix + "warning", Prefix + "error", Prefix + "selected" };

    private static readonly string[] PreviewLayerNames = { Prefix + "previewBefore", Prefix + "previewAfter" };

    public static readonly SolidColorBrush ErrorBrush = Frozen(Color.FromRgb(0xCC, 0x79, 0xA7));
    public static readonly SolidColorBrush WarningBrush = Frozen(Color.FromRgb(0xE6, 0x9F, 0x00));
    public static readonly SolidColorBrush InfoBrush = Frozen(Color.FromRgb(0x00, 0x72, 0xB2));
    public static readonly SolidColorBrush SelectedBrush = Frozen(Color.FromRgb(0xD5, 0x00, 0x00));

    /// <summary>A fix preview (doc 05 §7): the geometry as it is, dashed grey, and as it would be, bluish green.</summary>
    public static readonly SolidColorBrush PreviewBeforeBrush = Frozen(Color.FromRgb(0x80, 0x80, 0x80));
    public static readonly SolidColorBrush PreviewAfterBrush = Frozen(Color.FromRgb(0x00, 0x9E, 0x73));

    private readonly MapViewModelBase _map;

    private IReadOnlyList<TopologyIssue> _issues = Array.Empty<TopologyIssue>();
    private TopologyIssue? _selected;
    private int _version;
    private bool _isShown;
    private TopologyFixProposal? _preview;

    public TopologyOverlay(MapViewModelBase map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _map.SelectedLayers.CollectionChanged += OnSelectionChanged;
    }

    /// <summary>The only switch (the ribbon's Show on map): closing a panel does not hide the overlay.</summary>
    public bool IsShown
    {
        get => _isShown;
        set
        {
            if (_isShown == value)
                return;

            _isShown = value;
            _ = RedrawAsync();
            IsShownChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? IsShownChanged;

    /// <summary>True when the last drawing left out warnings and notices because there were too many issues.</summary>
    public bool IsLimited { get; private set; }

    /// <summary>Replaces what is drawn: the issues that pass the panel's filters, and the selected one.</summary>
    public Task ShowAsync(IReadOnlyList<TopologyIssue> issues, TopologyIssue? selected)
    {
        _issues = issues ?? Array.Empty<TopologyIssue>();
        _selected = selected;

        return RedrawAsync();
    }

    /// <summary>The fix being previewed; null when none.</summary>
    public TopologyFixProposal? Preview => _preview;

    /// <summary>Draws a fix's before and after, whether or not the issues are shown; null removes it.</summary>
    public Task ShowPreviewAsync(TopologyFixProposal? proposal)
    {
        _preview = proposal;

        return RedrawPreviewAsync();
    }

    /// <summary>What a fix preview draws: the old geometries, then the new ones (removed features have none). Pure, for tests.</summary>
    public static (IReadOnlyList<Geometry<Point>> Before, IReadOnlyList<Geometry<Point>> After) PreviewPlan(TopologyFixProposal proposal)
        => (proposal.Edits.Select(e => ToMap(e.Before)).ToList(),
            proposal.Edits.Where(e => e.After is not null).Select(e => ToMap(e.After!)).ToList());

    public void Clear()
    {
        _version++;

        foreach (var name in LayerNames)
            _map.ClearLayer(name);
    }

    /// <summary>What would be drawn: one list of map geometries per layer. Pure, for tests.</summary>
    public static IReadOnlyList<(string Layer, IReadOnlyList<Geometry<Point>> Geometries)> Plan(IReadOnlyList<TopologyIssue> issues, TopologyIssue? selected, out bool isLimited)
    {
        isLimited = issues.Count > Limit;

        var drawn = isLimited ? issues.Where(i => i.Severity == IssueSeverity.Error).ToList() : issues.ToList();

        var plan = new List<(string, IReadOnlyList<Geometry<Point>>)>
        {
            (LayerNames[0], Geometries(drawn.Where(i => i.Severity == IssueSeverity.Info))),
            (LayerNames[1], Geometries(drawn.Where(i => i.Severity == IssueSeverity.Warning))),
            (LayerNames[2], Geometries(drawn.Where(i => i.Severity == IssueSeverity.Error))),
            (LayerNames[3], selected is null ? Array.Empty<Geometry<Point>>() : Geometries(new[] { selected })),
        };

        return plan;
    }

    /// <summary>A map (Web Mercator) copy of a WGS84 location.</summary>
    public static Geometry<Point> ToMap(Geometry<Point> location)
        => location.Srid == SridHelper.WebMercator ? location : location.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);

    private async Task RedrawAsync()
    {
        var version = ++_version;

        foreach (var name in LayerNames)
            _map.ClearLayer(name);

        // No map view is attached (a test, or before the window is up): nothing can be drawn.
        if (!IsShown || _map.RequestHighlightGeometries is null)
            return;

        var plan = Plan(_issues, _selected, out var isLimited);

        IsLimited = isLimited;

        for (int i = 0; i < plan.Count; i++)
        {
            if (version != _version)
                return;

            if (plan[i].Geometries.Count > 0)
                await _map.HighlightGeometriesAsync(plan[i].Layer, plan[i].Geometries.ToList(), StyleOf(i));
        }
    }

    private async Task RedrawPreviewAsync()
    {
        foreach (var name in PreviewLayerNames)
            _map.ClearLayer(name);

        if (_preview is null || _map.RequestHighlightGeometries is null)
            return;

        var proposal = _preview;
        var (before, after) = PreviewPlan(proposal);

        if (before.Count > 0)
            await _map.HighlightGeometriesAsync(PreviewLayerNames[0], before.ToList(), PreviewStyle(PreviewBeforeBrush, dashed: true));

        if (after.Count > 0 && ReferenceEquals(proposal, _preview))
            await _map.HighlightGeometriesAsync(PreviewLayerNames[1], after.ToList(), PreviewStyle(PreviewAfterBrush, dashed: false));
    }

    private void OnSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!IsShown && _preview is null)
            return;

        // The map clears its highlight layers on a selection change; draw again once it has.
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (IsShown)
                _ = RedrawAsync();

            if (_preview is not null)
                _ = RedrawPreviewAsync();
        }));
    }

    private static VisualParameters PreviewStyle(SolidColorBrush stroke, bool dashed)
    {
        var style = new VisualParameters(null, stroke, dashed ? 2 : 3, 1)
        {
            PointSymbol = new SimplePointSymbolizer { SymbolWidth = 18, SymbolHeight = 18 },
        };

        if (dashed)
            style.DashStyle = DashStyles.Dash;

        return style;
    }

    private static IReadOnlyList<Geometry<Point>> Geometries(IEnumerable<TopologyIssue> issues)
        => issues.Select(i => i.Finding.Location).Where(l => l is not null && !l.IsNullOrEmpty()).Select(l => ToMap(l!)).ToList();

    private static VisualParameters StyleOf(int layer) => layer switch
    {
        0 => Style(InfoBrush, 2, 14),
        1 => Style(WarningBrush, 3, 16),
        2 => Style(ErrorBrush, 3, 16),
        _ => Style(SelectedBrush, 5, 22),
    };

    /// <summary>Hollow rings for points, so the feature underneath stays visible; a light fill for areas.</summary>
    private static VisualParameters Style(SolidColorBrush stroke, double thickness, double ring)
    {
        var fill = Frozen(Color.FromArgb(0x40, stroke.Color.R, stroke.Color.G, stroke.Color.B));

        return new VisualParameters(fill, stroke, thickness, 1)
        {
            PointSymbol = new SimplePointSymbolizer { SymbolWidth = ring, SymbolHeight = ring },
        };
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
