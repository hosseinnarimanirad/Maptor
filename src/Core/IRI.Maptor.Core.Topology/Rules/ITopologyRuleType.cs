using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// One kind of check, written once in the library and used by any rule set (e.g. "point must be on line").
/// Its only job is to detect: features in, findings out. It does not load data, store, fix or format text.
/// </summary>
public interface ITopologyRuleType
{
    /// <summary>Stable id that rules refer to, e.g. "pointOnLine".</summary>
    string Id { get; }

    /// <summary>True when a rule of this type compares layer A with a layer B.</summary>
    bool NeedsLayerB { get; }

    /// <summary>The kinds this type reports, in the order they are listed.</summary>
    IReadOnlyList<string> Kinds { get; }

    IssueSeverity DefaultSeverityOf(string kind);

    /// <summary>
    /// Layers beyond A and B that a rule of this type reads, named in its parameters (e.g. the layers a line
    /// may end at). They must be declared in the rule set.
    /// </summary>
    IReadOnlyCollection<string> ExtraLayersOf(TopologyRule rule);

    /// <summary>The attributes a rule of this type reads from the features of one side.</summary>
    IReadOnlyCollection<string> FieldsOf(TopologyRule rule, FeatureRole role);

    /// <summary>
    /// How far, in metres, a rule of this type looks around a feature. A check of part of the data loads its
    /// neighbours this far out, so that findings at the edge are right.
    /// </summary>
    double ReachOf(TopologyRule rule);

    /// <summary>Problems with a rule's parameters, as sentences; empty when the rule is fine.</summary>
    IReadOnlyList<string> Validate(TopologyRule rule);

    IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken);
}
