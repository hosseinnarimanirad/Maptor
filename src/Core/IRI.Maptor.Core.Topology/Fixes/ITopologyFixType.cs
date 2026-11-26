using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.Fixes;

/// <summary>
/// One kind of fix, for some kinds of one rule type (FR-5.1). Its only job is to compute the edits for one finding;
/// it does not detect, check its own result, apply or save. Where the expert must choose (which duplicate to delete),
/// it offers one proposal per choice.
/// </summary>
public interface ITopologyFixType
{
    /// <summary>Stable id, e.g. "pointOntoLine".</summary>
    string Id { get; }

    /// <summary>The rule type whose findings it fixes.</summary>
    string RuleTypeId { get; }

    /// <summary>The kinds of that rule type it fixes.</summary>
    IReadOnlyCollection<string> Kinds { get; }

    /// <summary>
    /// One offer, or one per choice. Message arguments of a proposal are feature references
    /// (<see cref="FindingFeature"/> or a list of them) and distances in metres (<see cref="double"/>).
    /// </summary>
    IReadOnlyList<TopologyFixOffer> Propose(TopologyFixContext context);
}

/// <summary>What a fix type gets: the finding, its rule and rule set (for parameters and layer ranks), and its features.</summary>
public sealed class TopologyFixContext
{
    private readonly IReadOnlyDictionary<(string EntityName, long Id), Feature<Point>> _features;

    /// <param name="ruleSet">The rule set (layer ranks).</param>
    /// <param name="rule">The rule that found it (parameters).</param>
    /// <param name="finding">The finding to fix.</param>
    /// <param name="features">The finding's features as the check reads them, by layer and id.</param>
    /// <param name="isGeodetic">Whether coordinates are longitude and latitude.</param>
    public TopologyFixContext(TopologyRuleSet ruleSet, TopologyRule rule, TopologyFinding finding, IReadOnlyDictionary<(string EntityName, long Id), Feature<Point>> features, bool isGeodetic)
    {
        RuleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Finding = finding ?? throw new ArgumentNullException(nameof(finding));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        IsGeodetic = isGeodetic;
    }

    public TopologyRuleSet RuleSet { get; }

    public TopologyRule Rule { get; }

    public TopologyFinding Finding { get; }

    public bool IsGeodetic { get; }

    /// <summary>The first feature of the finding on that side, if it is still in the data.</summary>
    public Feature<Point>? FeatureOf(FeatureRole role)
    {
        var feature = Finding.Features.FirstOrDefault(f => f.Role == role);

        return feature is not null && _features.TryGetValue((feature.EntityName, feature.FeatureId), out var found) ? found : null;
    }

    /// <summary>Every feature of the finding that is still in the data, in the finding's order.</summary>
    public IReadOnlyList<(FindingFeature Reference, Feature<Point> Feature)> Features
        => Finding.Features
            .Where(f => _features.ContainsKey((f.EntityName, f.FeatureId)))
            .Select(f => (f, _features[(f.EntityName, f.FeatureId)]))
            .ToList();

    /// <summary>The layer's rank in the rule set (FR-5.4): when two layers disagree, the lower rank moves.</summary>
    public int RankOf(string entityName) => RuleSet.FindLayer(entityName)?.Rank ?? 0;
}
