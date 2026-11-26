namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>The rule types a check can use, by id. Its only job is to know them; it runs nothing.</summary>
public sealed class TopologyRuleCatalog
{
    private readonly Dictionary<string, ITopologyRuleType> _types = new(StringComparer.Ordinal);

    public TopologyRuleCatalog(IEnumerable<ITopologyRuleType>? ruleTypes = null)
    {
        foreach (var ruleType in ruleTypes ?? Enumerable.Empty<ITopologyRuleType>())
            Register(ruleType);
    }

    public IReadOnlyCollection<ITopologyRuleType> RuleTypes => _types.Values;

    public void Register(ITopologyRuleType ruleType)
    {
        if (ruleType is null)
            throw new ArgumentNullException(nameof(ruleType));

        if (_types.ContainsKey(ruleType.Id))
            throw new ArgumentException($"A rule type '{ruleType.Id}' is already registered.", nameof(ruleType));

        _types.Add(ruleType.Id, ruleType);
    }

    public ITopologyRuleType? Find(string ruleTypeId) => _types.TryGetValue(ruleTypeId, out var ruleType) ? ruleType : null;
}
