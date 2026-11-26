using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// The usual parts of a rule type: its id, its kinds with their default severities, and defaults for
/// everything a simple rule type does not need (no fields, no reach, no parameter checks).
/// </summary>
public abstract class TopologyRuleTypeBase : ITopologyRuleType
{
    private static readonly IReadOnlyCollection<string> NoFields = Array.Empty<string>();
    private static readonly IReadOnlyList<string> NoProblems = Array.Empty<string>();

    private readonly Dictionary<string, IssueSeverity> _defaults;

    protected TopologyRuleTypeBase(string id, bool needsLayerB, params (string Kind, IssueSeverity Severity)[] kinds)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A rule type needs an id.", nameof(id));

        if (kinds is null || kinds.Length == 0)
            throw new ArgumentException("A rule type reports at least one kind.", nameof(kinds));

        Id = id;
        NeedsLayerB = needsLayerB;
        Kinds = kinds.Select(k => k.Kind).ToList();
        _defaults = kinds.ToDictionary(k => k.Kind, k => k.Severity, StringComparer.Ordinal);
    }

    public string Id { get; }

    public bool NeedsLayerB { get; }

    public IReadOnlyList<string> Kinds { get; }

    public IssueSeverity DefaultSeverityOf(string kind)
        => _defaults.TryGetValue(kind, out var severity) ? severity : IssueSeverity.Warning;

    public virtual IReadOnlyCollection<string> FieldsOf(TopologyRule rule, FeatureRole role) => NoFields;

    public virtual double ReachOf(TopologyRule rule) => 0;

    public virtual IReadOnlyList<string> Validate(TopologyRule rule) => NoProblems;

    public abstract IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken);

    public override string ToString() => Id;
}
