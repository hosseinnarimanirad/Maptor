namespace IRI.Maptor.Core.Topology.Findings;

/// <summary>Which side of a rule a feature belongs to.</summary>
public enum FeatureRole
{
    /// <summary>The checked layer (<see cref="Rules.TopologyRule.LayerA"/>).</summary>
    A = 0,

    /// <summary>The compared layer (<see cref="Rules.TopologyRule.LayerB"/>).</summary>
    B = 1,

    /// <summary>A further layer the rule type reads (<see cref="Rules.ITopologyRuleType.ExtraLayersOf"/>).</summary>
    Other = 2,
}

/// <summary>A feature a finding is about, referred to by its layer and id; the feature itself is never copied.</summary>
public sealed class FindingFeature : IEquatable<FindingFeature>
{
    public FindingFeature(string entityName, long featureId, FeatureRole role)
    {
        EntityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        FeatureId = featureId;
        Role = role;
    }

    public string EntityName { get; }

    public long FeatureId { get; }

    public FeatureRole Role { get; }

    public bool Equals(FindingFeature? other)
        => other is not null && EntityName == other.EntityName && FeatureId == other.FeatureId && Role == other.Role;

    public override bool Equals(object? obj) => Equals(obj as FindingFeature);

    public override int GetHashCode() => HashCode.Combine(EntityName, FeatureId, Role);

    public override string ToString() => $"{EntityName}:{FeatureId}";
}
