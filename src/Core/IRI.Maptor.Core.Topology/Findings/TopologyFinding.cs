using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Topology.Findings;

/// <summary>The unit of <see cref="TopologyFinding.Value"/>.</summary>
public enum ValueUnit
{
    None = 0,

    Metres = 1,

    SquareMetres = 2,

    Count = 3,

    Degrees = 4,
}

/// <summary>
/// One thing a rule found: which rule, which kind, which features, where, and how much. Immutable. A rule
/// knows nothing about status or history; that is added by whoever keeps findings.
/// </summary>
public sealed class TopologyFinding
{
    /// <param name="ruleId">The <see cref="Rules.TopologyRule.Id"/> that found it.</param>
    /// <param name="kind">The sub-case, one of the rule type's kinds.</param>
    /// <param name="severity">The rule type's default severity for the kind.</param>
    /// <param name="features">The features involved, in the rule's order.</param>
    /// <param name="location">Where to show it on a map (a point, a stretch or an area); null when there is no place.</param>
    /// <param name="value">The measured value (a distance, an area, a count), if any.</param>
    /// <param name="valueUnit">The unit of <paramref name="value"/>.</param>
    /// <param name="discriminator">Tells apart findings of one kind about the same features (e.g. "part 2 end").</param>
    /// <param name="messageArguments">Values for the sentence that describes the finding.</param>
    public TopologyFinding(
        string ruleId,
        string kind,
        IssueSeverity severity,
        IReadOnlyList<FindingFeature> features,
        Geometry<Point>? location = null,
        double? value = null,
        ValueUnit valueUnit = ValueUnit.None,
        string? discriminator = null,
        IReadOnlyList<object>? messageArguments = null)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
            throw new ArgumentException("A finding needs its rule.", nameof(ruleId));

        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("A finding needs a kind.", nameof(kind));

        RuleId = ruleId;
        Kind = kind;
        Severity = severity;
        Features = (features ?? throw new ArgumentNullException(nameof(features))).ToList();
        Location = location;
        Value = value;
        ValueUnit = valueUnit;
        Discriminator = string.IsNullOrEmpty(discriminator) ? null : discriminator;
        MessageArguments = messageArguments?.ToList() ?? (IReadOnlyList<object>)Array.Empty<object>();
    }

    public string RuleId { get; }

    public string Kind { get; }

    public IssueSeverity Severity { get; }

    public IReadOnlyList<FindingFeature> Features { get; }

    public Geometry<Point>? Location { get; }

    public double? Value { get; }

    public ValueUnit ValueUnit { get; }

    public string? Discriminator { get; }

    public IReadOnlyList<object> MessageArguments { get; }

    public bool IsAbout(string entityName, long featureId)
        => Features.Any(f => f.EntityName == entityName && f.FeatureId == featureId);

    public override string ToString() => $"{RuleId}/{Kind} [{string.Join(", ", Features)}]";
}
