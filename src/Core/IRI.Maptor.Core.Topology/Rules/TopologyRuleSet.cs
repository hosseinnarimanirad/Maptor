namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// The rules of one app or data model, declared in code by the app. The engine knows nothing about the
/// app's layers beyond what the rule set says.
/// </summary>
public sealed class TopologyRuleSet
{
    /// <summary>The default distance below which two positions are the same place: 1 cm.</summary>
    public const double DefaultSamePlace = 0.01;

    /// <param name="key">Short stable name of the set, e.g. the app's name.</param>
    /// <param name="version">A number the app raises whenever it changes the set.</param>
    /// <param name="layers">The layers the rules use.</param>
    /// <param name="rules">The rules, in the order their issues are listed.</param>
    /// <param name="samePlace">Metres below which two positions are the same place, for every rule of the set.</param>
    public TopologyRuleSet(string key, int version, IEnumerable<TopologyLayer> layers, IEnumerable<TopologyRule> rules, double samePlace = DefaultSamePlace)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A rule set needs a key.", nameof(key));

        Key = key;
        Version = version;
        Layers = (layers ?? throw new ArgumentNullException(nameof(layers))).ToList();
        Rules = (rules ?? throw new ArgumentNullException(nameof(rules))).ToList();
        SamePlace = samePlace;
    }

    public string Key { get; }

    public int Version { get; }

    public IReadOnlyList<TopologyLayer> Layers { get; }

    public IReadOnlyList<TopologyRule> Rules { get; }

    public double SamePlace { get; }

    public TopologyLayer? FindLayer(string entityName) => Layers.FirstOrDefault(l => l.EntityName == entityName);

    public TopologyRule? FindRule(string ruleId) => Rules.FirstOrDefault(r => r.Id == ruleId);

    /// <summary>A copy with one rule replaced (matched by id); used for settings that last one session.</summary>
    public TopologyRuleSet WithRule(TopologyRule rule)
    {
        if (rule is null)
            throw new ArgumentNullException(nameof(rule));

        if (FindRule(rule.Id) is null)
            throw new ArgumentException($"The rule set has no rule '{rule.Id}'.", nameof(rule));

        return new TopologyRuleSet(Key, Version, Layers, Rules.Select(r => r.Id == rule.Id ? rule : r), SamePlace);
    }

    public override string ToString() => $"{Key} v{Version}: {Rules.Count} rules";
}
