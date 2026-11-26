using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// One check of a rule set: a rule type applied to one layer, or to a pair of layers, with its parameters
/// and its settings per kind of finding. Immutable; the <c>With…</c> methods return changed copies (used for
/// an expert's settings that last one session).
/// </summary>
public sealed class TopologyRule
{
    private static readonly IReadOnlyDictionary<string, IssueSeverity> NoSeverities = new Dictionary<string, IssueSeverity>();

    /// <param name="id">Stable id, part of every issue's identity; never reuse it for a different meaning.</param>
    /// <param name="ruleTypeId">The <see cref="ITopologyRuleType.Id"/> that does the checking.</param>
    /// <param name="layerA">The layer that is checked.</param>
    /// <param name="layerB">The second layer, for rule types that compare two layers.</param>
    /// <param name="parameters">Distances in metres and other values the rule type reads.</param>
    /// <param name="severities">Severity per kind where it differs from the rule type's default.</param>
    /// <param name="hiddenKinds">Kinds that are not reported at all (kinds that are normally correct).</param>
    /// <param name="enabled">False to leave the rule out of checks.</param>
    /// <param name="messageKeyPrefix">Prefix of the app's resource keys for this rule's sentences, if any.</param>
    public TopologyRule(
        string id,
        string ruleTypeId,
        string layerA,
        string? layerB = null,
        RuleParameters? parameters = null,
        IReadOnlyDictionary<string, IssueSeverity>? severities = null,
        IEnumerable<string>? hiddenKinds = null,
        bool enabled = true,
        string? messageKeyPrefix = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A rule needs an id.", nameof(id));

        if (string.IsNullOrWhiteSpace(ruleTypeId))
            throw new ArgumentException("A rule needs a rule type.", nameof(ruleTypeId));

        if (string.IsNullOrWhiteSpace(layerA))
            throw new ArgumentException("A rule needs a layer.", nameof(layerA));

        Id = id;
        RuleTypeId = ruleTypeId;
        LayerA = layerA;
        LayerB = string.IsNullOrWhiteSpace(layerB) ? null : layerB;
        Parameters = parameters ?? RuleParameters.Empty;
        Severities = severities is null ? NoSeverities : severities.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        HiddenKinds = new HashSet<string>(hiddenKinds ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        Enabled = enabled;
        MessageKeyPrefix = messageKeyPrefix;
    }

    public string Id { get; }

    public string RuleTypeId { get; }

    public string LayerA { get; }

    public string? LayerB { get; }

    public RuleParameters Parameters { get; }

    public IReadOnlyDictionary<string, IssueSeverity> Severities { get; }

    public IReadOnlyCollection<string> HiddenKinds { get; }

    public bool Enabled { get; }

    public string? MessageKeyPrefix { get; }

    public bool Involves(string entityName)
        => string.Equals(LayerA, entityName, StringComparison.Ordinal) || string.Equals(LayerB, entityName, StringComparison.Ordinal);

    public bool IsHidden(string kind) => HiddenKinds.Contains(kind);

    /// <summary>The severity this rule gives <paramref name="kind"/>: its own setting, or the rule type's default.</summary>
    public IssueSeverity SeverityOf(string kind, IssueSeverity defaultSeverity)
        => Severities.TryGetValue(kind, out var severity) ? severity : defaultSeverity;

    public TopologyRule WithEnabled(bool enabled)
        => Copy(enabled: enabled);

    public TopologyRule WithParameters(RuleParameters parameters)
        => Copy(parameters: parameters ?? throw new ArgumentNullException(nameof(parameters)));

    /// <summary>A copy in which <paramref name="kind"/> is reported with <paramref name="severity"/> (and not hidden).</summary>
    public TopologyRule WithSeverity(string kind, IssueSeverity severity)
    {
        var severities = Severities.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        severities[kind] = severity;

        return Copy(severities: severities, hiddenKinds: HiddenKinds.Where(k => k != kind));
    }

    public TopologyRule WithHidden(string kind, bool hidden)
    {
        var hiddenKinds = hidden ? HiddenKinds.Append(kind).Distinct() : HiddenKinds.Where(k => k != kind);

        return Copy(hiddenKinds: hiddenKinds);
    }

    public override string ToString() => LayerB is null ? $"{Id} ({RuleTypeId}: {LayerA})" : $"{Id} ({RuleTypeId}: {LayerA} → {LayerB})";

    private TopologyRule Copy(
        RuleParameters? parameters = null,
        IReadOnlyDictionary<string, IssueSeverity>? severities = null,
        IEnumerable<string>? hiddenKinds = null,
        bool? enabled = null)
        => new TopologyRule(
            Id,
            RuleTypeId,
            LayerA,
            LayerB,
            parameters ?? Parameters,
            severities ?? Severities,
            hiddenKinds ?? HiddenKinds,
            enabled ?? Enabled,
            MessageKeyPrefix);
}
