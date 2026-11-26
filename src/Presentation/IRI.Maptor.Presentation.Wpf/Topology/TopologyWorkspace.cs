using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.ViewModels;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>Whether the map layers a rule set needs are there, and in which mode.</summary>
public enum TopologyLayerState
{
    /// <summary>No map layer holds the entity.</summary>
    NotLoaded = 0,

    /// <summary>Loaded in Cache mode: the whole layer is in memory.</summary>
    Loaded = 1,

    /// <summary>A layer holds only what is in view (Live mode): only partial scopes make sense.</summary>
    Live = 2,
}

/// <summary>
/// One Topology tab's session: the app's rule set and the expert's changes to it for this session (never stored, D50),
/// the last check and the one before, and running a check. The panels and the map overlay share one workspace; it
/// holds no UI.
/// </summary>
public sealed class TopologyWorkspace : Notifier
{
    /// <summary>A whole-data or one-layer check above this many features warns first (D51; provisional until the
    /// count-only run, D61).</summary>
    public const int LargeScopeFeatureLimit = 300_000;

    private readonly Func<string, IReadOnlyList<VectorLayer>> _layersOf;

    /// <param name="map">The map the layers are on.</param>
    /// <param name="ruleSet">The app's rule set.</param>
    /// <param name="catalog">The rule types the rule set uses.</param>
    /// <param name="layersOf">The loaded map layers that hold an entity of the rule set (none when it is not loaded).</param>
    public TopologyWorkspace(MapViewModelBase map, TopologyRuleSet ruleSet, TopologyRuleCatalog catalog, Func<string, IReadOnlyList<VectorLayer>> layersOf)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        DefaultRuleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _layersOf = layersOf ?? throw new ArgumentNullException(nameof(layersOf));
        _ruleSet = ruleSet;
    }

    public MapViewModelBase Map { get; }

    /// <summary>The app's rule set as declared.</summary>
    public TopologyRuleSet DefaultRuleSet { get; }

    public TopologyRuleCatalog Catalog { get; }

    private TopologyRuleSet _ruleSet;

    /// <summary>The rule set with this session's changes.</summary>
    public TopologyRuleSet RuleSet
    {
        get => _ruleSet;
        private set
        {
            _ruleSet = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSessionChanges));
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool HasSessionChanges => !DefaultRuleSet.Rules.Select(r => r).SequenceEqual(RuleSet.Rules);

    public event EventHandler? SettingsChanged;

    public event EventHandler? ResultChanged;

    /// <summary>Replaces a rule for this session (switch off, distances, kind settings).</summary>
    public void UpdateRule(TopologyRule rule) => RuleSet = RuleSet.WithRule(rule);

    public void ResetSettings() => RuleSet = DefaultRuleSet;

    public TopologyRule? DefaultRule(string ruleId) => DefaultRuleSet.FindRule(ruleId);

    /// <summary>Problems with the session's settings (e.g. a search distance below the rule distance).</summary>
    public IReadOnlyList<string> SettingsProblems => RuleSetValidator.Validate(RuleSet, Catalog);

    #region Results

    private TopologyCheckResult? _result;

    public TopologyCheckResult? Result
    {
        get => _result;
        private set
        {
            _result = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasResult));
        }
    }

    public bool HasResult => Result is not null;

    /// <summary>The check before the last one, when both covered the same scope; null otherwise.</summary>
    public TopologyCheckResult? Previous { get; private set; }

    /// <summary>New, still there and gone between <see cref="Previous"/> and <see cref="Result"/>; null without a previous check.</summary>
    public CheckComparison? Comparison { get; private set; }

    /// <summary>The rule set the last check ran with.</summary>
    public TopologyRuleSet? CheckedRuleSet { get; private set; }

    /// <summary>The rules the last check could not run because a layer was not loaded.</summary>
    public IReadOnlyList<TopologyRule> SkippedRules { get; private set; } = Array.Empty<TopologyRule>();

    public DateTime? CheckedAt { get; private set; }

    /// <summary>Whether the last check ran with this session's changes.</summary>
    public bool CheckedWithSessionChanges { get; private set; }

    #endregion

    #region Layers

    /// <summary>Every layer the enabled rules read (A, B and extra layers), in rule set order.</summary>
    public IReadOnlyList<string> NeededLayers
        => RuleSet.Rules
            .Where(r => r.Enabled)
            .SelectMany(r => LayersOf(r))
            .Distinct()
            .ToList();

    public IReadOnlyList<VectorLayer> MapLayersOf(string entityName) => _layersOf(entityName) ?? Array.Empty<VectorLayer>();

    public TopologyLayerState StateOf(string entityName)
    {
        var layers = MapLayersOf(entityName);

        if (layers.Count == 0)
            return TopologyLayerState.NotLoaded;

        return layers.Any(MapLayerFeatureSource.IsLiveLayer) ? TopologyLayerState.Live : TopologyLayerState.Loaded;
    }

    /// <summary>The number of features in the loaded (Cache) layers the rules need; Live layers are not counted.</summary>
    public async Task<int> CountFeaturesAsync()
    {
        var count = 0;

        foreach (var layer in NeededLayers.SelectMany(MapLayersOf).Distinct().Where(l => !MapLayerFeatureSource.IsLiveLayer(l)))
        {
            var set = await layer.GetFeaturesAsync();
            count += set?.Features?.Count ?? 0;
        }

        return count;
    }

    #endregion

    /// <summary>
    /// Checks <paramref name="scope"/> with the session's rules. Rules whose layers are not loaded are skipped
    /// (<see cref="SkippedRules"/>). The layers are read on the UI thread, the rules run on the thread pool.
    /// </summary>
    public async Task<TopologyCheckResult> CheckAsync(TopologyCheckScope scope, IProgress<TopologyCheckProgress>? progress, CancellationToken cancellationToken)
    {
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        var ruleSet = RuleSet;

        var skipped = ruleSet.Rules.Where(r => r.Enabled && LayersOf(r).Any(l => StateOf(l) == TopologyLayerState.NotLoaded)).ToList();

        var runnable = skipped.Aggregate(ruleSet, (set, rule) => set.WithRule(rule.WithEnabled(false)));

        var sources = runnable.Rules
            .Where(r => r.Enabled)
            .SelectMany(LayersOf)
            .Concat(scope.EntityName is null ? Enumerable.Empty<string>() : new[] { scope.EntityName })
            .Distinct()
            .Where(l => StateOf(l) != TopologyLayerState.NotLoaded)
            .Select(l => (ITopologyFeatureSource)new MapLayerFeatureSource(MapLayersOf(l), l))
            .ToList();

        var runner = new ValidationRunner(Catalog);

        var result = await Task.Run(() => runner.RunAsync(runnable, scope, sources, progress, cancellationToken), cancellationToken);

        Previous = Result is not null && SameScope(Result.Summary.Scope, scope) ? Result : null;
        Comparison = Previous is null ? null : CheckComparer.Compare(Previous.Issues, result.Issues);
        CheckedRuleSet = ruleSet;
        SkippedRules = skipped;
        CheckedAt = DateTime.Now;
        CheckedWithSessionChanges = HasSessionChanges;

        Result = result;

        ResultChanged?.Invoke(this, EventArgs.Empty);

        return result;
    }

    /// <summary>The map layers' own features with these ids, for "show in attribute table".</summary>
    public async Task<IReadOnlyList<(VectorLayer Layer, Feature<Point> Feature)>> MapFeaturesAsync(string entityName, IReadOnlyCollection<long> ids)
    {
        var wanted = new HashSet<long>(ids);
        var found = new List<(VectorLayer, Feature<Point>)>();

        foreach (var layer in MapLayersOf(entityName))
        {
            var set = await layer.GetFeaturesAsync();

            found.AddRange((set?.Features ?? new List<Feature<Point>>()).Where(f => wanted.Contains(f.Id)).Select(f => (layer, f)));
        }

        return found;
    }

    private IEnumerable<string> LayersOf(TopologyRule rule)
    {
        yield return rule.LayerA;

        if (rule.LayerB is not null)
            yield return rule.LayerB;

        foreach (var extra in Catalog.Find(rule.RuleTypeId)?.ExtraLayersOf(rule) ?? Array.Empty<string>())
            yield return extra;
    }

    private static bool SameScope(TopologyCheckScope a, TopologyCheckScope b)
        => a.Kind == b.Kind
           && a.EntityName == b.EntityName
           && Nullable.Equals(a.Extent, b.Extent)
           && a.FeatureIds.OrderBy(i => i).SequenceEqual(b.FeatureIds.OrderBy(i => i));
}
