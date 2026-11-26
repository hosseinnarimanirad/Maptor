using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Presentation.Core;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>A scope the expert can choose in the Check panel.</summary>
public sealed class TopologyScopeOption
{
    public TopologyScopeOption(CheckScopeKind kind, string text)
    {
        Kind = kind;
        Text = text;
    }

    public CheckScopeKind Kind { get; }

    public string Text { get; }

    public override string ToString() => Text;
}

/// <summary>One layer the rules need, with its state.</summary>
public sealed class TopologyLayerRow
{
    public TopologyLayerRow(string entityName, string name, TopologyLayerState state)
    {
        EntityName = entityName;
        Name = name;
        State = state;
    }

    public string EntityName { get; }

    public string Name { get; }

    public TopologyLayerState State { get; }

    public string StateText => State switch
    {
        TopologyLayerState.Loaded => TopologyMessages.Text("topology_check_layerLoaded") ?? "loaded",
        TopologyLayerState.Live => TopologyMessages.Text("topology_check_layerLive") ?? "Live: map extent or selection only",
        _ => TopologyMessages.Text("topology_check_layerNotLoaded") ?? "not loaded: its rules are skipped",
    };

    public bool IsLoaded => State == TopologyLayerState.Loaded;

    public bool IsLive => State == TopologyLayerState.Live;

    public bool IsMissing => State == TopologyLayerState.NotLoaded;
}

/// <summary>
/// The Check panel (doc 05 §3): what to check, the layers it needs, and running the check. When a check ends, the app
/// opens the Issues panel (<c>onChecked</c>). Holds state and commands only.
/// </summary>
public sealed class TopologyCheckPanelViewModel : Notifier
{
    private readonly TopologyWorkspace _workspace;
    private readonly Action _onChecked;

    private CancellationTokenSource? _cancel;

    public TopologyCheckPanelViewModel(TopologyWorkspace workspace, Action onChecked)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _onChecked = onChecked ?? (() => { });

        ScopeOptions = new[]
        {
            new TopologyScopeOption(CheckScopeKind.WholeData, TopologyMessages.Text("topology_check_scopeWholeData") ?? "Whole data"),
            new TopologyScopeOption(CheckScopeKind.Layer, TopologyMessages.Text("topology_check_scopeLayer") ?? "One layer"),
            new TopologyScopeOption(CheckScopeKind.Extent, TopologyMessages.Text("topology_check_scopeExtent") ?? "Map extent"),
            new TopologyScopeOption(CheckScopeKind.Features, TopologyMessages.Text("topology_check_scopeFeatures") ?? "Selected features"),
        };

        _selectedScope = ScopeOptions[0];

        CheckCommand = new RelayCommand(async _ => await CheckAsync(), _ => CanCheck);
        CancelCommand = new RelayCommand(_ => _cancel?.Cancel(), _ => IsRunning);
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync());

        _workspace.SettingsChanged += (_, _) => _ = RefreshAsync();
    }

    public IReadOnlyList<TopologyScopeOption> ScopeOptions { get; }

    private TopologyScopeOption _selectedScope;

    public TopologyScopeOption SelectedScope
    {
        get => _selectedScope;
        set
        {
            if (value is null || ReferenceEquals(_selectedScope, value))
                return;

            _selectedScope = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(NeedsLayer));
            UpdateMessages();
        }
    }

    /// <summary>One-layer and selected-feature scopes name a layer.</summary>
    public bool NeedsLayer => SelectedScope.Kind is CheckScopeKind.Layer or CheckScopeKind.Features;

    public ObservableCollection<TopologyLayerRow> Layers { get; } = new();

    private TopologyLayerRow? _selectedLayer;

    public TopologyLayerRow? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            _selectedLayer = value;
            RaisePropertyChanged();
            UpdateMessages();
        }
    }

    public string RulesText => TopologyMessages.Format("topology_check_rules", _workspace.RuleSet.Rules.Count(r => r.Enabled), _workspace.RuleSet.Key);

    public bool HasSessionChanges => _workspace.HasSessionChanges;

    private int _featureCount;

    private string _warning = string.Empty;

    /// <summary>Why the chosen scope cannot run, or what to know before it does; empty when nothing.</summary>
    public string Warning
    {
        get => _warning;
        private set
        {
            _warning = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasWarning));
        }
    }

    public bool HasWarning => Warning.Length > 0;

    private bool _isBlocked;

    private bool _isRunning;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            _isRunning = value;
            RaisePropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private string _progress = string.Empty;

    public string Progress
    {
        get => _progress;
        private set
        {
            _progress = value;
            RaisePropertyChanged();
        }
    }

    private string _status = string.Empty;

    /// <summary>The outcome of the last check: a summary, "cancelled", or an error.</summary>
    public string Status
    {
        get => _status;
        private set
        {
            _status = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasStatus));
        }
    }

    public bool HasStatus => Status.Length > 0;

    public bool CanCheck => !IsRunning && !_isBlocked && Layers.Any(l => !l.IsMissing);

    public ICommand CheckCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand RefreshCommand { get; }

    /// <summary>Reads the layers' states again; call when the panel opens.</summary>
    public async Task RefreshAsync()
    {
        var previous = SelectedLayer?.EntityName;

        Layers.Clear();

        foreach (var entity in _workspace.NeededLayers)
            Layers.Add(new TopologyLayerRow(entity, TopologyMessages.LayerName(_workspace.RuleSet, entity), _workspace.StateOf(entity)));

        SelectedLayer = Layers.FirstOrDefault(l => l.EntityName == previous) ?? Layers.FirstOrDefault(l => !l.IsMissing);

        _featureCount = await _workspace.CountFeaturesAsync();

        RaisePropertyChanged(nameof(RulesText));
        RaisePropertyChanged(nameof(HasSessionChanges));
        UpdateMessages();
    }

    /// <summary>The scope as the engine takes it, in WGS84; null with the reason in <see cref="Warning"/>.</summary>
    public TopologyCheckScope? BuildScope()
    {
        switch (SelectedScope.Kind)
        {
            case CheckScopeKind.Layer:
                return SelectedLayer is null ? null : TopologyCheckScope.ForLayer(SelectedLayer.EntityName);

            case CheckScopeKind.Extent:
                return TopologyCheckScope.ForExtent(_workspace.Map.CurrentExtent.WebMercatorToGeodeticWgs84Mbb());

            case CheckScopeKind.Features:
                if (SelectedLayer is null)
                    return null;

                var layers = _workspace.MapLayersOf(SelectedLayer.EntityName);

                var ids = _workspace.Map.SelectedLayers
                    .Where(s => layers.Contains(s.AssociatedLayer))
                    .SelectMany(s => s.GetSelectedFeatures())
                    .Select(f => (long)f.Id)
                    .Distinct()
                    .ToList();

                return ids.Count == 0 ? null : TopologyCheckScope.ForFeatures(SelectedLayer.EntityName, ids);

            default:
                return TopologyCheckScope.WholeData;
        }
    }

    private void UpdateMessages()
    {
        var messages = new List<string>();

        _isBlocked = false;

        var problems = _workspace.SettingsProblems;

        if (problems.Count > 0)
        {
            _isBlocked = true;
            messages.Add(TopologyMessages.Text("topology_check_settingsInvalid") ?? "The Rules panel has a setting that cannot be used.");
        }

        var wholeLayers = SelectedScope.Kind switch
        {
            CheckScopeKind.WholeData => Layers.ToList(),
            CheckScopeKind.Layer => Layers.Where(l => l == SelectedLayer).ToList(),
            _ => new List<TopologyLayerRow>(),
        };

        if (wholeLayers.Any(l => l.IsLive))
        {
            _isBlocked = true;
            messages.Add(TopologyMessages.Text("topology_check_liveNeedsPartialScope") ?? "A Live layer holds only what is in view: check the map extent or a selection, or switch the layer to Cache.");
        }

        if (NeedsLayer && SelectedLayer is null)
            _isBlocked = true;

        if (SelectedScope.Kind == CheckScopeKind.Features && SelectedLayer is not null && BuildScope() is null)
        {
            _isBlocked = true;
            messages.Add(TopologyMessages.Text("topology_check_noSelection") ?? "Select features of this layer on the map first.");
        }

        if (SelectedScope.Kind is CheckScopeKind.WholeData or CheckScopeKind.Layer && _featureCount > TopologyWorkspace.LargeScopeFeatureLimit)
            messages.Add(TopologyMessages.Format("topology_check_largeScope", _featureCount));

        if (Layers.Any(l => l.IsMissing))
            messages.Add(TopologyMessages.Text("topology_check_someNotLoaded") ?? "Layers that are not loaded are skipped, with their rules.");

        Warning = string.Join(Environment.NewLine, messages);

        CommandManager.InvalidateRequerySuggested();
    }

    private async Task CheckAsync()
    {
        var scope = BuildScope();

        if (scope is null)
            return;

        _cancel = new CancellationTokenSource();
        IsRunning = true;
        Status = string.Empty;

        var progress = new Progress<TopologyCheckProgress>(p =>
            Progress = TopologyMessages.Format("topology_check_progress", p.RuleIndex + 1, p.RuleCount, RuleTitle(p.RuleId)));

        try
        {
            var result = await _workspace.CheckAsync(scope, progress, _cancel.Token);

            Status = TopologyMessages.Format("topology_check_done", result.Summary.IssueCount, result.Summary.FeaturesRead.Values.Sum());

            _onChecked();
        }
        catch (OperationCanceledException)
        {
            Status = TopologyMessages.Text("topology_check_cancelled") ?? "Cancelled: the previous results are kept.";
        }
        catch (Exception ex) when (ex is TopologyCheckException or NotSupportedException)
        {
            Status = ex.Message;
        }
        finally
        {
            Progress = string.Empty;
            IsRunning = false;
            _cancel.Dispose();
            _cancel = null;
        }
    }

    private string RuleTitle(string ruleId)
    {
        var rule = _workspace.RuleSet.FindRule(ruleId);

        return rule is null ? ruleId : TopologyMessages.RuleTitle(rule);
    }
}
