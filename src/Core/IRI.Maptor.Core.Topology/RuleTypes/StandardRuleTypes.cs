using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>The rule types that come with the library.</summary>
public static class StandardRuleTypes
{
    /// <summary>A new instance of every standard rule type.</summary>
    public static IReadOnlyList<ITopologyRuleType> All() => new ITopologyRuleType[]
    {
        new GeometryValidRuleType(),
        new PointOnLineRuleType(),
        new LineEndCoveredRuleType(),
        new LineCarriesPointsRuleType(),
        new PointOnOneLineRuleType(),
        new SequenceAlongLineRuleType(),
    };

    /// <summary>A catalog with every standard rule type registered.</summary>
    public static TopologyRuleCatalog CreateCatalog() => new TopologyRuleCatalog(All());
}
