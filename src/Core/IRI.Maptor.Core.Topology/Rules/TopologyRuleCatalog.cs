using IRI.Maptor.Core.Topology.Fixes;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// The rule types a check can use, by id, and the fix types that suit their kinds. Its only job is to know them; it
/// runs nothing.
/// </summary>
public sealed class TopologyRuleCatalog
{
    private readonly Dictionary<string, ITopologyRuleType> _types = new(StringComparer.Ordinal);
    private readonly List<ITopologyFixType> _fixTypes = new();

    public TopologyRuleCatalog(IEnumerable<ITopologyRuleType>? ruleTypes = null)
    {
        foreach (var ruleType in ruleTypes ?? Enumerable.Empty<ITopologyRuleType>())
            Register(ruleType);
    }

    public IReadOnlyCollection<ITopologyRuleType> RuleTypes => _types.Values;

    public IReadOnlyCollection<ITopologyFixType> FixTypes => _fixTypes;

    public void Register(ITopologyRuleType ruleType)
    {
        if (ruleType is null)
            throw new ArgumentNullException(nameof(ruleType));

        if (_types.ContainsKey(ruleType.Id))
            throw new ArgumentException($"A rule type '{ruleType.Id}' is already registered.", nameof(ruleType));

        _types.Add(ruleType.Id, ruleType);
    }

    public void RegisterFix(ITopologyFixType fixType)
    {
        if (fixType is null)
            throw new ArgumentNullException(nameof(fixType));

        if (_fixTypes.Any(f => f.Id == fixType.Id))
            throw new ArgumentException($"A fix type '{fixType.Id}' is already registered.", nameof(fixType));

        _fixTypes.Add(fixType);
    }

    public ITopologyRuleType? Find(string ruleTypeId) => _types.TryGetValue(ruleTypeId, out var ruleType) ? ruleType : null;

    /// <summary>The fix types for one kind of one rule type, in the order they were registered; empty when there is none.</summary>
    public IReadOnlyList<ITopologyFixType> FixesFor(string ruleTypeId, string kind)
        => _fixTypes.Where(f => f.RuleTypeId == ruleTypeId && f.Kinds.Contains(kind)).ToList();
}
