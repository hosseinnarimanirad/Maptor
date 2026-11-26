using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// What a rule type sees while it checks one rule: the rule, the features of its layers, how to measure, and
/// a store for work that several rules of one check share (e.g. one points-on-lines topology used by five
/// rules).
/// </summary>
public sealed class TopologyRuleContext
{
    private static readonly IReadOnlyList<Feature<Point>> NoFeatures = Array.Empty<Feature<Point>>();

    private readonly IDictionary<string, object> _shared;

    private readonly IReadOnlyDictionary<string, IReadOnlyList<Feature<Point>>> _layers;

    /// <param name="ruleSet">The rule set the rule belongs to.</param>
    /// <param name="rule">The rule being checked.</param>
    /// <param name="ruleType">The rule's type.</param>
    /// <param name="featuresA">The features of layer A.</param>
    /// <param name="featuresB">The features of layer B; null when the rule has none.</param>
    /// <param name="isGeodetic">True for WGS84 longitude/latitude, false for an SRS in metres.</param>
    /// <param name="shared">Work shared between the rules of one check; a new dictionary when null.</param>
    /// <param name="layers">The features of every loaded layer, for the extra layers a rule type reads.</param>
    public TopologyRuleContext(
        TopologyRuleSet ruleSet,
        TopologyRule rule,
        ITopologyRuleType ruleType,
        IReadOnlyList<Feature<Point>> featuresA,
        IReadOnlyList<Feature<Point>>? featuresB,
        bool isGeodetic,
        IDictionary<string, object>? shared = null,
        IReadOnlyDictionary<string, IReadOnlyList<Feature<Point>>>? layers = null)
    {
        RuleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        RuleType = ruleType ?? throw new ArgumentNullException(nameof(ruleType));
        FeaturesA = featuresA ?? throw new ArgumentNullException(nameof(featuresA));
        FeaturesB = featuresB ?? NoFeatures;
        IsGeodetic = isGeodetic;
        _shared = shared ?? new Dictionary<string, object>(StringComparer.Ordinal);
        _layers = layers ?? new Dictionary<string, IReadOnlyList<Feature<Point>>>(StringComparer.Ordinal);
    }

    public TopologyRuleSet RuleSet { get; }

    public TopologyRule Rule { get; }

    public ITopologyRuleType RuleType { get; }

    public IReadOnlyList<Feature<Point>> FeaturesA { get; }

    /// <summary>Empty when the rule has no layer B.</summary>
    public IReadOnlyList<Feature<Point>> FeaturesB { get; }

    public bool IsGeodetic { get; }

    /// <summary>Metres below which two positions are the same place (the rule set's value).</summary>
    public double SamePlace => RuleSet.SamePlace;

    /// <summary>
    /// The value stored under <paramref name="key"/> by an earlier rule of this check, or the one
    /// <paramref name="create"/> makes now. Keys should name the work and its inputs, e.g.
    /// "pointsOnLines:Tower:Route:0.5".
    /// </summary>
    public TValue GetShared<TValue>(string key, Func<TValue> create) where TValue : class
    {
        if (_shared.TryGetValue(key, out var existing))
            return (TValue)existing;

        var created = create();

        _shared[key] = created;

        return created;
    }

    /// <summary>A feature of layer A, as a finding refers to it.</summary>
    public FindingFeature FeatureA(Feature<Point> feature) => new FindingFeature(Rule.LayerA, feature.Id, FeatureRole.A);

    /// <summary>A feature of layer B, as a finding refers to it.</summary>
    public FindingFeature FeatureB(Feature<Point> feature)
        => new FindingFeature(Rule.LayerB ?? throw new InvalidOperationException($"Rule '{Rule.Id}' has no layer B."), feature.Id, FeatureRole.B);

    /// <summary>The features of another loaded layer (an extra layer of the rule); empty when it was not loaded.</summary>
    public IReadOnlyList<Feature<Point>> FeaturesOf(string entityName)
        => entityName == Rule.LayerA ? FeaturesA
         : entityName == Rule.LayerB ? FeaturesB
         : _layers.TryGetValue(entityName, out var features) ? features : NoFeatures;

    /// <summary>
    /// The name of the filter this rule applies to <paramref name="entityName"/>, or "all"; part of the key of any
    /// work shared between rules, since two rules with different filters see different features.
    /// </summary>
    public string SelectionOf(string entityName) => Rule.FilterOf(entityName)?.Name ?? "all";

    /// <summary>A feature of an extra layer, as a finding refers to it.</summary>
    public FindingFeature FeatureOf(string entityName, Feature<Point> feature)
        => entityName == Rule.LayerA ? FeatureA(feature)
         : entityName == Rule.LayerB ? FeatureB(feature)
         : new FindingFeature(entityName, feature.Id, FeatureRole.Other);

    /// <summary>A finding of this rule, with the rule type's default severity for <paramref name="kind"/>.</summary>
    public TopologyFinding Finding(
        string kind,
        IReadOnlyList<FindingFeature> features,
        Geometry<Point>? location = null,
        double? value = null,
        ValueUnit valueUnit = ValueUnit.None,
        string? discriminator = null,
        IReadOnlyList<object>? messageArguments = null)
        => new TopologyFinding(Rule.Id, kind, RuleType.DefaultSeverityOf(kind), features, location, value, valueUnit, discriminator, messageArguments);
}
